using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfoTrack.Application.Mcp;

public static class McpJsonDefaults
{
    public static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };
}
