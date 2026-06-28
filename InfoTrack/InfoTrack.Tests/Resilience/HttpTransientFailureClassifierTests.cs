using FluentAssertions;
using InfoTrack.Infrastructure.Resilience;
using System.Net;

namespace InfoTrack.Tests.Resilience;

public sealed class HttpTransientFailureClassifierTests
{
    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public void IsTransientHttpStatus_RetriesExpectedCodes(HttpStatusCode statusCode)
    {
        HttpTransientFailureClassifier.IsTransientHttpStatus(statusCode).Should().BeTrue();
        HttpTransientFailureClassifier.ShouldRetryHttpResponse(new HttpResponseMessage(statusCode)).Should().BeTrue();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public void IsTransientHttpStatus_DoesNotRetryClientErrors(HttpStatusCode statusCode)
    {
        HttpTransientFailureClassifier.IsTransientHttpStatus(statusCode).Should().BeFalse();
        HttpTransientFailureClassifier.IsNonRetryableClientError(statusCode).Should().BeTrue();
    }

    [Fact]
    public void ShouldRetryException_IncludesNetworkFailures()
    {
        using var cts = new CancellationTokenSource();
        HttpTransientFailureClassifier.ShouldRetryException(new HttpRequestException(), cts.Token).Should().BeTrue();
        HttpTransientFailureClassifier.ShouldRetryException(new HttpRequestException(), cts.Token).Should().BeTrue();
    }

    [Fact]
    public void ShouldRetryException_IgnoresUserCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        HttpTransientFailureClassifier.ShouldRetryException(new TaskCanceledException(), cts.Token).Should().BeFalse();
    }
}
