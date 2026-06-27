using InfoTrack.Application.Features.Discovery.GetDiscoverySummary;
using InfoTrack.Contracts.Discovery;
using InfoTrack.Contracts.Insights;
using InfoTrack.Domain.Analytics;
using InfoTrack.Domain.Repositories;

namespace InfoTrack.Application.Features.Insights.GetDashboard;

public sealed class GetDashboardHandler(
    IScrapeSnapshotRepository snapshotRepository,
    IAnalyticsEngine analyticsEngine,
    GetDiscoverySummaryHandler discoverySummaryHandler)
{
    public async Task<DashboardResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        var discovery = await discoverySummaryHandler.HandleAsync(cancellationToken: cancellationToken);
        var context = await BuildContextAsync(cancellationToken);

        if (context is null)
        {
            return EmptyDashboard(discovery);
        }

        var dashboard = analyticsEngine.BuildDashboard(context);
        return Map(dashboard, discovery);
    }

    internal async Task<AnalyticsContext?> BuildContextAsync(CancellationToken cancellationToken)
    {
        var currentSnapshot = await snapshotRepository.GetLatestAsync(cancellationToken);
        if (currentSnapshot is null)
        {
            return null;
        }

        var previousSnapshot = await snapshotRepository.GetPreviousAsync(cancellationToken);
        var current = await LoadSnapshotContextAsync(currentSnapshot.Id, cancellationToken);
        ScrapeSnapshotContext? previous = null;

        if (previousSnapshot is not null)
        {
            previous = await LoadSnapshotContextAsync(previousSnapshot.Id, cancellationToken);
        }

        return new AnalyticsContext(current, previous);
    }

    private async Task<ScrapeSnapshotContext> LoadSnapshotContextAsync(Guid snapshotId, CancellationToken cancellationToken)
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
                    x.Solicitor.Rating,
                    x.Solicitor.ReviewCount,
                    x.Rank)));

        return new ScrapeSnapshotContext(snapshot.Id, snapshot.ScrapedAt, records);
    }

    private static DashboardResponse EmptyDashboard(DiscoverySummaryDto discovery) =>
        new(
            0,
            0,
            0,
            0,
            null,
            [],
            [],
            [],
            [],
            null,
            null,
            null,
            discovery);

    private static DashboardResponse Map(DashboardSummary dashboard, DiscoverySummaryDto discovery) =>
        new(
            dashboard.TotalFirms,
            dashboard.LocationsSearched,
            dashboard.NewFirms,
            dashboard.RemovedFirms,
            dashboard.AverageRating,
            dashboard.RegionalBreakdown
                .Select(x => new RegionalStatisticDto(x.LocationName, x.FirmCount, x.AverageRating, x.TotalReviews))
                .ToList(),
            dashboard.TopFirms
                .Select(x => new FirmRankingDto(x.Rank, x.FirmName, x.LocationName, x.Rating, x.ReviewCount, x.RankChange))
                .ToList(),
            dashboard.NationalLeaderboard
                .Select(x => new FirmRankingDto(x.Rank, x.FirmName, x.LocationName, x.Rating, x.ReviewCount, x.RankChange))
                .ToList(),
            dashboard.GrowthSignals
                .Select(x => new GrowthSignalDto(x.FirmName, x.LocationName, x.SignalType, x.Description))
                .ToList(),
            dashboard.LastScrapedAt,
            dashboard.CurrentSnapshotId,
            dashboard.PreviousSnapshotId,
            discovery);
}
