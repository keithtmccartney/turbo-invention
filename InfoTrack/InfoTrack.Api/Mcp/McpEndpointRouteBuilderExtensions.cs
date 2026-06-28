using System.Text.Json;
using InfoTrack.Api.Mcp.Authentication;
using InfoTrack.Api.Mcp.JsonRpc;
using InfoTrack.Api.Mcp.Services;
using InfoTrack.Application.Mcp;
using Microsoft.Extensions.Options;

namespace InfoTrack.Api.Mcp;

public static class McpEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapMcpEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mcp")
            .WithTags("MCP");

        group.MapPost("/", (HttpContext httpContext, McpJsonRpcDispatcher dispatcher, McpApiKeyValidator validator, IOptions<McpOptions> options, CancellationToken cancellationToken) =>
                HandleJsonRpcAsync(httpContext, dispatcher, validator, options, cancellationToken))
            .WithName("McpJsonRpc")
            .WithSummary("InfoTrack Model Context Protocol JSON-RPC endpoint.");

        group.MapGet("/tools", (IMcpToolRegistry registry) =>
                Results.Ok(registry.GetDefinitions()))
            .WithName("McpListTools")
            .WithSummary("Lists InfoTrack MCP tools and JSON schemas.")
            .AddEndpointFilter<McpAuthorizationFilter>();

        group.MapPost("/assistant", async (
                McpAssistantRequest request,
                IMcpAssistantService assistantService,
                IMcpToolRegistry registry,
                CancellationToken cancellationToken) =>
            {
                var enriched = request with
                {
                    AllowedTools = request.AllowedTools
                        ?? registry.GetDefinitions().Select(x => x.Name).ToList(),
                };

                var response = await assistantService.CompleteAsync(enriched, cancellationToken);
                if (response.IsError)
                {
                    return Results.Json(
                        new
                        {
                            systemPrompt = McpAssistantService.SystemPrompt,
                            response,
                        },
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                return Results.Ok(new
                {
                    systemPrompt = McpAssistantService.SystemPrompt,
                    response,
                });
            })
            .WithName("McpAssistant")
            .WithSummary("Domain-aware MCP assistant that can invoke InfoTrack tools.")
            .AddEndpointFilter<McpAuthorizationFilter>();

        return group;
    }

    private static async Task<IResult> HandleJsonRpcAsync(
        HttpContext httpContext,
        McpJsonRpcDispatcher dispatcher,
        McpApiKeyValidator validator,
        IOptions<McpOptions> options,
        CancellationToken cancellationToken)
    {
        var mcpOptions = options.Value;
        if (!mcpOptions.Enabled)
        {
            return Results.NotFound(new { message = "MCP is disabled." });
        }

        if (mcpOptions.RequireHttps && !httpContext.Request.IsHttps)
        {
            return Results.Problem(
                title: "HTTPS required",
                detail: "MCP requests must use HTTPS when Mcp:RequireHttps is enabled.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (!validator.TryValidateAuthorizationHeader(
                httpContext.Request.Headers.Authorization,
                out var failureReason))
        {
            return Results.Json(
                JsonRpcResponse.Failure(
                    null,
                    new JsonRpcError(-32001, failureReason ?? "Unauthorized")),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        JsonRpcRequest? request;
        try
        {
            request = await httpContext.Request.ReadFromJsonAsync<JsonRpcRequest>(cancellationToken);
        }
        catch (JsonException ex)
        {
            return Results.Json(
                JsonRpcResponse.Failure(
                    null,
                    new JsonRpcError(JsonRpcErrorCodes.ParseError, ex.Message)),
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (request is null)
        {
            return Results.Json(
                JsonRpcResponse.Failure(
                    null,
                    new JsonRpcError(JsonRpcErrorCodes.InvalidRequest, "Request body is required.")),
                statusCode: StatusCodes.Status400BadRequest);
        }

        var response = await dispatcher.DispatchAsync(request, cancellationToken);
        return Results.Json(response);
    }
}

public sealed class McpAuthorizationFilter(McpApiKeyValidator validator, IOptions<McpOptions> options) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!options.Value.Enabled)
        {
            return Results.NotFound(new { message = "MCP is disabled." });
        }

        if (options.Value.RequireHttps && !context.HttpContext.Request.IsHttps)
        {
            return Results.Problem(
                title: "HTTPS required",
                detail: "MCP requests must use HTTPS when Mcp:RequireHttps is enabled.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (!validator.TryValidateAuthorizationHeader(
                context.HttpContext.Request.Headers.Authorization,
                out var failureReason))
        {
            return Results.Json(new { message = failureReason }, statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
    }
}
