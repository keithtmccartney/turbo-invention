using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfoTrack.Api.Mcp.JsonRpc;

public static class JsonRpcErrorCodes
{
    public const int ParseError = -32700;
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InvalidParams = -32602;
}

public sealed record JsonRpcRequest(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("id")] JsonElement? Id,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("params")] JsonElement? Params);

public sealed record JsonRpcError(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message);

public sealed record JsonRpcResponse(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("id")] JsonElement? Id,
    [property: JsonPropertyName("result")] object? Result,
    [property: JsonPropertyName("error")] JsonRpcError? Error)
{
    public static JsonRpcResponse Success(JsonElement? id, object result) =>
        new("2.0", id, result, null);

    public static JsonRpcResponse Failure(JsonElement? id, JsonRpcError error) =>
        new("2.0", id, null, error);
}
