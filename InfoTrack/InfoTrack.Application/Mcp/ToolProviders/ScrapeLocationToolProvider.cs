using System.Text.Json;
using System.Text.Json.Nodes;
using InfoTrack.Application.Features.Locations.UpdateLocations;
using InfoTrack.Application.Features.Scraping.RunScrape;
using InfoTrack.Contracts.Locations;

namespace InfoTrack.Application.Mcp.ToolProviders;

[McpTool(
    "scrape_location",
    "Configure a single location and run a solicitor scrape for it.")]
public sealed class ScrapeLocationToolProvider(
    UpdateLocationsHandler updateLocationsHandler,
    RunScrapeHandler runScrapeHandler) : McpToolProviderBase
{
    protected override JsonObject BuildInputSchema() =>
        McpToolSchemaBuilder.Object(
            new Dictionary<string, JsonObject>
            {
                ["location"] = McpToolSchemaBuilder.StringProperty("Location name to scrape, e.g. London."),
            },
            ["location"]);

    public override async Task<McpToolExecutionResult> ExecuteAsync(
        JsonElement? arguments,
        CancellationToken cancellationToken = default)
    {
        var args = GetArguments(arguments)
            ?? throw new ArgumentException("Arguments object is required.");

        var location = GetRequiredString(args, "location");
        await updateLocationsHandler.HandleAsync(new UpdateLocationsRequest([location]), cancellationToken);
        var scrape = await runScrapeHandler.HandleAsync(cancellationToken);

        return McpToolExecutionResult.SuccessJson(JsonSerializer.SerializeToNode(new
        {
            location,
            scrape,
        }, McpJsonDefaults.SerializerOptions)!);
    }
}
