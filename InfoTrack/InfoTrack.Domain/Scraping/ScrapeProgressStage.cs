namespace InfoTrack.Domain.Scraping;

public enum ScrapeProgressStage
{
    Queued = 0,
    ValidatingLocations = 10,
    WaitingForRateLimit = 15,
    RetryingRequest = 18,
    ScrapingLocation = 30,
    LocationScraped = 50,
    PersistingSolicitors = 70,
    BuildingSnapshot = 85,
    ComputingAnalytics = 95,
    Completed = 100,
    Failed = 100,
}
