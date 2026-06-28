using System.Net.Http.Headers;

namespace InfoTrack.Infrastructure.Resilience;

/// <summary>
/// Parses Retry-After for structured logging and progress messaging.
/// Polly's built-in generator handles delays; this type surfaces the parsed value to telemetry.
/// </summary>
public static class RetryAfterHeaderParser
{
    public static TimeSpan? TryGetDelay(HttpResponseHeaders headers, DateTimeOffset? utcNow = null)
    {
        if (headers.RetryAfter is not { } retryAfter)
        {
            return null;
        }

        var now = utcNow ?? DateTimeOffset.UtcNow;

        if (retryAfter.Delta is { } delta)
        {
            return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        }

        if (retryAfter.Date is { } date)
        {
            var wait = date - now;
            return wait < TimeSpan.Zero ? TimeSpan.Zero : wait;
        }

        return null;
    }
}
