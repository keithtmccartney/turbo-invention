using FluentAssertions;
using InfoTrack.Infrastructure.Discovery;

namespace InfoTrack.Tests.Discovery;

public sealed class SitemapXmlParserTests
{
    private readonly SitemapXmlParser _parser = new();

    [Fact]
    public void ParseLocations_ExtractsLocElementsFromSampleSitemap()
    {
        var xml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "conveyancing-sitemap-sample.xml"));

        var locations = _parser.ParseLocations(xml);

        locations.Should().HaveCount(4);
        locations.Should().Contain("https://www.solicitors.com/conveyancing+london.html");
    }

    [Fact]
    public void ParseLocations_SupportsGoogleSitemapNamespace()
    {
        var xml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "google-sitemap-sample.xml"));

        var locations = _parser.ParseLocations(xml);

        locations.Should().Equal(
            "https://www.solicitors.com/conveyancing+london.html",
            "https://www.solicitors.com/conveyancing+manchester.html");
    }

    [Fact]
    public void ParseAndExtract_FromGoogleSitemapSample_FindsConveyancingSlugs()
    {
        var xml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "google-sitemap-sample.xml"));
        var extractor = new ConveyancingUrlExtractor();

        var slugs = extractor.ExtractSlugs(_parser.ParseLocations(xml));

        slugs.Should().Equal("london", "manchester");
    }
}
