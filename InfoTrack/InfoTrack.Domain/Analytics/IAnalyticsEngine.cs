using InfoTrack.Domain.Analytics;
using InfoTrack.Domain.Entities;

namespace InfoTrack.Domain.Analytics;

/// <summary>
/// Analytics engine boundary — designed as an extractable microservice contract.
/// </summary>
public interface IAnalyticsEngine
{
    SnapshotComparisonResult CompareSnapshots(AnalyticsContext context);

    DashboardSummary BuildDashboard(AnalyticsContext context, InsightSummary? cachedSummary = null);

    InsightSummary PersistSummary(AnalyticsContext context);
}
