using System.Text.Json;
using System.Text.Json.Nodes;
using InfoTrack.Application.Features.Scraping.GetResults;

namespace InfoTrack.Application.Mcp.ToolProviders;

[McpTool(
    "search_firms",
    "Search scraped solicitor firms by location and/or text matched against firm fields (name, address, phone, website, description, ratings).")]
public sealed class SearchFirmsToolProvider(GetResultsHandler resultsHandler) : McpToolProviderBase
{
    protected override JsonObject BuildInputSchema() =>
        McpToolSchemaBuilder.Object(
            new Dictionary<string, JsonObject>
            {
                ["query"] = McpToolSchemaBuilder.StringProperty(
                    "Optional text to match against firm name, address, phone, website, description, or ratings.",
                    nullable: true),
                ["location"] = McpToolSchemaBuilder.StringProperty("Optional location filter.", nullable: true),
            });

    public override async Task<McpToolExecutionResult> ExecuteAsync(
        JsonElement? arguments,
        CancellationToken cancellationToken = default)
    {
        var args = GetArguments(arguments);
        string? query = null;
        string? locationFilter = null;

        if (args is { } jsonArgs)
        {
            query = GetOptionalString(jsonArgs, "query");
            locationFilter = GetOptionalString(jsonArgs, "location");
        }

        if (query is null && locationFilter is null)
        {
            throw new ArgumentException("Provide a location and/or search query.");
        }

        var results = await resultsHandler.HandleAsync(cancellationToken);
        var matches = FirmSearchMatcher.Filter(results, locationFilter, query).ToList();

        return McpToolExecutionResult.SuccessJson(JsonSerializer.SerializeToNode(new
        {
            lastScrapedAt = results.LastScrapedAt,
            query,
            location = locationFilter,
            matchCount = matches.Count,
            matches,
        }, McpJsonDefaults.SerializerOptions)!);
    }
}
