using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;

namespace InfoTrack.Tests.Resilience;

/// <summary>
/// Returns scripted HTTP responses in order for outbound resilience tests.
/// </summary>
public sealed class SequenceResponseHandler : HttpMessageHandler
{
    private readonly Func<HttpResponseMessage>[] _responses;
    private int _callCount;

    public SequenceResponseHandler(params Func<HttpResponseMessage>[] responses)
    {
        ArgumentNullException.ThrowIfNull(responses);
        if (responses.Length == 0)
        {
            throw new ArgumentException("At least one response factory is required.", nameof(responses));
        }

        _responses = responses;
    }

    public int RequestCount => Volatile.Read(ref _callCount);

    public ConcurrentQueue<HttpRequestMessage> Requests { get; } = new();

    public static HttpResponseMessage TooManyRequests(int retryAfterSeconds = 1)
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(retryAfterSeconds));
        return response;
    }

    public static HttpResponseMessage Ok(string content, string mediaType = "text/html") =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, mediaType),
        };

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Enqueue(request);

        var index = Interlocked.Increment(ref _callCount) - 1;
        if (index >= _responses.Length)
        {
            index = _responses.Length - 1;
        }

        return Task.FromResult(_responses[index]());
    }
}
