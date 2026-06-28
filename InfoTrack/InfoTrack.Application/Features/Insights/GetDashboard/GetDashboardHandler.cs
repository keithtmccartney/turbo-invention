using InfoTrack.Application.Features.Discovery.GetDiscoverySummary;
using InfoTrack.Contracts.Discovery;
using InfoTrack.Contracts.Insights;
using InfoTrack.Contracts.Scraping;
using InfoTrack.Domain.Analytics;
using InfoTrack.Domain.Entities;
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
        var scrapeHistory = await LoadScrapeHistoryAsync(cancellationToken);
        var context = await BuildContextAsync(cancellationToken);

        if (context is null)
        {
            return EmptyDashboard(discovery, scrapeHistory);
        }

        var dashboard = analyticsEngine.BuildDashboard(context);
        return Map(dashboard, discovery, scrapeHistory);
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
                    x.Solicitor.Website,
                    x.Solicitor.Rating,
                    x.Solicitor.ReviewCount,
                    x.Rank)));

        return new ScrapeSnapshotContext(snapshot.Id, snapshot.ScrapedAt, records);
    }

    private async Task<IReadOnlyList<ScrapeRunSummaryDto>> LoadScrapeHistoryAsync(CancellationToken cancellationToken)
    {
        var snapshots = await snapshotRepository.GetHistoryAsync(cancellationToken: cancellationToken);
        return snapshots
            .Select(MapScrapeRun)
            .OrderBy(x => x.ScrapedAt)
            .ToList();
    }

    private static ScrapeRunSummaryDto MapScrapeRun(ScrapeSnapshot snapshot)
    {
        var locationNames = snapshot.Entries
            .Where(entry => entry.Location is not null)
            .Select(entry => entry.Location!.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var totalFirms = snapshot.Entries.Count > 0
            ? snapshot.Entries.Select(entry => entry.SolicitorId).Distinct().Count()
            : snapshot.TotalFirms;

        return new ScrapeRunSummaryDto(snapshot.ScrapedAt, locationNames, totalFirms);
    }

    private static DashboardResponse EmptyDashboard(
        DiscoverySummaryDto discovery,
        IReadOnlyList<ScrapeRunSummaryDto> scrapeHistory) =>
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
            scrapeHistory.LastOrDefault()?.ScrapedAt,
            null,
            null,
            scrapeHistory,
            discovery);

    private static DashboardResponse Map(
        DashboardSummary dashboard,
        DiscoverySummaryDto discovery,
        IReadOnlyList<ScrapeRunSummaryDto> scrapeHistory) =>
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
            scrapeHistory,
            discovery);
}
