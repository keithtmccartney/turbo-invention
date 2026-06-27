namespace InfoTrack.Api.Mcp.Services;

public sealed record McpAssistantMessage(string Role, string Content);

public sealed record McpAssistantRequest(
    IReadOnlyList<McpAssistantMessage> Messages,
    IReadOnlyList<string>? AllowedTools = null);

public sealed record McpAssistantResponse(
    string Reply,
    IReadOnlyList<string> ToolsInvoked,
    bool IsError = false);

public interface IMcpAssistantService
{
    Task<McpAssistantResponse> CompleteAsync(
        McpAssistantRequest request,
        CancellationToken cancellationToken = default);
}
