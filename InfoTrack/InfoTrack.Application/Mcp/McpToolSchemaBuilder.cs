using System.Text.Json.Nodes;

namespace InfoTrack.Application.Mcp;

public static class McpToolSchemaBuilder
{
    public static JsonObject EmptyObject() => Object(new Dictionary<string, JsonObject>());

    public static JsonObject Object(
        IReadOnlyDictionary<string, JsonObject> properties,
        IReadOnlyList<string>? required = null)
    {
        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject(properties.ToDictionary(
                pair => pair.Key,
                pair => (JsonNode)pair.Value)),
        };

        if (required is { Count: > 0 })
        {
            schema["required"] = new JsonArray(required.Select(x => JsonValue.Create(x)).ToArray());
        }

        return schema;
    }

    public static JsonObject StringProperty(string description, bool nullable = false) =>
        new()
        {
            ["type"] = nullable ? "string" : "string",
            ["description"] = description,
        };

    public static JsonObject IntegerProperty(string description, int? minimum = null) =>
        new()
        {
            ["type"] = "integer",
            ["description"] = description,
            ["minimum"] = minimum,
        };

    public static JsonObject StringArrayProperty(string description, int minItems = 1) =>
        new()
        {
            ["type"] = "array",
            ["description"] = description,
            ["items"] = new JsonObject { ["type"] = "string" },
            ["minItems"] = minItems,
        };
}
