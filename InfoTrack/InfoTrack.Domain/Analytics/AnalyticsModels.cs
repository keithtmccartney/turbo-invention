namespace InfoTrack.Domain.Analytics;

public sealed record SolicitorSnapshotRecord(
    string ExternalKey,
    string FirmName,
    string LocationName,
    Guid LocationId,
    string? Phone,
    string? Address,
    decimal? Rating,
    int? ReviewCount,
    int Rank);

public sealed record SnapshotComparisonResult(
    Guid CurrentSnapshotId,
    Guid? PreviousSnapshotId,
    IReadOnlyList<SolicitorSnapshotRecord> NewSolicitors,
    IReadOnlyList<SolicitorSnapshotRecord> RemovedSolicitors,
    IReadOnlyList<RegionalDelta> RegionalDeltas,
    IReadOnlyList<FirmRanking> NationalLeaderboard,
    IReadOnlyList<GrowthSignal> GrowthSignals);

public sealed record RegionalDelta(
    string LocationName,
    int PreviousCount,
    int CurrentCount,
    int NetChange,
    int NewCount,
    int RemovedCount,
    decimal? AverageRating);

public sealed record FirmRanking(
    int Rank,
    string FirmName,
    string LocationName,
    decimal? Rating,
    int? ReviewCount,
    int RankChange);

public sealed record GrowthSignal(
    string FirmName,
    string LocationName,
    string SignalType,
    string Description);

public sealed record DashboardSummary(
    int TotalFirms,
    int LocationsSearched,
    int NewFirms,
    int RemovedFirms,
    decimal? AverageRating,
    IReadOnlyList<RegionalStatistic> RegionalBreakdown,
    IReadOnlyList<FirmRanking> TopFirms,
    IReadOnlyList<FirmRanking> NationalLeaderboard,
    IReadOnlyList<GrowthSignal> GrowthSignals,
    DateTimeOffset? LastScrapedAt,
    Guid? CurrentSnapshotId,
    Guid? PreviousSnapshotId);

public sealed record RegionalStatistic(
    string LocationName,
    int FirmCount,
    decimal? AverageRating,
    int TotalReviews);

public sealed record AnalyticsContext(
    ScrapeSnapshotContext Current,
    ScrapeSnapshotContext? Previous);

public sealed record ScrapeSnapshotContext(
    Guid SnapshotId,
    DateTimeOffset ScrapedAt,
    IReadOnlyList<SolicitorSnapshotRecord> Solicitors);
