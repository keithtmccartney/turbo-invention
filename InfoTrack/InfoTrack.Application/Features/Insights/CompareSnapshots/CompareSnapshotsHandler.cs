using InfoTrack.Contracts.Insights;
using InfoTrack.Domain.Analytics;
using InfoTrack.Domain.Repositories;

namespace InfoTrack.Application.Features.Insights.CompareSnapshots;

public sealed class CompareSnapshotsHandler(
    IScrapeSnapshotRepository snapshotRepository,
    IAnalyticsEngine analyticsEngine)
{
    public async Task<SnapshotComparisonResponse> HandleAsync(
        Guid? currentSnapshotId = null,
        Guid? previousSnapshotId = null,
        CancellationToken cancellationToken = default)
    {
        var currentId = currentSnapshotId ?? (await snapshotRepository.GetLatestAsync(cancellationToken))?.Id;
        if (currentId is null)
        {
            return new SnapshotComparisonResponse(Guid.Empty, null, [], [], []);
        }

        var previousId = previousSnapshotId;
        if (previousId is null)
        {
            previousId = (await snapshotRepository.GetPreviousAsync(cancellationToken))?.Id;
        }

        var current = await LoadContextAsync(currentId.Value, cancellationToken);
        ScrapeSnapshotContext? previousContext = null;
        if (previousId.HasValue && previousId.Value != currentId.Value)
        {
            previousContext = await LoadContextAsync(previousId.Value, cancellationToken);
        }

        var comparison = analyticsEngine.CompareSnapshots(new AnalyticsContext(current, previousContext));

        return new SnapshotComparisonResponse(
            comparison.CurrentSnapshotId,
            comparison.PreviousSnapshotId,
            comparison.NewSolicitors
                .Select(x => new SolicitorDeltaDto(
                    x.FirmName,
                    x.LocationName,
                    x.Phone,
                    x.Address,
                    x.Website,
                    x.Rating,
                    x.ReviewCount,
                    x.Rank))
                .ToList(),
            comparison.RemovedSolicitors
                .Select(x => new SolicitorDeltaDto(
                    x.FirmName,
                    x.LocationName,
                    x.Phone,
                    x.Address,
                    x.Website,
                    x.Rating,
                    x.ReviewCount,
                    x.Rank))
                .ToList(),
            comparison.RegionalDeltas
                .Select(x => new RegionalDeltaDto(
                    x.LocationName,
                    x.PreviousCount,
                    x.CurrentCount,
                    x.NetChange,
                    x.NewCount,
                    x.RemovedCount,
                    x.AverageRating))
                .ToList());
    }

    private async Task<ScrapeSnapshotContext> LoadContextAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        var snapshot = await snapshotRepository.GetByIdWithEntriesAsync(snapshotId, cancellationToken)
            ?? throw new InvalidOperationException($"Snapshot {snapshotId} was not found.");

        var records = SolicitorSnapshotRecords.DeduplicateByFirmAndLocation(
            snapshot.Entries
                .Where(x => x.Solicitor is not null && x.Location is not null)
                .Select(x => new SolicitorSnapshotRecord(
                    x.Solicitor!.ExternalKey,
                    x.Solicitor.FirmName,
                    x.Location!.Name,
                    x.LocationId,
                    x.Solicitor.Phone,
                    x.Solicitor.Address,
                    x.Solicitor.Website,
                    x.Solicitor.Rating,
                    x.Solicitor.ReviewCount,
                    x.Rank)));

        return new ScrapeSnapshotContext(snapshot.Id, snapshot.ScrapedAt, records);
    }
}
