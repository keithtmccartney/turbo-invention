using System.Collections.Concurrent;

namespace InfoTrack.Infrastructure.Resilience;

/// <summary>
/// Enforces a minimum interval between outbound requests to avoid hammering upstream hosts.
/// Complements the concurrency limiter with a requests-per-second pacing guard.
/// </summary>
public sealed class HttpOutboundPacingHandler(string pipelineName, double requestsPerSecond) : DelegatingHandler
{
    private static readonly ConcurrentDictionary<string, PacingState> States = new(StringComparer.OrdinalIgnoreCase);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (requestsPerSecond <= 0)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var interval = TimeSpan.FromSeconds(1d / requestsPerSecond);
        var state = States.GetOrAdd(pipelineName, _ => new PacingState());

        await state.Gate.WaitAsync(cancellationToken);
        try
        {
            var now = DateTimeOffset.UtcNow;
            if (state.LastRequestAt is { } last)
            {
                var wait = last + interval - now;
                if (wait > TimeSpan.Zero)
                {
                    await Task.Delay(wait, cancellationToken);
                }
            }

            state.LastRequestAt = DateTimeOffset.UtcNow;
        }
        finally
        {
            state.Gate.Release();
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private sealed class PacingState
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);

        public DateTimeOffset? LastRequestAt { get; set; }
    }
}
