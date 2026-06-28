using System.Text.Json;

namespace InfoTrack.Application.Mcp;

public interface IMcpToolProvider
{
    McpToolDefinition Definition { get; }

    Task<McpToolExecutionResult> ExecuteAsync(
        JsonElement? arguments,
        CancellationToken cancellationToken = default);
}
