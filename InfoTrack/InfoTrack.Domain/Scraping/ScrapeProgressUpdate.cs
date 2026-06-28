namespace InfoTrack.Domain.Scraping;

public sealed record ScrapeProgressUpdate(
    ScrapeProgressStage Stage,
    string? Message = null,
    int LocationsTotal = 0,
    int LocationsCompleted = 0,
    int FirmsDiscovered = 0,
    int ErrorsEncountered = 0)
{
    public int PercentComplete => (int)Stage;
}
