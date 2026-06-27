using InfoTrack.Domain.Discovery;
using InfoTrack.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InfoTrack.Infrastructure.Discovery;

public sealed class SitemapDiscoveryProvider(
    HttpClient httpClient,
    SitemapXmlParser sitemapParser,
    ConveyancingUrlExtractor urlExtractor,
    IOptions<DiscoveryOptions> options,
    ILogger<SitemapDiscoveryProvider> logger) : IDiscoveryProvider
{
    private readonly DiscoveryOptions _options = options.Value;

    public string SourceName => "Sitemap";

    public async Task<IReadOnlyList<DiscoveredLocation>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting sitemap discovery from {BaseUrl}", _options.BaseUrl);

        var sitemapUrls = await ResolveSitemapUrlsAsync(cancellationToken);
        var discovered = new Dictionary<string, DiscoveredLocation>(StringComparer.OrdinalIgnoreCase);

        foreach (var sitemapUrl in sitemapUrls)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogInformation("Downloading sitemap {SitemapUrl}", sitemapUrl);
            var xml = await DownloadAsync(sitemapUrl, cancellationToken);
            var pageUrls = sitemapParser.ParseLocations(xml);
            var slugs = urlExtractor.ExtractSlugs(pageUrls);

            foreach (var slug in slugs)
            {
                if (discovered.ContainsKey(slug))
                {
                    continue;
                }

                discovered[slug] = new DiscoveredLocation(slug, LocationNameNormalizer.FromSlug(slug));
            }
        }

        logger.LogInformation("Discovered {Count} conveyancing locations from sitemap", discovered.Count);

        return discovered.Values
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<IReadOnlyList<string>> ResolveSitemapUrlsAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.ConveyancingSitemapPath))
        {
            return [BuildAbsoluteUrl(_options.ConveyancingSitemapPath)];
        }

        var indexXml = await DownloadAsync(BuildAbsoluteUrl(_options.SitemapIndexPath), cancellationToken);
        var candidateUrls = sitemapParser.ParseLocations(indexXml);

        return candidateUrls
            .Where(url => url.Contains(_options.ConveyancingUrlSegment, StringComparison.OrdinalIgnoreCase)
                || url.Contains("google-sitemap", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async Task<string> DownloadAsync(string requestUri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private string BuildAbsoluteUrl(string path) =>
        new Uri(new Uri(_options.BaseUrl), path).ToString();
}
