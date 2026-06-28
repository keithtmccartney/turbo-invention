using System.Text.Json;
using InfoTrack.Domain.Analytics;
using InfoTrack.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Infrastructure.Analytics;

/// <summary>
/// Composable analytics pipeline designed for future extraction into a dedicated microservice.
/// Each internal component owns a single analytical concern (compare, rank, regional stats, growth).
/// </summary>
public sealed class AnalyticsEngine(
    SnapshotComparer comparer,
    DashboardSummaryBuilder dashboardBuilder,
    ILogger<AnalyticsEngine> logger) : IAnalyticsEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public SnapshotComparisonResult CompareSnapshots(AnalyticsContext context) =>
        comparer.Compare(context);

    public DashboardSummary BuildDashboard(AnalyticsContext context, InsightSummary? cachedSummary = null)
    {
        var comparison = comparer.Compare(context);
        return dashboardBuilder.Build(context, comparison);
    }

    public InsightSummary PersistSummary(AnalyticsContext context)
    {
        var comparison = comparer.Compare(context);
        var dashboard = dashboardBuilder.Build(context, comparison);

        var payload = new InsightPayload(
            comparison.RegionalDeltas,
            comparison.NationalLeaderboard,
            dashboard.RegionalBreakdown,
            dashboard.TopFirms,
            comparison.GrowthSignals,
            comparison.NewSolicitors,
            comparison.RemovedSolicitors);

        var summary = new InsightSummary
        {
            Id = Guid.NewGuid(),
            CurrentSnapshotId = context.Current.SnapshotId,
            PreviousSnapshotId = context.Previous?.SnapshotId,
            GeneratedAt = DateTimeOffset.UtcNow,
            TotalFirms = dashboard.TotalFirms,
            LocationsSearched = dashboard.LocationsSearched,
            NewFirms = dashboard.NewFirms,
            RemovedFirms = dashboard.RemovedFirms,
            AverageRating = dashboard.AverageRating,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions)
        };

        logger.LogInformation(
            "Analytics summary generated for snapshot {SnapshotId}: +{New} / -{Removed} firms",
            summary.CurrentSnapshotId,
            summary.NewFirms,
            summary.RemovedFirms);

        return summary;
    }

    private sealed record InsightPayload(
        IReadOnlyList<RegionalDelta> RegionalDeltas,
        IReadOnlyList<FirmRanking> NationalLeaderboard,
        IReadOnlyList<RegionalStatistic> RegionalBreakdown,
        IReadOnlyList<FirmRanking> TopFirms,
        IReadOnlyList<GrowthSignal> GrowthSignals,
        IReadOnlyList<SolicitorSnapshotRecord> NewSolicitors,
        IReadOnlyList<SolicitorSnapshotRecord> RemovedSolicitors);
}
