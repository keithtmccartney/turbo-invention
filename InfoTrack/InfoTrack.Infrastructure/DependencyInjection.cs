using InfoTrack.Domain.Analytics;
using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Operations;
using InfoTrack.Domain.Repositories;
using InfoTrack.Domain.Scraping;
using InfoTrack.Infrastructure.Analytics;
using InfoTrack.Infrastructure.Discovery;
using InfoTrack.Infrastructure.Operations;
using InfoTrack.Infrastructure.Options;
using InfoTrack.Infrastructure.Persistence;
using InfoTrack.Infrastructure.Repositories;
using InfoTrack.Infrastructure.Scraping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace InfoTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ScrapingOptions>(configuration.GetSection(ScrapingOptions.SectionName));
        services.Configure<DiscoveryOptions>(configuration.GetSection(DiscoveryOptions.SectionName));
        services.Configure<OperationWorkerOptions>(configuration.GetSection(OperationWorkerOptions.SectionName));

        services.AddDbContext<InfoTrackDbContext>(options =>
            options.UseInMemoryDatabase("InfoTrackDb"));

        services.AddHttpClient<ISolicitorsScrapeClient, SolicitorsScrapeClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ScrapingOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            client.Timeout = Timeout.InfiniteTimeSpan;
        })
        .AddStandardResilienceHandler()
        .Configure((options, sp) =>
        {
            var scrapingOptions = sp.GetRequiredService<IOptions<ScrapingOptions>>().Value;
            var resilience = scrapingOptions.Resilience;

            options.Retry.MaxRetryAttempts = resilience.MaxRetryAttempts;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.Retry.Delay = TimeSpan.FromMilliseconds(resilience.RetryDelayMilliseconds);
            options.Retry.MaxDelay = TimeSpan.FromMilliseconds(resilience.MaxRetryDelayMilliseconds);

            options.CircuitBreaker.FailureRatio = resilience.CircuitBreakerFailureRatio / 100d;
            options.CircuitBreaker.MinimumThroughput = resilience.CircuitBreakerMinimumThroughput;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(resilience.CircuitBreakerBreakDurationSeconds);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(
                Math.Max(scrapingOptions.RequestTimeoutSeconds * 2, 60));

            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(scrapingOptions.RequestTimeoutSeconds);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(
                scrapingOptions.RequestTimeoutSeconds * (resilience.MaxRetryAttempts + 1));
        });

        services.AddHttpClient<IDiscoveryProvider, SitemapDiscoveryProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DiscoveryOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            client.Timeout = Timeout.InfiniteTimeSpan;
        })
        .AddStandardResilienceHandler()
        .Configure((options, sp) =>
        {
            var discoveryOptions = sp.GetRequiredService<IOptions<DiscoveryOptions>>().Value;
            var resilience = discoveryOptions.Resilience;

            options.Retry.MaxRetryAttempts = resilience.MaxRetryAttempts;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.Retry.Delay = TimeSpan.FromMilliseconds(resilience.RetryDelayMilliseconds);
            options.Retry.MaxDelay = TimeSpan.FromMilliseconds(resilience.MaxRetryDelayMilliseconds);

            options.CircuitBreaker.FailureRatio = resilience.CircuitBreakerFailureRatio / 100d;
            options.CircuitBreaker.MinimumThroughput = resilience.CircuitBreakerMinimumThroughput;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(resilience.CircuitBreakerBreakDurationSeconds);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(
                Math.Max(discoveryOptions.RequestTimeoutSeconds * 2, 60));

            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(discoveryOptions.RequestTimeoutSeconds);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(
                discoveryOptions.RequestTimeoutSeconds * (resilience.MaxRetryAttempts + 1));
        });

        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<ISolicitorRepository, SolicitorRepository>();
        services.AddScoped<IScrapeSnapshotRepository, ScrapeSnapshotRepository>();
        services.AddScoped<IScrapeRunRepository, ScrapeRunRepository>();
        services.AddScoped<IInsightSummaryRepository, InsightSummaryRepository>();
        services.AddScoped<IDiscoveryRunRepository, DiscoveryRunRepository>();

        services.AddSingleton<ISolicitorsHtmlParser, SolicitorsHtmlParser>();
        services.AddScoped<IScrapeOrchestrator, ScrapeOrchestrator>();
        services.AddScoped<IScrapeProgressReporter, ScrapeProgressReporter>();
        services.AddScoped<IOperationProcessor, ScrapeOperationProcessor>();

        services.AddSingleton<SitemapXmlParser>();
        services.AddSingleton<ConveyancingUrlExtractor>();
        services.AddScoped<IDiscoveryOrchestrator, DiscoveryOrchestrator>();
        services.AddScoped<IDiscoveryProgressReporter, DiscoveryProgressReporter>();
        services.AddScoped<IOperationProcessor, DiscoveryOperationProcessor>();

        services.AddSingleton<IOperationQueue, OperationQueue>();
        services.AddHostedService<OperationWorkerBackgroundService>();

        services.AddSingleton<SnapshotComparer>();
        services.AddSingleton<RankingEngine>();
        services.AddSingleton<RegionalStatisticsCalculator>();
        services.AddSingleton<GrowthDetector>();
        services.AddSingleton<DashboardSummaryBuilder>();
        services.AddScoped<IAnalyticsEngine, AnalyticsEngine>();

        return services;
    }
}
