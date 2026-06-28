namespace InfoTrack.Infrastructure.Resilience.Options;

/// <summary>
/// Root configuration section (<c>Resilience</c>) with shared defaults and named client overrides.
/// </summary>
public sealed class ResilienceOptions
{
    public const string SectionName = "Resilience";

    public HttpResilienceClientOptions Defaults { get; set; } = new();

    /// <summary>Named overrides keyed by pipeline name (e.g. <c>Scraping</c>, <c>Discovery</c>).</summary>
    public Dictionary<string, HttpResilienceClientOptions> Clients { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
