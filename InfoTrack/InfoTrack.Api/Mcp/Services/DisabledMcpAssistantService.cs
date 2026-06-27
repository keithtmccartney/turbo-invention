namespace InfoTrack.Api.Mcp.Services;

public sealed class DisabledMcpAssistantService : IMcpAssistantService
{
    public Task<McpAssistantResponse> CompleteAsync(
        McpAssistantRequest request,
        CancellationToken cancellationToken = default)
    {
        const string reply =
            "The assistant is not enabled. Set Mcp:EnableAssistant to true, configure LmStudio in appsettings, and ensure LM Studio is running.";

        return Task.FromResult(new McpAssistantResponse(reply, [], IsError: true));
    }
}
