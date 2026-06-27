namespace InfoTrack.Contracts.Discovery;

public sealed record DiscoveryStatisticsDto(
    int TotalDiscovered,
    int Added,
    int Updated,
    int Removed,
    int Skipped,
    int Existing,
    long DurationMilliseconds);

public sealed record DiscoveredLocationDto(string Slug, string Name);

public sealed record DiscoveryRunResponse(
    Guid RunId,
    string Source,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    long DurationMilliseconds,
    DiscoveryStatisticsDto Statistics,
    IReadOnlyList<DiscoveredLocationDto> Locations);

public sealed record DiscoveryRunSummaryDto(
    Guid Id,
    string Source,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    long? DurationMilliseconds,
    string Status,
    int LocationsFound,
    int NewLocations,
    int UpdatedLocations,
    int RemovedLocations,
    int SkippedLocations,
    string? ErrorMessage);

public sealed record DiscoveryHistoryResponse(IReadOnlyList<DiscoveryRunSummaryDto> Runs);

public sealed record DiscoveryRunTrendPointDto(
    DateTimeOffset CompletedAt,
    int LocationsFound,
    int Added,
    int Removed);

public sealed record DiscoverySummaryDto(
    int ActiveLocationCount,
    Guid? LastRunId,
    string? LastRunSource,
    DateTimeOffset? LastRunCompletedAt,
    long? LastRunDurationMilliseconds,
    int? LastRunLocationsFound,
    int? LastRunAdded,
    int? LastRunUpdated,
    int? LastRunRemoved,
    int? LastRunSkipped,
    IReadOnlyList<DiscoveryRunTrendPointDto> HistoricalTrend);
