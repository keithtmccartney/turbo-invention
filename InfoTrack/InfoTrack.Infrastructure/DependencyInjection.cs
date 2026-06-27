using InfoTrack.Domain.Analytics;
using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Repositories;
using InfoTrack.Domain.Scraping;
using InfoTrack.Infrastructure.Analytics;
using InfoTrack.Infrastructure.Discovery;
using InfoTrack.Infrastructure.Options;
using InfoTrack.Infrastructure.Persistence;
using InfoTrack.Infrastructure.Repositories;
using InfoTrack.Infrastructure.Scraping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InfoTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ScrapingOptions>(configuration.GetSection(ScrapingOptions.SectionName));
        services.Configure<DiscoveryOptions>(configuration.GetSection(DiscoveryOptions.SectionName));

        services.AddDbContext<InfoTrackDbContext>(options =>
            options.UseInMemoryDatabase("InfoTrackDb"));

        services.AddHttpClient<ISolicitorsScrapeClient, SolicitorsScrapeClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ScrapingOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<IDiscoveryProvider, SitemapDiscoveryProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DiscoveryOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        });

        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<ISolicitorRepository, SolicitorRepository>();
        services.AddScoped<IScrapeSnapshotRepository, ScrapeSnapshotRepository>();
        services.AddScoped<IInsightSummaryRepository, InsightSummaryRepository>();
        services.AddScoped<IDiscoveryRunRepository, DiscoveryRunRepository>();

        services.AddSingleton<ISolicitorsHtmlParser, SolicitorsHtmlParser>();
        services.AddScoped<IScrapeOrchestrator, ScrapeOrchestrator>();

        services.AddSingleton<SitemapXmlParser>();
        services.AddSingleton<ConveyancingUrlExtractor>();
        services.AddScoped<IDiscoveryOrchestrator, DiscoveryOrchestrator>();

        services.AddSingleton<SnapshotComparer>();
        services.AddSingleton<RankingEngine>();
        services.AddSingleton<RegionalStatisticsCalculator>();
        services.AddSingleton<GrowthDetector>();
        services.AddSingleton<DashboardSummaryBuilder>();
        services.AddScoped<IAnalyticsEngine, AnalyticsEngine>();

        return services;
    }
}
