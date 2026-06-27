using System.Text.Json;
using System.Text.Json.Nodes;
using InfoTrack.Application.Features.Scraping.GetResults;

namespace InfoTrack.Application.Mcp.ToolProviders;

[McpTool(
    "search_firms",
    "Search scraped solicitor firms by location and/or firm name.")]
public sealed class SearchFirmsToolProvider(GetResultsHandler resultsHandler) : McpToolProviderBase
{
    protected override JsonObject BuildInputSchema() =>
        McpToolSchemaBuilder.Object(
            new Dictionary<string, JsonObject>
            {
                ["query"] = McpToolSchemaBuilder.StringProperty("Optional firm name search text.", nullable: true),
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
            throw new ArgumentException("Provide a location and/or firm name query.");
        }

        var results = await resultsHandler.HandleAsync(cancellationToken);
        var matches = results.Results
            .Where(location => locationFilter is null
                || location.LocationName.Equals(locationFilter, StringComparison.OrdinalIgnoreCase))
            .SelectMany(location => location.Solicitors.Select(firm => new
            {
                location.LocationName,
                firm.FirmName,
                firm.Phone,
                firm.Address,
                firm.Website,
                firm.Rating,
                firm.ReviewCount,
            }))
            .Where(firm => query is null
                || firm.FirmName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(firm => firm.FirmName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return McpToolExecutionResult.SuccessJson(JsonSerializer.SerializeToNode(new
        {
            query,
            location = locationFilter,
            matchCount = matches.Count,
            matches,
        }, McpJsonDefaults.SerializerOptions)!);
    }
}
