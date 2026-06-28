using FluentAssertions;
using InfoTrack.Infrastructure.Resilience;

namespace InfoTrack.Tests.Resilience;

public sealed class RetryAfterHeaderParserTests
{
    [Fact]
    public void TryGetDelay_ParsesDeltaSeconds()
    {
        using var response = new HttpResponseMessage();
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(30));

        var delay = RetryAfterHeaderParser.TryGetDelay(response.Headers);

        delay.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void TryGetDelay_ParsesAbsoluteDate()
    {
        using var response = new HttpResponseMessage();
        var target = DateTimeOffset.UtcNow.AddMinutes(2);
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(target);

        var delay = RetryAfterHeaderParser.TryGetDelay(response.Headers, DateTimeOffset.UtcNow);

        delay.Should().NotBeNull();
        delay!.Value.Should().BeCloseTo(TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void TryGetDelay_ReturnsNullWhenHeaderMissing()
    {
        using var response = new HttpResponseMessage();

        RetryAfterHeaderParser.TryGetDelay(response.Headers).Should().BeNull();
    }
}
