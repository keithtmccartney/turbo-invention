using System.Text.Json;
using InfoTrack.Application.Mcp;

namespace InfoTrack.Api.Assistant;

public static class McpOpenAiToolMapper
{
    public static List<OpenAiToolDefinition> MapTools(
        IEnumerable<McpToolDefinition> definitions,
        IReadOnlySet<string>? allowedToolNames = null)
    {
        return definitions
            .Where(definition => allowedToolNames is null
                || allowedToolNames.Contains(definition.Name, StringComparer.OrdinalIgnoreCase))
            .Select(definition => new OpenAiToolDefinition
            {
                Function = new OpenAiFunctionDefinition
                {
                    Name = definition.Name,
                    Description = definition.Description,
                    Parameters = JsonSerializer.SerializeToElement(definition.InputSchema),
                },
            })
            .ToList();
    }
}
