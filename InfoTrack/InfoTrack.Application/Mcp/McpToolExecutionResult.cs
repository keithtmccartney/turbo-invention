using System.Text.Json.Nodes;

namespace InfoTrack.Application.Mcp;

public sealed record McpToolExecutionResult(
    bool IsError,
    IReadOnlyList<McpContentBlock> Content)
{
    public static McpToolExecutionResult Success(string text) =>
        new(false, [new McpContentBlock("text", text)]);

    public static McpToolExecutionResult SuccessJson(JsonNode payload) =>
        Success(payload.ToJsonString(McpJsonDefaults.SerializerOptions));

    public static McpToolExecutionResult Error(string message) =>
        new(true, [new McpContentBlock("text", message)]);
}

public sealed record McpContentBlock(string Type, string Text);
