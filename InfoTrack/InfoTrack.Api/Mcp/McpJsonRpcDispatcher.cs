using System.Text.Json;
using InfoTrack.Api.Mcp.JsonRpc;
using InfoTrack.Application.Mcp;
using Microsoft.Extensions.Options;

namespace InfoTrack.Api.Mcp;

public sealed class McpJsonRpcDispatcher(
    IMcpToolRegistry toolRegistry,
    IOptions<McpOptions> options)
{
    public async Task<JsonRpcResponse> DispatchAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.JsonRpc, "2.0", StringComparison.Ordinal))
        {
            return JsonRpcResponse.Failure(
                request.Id,
                new JsonRpcError(JsonRpcErrorCodes.InvalidRequest, "jsonrpc must be '2.0'."));
        }

        return request.Method switch
        {
            "initialize" => HandleInitialize(request),
            "tools/list" => HandleToolsList(request),
            "tools/call" => await HandleToolsCallAsync(request, cancellationToken),
            "ping" => JsonRpcResponse.Success(request.Id, new { status = "ok" }),
            _ => JsonRpcResponse.Failure(
                request.Id,
                new JsonRpcError(JsonRpcErrorCodes.MethodNotFound, $"Method '{request.Method}' is not supported.")),
        };
    }

    private JsonRpcResponse HandleInitialize(JsonRpcRequest request)
    {
        var mcpOptions = options.Value;
        var result = new
        {
            protocolVersion = "2024-11-05",
            serverInfo = new
            {
                name = mcpOptions.ServerName,
                version = mcpOptions.ServerVersion,
            },
            capabilities = new
            {
                tools = new { listChanged = false },
            },
        };

        return JsonRpcResponse.Success(request.Id, result);
    }

    private JsonRpcResponse HandleToolsList(JsonRpcRequest request)
    {
        var tools = toolRegistry.GetDefinitions()
            .Select(tool => new
            {
                name = tool.Name,
                description = tool.Description,
                inputSchema = tool.InputSchema,
            })
            .ToList();

        return JsonRpcResponse.Success(request.Id, new { tools });
    }

    private async Task<JsonRpcResponse> HandleToolsCallAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Params is not { ValueKind: JsonValueKind.Object } paramsElement)
        {
            return JsonRpcResponse.Failure(
                request.Id,
                new JsonRpcError(JsonRpcErrorCodes.InvalidParams, "params object is required."));
        }

        if (!paramsElement.TryGetProperty("name", out var nameElement)
            || nameElement.ValueKind != JsonValueKind.String)
        {
            return JsonRpcResponse.Failure(
                request.Id,
                new JsonRpcError(JsonRpcErrorCodes.InvalidParams, "params.name is required."));
        }

        JsonElement? arguments = null;
        if (paramsElement.TryGetProperty("arguments", out var argumentsElement))
        {
            arguments = argumentsElement;
        }

        var toolName = nameElement.GetString()!;
        var execution = await toolRegistry.ExecuteAsync(toolName, arguments, cancellationToken);

        var result = new
        {
            content = execution.Content.Select(block => new { type = block.Type, text = block.Text }).ToList(),
            isError = execution.IsError,
        };

        return JsonRpcResponse.Success(request.Id, result);
    }
}
