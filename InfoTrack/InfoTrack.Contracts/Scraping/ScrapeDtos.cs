namespace InfoTrack.Contracts.Scraping;

public sealed record ScrapeRunSummaryDto(
    DateTimeOffset ScrapedAt,
    IReadOnlyList<string> LocationNames,
    int TotalFirms);

public sealed record ScrapeResponse(
    Guid SnapshotId,
    DateTimeOffset ScrapedAt,
    int TotalFirms,
    int LocationsSearched,
    int NewFirms,
    int RemovedFirms);

public sealed record StartScrapeResponse(
    Guid OperationId,
    string CorrelationId,
    string Status);

public sealed record ScrapeProgressDto(
    string Stage,
    string? Message,
    int LocationsTotal,
    int LocationsCompleted,
    int FirmsDiscovered,
    int NewFirms,
    int RemovedFirms,
    int ErrorsEncountered,
    int PercentComplete);

public sealed record ScrapeRunStatusResponse(
    Guid OperationId,
    string CorrelationId,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    long? DurationMilliseconds,
    ScrapeProgressDto Progress,
    ScrapeResponse? Result,
    string? ErrorMessage);
