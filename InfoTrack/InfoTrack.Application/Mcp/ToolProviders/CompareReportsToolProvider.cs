using System.Text.Json;
using System.Text.Json.Nodes;
using InfoTrack.Application.Features.Insights.CompareSnapshots;

namespace InfoTrack.Application.Mcp.ToolProviders;

[McpTool(
    "compare_reports",
    "Compare two scrape snapshots and return delta analytics.")]
public sealed class CompareReportsToolProvider(CompareSnapshotsHandler handler) : McpToolProviderBase
{
    protected override JsonObject BuildInputSchema() =>
        McpToolSchemaBuilder.Object(new Dictionary<string, JsonObject>
        {
            ["currentSnapshotId"] = McpToolSchemaBuilder.StringProperty("Optional current snapshot ID.", nullable: true),
            ["previousSnapshotId"] = McpToolSchemaBuilder.StringProperty("Optional previous snapshot ID.", nullable: true),
        });

    public override async Task<McpToolExecutionResult> ExecuteAsync(
        JsonElement? arguments,
        CancellationToken cancellationToken = default)
    {
        Guid? currentSnapshotId = null;
        Guid? previousSnapshotId = null;

        if (GetArguments(arguments) is { } args)
        {
            currentSnapshotId = GetOptionalGuid(args, "currentSnapshotId");
            previousSnapshotId = GetOptionalGuid(args, "previousSnapshotId");
        }

        var comparison = await handler.HandleAsync(currentSnapshotId, previousSnapshotId, cancellationToken);
        return McpToolExecutionResult.SuccessJson(JsonSerializer.SerializeToNode(comparison, McpJsonDefaults.SerializerOptions)!);
    }
}
