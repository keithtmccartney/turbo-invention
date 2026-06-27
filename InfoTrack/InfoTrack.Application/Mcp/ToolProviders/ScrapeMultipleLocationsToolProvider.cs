using System.Text.Json;
using System.Text.Json.Nodes;
using InfoTrack.Application.Features.Locations.UpdateLocations;
using InfoTrack.Application.Features.Scraping.RunScrape;
using InfoTrack.Contracts.Locations;

namespace InfoTrack.Application.Mcp.ToolProviders;

[McpTool(
    "scrape_multiple_locations",
    "Configure multiple locations and run a solicitor scrape across all of them.")]
public sealed class ScrapeMultipleLocationsToolProvider(
    UpdateLocationsHandler updateLocationsHandler,
    RunScrapeHandler runScrapeHandler) : McpToolProviderBase
{
    protected override JsonObject BuildInputSchema() =>
        McpToolSchemaBuilder.Object(
            new Dictionary<string, JsonObject>
            {
                ["locations"] = McpToolSchemaBuilder.StringArrayProperty(
                    "Location names to scrape, e.g. [\"London\", \"Cardiff\"]."),
            },
            ["locations"]);

    public override async Task<McpToolExecutionResult> ExecuteAsync(
        JsonElement? arguments,
        CancellationToken cancellationToken = default)
    {
        var args = GetArguments(arguments)
            ?? throw new ArgumentException("Arguments object is required.");

        var locations = GetRequiredStringArray(args, "locations");
        await updateLocationsHandler.HandleAsync(new UpdateLocationsRequest(locations), cancellationToken);
        var scrape = await runScrapeHandler.HandleAsync(cancellationToken);

        return McpToolExecutionResult.SuccessJson(JsonSerializer.SerializeToNode(new
        {
            locations,
            scrape,
        }, McpJsonDefaults.SerializerOptions)!);
    }
}
