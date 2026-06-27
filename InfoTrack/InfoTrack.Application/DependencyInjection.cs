using InfoTrack.Application.Features.Discovery.GetDiscoveryHistory;
using InfoTrack.Application.Features.Discovery.GetDiscoverySummary;
using InfoTrack.Application.Features.Discovery.GetLatestDiscovery;
using InfoTrack.Application.Features.Discovery.RunDiscovery;
using InfoTrack.Application.Features.Insights.CompareSnapshots;
using InfoTrack.Application.Features.Insights.GetDashboard;
using InfoTrack.Application.Features.Locations.GetLocations;
using InfoTrack.Application.Features.Locations.UpdateLocations;
using InfoTrack.Application.Features.Scraping.GetResults;
using InfoTrack.Application.Features.Scraping.RunScrape;
using InfoTrack.Application.Mcp;
using InfoTrack.Application.Mcp.Export;
using Microsoft.Extensions.DependencyInjection;

namespace InfoTrack.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetLocationsHandler>();
        services.AddScoped<UpdateLocationsHandler>();
        services.AddScoped<RunScrapeHandler>();
        services.AddScoped<GetResultsHandler>();
        services.AddScoped<GetDashboardHandler>();
        services.AddScoped<CompareSnapshotsHandler>();
        services.AddScoped<RunDiscoveryHandler>();
        services.AddScoped<GetDiscoveryHistoryHandler>();
        services.AddScoped<GetLatestDiscoveryHandler>();
        services.AddScoped<GetDiscoverySummaryHandler>();

        services.AddScoped<McpResultExporter>();
        services.AddMcpTools();

        return services;
    }
}
