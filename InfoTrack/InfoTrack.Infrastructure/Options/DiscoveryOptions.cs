namespace InfoTrack.Infrastructure.Options;

public sealed class DiscoveryOptions
{
    public const string SectionName = "Discovery";

    public string BaseUrl { get; set; } = "https://www.solicitors.com";

    public string SitemapIndexPath { get; set; } = "/sitemap.xml";

    public string ConveyancingSitemapPath { get; set; } = "/google-sitemap4.xml";

    public string ConveyancingUrlSegment { get; set; } = "conveyancing+";

    public string UserAgent { get; set; } = "InfoTrack-Assessment/1.0 (+https://github.com/infotrack)";

    public int RequestTimeoutSeconds { get; set; } = 30;
}
