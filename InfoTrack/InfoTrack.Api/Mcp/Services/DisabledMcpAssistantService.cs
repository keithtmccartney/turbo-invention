namespace InfoTrack.Api.Mcp.Services;

public sealed class DisabledMcpAssistantService : IMcpAssistantService
{
    public Task<McpAssistantResponse> CompleteAsync(
        McpAssistantRequest request,
        CancellationToken cancellationToken = default)
    {
        const string reply =
            "The assistant is not enabled. Set Mcp:EnableAssistant to true, configure LocalLlm in appsettings, and ensure your model server is running.";

        return Task.FromResult(new McpAssistantResponse(reply, [], IsError: true));
    }
}
