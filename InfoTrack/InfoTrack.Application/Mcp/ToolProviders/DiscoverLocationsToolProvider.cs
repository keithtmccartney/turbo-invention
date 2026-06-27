using System.Text.Json;
using System.Text.Json.Nodes;
using InfoTrack.Application.Features.Discovery.RunDiscovery;

namespace InfoTrack.Application.Mcp.ToolProviders;

[McpTool(
    "discover_locations",
    "Discover conveyancing scrape targets from the configured sitemap provider and synchronise locations.")]
public sealed class DiscoverLocationsToolProvider(RunDiscoveryHandler handler) : McpToolProviderBase
{
    protected override JsonObject BuildInputSchema() => McpToolSchemaBuilder.EmptyObject();

    public override async Task<McpToolExecutionResult> ExecuteAsync(
        JsonElement? arguments,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return McpToolExecutionResult.SuccessJson(JsonSerializer.SerializeToNode(result, McpJsonDefaults.SerializerOptions)!);
    }
}
