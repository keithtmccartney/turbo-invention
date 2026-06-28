using InfoTrack.Infrastructure.Resilience.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace InfoTrack.Infrastructure.Resilience;

public static class HttpResilienceServiceCollectionExtensions
{
    public static IServiceCollection AddInfoTrackHttpResilience(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ResilienceOptions>(configuration.GetSection(ResilienceOptions.SectionName));
        services.AddSingleton<HttpResilienceOptionsResolver>();
        services.AddSingleton<IHttpResilienceTelemetry, HttpResilienceTelemetry>();
        services.AddScoped<DiscoveryResilienceProgressNotifier>();
        services.AddScoped<ScrapeResilienceProgressNotifier>();
        services.AddSingleton<IResilienceProgressNotifier, ScopedResilienceProgressNotifier>();

        return services;
    }

    public static IHttpClientBuilder AddInfoTrackResiliencePipeline(
        this IHttpClientBuilder builder,
        string pipelineName,
        Func<IServiceProvider, HttpResilienceClientOptions> optionsFactory)
    {
        builder.AddHttpMessageHandler(sp =>
            new HttpResilienceTelemetryHandler(
                sp.GetRequiredService<IHttpResilienceTelemetry>(),
                pipelineName));

        builder.AddHttpMessageHandler(sp =>
        {
            var options = optionsFactory(sp);
            return new HttpOutboundPacingHandler(pipelineName, options.RequestsPerSecond);
        });

        builder.ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var options = optionsFactory(sp);
            return new SocketsHttpHandler
            {
                ConnectTimeout = options.ConnectTimeout,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            };
        });

        builder.AddResilienceHandler(pipelineName, (pipelineBuilder, context) =>
        {
            var options = optionsFactory(context.ServiceProvider);
            var telemetry = context.ServiceProvider.GetRequiredService<IHttpResilienceTelemetry>();
            var progressNotifier = context.ServiceProvider.GetRequiredService<IResilienceProgressNotifier>();

            HttpResiliencePipelineConfigurator.Configure(
                pipelineBuilder,
                options,
                pipelineName,
                telemetry,
                progressNotifier);
        });

        return builder;
    }

    public static IHttpClientBuilder AddInfoTrackScrapingResilience(this IHttpClientBuilder builder) =>
        builder.AddInfoTrackResiliencePipeline(
            ResiliencePipelineNames.Scraping,
            sp => sp.GetRequiredService<HttpResilienceOptionsResolver>().ResolveScraping(
                sp.GetRequiredService<IOptions<InfoTrack.Infrastructure.Options.ScrapingOptions>>()));

    public static IHttpClientBuilder AddInfoTrackDiscoveryResilience(this IHttpClientBuilder builder) =>
        builder.AddInfoTrackResiliencePipeline(
            ResiliencePipelineNames.Discovery,
            sp => sp.GetRequiredService<HttpResilienceOptionsResolver>().ResolveDiscovery(
                sp.GetRequiredService<IOptions<InfoTrack.Infrastructure.Options.DiscoveryOptions>>()));
}
