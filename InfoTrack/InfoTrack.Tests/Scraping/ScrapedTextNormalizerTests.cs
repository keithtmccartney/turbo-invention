using FluentAssertions;
using InfoTrack.Domain.Scraping;

namespace InfoTrack.Tests.Scraping;

public sealed class ScrapedTextNormalizerTests
{
    [Fact]
    public void Normalize_DecodesHtmlEntitiesAndUnicodeEscapes()
    {
        ScrapedTextNormalizer.Normalize("Scotland\\u0026nbsp;ML6 6AB")
            .Should()
            .Be("Scotland ML6 6AB");

        ScrapedTextNormalizer.Normalize("Aberdeen, Aberdeenshire&nbsp;AB11 6XY")
            .Should()
            .Be("Aberdeen, Aberdeenshire AB11 6XY");
    }
}
