using InfoTrack.Contracts.Scraping;
using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;

namespace InfoTrack.Application.Features.Scraping;

internal static class ScrapeRunMapping
{
    internal static ScrapeProgressDto MapProgress(ScrapeRun run) =>
        new(
            run.ProgressStage.ToString(),
            run.ProgressMessage,
            run.LocationsTotal,
            run.LocationsCompleted,
            run.FirmsDiscovered,
            run.NewFirms,
            run.RemovedFirms,
            run.ErrorsEncountered,
            (int)run.ProgressStage);

    internal static ScrapeRunStatusResponse MapStatus(ScrapeRun run, ScrapeSnapshot? snapshot) =>
        new(
            run.Id,
            run.CorrelationId ?? string.Empty,
            run.Status.ToString(),
            run.StartedAt,
            run.CompletedAt,
            run.Duration.HasValue ? (long)run.Duration.Value.TotalMilliseconds : null,
            MapProgress(run),
            run.Status == ScrapeRunStatus.Completed && run.SnapshotId.HasValue && snapshot is not null
                ? new ScrapeResponse(
                    snapshot.Id,
                    snapshot.ScrapedAt,
                    snapshot.TotalFirms,
                    snapshot.LocationsSearched,
                    run.NewFirms,
                    run.RemovedFirms)
                : null,
            run.ErrorMessage);
}
