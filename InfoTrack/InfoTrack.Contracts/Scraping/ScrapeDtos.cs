namespace InfoTrack.Contracts.Scraping;

public sealed record ScrapeResponse(
    Guid SnapshotId,
    DateTimeOffset ScrapedAt,
    int TotalFirms,
    int LocationsSearched,
    int NewFirms,
    int RemovedFirms);
