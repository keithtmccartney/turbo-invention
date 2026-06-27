using InfoTrack.Api.Mcp.Services;

namespace InfoTrack.Api.Assistant;

public static class AssistantEndpointExtensions
{
    public static RouteGroupBuilder MapAssistantEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/chat", async (
                ChatRequest request,
                IMcpAssistantService assistantService,
                CancellationToken cancellationToken) =>
            {
                if (request.Messages is not { Count: > 0 })
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["messages"] = ["At least one message is required."],
                    });
                }

                var response = await assistantService.CompleteAsync(
                    new McpAssistantRequest(request.Messages),
                    cancellationToken);

                var payload = new ChatResponse(response.Reply, response.ToolsInvoked, response.IsError);
                return response.IsError
                    ? Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable)
                    : Results.Ok(payload);
            })
            .WithName("ChatWithAssistant")
            .WithSummary("Ask natural-language questions about conveyancing solicitor data via a local LLM and MCP tools.");

        return group;
    }
}

public sealed record ChatRequest(IReadOnlyList<McpAssistantMessage> Messages);

public sealed record ChatResponse(string Reply, IReadOnlyList<string> ToolsInvoked, bool IsError = false);
