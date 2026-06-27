using FluentAssertions;
using InfoTrack.Infrastructure.Scraping;
using Microsoft.Extensions.Logging.Abstractions;

namespace InfoTrack.Tests.Scraping;

public sealed class SolicitorsHtmlParserTests
{
    private readonly SolicitorsHtmlParser _parser = new(NullLogger<SolicitorsHtmlParser>.Instance);

    [Fact]
    public void Parse_LondonSample_ExtractsMultipleFirmsWithContactDetails()
    {
        var html = File.ReadAllText(Path.Combine("TestData", "london-sample.html"));

        var results = _parser.Parse(html, "London");

        results.Should().NotBeEmpty();
        results.Should().Contain(x => x.FirmName.Contains("Blandy", StringComparison.OrdinalIgnoreCase));
        results.Should().Contain(x => x.Phone == "020 3031 6605");
        results.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.FirmName));
        results.Select(x => x.Position).Should().BeInAscendingOrder();
    }

    [Fact]
    public void Parse_LondonSample_ExtractsRatingsAndReviewCounts()
    {
        var html = File.ReadAllText(Path.Combine("TestData", "london-sample.html"));

        var blandy = _parser.Parse(html, "London")
            .First(x => x.FirmName.Contains("Blandy", StringComparison.OrdinalIgnoreCase));

        blandy.Rating.Should().Be(4.5m);
        blandy.ReviewCount.Should().Be(968);
        blandy.Address.Should().Contain("Holborn");
    }

    [Fact]
    public void Parse_EmptyHtml_ReturnsEmptyCollection()
    {
        _parser.Parse(string.Empty, "London").Should().BeEmpty();
    }

    [Fact]
    public void Parse_SingleBlock_ExtractsWebsiteAndEmailLinks()
    {
        const string html = """
            <div class="result-section">
            <div class="result-item">
                <span class="h2">Example Solicitors LLP<span class="rev-results"><div class="star-full rating-lrg"></div> (10)</span></span>
                <a rel="noindex" href="tel:01234567890">01234 567890</a>
                <address>1 High Street, London</address>
                <p>Trusted local firm.</p>
                <a href="/enquiry-form.asp?SiD=123"><i class="fa fa-envelope"></i>Email</a>
                <a href="http://www.example.com"><i class="fa fa-globe"></i>Website</a>
            </div>
            </div>
            """;

        var result = _parser.Parse(html, "London").Single();

        result.FirmName.Should().Be("Example Solicitors LLP");
        result.Phone.Should().Be("01234 567890");
        result.Website.Should().Be("http://www.example.com");
        result.EmailEnquiryUrl.Should().Contain("enquiry-form.asp");
    }

    [Fact]
    public void Parse_ItemSmallBlock_ExtractsFirmPhoneAndAddress()
    {
        const string html = """
            <div class="result-item item-small">
                <span class="h2">Harold Benjamin<span class="rev-results"><div class="star-full rating-sml pad-top"></div> (190)</span></span>
                <a href="/harold-benjamin.html" class="link-map"><address>67 - 71 Lowlands Road, Harrow, HA1 3EQ</address></a>
                <a class="tel" rel="noindex" href="tel:02084225678"> 020 8422 5678 </a>
            </div>
            """;

        var result = _parser.Parse(html, "London").Single();

        result.FirmName.Should().Be("Harold Benjamin");
        result.Phone.Should().Be("020 8422 5678");
        result.Address.Should().Contain("Harrow");
        result.ReviewCount.Should().Be(190);
    }

    [Fact]
    public void Parse_GreentickSmall_DoesNotPolluteFirmName()
    {
        const string html = """
            <div class="result-item item-small">
                <span class="h2">s.satha &amp; co<div class="greentick-small" title="quality marks"></div><span class="rev-results"> (130)</span></span>
                <address>358 High Street North, London E12 6PH</address>
                <a class="tel" href="tel:02084719484">0208 471 9484</a>
            </div>
            """;

        var result = _parser.Parse(html, "London").Single();

        result.FirmName.Should().Be("s.satha & co");
    }
}
