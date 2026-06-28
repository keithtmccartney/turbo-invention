using InfoTrack.Domain.Analytics;
using InfoTrack.Domain.Entities;

namespace InfoTrack.Infrastructure.Analytics;

public sealed class DashboardSummaryBuilder(
    RegionalStatisticsCalculator regionalCalculator,
    RankingEngine rankingEngine)
{
    public DashboardSummary Build(AnalyticsContext context, SnapshotComparisonResult comparison)
    {
        var uniqueFirms = SolicitorSnapshotRecords.NormalizeForAnalytics(context.Current.Solicitors);
        var regional = regionalCalculator.Calculate(uniqueFirms);
        var topFirms = rankingEngine.BuildTopFirms(comparison.NationalLeaderboard);

        var rated = uniqueFirms.Where(x => x.Rating.HasValue).ToList();
        var averageRating = rated.Count == 0 ? null : (decimal?)Math.Round(rated.Average(x => x.Rating!.Value), 2);

        return new DashboardSummary(
            uniqueFirms.Count,
            regional.Count,
            comparison.NewSolicitors.Count,
            comparison.RemovedSolicitors.Count,
            averageRating,
            regional,
            topFirms,
            comparison.NationalLeaderboard,
            comparison.GrowthSignals,
            context.Current.ScrapedAt,
            context.Current.SnapshotId,
            context.Previous?.SnapshotId);
    }
}
