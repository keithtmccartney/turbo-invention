using System.Text.Json;
using System.Text.Json.Nodes;
using InfoTrack.Application.Features.Scraping.GetResults;
using InfoTrack.Application.Mcp.Export;

namespace InfoTrack.Application.Mcp.ToolProviders;

[McpTool(
    "get_statistics",
    "Return headline statistics from the latest dashboard analytics, optionally filtered by location.")]
public sealed class GetStatisticsToolProvider(
    McpResultExporter exporter,
    GetResultsHandler resultsHandler) : McpToolProviderBase
{
    protected override JsonObject BuildInputSchema() =>
        McpToolSchemaBuilder.Object(
            new Dictionary<string, JsonObject>
            {
                ["location"] = McpToolSchemaBuilder.StringProperty("Optional location name to filter firm counts.", nullable: true),
            });

    public override async Task<McpToolExecutionResult> ExecuteAsync(
        JsonElement? arguments,
        CancellationToken cancellationToken = default)
    {
        var args = GetArguments(arguments);
        var locationFilter = args is { } jsonArgs ? GetOptionalString(jsonArgs, "location") : null;
        var report = await exporter.GetReportAsync(cancellationToken);

        if (locationFilter is not null)
        {
            var results = await resultsHandler.HandleAsync(cancellationToken);
            var locationResults = results.Results
                .FirstOrDefault(location => location.LocationName.Equals(locationFilter, StringComparison.OrdinalIgnoreCase));
            var regional = report.RegionalBreakdown
                .FirstOrDefault(location => location.LocationName.Equals(locationFilter, StringComparison.OrdinalIgnoreCase));

            var statistics = new
            {
                location = locationFilter,
                firmCount = locationResults?.Solicitors.Count ?? 0,
                averageRating = regional?.AverageRating,
                totalReviews = regional?.TotalReviews ?? 0,
                lastScrapedAt = report.LastScrapedAt,
            };

            return McpToolExecutionResult.SuccessJson(JsonSerializer.SerializeToNode(statistics, McpJsonDefaults.SerializerOptions)!);
        }

        var headline = new
        {
            totalFirms = report.TotalFirms,
            locationsSearched = report.LocationsSearched,
            newFirms = report.NewFirms,
            removedFirms = report.RemovedFirms,
            averageRating = report.AverageRating,
            lastScrapedAt = report.LastScrapedAt,
            currentSnapshotId = report.CurrentSnapshotId,
            previousSnapshotId = report.PreviousSnapshotId,
            topFirmCount = report.TopFirms.Count,
            growthSignalCount = report.GrowthSignals.Count,
            regionalBreakdown = report.RegionalBreakdown,
        };

        return McpToolExecutionResult.SuccessJson(JsonSerializer.SerializeToNode(headline, McpJsonDefaults.SerializerOptions)!);
    }
}
