using InfoTrack.Application.Features.Discovery.GetDiscoveryHistory;
using InfoTrack.Contracts.Discovery;
using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;

namespace InfoTrack.Application.Features.Discovery;

internal static class DiscoveryRunMapping
{
    internal static DiscoveryProgressDto MapProgress(DiscoveryRun run) =>
        new(
            run.ProgressStage.ToString(),
            run.ProgressMessage,
            run.SitemapsDownloaded,
            run.UrlsParsed,
            run.LocationsFound,
            run.NewLocations,
            run.ExistingLocations,
            run.ErrorsEncountered,
            (int)run.ProgressStage);

    internal static DiscoveryRunStatusResponse MapStatus(DiscoveryRun run) =>
        new(
            run.Id,
            run.CorrelationId ?? string.Empty,
            run.Source,
            run.Status.ToString(),
            run.StartedAt,
            run.CompletedAt,
            run.Duration.HasValue ? (long)run.Duration.Value.TotalMilliseconds : null,
            MapProgress(run),
            run.Status == DiscoveryRunStatus.Completed
                ? new DiscoveryStatisticsDto(
                    run.LocationsFound,
                    run.NewLocations,
                    run.UpdatedLocations,
                    run.RemovedLocations,
                    run.SkippedLocations,
                    run.ExistingLocations,
                    run.Duration.HasValue ? (long)run.Duration.Value.TotalMilliseconds : 0)
                : null,
            run.ErrorMessage,
            null);

    internal static DiscoveryRunSummaryDto MapSummary(DiscoveryRun run) =>
        GetDiscoveryHistoryHandler.Map(run);
}
