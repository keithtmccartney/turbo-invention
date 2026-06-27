using FluentAssertions;
using InfoTrack.Application.Mcp;

namespace InfoTrack.Tests.Mcp;

public sealed class McpToolDiscoveryTests
{
    [Fact]
    public void DiscoverToolTypes_FindsAllExpectedTools()
    {
        var types = McpServiceCollectionExtensions.DiscoverToolTypes(typeof(McpServiceCollectionExtensions).Assembly);

        types.Should().HaveCount(10);
        types.Select(type => type.GetCustomAttributes(typeof(McpToolAttribute), inherit: false).Cast<McpToolAttribute>().Single().Name)
            .Should().BeEquivalentTo(
            [
                "compare_reports",
                "discover_locations",
                "export_csv",
                "export_excel",
                "export_json",
                "get_report",
                "get_statistics",
                "scrape_location",
                "scrape_multiple_locations",
                "search_firms",
            ]);
    }
}
