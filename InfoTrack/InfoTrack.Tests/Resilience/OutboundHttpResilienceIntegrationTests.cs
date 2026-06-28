using FluentAssertions;
using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Scraping;
using InfoTrack.Infrastructure.Discovery;
using InfoTrack.Infrastructure.Options;
using InfoTrack.Infrastructure.Resilience;
using InfoTrack.Infrastructure.Scraping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Tests.Resilience;

/// <summary>
/// Exercises production Polly pipelines against scripted upstream 429 → 200 sequences.
/// </summary>
public sealed class OutboundHttpResilienceIntegrationTests
{
    private const string MinimalSitemapXml =
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
          <url><loc>https://resilience.test/conveyancing+london.html</loc></url>
        </urlset>
        """;

    [Fact]
    public async Task ScrapingClient_WhenUpstreamReturns429Then200_RetriesAndReturnsHtml()
    {
        var londonHtml = await File.ReadAllTextAsync(Path.Combine("TestData", "london-sample.html"));
        var handler = new SequenceResponseHandler(
            () => SequenceResponseHandler.TooManyRequests(retryAfterSeconds: 1),
            () => SequenceResponseHandler.Ok(londonHtml));

        await using var provider = BuildScrapingServices(handler);
        var client = provider.GetRequiredService<ISolicitorsScrapeClient>();

        var html = await client.FetchLocationPageAsync("london", operationId: Guid.NewGuid());

        html.Should().Contain("result-item");
        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task DiscoveryProvider_WhenSitemapReturns429Then200_RetriesAndDiscoversLocations()
    {
        var handler = new SequenceResponseHandler(
            () => SequenceResponseHandler.TooManyRequests(retryAfterSeconds: 1),
            () => SequenceResponseHandler.Ok(MinimalSitemapXml, "application/xml"));

        await using var provider = BuildDiscoveryServices(handler);
        var discovery = provider.GetRequiredService<IDiscoveryProvider>();

        var locations = await discovery.DiscoverAsync(Guid.NewGuid());

        locations.Should().ContainSingle(x => x.Name == "London");
        handler.RequestCount.Should().Be(2);
    }

    private static ServiceProvider BuildScrapingServices(SequenceResponseHandler handler)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(ResilienceTestConfiguration.FastRetries)
            .Build();

        services.AddInfoTrackHttpResilience(configuration);
        services.Configure<ScrapingOptions>(configuration.GetSection(ScrapingOptions.SectionName));
        RegisterNoOpProgressReporters(services);

        services.AddHttpClient<ISolicitorsScrapeClient, SolicitorsScrapeClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ScrapingOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddInfoTrackScrapingResilience()
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildDiscoveryServices(SequenceResponseHandler handler)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(ResilienceTestConfiguration.FastRetries)
            .Build();

        services.AddInfoTrackHttpResilience(configuration);
        services.Configure<DiscoveryOptions>(configuration.GetSection(DiscoveryOptions.SectionName));
        services.AddSingleton<SitemapXmlParser>();
        services.AddSingleton<ConveyancingUrlExtractor>();
        RegisterNoOpProgressReporters(services);

        services.AddHttpClient<IDiscoveryProvider, SitemapDiscoveryProvider>((sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiscoveryOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddInfoTrackDiscoveryResilience()
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        return services.BuildServiceProvider();
    }

    private static void RegisterNoOpProgressReporters(IServiceCollection services)
    {
        services.AddScoped<IScrapeProgressReporter, NoOpScrapeProgressReporter>();
        services.AddScoped<IDiscoveryProgressReporter, NoOpDiscoveryProgressReporter>();
    }

    private sealed class NoOpScrapeProgressReporter : IScrapeProgressReporter
    {
        public Task ReportAsync(Guid runId, ScrapeProgressUpdate update, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoOpDiscoveryProgressReporter : IDiscoveryProgressReporter
    {
        public Task ReportAsync(Guid runId, DiscoveryProgressUpdate update, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
