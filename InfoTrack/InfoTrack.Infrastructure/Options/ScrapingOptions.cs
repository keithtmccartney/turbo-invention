namespace InfoTrack.Infrastructure.Options;

public sealed class ScrapingOptions
{
    public const string SectionName = "Scraping";

    public string BaseUrl { get; set; } = "https://www.solicitors.com";

    public string EntryPath { get; set; } = "/conveyancing.html";

    public string LocationPathTemplate { get; set; } = "/conveyancing+{location}.html";

    public string UserAgent { get; set; } = "InfoTrack-Assessment/1.0 (+https://github.com/infotrack)";

    public int RequestDelayMilliseconds { get; set; } = 250;
}
