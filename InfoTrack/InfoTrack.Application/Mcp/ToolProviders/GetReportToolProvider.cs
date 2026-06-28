using System.Text.Json;
using System.Text.Json.Nodes;
using InfoTrack.Application.Mcp.Export;

namespace InfoTrack.Application.Mcp.ToolProviders;

[McpTool(
    "get_report",
    "Return the latest dashboard analytics report.")]
public sealed class GetReportToolProvider(McpResultExporter exporter) : McpToolProviderBase
{
    protected override JsonObject BuildInputSchema() => McpToolSchemaBuilder.EmptyObject();

    public override async Task<McpToolExecutionResult> ExecuteAsync(
        JsonElement? arguments,
        CancellationToken cancellationToken = default)
    {
        var report = await exporter.GetReportAsync(cancellationToken);
        return McpToolExecutionResult.SuccessJson(JsonSerializer.SerializeToNode(report, McpJsonDefaults.SerializerOptions)!);
    }
}
