namespace InfoTrack.Domain.Discovery;

public enum DiscoveryProgressStage
{
    Queued = 0,
    DownloadingSitemap = 10,
    WaitingForRateLimit = 15,
    RetryingRequest = 18,
    ContinuingAfterRetry = 19,
    SitemapDownloaded = 20,
    ParsingUrls = 30,
    UrlsParsed = 40,
    DiscoveringLocations = 50,
    LocationsDiscovered = 60,
    SyncingLocations = 70,
    NewLocationsAdded = 80,
    ExistingLocationsUpdated = 85,
    Completed = 100,
    Failed = 100,
}
