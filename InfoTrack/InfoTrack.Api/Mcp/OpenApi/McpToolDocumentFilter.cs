using System.Text.Json.Nodes;
using InfoTrack.Application.Mcp;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InfoTrack.Api.Mcp.OpenApi;

public sealed class McpToolDocumentFilter(IMcpToolRegistry toolRegistry) : IDocumentFilter
{
    private const string McpToolsPrefix = "MCP tools: ";
    private const string McpToolsTag = "MCP Tools";

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Info.Description = StripExistingMcpToolsSection(swaggerDoc.Info.Description);

        foreach (var tool in toolRegistry.GetDefinitions())
        {
            var path = $"/api/mcp/tools/{tool.Name}";
            var operation = new OpenApiOperation
            {
                OperationId = $"McpTool_{ToOperationId(tool.Name)}",
                Summary = tool.Description,
                Description =
                    "Invoke via JSON-RPC on POST /api/mcp using method `tools/call`, " +
                    $"with params.name set to `{tool.Name}` and params.arguments matching the request body schema.",
                RequestBody = CreateRequestBody(tool),
            };
            operation.Tags = new HashSet<OpenApiTagReference> { new(McpToolsTag) };

            var pathItem = new OpenApiPathItem();
            pathItem.AddOperation(HttpMethod.Post, operation);
            swaggerDoc.Paths[path] = pathItem;
        }
    }

    private static OpenApiRequestBody CreateRequestBody(McpToolDefinition tool)
    {
        return new OpenApiRequestBody
        {
            Required = true,
            Description = "Tool arguments (maps to JSON-RPC params.arguments).",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new()
                {
                    Schema = ConvertJsonSchema(tool.InputSchema),
                },
            },
        };
    }

    private static OpenApiSchema ConvertJsonSchema(JsonNode node)
    {
        if (node is not JsonObject obj)
        {
            return new OpenApiSchema { Type = JsonSchemaType.Object };
        }

        var schema = new OpenApiSchema();
        if (obj.TryGetPropertyValue("description", out var descriptionNode)
            && descriptionNode is JsonValue descriptionValue)
        {
            schema.Description = descriptionValue.GetValue<string>();
        }

        if (obj.TryGetPropertyValue("type", out var typeNode) && typeNode is JsonValue typeValue)
        {
            schema.Type = MapJsonSchemaType(typeValue.GetValue<string>());
        }

        if (obj.TryGetPropertyValue("properties", out var propertiesNode)
            && propertiesNode is JsonObject properties)
        {
            schema.Properties = properties.ToDictionary(
                pair => pair.Key,
                pair => (IOpenApiSchema)ConvertJsonSchema(pair.Value!));
        }

        if (obj.TryGetPropertyValue("required", out var requiredNode) && requiredNode is JsonArray required)
        {
            schema.Required = required
                .Select(item => item?.GetValue<string>())
                .Where(value => value is not null)
                .Cast<string>()
                .ToHashSet();
        }

        if (obj.TryGetPropertyValue("items", out var itemsNode) && itemsNode is not null)
        {
            schema.Items = ConvertJsonSchema(itemsNode);
        }

        if (obj.TryGetPropertyValue("minimum", out var minimumNode) && minimumNode is JsonValue minimumValue)
        {
            schema.Minimum = minimumValue.ToString();
        }

        if (obj.TryGetPropertyValue("minItems", out var minItemsNode) && minItemsNode is JsonValue minItemsValue)
        {
            schema.MinItems = minItemsValue.GetValue<int>();
        }

        return schema;
    }

    private static JsonSchemaType MapJsonSchemaType(string? type) =>
        type switch
        {
            "string" => JsonSchemaType.String,
            "integer" => JsonSchemaType.Integer,
            "number" => JsonSchemaType.Number,
            "boolean" => JsonSchemaType.Boolean,
            "array" => JsonSchemaType.Array,
            _ => JsonSchemaType.Object,
        };

    private static string ToOperationId(string toolName) =>
        string.Concat(toolName.Split('_').Select(part =>
            part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..]));

    private static string StripExistingMcpToolsSection(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        var markerIndex = description.IndexOf(McpToolsPrefix, StringComparison.Ordinal);
        return markerIndex >= 0 ? description[..markerIndex].TrimEnd() : description.TrimEnd();
    }
}
