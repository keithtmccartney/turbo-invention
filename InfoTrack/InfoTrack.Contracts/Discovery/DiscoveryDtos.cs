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

public sealed record StartDiscoveryResponse(
    Guid OperationId,
    string CorrelationId,
    string Status);

public sealed record DiscoveryProgressDto(
    string Stage,
    string? Message,
    int SitemapsDownloaded,
    int UrlsParsed,
    int LocationsDiscovered,
    int NewLocationsAdded,
    int ExistingLocationsUpdated,
    int ErrorsEncountered,
    int PercentComplete);

public sealed record DiscoveryRunStatusResponse(
    Guid OperationId,
    string CorrelationId,
    string Source,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    long? DurationMilliseconds,
    DiscoveryProgressDto Progress,
    DiscoveryStatisticsDto? Statistics,
    string? ErrorMessage,
    IReadOnlyList<DiscoveredLocationDto>? Locations);

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
