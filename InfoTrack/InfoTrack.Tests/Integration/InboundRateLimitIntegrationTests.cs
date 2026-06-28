using System.Net;
using FluentAssertions;

namespace InfoTrack.Tests.Integration;

public sealed class InboundRateLimitIntegrationTests : IClassFixture<RateLimitWebApplicationFactory>
{
    private readonly RateLimitWebApplicationFactory _factory;

    public InboundRateLimitIntegrationTests(RateLimitWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task ApiReads_WhenClientExceedsPerIpLimit_Returns429WithRetryAfter()
    {
        using var client = _factory.CreateClient();

        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < 7; i++)
        {
            responses.Add(await client.GetAsync("/api/insights"));
        }

        responses.Count(x => x.StatusCode == HttpStatusCode.OK).Should().Be(5);
        responses.Count(x => x.StatusCode == HttpStatusCode.TooManyRequests).Should().Be(2);

        var throttled = responses.First(x => x.StatusCode == HttpStatusCode.TooManyRequests);
        throttled.Headers.RetryAfter.Should().NotBeNull();
        throttled.Headers.RetryAfter!.Delta.Should().NotBeNull();
    }

    [Fact]
    public async Task ApiWrites_WhenClientExceedsPerIpLimit_Returns429()
    {
        using var client = _factory.CreateClient();

        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < 5; i++)
        {
            responses.Add(await client.PostAsync("/api/scrape", null));
        }

        responses.Count(x => x.StatusCode == HttpStatusCode.TooManyRequests).Should().BeGreaterThan(0);
        responses.Should().NotContain(x => (int)x.StatusCode >= 500);
    }
}
