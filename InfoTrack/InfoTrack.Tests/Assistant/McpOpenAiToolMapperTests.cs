using FluentAssertions;
using InfoTrack.Api.Assistant;
using InfoTrack.Application.Mcp;

namespace InfoTrack.Tests.Assistant;

public sealed class McpOpenAiToolMapperTests
{
    [Fact]
    public void MapTools_FiltersByAllowedNames()
    {
        var definitions = new List<McpToolDefinition>
        {
            new("get_statistics", "Stats", McpToolSchemaBuilder.EmptyObject()),
            new("search_firms", "Search", McpToolSchemaBuilder.EmptyObject()),
        };

        var mapped = McpOpenAiToolMapper.MapTools(definitions, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "get_statistics" });

        mapped.Should().ContainSingle();
        mapped[0].Function.Name.Should().Be("get_statistics");
    }
}
