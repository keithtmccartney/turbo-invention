using InfoTrack.Application.Mcp;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InfoTrack.Api.Mcp.OpenApi;

public sealed class McpToolDocumentFilter(IMcpToolRegistry toolRegistry) : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var toolNames = string.Join(", ", toolRegistry.GetDefinitions().Select(x => x.Name));
        var existing = swaggerDoc.Info.Description ?? string.Empty;
        swaggerDoc.Info.Description =
            $"{existing}{Environment.NewLine}{Environment.NewLine}MCP tools: {toolNames}";
    }
}
