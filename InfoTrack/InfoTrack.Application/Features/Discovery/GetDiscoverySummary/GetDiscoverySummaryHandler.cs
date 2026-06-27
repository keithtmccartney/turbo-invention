using InfoTrack.Contracts.Discovery;
using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;

namespace InfoTrack.Application.Features.Discovery.GetDiscoverySummary;

public sealed class GetDiscoverySummaryHandler(
    ILocationRepository locationRepository,
    IDiscoveryRunRepository discoveryRunRepository)
{
    public async Task<DiscoverySummaryDto> HandleAsync(int historyTake = 20, CancellationToken cancellationToken = default)
    {
        var summary = await BuildSummaryAsync(historyTake, cancellationToken);
        return Map(summary);
    }

    private async Task<DiscoverySummary> BuildSummaryAsync(int historyTake, CancellationToken cancellationToken)
    {
        var activeCount = await locationRepository.GetActiveCountAsync(cancellationToken);
        var latestRun = await discoveryRunRepository.GetLatestCompletedAsync(cancellationToken);
        var history = await discoveryRunRepository.GetHistoryAsync(historyTake, cancellationToken);

        var trend = history
            .Where(x => x.Status == DiscoveryRunStatus.Completed && x.CompletedAt.HasValue)
            .OrderBy(x => x.CompletedAt)
            .Select(x => new DiscoveryRunTrendPoint(
                x.CompletedAt!.Value,
                x.LocationsFound,
                x.NewLocations,
                x.RemovedLocations))
            .ToList();

        if (latestRun is null)
        {
            return new DiscoverySummary(
                activeCount,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                trend);
        }

        return new DiscoverySummary(
            activeCount,
            latestRun.Id,
            latestRun.Source,
            latestRun.CompletedAt,
            latestRun.Duration,
            latestRun.LocationsFound,
            latestRun.NewLocations,
            latestRun.UpdatedLocations,
            latestRun.RemovedLocations,
            latestRun.SkippedLocations,
            trend);
    }

    private static DiscoverySummaryDto Map(DiscoverySummary summary) =>
        new(
            summary.ActiveLocationCount,
            summary.LastRunId,
            summary.LastRunSource,
            summary.LastRunCompletedAt,
            summary.LastRunDuration.HasValue ? (long)summary.LastRunDuration.Value.TotalMilliseconds : null,
            summary.LastRunLocationsFound,
            summary.LastRunAdded,
            summary.LastRunUpdated,
            summary.LastRunRemoved,
            summary.LastRunSkipped,
            summary.HistoricalTrend
                .Select(x => new DiscoveryRunTrendPointDto(x.CompletedAt, x.LocationsFound, x.Added, x.Removed))
                .ToList());
}
