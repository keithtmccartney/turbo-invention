using System.Text.Json.Nodes;

namespace InfoTrack.Application.Mcp;

public sealed record McpToolDefinition(
    string Name,
    string Description,
    JsonObject InputSchema);
