using InfoTrack.Application.Features.Discovery.GetDiscoveryRunStatus;
using InfoTrack.Application.Features.Discovery.StartDiscovery;
using InfoTrack.Contracts.Discovery;
using InfoTrack.Domain.Entities;

namespace InfoTrack.Application.Features.Discovery.RunDiscovery;

/// <summary>
/// Waits for an asynchronous discovery operation to complete. Used by MCP tools that require a synchronous result.
/// </summary>
public sealed class RunDiscoveryHandler(
    StartDiscoveryHandler startDiscoveryHandler,
    GetDiscoveryRunStatusHandler getDiscoveryRunStatusHandler)
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    public async Task<DiscoveryRunResponse> HandleAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var started = await startDiscoveryHandler.HandleAsync(correlationId, cancellationToken);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(DefaultTimeout);

        while (!timeoutCts.IsCancellationRequested)
        {
            var status = await getDiscoveryRunStatusHandler.HandleAsync(started.OperationId, cancellationToken);
            if (status is null)
            {
                throw new InvalidOperationException($"Discovery operation {started.OperationId} was not found.");
            }

            if (status.Status == nameof(DiscoveryRunStatus.Completed))
            {
                return MapCompleted(status);
            }

            if (status.Status == nameof(DiscoveryRunStatus.Failed))
            {
                throw new InvalidOperationException(status.ErrorMessage ?? "Discovery failed.");
            }

            await Task.Delay(PollInterval, timeoutCts.Token);
        }

        throw new TimeoutException($"Discovery operation {started.OperationId} did not complete in time.");
    }

    private static DiscoveryRunResponse MapCompleted(DiscoveryRunStatusResponse status)
    {
        var statistics = status.Statistics
            ?? throw new InvalidOperationException("Completed discovery run is missing statistics.");

        return new DiscoveryRunResponse(
            status.OperationId,
            status.Source,
            status.StartedAt,
            status.CompletedAt ?? status.StartedAt,
            status.DurationMilliseconds ?? statistics.DurationMilliseconds,
            statistics,
            status.Locations ?? []);
    }
}
