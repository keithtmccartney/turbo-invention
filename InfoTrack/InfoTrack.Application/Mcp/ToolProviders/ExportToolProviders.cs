using System.Text.Json.Nodes;
using InfoTrack.Application.Mcp.Export;

namespace InfoTrack.Application.Mcp.ToolProviders;

[McpTool(
    "export_csv",
    "Export the latest solicitor results as CSV.")]
public sealed class ExportCsvToolProvider(McpResultExporter exporter) : McpToolProviderBase
{
    protected override JsonObject BuildInputSchema() => McpToolSchemaBuilder.EmptyObject();

    public override async Task<McpToolExecutionResult> ExecuteAsync(
        System.Text.Json.JsonElement? arguments,
        CancellationToken cancellationToken = default)
    {
        var csv = await exporter.ExportCsvAsync(cancellationToken);
        return McpToolExecutionResult.Success(csv);
    }
}

[McpTool(
    "export_excel",
    "Export the latest solicitor results as tab-separated values suitable for Excel.")]
public sealed class ExportExcelToolProvider(McpResultExporter exporter) : McpToolProviderBase
{
    protected override JsonObject BuildInputSchema() => McpToolSchemaBuilder.EmptyObject();

    public override async Task<McpToolExecutionResult> ExecuteAsync(
        System.Text.Json.JsonElement? arguments,
        CancellationToken cancellationToken = default)
    {
        var tsv = await exporter.ExportExcelAsync(cancellationToken);
        return McpToolExecutionResult.Success(tsv);
    }
}

[McpTool(
    "export_json",
    "Export the latest solicitor results as JSON.")]
public sealed class ExportJsonToolProvider(McpResultExporter exporter) : McpToolProviderBase
{
    protected override JsonObject BuildInputSchema() => McpToolSchemaBuilder.EmptyObject();

    public override async Task<McpToolExecutionResult> ExecuteAsync(
        System.Text.Json.JsonElement? arguments,
        CancellationToken cancellationToken = default)
    {
        var json = await exporter.ExportJsonAsync(cancellationToken);
        return McpToolExecutionResult.Success(json);
    }
}
