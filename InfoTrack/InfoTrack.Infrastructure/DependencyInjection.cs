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
using InfoTrack.Infrastructure.Resilience;
using InfoTrack.Infrastructure.Scraping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InfoTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ScrapingOptions>(configuration.GetSection(ScrapingOptions.SectionName));
        services.Configure<DiscoveryOptions>(configuration.GetSection(DiscoveryOptions.SectionName));
        services.Configure<OperationWorkerOptions>(configuration.GetSection(OperationWorkerOptions.SectionName));

        services.AddInfoTrackHttpResilience(configuration);

        services.AddDbContext<InfoTrackDbContext>(options =>
            options.UseInMemoryDatabase("InfoTrackDb"));

        services.AddHttpClient<ISolicitorsScrapeClient, SolicitorsScrapeClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ScrapingOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            client.Timeout = Timeout.InfiniteTimeSpan;
        })
        .AddInfoTrackScrapingResilience();

        services.AddHttpClient<IDiscoveryProvider, SitemapDiscoveryProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiscoveryOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            client.Timeout = Timeout.InfiniteTimeSpan;
        })
        .AddInfoTrackDiscoveryResilience();

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
