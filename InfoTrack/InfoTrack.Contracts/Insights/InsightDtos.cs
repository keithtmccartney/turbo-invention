namespace InfoTrack.Contracts.Insights;

using InfoTrack.Contracts.Discovery;
using InfoTrack.Contracts.Scraping;

public sealed record RegionalStatisticDto(
    string LocationName,
    int FirmCount,
    decimal? AverageRating,
    int TotalReviews);

public sealed record FirmRankingDto(
    int Rank,
    string FirmName,
    string LocationName,
    decimal? Rating,
    int? ReviewCount,
    int RankChange);

public sealed record GrowthSignalDto(
    string FirmName,
    string LocationName,
    string SignalType,
    string Description);

public sealed record DashboardResponse(
    int TotalFirms,
    int LocationsSearched,
    int NewFirms,
    int RemovedFirms,
    decimal? AverageRating,
    IReadOnlyList<RegionalStatisticDto> RegionalBreakdown,
    IReadOnlyList<FirmRankingDto> TopFirms,
    IReadOnlyList<FirmRankingDto> NationalLeaderboard,
    IReadOnlyList<GrowthSignalDto> GrowthSignals,
    DateTimeOffset? LastScrapedAt,
    Guid? CurrentSnapshotId,
    Guid? PreviousSnapshotId,
    IReadOnlyList<ScrapeRunSummaryDto> ScrapeHistory,
    DiscoverySummaryDto? Discovery);

public sealed record SnapshotComparisonResponse(
    Guid CurrentSnapshotId,
    Guid? PreviousSnapshotId,
    IReadOnlyList<SolicitorDeltaDto> NewSolicitors,
    IReadOnlyList<SolicitorDeltaDto> RemovedSolicitors,
    IReadOnlyList<RegionalDeltaDto> RegionalDeltas);

public sealed record SolicitorDeltaDto(
    string FirmName,
    string LocationName,
    string? Phone,
    decimal? Rating,
    int Rank);

public sealed record RegionalDeltaDto(
    string LocationName,
    int PreviousCount,
    int CurrentCount,
    int NetChange,
    int NewCount,
    int RemovedCount,
    decimal? AverageRating);
