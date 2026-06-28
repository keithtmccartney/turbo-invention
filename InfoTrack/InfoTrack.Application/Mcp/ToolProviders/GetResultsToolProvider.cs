using System.Text.Json;
using System.Text.Json.Nodes;
using InfoTrack.Application.Features.Scraping.GetResults;

namespace InfoTrack.Application.Mcp.ToolProviders;

[McpTool(
    "get_results",
    "Return the latest solicitor results grouped by location, optionally filtered to one location.")]
public sealed class GetResultsToolProvider(GetResultsHandler resultsHandler) : McpToolProviderBase
{
    protected override JsonObject BuildInputSchema() =>
        McpToolSchemaBuilder.Object(
            new Dictionary<string, JsonObject>
            {
                ["location"] = McpToolSchemaBuilder.StringProperty(
                    "Optional location name to return only that group.",
                    nullable: true),
            });

    public override async Task<McpToolExecutionResult> ExecuteAsync(
        JsonElement? arguments,
        CancellationToken cancellationToken = default)
    {
        var locationFilter = GetArguments(arguments) is { } jsonArgs
            ? GetOptionalString(jsonArgs, "location")
            : null;

        var results = await resultsHandler.HandleAsync(cancellationToken);
        if (locationFilter is not null)
        {
            results = results with
            {
                Results = results.Results
                    .Where(location => location.LocationName.Equals(locationFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList(),
            };
        }

        return McpToolExecutionResult.SuccessJson(JsonSerializer.SerializeToNode(results, McpJsonDefaults.SerializerOptions)!);
    }
}
