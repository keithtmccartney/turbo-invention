namespace InfoTrack.Domain.Discovery;

public sealed record DiscoveredLocation(string Slug, string Name);

public sealed record DiscoveryStatistics(
    int TotalDiscovered,
    int Added,
    int Updated,
    int Removed,
    int Skipped,
    int Existing,
    TimeSpan Duration);

public sealed record DiscoveryResult(
    IReadOnlyList<DiscoveredLocation> Locations,
    DiscoveryStatistics Statistics,
    Guid RunId,
    string Source,
    DateTimeOffset CompletedAt);

public sealed record DiscoverySyncOutcome(
    int Added,
    int Updated,
    int Removed,
    int Skipped,
    int Existing);

public sealed record DiscoverySummary(
    int ActiveLocationCount,
    Guid? LastRunId,
    string? LastRunSource,
    DateTimeOffset? LastRunCompletedAt,
    TimeSpan? LastRunDuration,
    int? LastRunLocationsFound,
    int? LastRunAdded,
    int? LastRunUpdated,
    int? LastRunRemoved,
    int? LastRunSkipped,
    IReadOnlyList<DiscoveryRunTrendPoint> HistoricalTrend);

public sealed record DiscoveryRunTrendPoint(
    DateTimeOffset CompletedAt,
    int LocationsFound,
    int Added,
    int Removed);
