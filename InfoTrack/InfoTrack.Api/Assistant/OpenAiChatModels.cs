using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfoTrack.Api.Assistant;

public sealed class OpenAiChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OpenAiChatMessage> Messages { get; set; } = [];

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OpenAiToolDefinition>? Tools { get; set; }

    [JsonPropertyName("tool_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolChoice { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.2;
}

public sealed class OpenAiChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OpenAiToolCall>? ToolCalls { get; set; }

    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }
}

public sealed class OpenAiToolDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; } = "function";

    [JsonPropertyName("function")]
    public OpenAiFunctionDefinition Function { get; set; } = new();
}

public sealed class OpenAiFunctionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public JsonElement Parameters { get; set; }
}

public sealed class OpenAiToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public OpenAiToolCallFunction Function { get; set; } = new();
}

public sealed class OpenAiToolCallFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = "{}";
}

public sealed class OpenAiChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public List<OpenAiChatChoice> Choices { get; set; } = [];
}

public sealed class OpenAiChatChoice
{
    [JsonPropertyName("message")]
    public OpenAiChatMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}
