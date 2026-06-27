using InfoTrack.Application;
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
using InfoTrack.Contracts.Insights;
using InfoTrack.Contracts.Locations;
using InfoTrack.Infrastructure;
using InfoTrack.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "InfoTrack Solicitor Intelligence API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

var app = builder.Build();

// I considered database seeding during initial design but deliberately omitted it.
// The application should be witnessed in its native, fresh state: an empty catalogue
// that the user fills through discovery, configuration, and scrape — start to finish.
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider
        .GetRequiredService<InfoTrackDbContext>()
        .Database.EnsureCreatedAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Frontend");

var api = app.MapGroup("/api").WithTags("InfoTrack");

api.MapGet("/locations", async (GetLocationsHandler handler, CancellationToken ct) =>
    Results.Ok(await handler.HandleAsync(ct)))
    .WithName("GetLocations")
    .WithSummary("Returns configured scrape locations.");

api.MapPost("/locations", async (UpdateLocationsRequest request, UpdateLocationsHandler handler, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await handler.HandleAsync(request, ct));
    }
    catch (ArgumentException ex)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [""] = [ex.Message]
        });
    }
})
    .WithName("UpdateLocations")
    .WithSummary("Replaces the active location list.");

api.MapPost("/discovery/run", async (RunDiscoveryHandler handler, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await handler.HandleAsync(ct));
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem(
            title: "Discovery failed",
            detail: ex.Message,
            statusCode: StatusCodes.Status502BadGateway);
    }
})
    .WithName("RunDiscovery")
    .WithSummary("Discovers scrape targets from the configured discovery provider and synchronises locations.");

api.MapGet("/discovery/summary", async (GetDiscoverySummaryHandler handler, CancellationToken ct) =>
    Results.Ok(await handler.HandleAsync(cancellationToken: ct)))
    .WithName("GetDiscoverySummary")
    .WithSummary("Returns discovery summary including active locations and historical trend.");

api.MapGet("/discovery/runs/latest", async (GetLatestDiscoveryHandler handler, CancellationToken ct) =>
{
    var latest = await handler.HandleAsync(ct);
    return latest is null ? Results.NotFound() : Results.Ok(latest);
})
    .WithName("GetLatestDiscoveryRun")
    .WithSummary("Returns the most recent completed discovery run.");

api.MapGet("/discovery/runs", async (int? take, GetDiscoveryHistoryHandler handler, CancellationToken ct) =>
    Results.Ok(await handler.HandleAsync(take ?? 20, ct)))
    .WithName("GetDiscoveryHistory")
    .WithSummary("Returns discovery run history.");

api.MapPost("/scrape", async (RunScrapeHandler handler, CancellationToken ct) =>
    Results.Ok(await handler.HandleAsync(ct)))
    .WithName("RunScrape")
    .WithSummary("Scrapes solicitor listings for all active locations and generates analytics.");

api.MapGet("/results", async (GetResultsHandler handler, CancellationToken ct) =>
    Results.Ok(await handler.HandleAsync(ct)))
    .WithName("GetResults")
    .WithSummary("Returns the latest solicitor results grouped by location.");

api.MapGet("/insights", async (GetDashboardHandler handler, CancellationToken ct) =>
    Results.Ok(await handler.HandleAsync(ct)))
    .WithName("GetInsights")
    .WithSummary("Returns dashboard analytics including regional breakdown and leaderboard.");

api.MapGet("/insights/compare", async (
    Guid? currentSnapshotId,
    Guid? previousSnapshotId,
    CompareSnapshotsHandler handler,
    CancellationToken ct) =>
    Results.Ok(await handler.HandleAsync(currentSnapshotId, previousSnapshotId, ct)))
    .WithName("CompareSnapshots")
    .WithSummary("Compares two scrape snapshots and returns delta analytics.");

app.Run();

public partial class Program;
