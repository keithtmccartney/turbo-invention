using InfoTrack.Api.Assistant;
using InfoTrack.Api.Mcp;
using InfoTrack.Api.Mcp.OpenApi;
using InfoTrack.Api.Middleware;
using InfoTrack.Api.RateLimiting;
using InfoTrack.Application;
using InfoTrack.Application.Features.Discovery.GetDiscoveryHistory;
using InfoTrack.Application.Features.Discovery.GetDiscoveryRunStatus;
using InfoTrack.Application.Features.Discovery.GetDiscoverySummary;
using InfoTrack.Application.Features.Discovery.GetLatestDiscovery;
using InfoTrack.Application.Features.Discovery.StartDiscovery;
using InfoTrack.Application.Features.Insights.CompareSnapshots;
using InfoTrack.Application.Features.Insights.GetDashboard;
using InfoTrack.Application.Features.Locations.GetLocations;
using InfoTrack.Application.Features.Locations.UpdateLocations;
using InfoTrack.Application.Features.Scraping.GetResults;
using InfoTrack.Application.Features.Scraping.GetScrapeRunStatus;
using InfoTrack.Application.Features.Scraping.StartScrape;
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
    options.SwaggerDoc("v1", new()
    {
        Title = "InfoTrack Solicitor Intelligence API",
        Version = "v1",
        Description = "REST endpoints for locations, discovery, scraping, results, and insights.",
    });
    options.DocumentFilter<McpToolDocumentFilter>();
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
builder.Services.AddMcpServer(builder.Configuration);
builder.Services.AddInfoTrackInboundRateLimiting(builder.Configuration);

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
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseRateLimiter();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Frontend");

var api = app.MapGroup("/api").WithTags("InfoTrack");

api.MapGet("/locations", async (GetLocationsHandler handler, CancellationToken ct) =>
    Results.Ok(await handler.HandleAsync(ct)))
    .WithName("GetLocations")
    .WithSummary("Returns configured scrape locations.")
    .WithApiReadRateLimit();

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
    .WithSummary("Replaces the active location list.")
    .WithApiWriteRateLimit();

api.MapPost("/discovery/run", async (HttpContext httpContext, StartDiscoveryHandler handler, CancellationToken ct) =>
{
    try
    {
        var correlationId = httpContext.GetCorrelationId();
        var response = await handler.HandleAsync(correlationId, ct);
        return Results.Accepted($"/api/discovery/runs/{response.OperationId}/status", response);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { title = "Discovery already in progress", detail = ex.Message });
    }
})
    .WithName("StartDiscovery")
    .WithSummary("Starts an asynchronous discovery operation and returns an operation identifier for polling.")
    .WithApiWriteRateLimit();

api.MapGet("/discovery/runs/{operationId:guid}/status", async (
    Guid operationId,
    GetDiscoveryRunStatusHandler handler,
    CancellationToken ct) =>
{
    var status = await handler.HandleAsync(operationId, ct);
    return status is null ? Results.NotFound() : Results.Ok(status);
})
    .WithName("GetDiscoveryRunStatus")
    .WithSummary("Returns progress and status for a discovery operation.")
    .WithApiReadRateLimit();

api.MapGet("/discovery/summary", async (GetDiscoverySummaryHandler handler, CancellationToken ct) =>
    Results.Ok(await handler.HandleAsync(cancellationToken: ct)))
    .WithName("GetDiscoverySummary")
    .WithSummary("Returns discovery summary including active locations and historical trend.")
    .WithApiReadRateLimit();

api.MapGet("/discovery/runs/latest", async (GetLatestDiscoveryHandler handler, CancellationToken ct) =>
{
    var latest = await handler.HandleAsync(ct);
    return latest is null ? Results.NotFound() : Results.Ok(latest);
})
    .WithName("GetLatestDiscoveryRun")
    .WithSummary("Returns the most recent completed discovery run.")
    .WithApiReadRateLimit();

api.MapGet("/discovery/runs", async (int? take, GetDiscoveryHistoryHandler handler, CancellationToken ct) =>
    Results.Ok(await handler.HandleAsync(take ?? 20, ct)))
    .WithName("GetDiscoveryHistory")
    .WithSummary("Returns discovery run history.")
    .WithApiReadRateLimit();

api.MapPost("/scrape", async (HttpContext httpContext, StartScrapeHandler handler, CancellationToken ct) =>
{
    try
    {
        var correlationId = httpContext.GetCorrelationId();
        var response = await handler.HandleAsync(correlationId, ct);
        return Results.Accepted($"/api/scrape/runs/{response.OperationId}/status", response);
    }
    catch (ArgumentException ex)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [""] = [ex.Message]
        });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { title = "Scrape already in progress", detail = ex.Message });
    }
})
    .WithName("StartScrape")
    .WithSummary("Starts an asynchronous scrape operation and returns an operation identifier for polling.")
    .WithApiWriteRateLimit();

api.MapGet("/scrape/runs/{operationId:guid}/status", async (
    Guid operationId,
    GetScrapeRunStatusHandler handler,
    CancellationToken ct) =>
{
    var status = await handler.HandleAsync(operationId, ct);
    return status is null ? Results.NotFound() : Results.Ok(status);
})
    .WithName("GetScrapeRunStatus")
    .WithSummary("Returns progress and status for a scrape operation.")
    .WithApiReadRateLimit();

api.MapGet("/results", async (GetResultsHandler handler, CancellationToken ct) =>
    Results.Ok(await handler.HandleAsync(ct)))
    .WithName("GetResults")
    .WithSummary("Returns the latest solicitor results grouped by location.")
    .WithApiReadRateLimit();

api.MapGet("/insights", async (GetDashboardHandler handler, CancellationToken ct) =>
    Results.Ok(await handler.HandleAsync(ct)))
    .WithName("GetInsights")
    .WithSummary("Returns dashboard analytics including regional breakdown and leaderboard.")
    .WithApiReadRateLimit();

api.MapGet("/insights/compare", async (
    Guid? currentSnapshotId,
    Guid? previousSnapshotId,
    CompareSnapshotsHandler handler,
    CancellationToken ct) =>
    Results.Ok(await handler.HandleAsync(currentSnapshotId, previousSnapshotId, ct)))
    .WithName("CompareSnapshots")
    .WithSummary("Compares two scrape snapshots and returns delta analytics.")
    .WithApiReadRateLimit();

api.MapAssistantEndpoints();

app.MapMcpEndpoints();

app.Run();

public partial class Program;
