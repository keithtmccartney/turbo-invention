namespace InfoTrack.Domain.Discovery;

/// <summary>
/// Immutable snapshot of discovery progress suitable for persistence and API responses.
/// </summary>
public sealed record DiscoveryProgressUpdate(
    DiscoveryProgressStage Stage,
    string? Message = null,
    int SitemapsDownloaded = 0,
    int UrlsParsed = 0,
    int LocationsDiscovered = 0,
    int NewLocationsAdded = 0,
    int ExistingLocationsUpdated = 0,
    int ErrorsEncountered = 0)
{
    public int PercentComplete => (int)Stage;
}
