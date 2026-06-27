using FluentAssertions;
using InfoTrack.Infrastructure.Discovery;

namespace InfoTrack.Tests.Discovery;

public sealed class ConveyancingUrlExtractorTests
{
    private readonly ConveyancingUrlExtractor _extractor = new();

    [Fact]
    public void ExtractSlugs_ReturnsUniqueOrderedSlugs()
    {
        var slugs = _extractor.ExtractSlugs(
        [
            "https://www.solicitors.com/conveyancing+london.html",
            "https://www.solicitors.com/conveyancing+manchester.html",
            "https://www.solicitors.com/conveyancing+london.html",
            "https://www.solicitors.com/about.html",
        ]);

        slugs.Should().Equal("london", "manchester");
    }
}
