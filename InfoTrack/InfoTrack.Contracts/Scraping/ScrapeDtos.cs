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
