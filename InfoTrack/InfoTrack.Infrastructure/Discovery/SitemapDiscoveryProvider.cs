using InfoTrack.Domain.Discovery;
using InfoTrack.Infrastructure.Options;
using InfoTrack.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InfoTrack.Infrastructure.Discovery;

public sealed class SitemapDiscoveryProvider(
    HttpClient httpClient,
    SitemapXmlParser sitemapParser,
    ConveyancingUrlExtractor urlExtractor,
    IDiscoveryProgressReporter progressReporter,
    IOptions<DiscoveryOptions> options,
    ILogger<SitemapDiscoveryProvider> logger) : IDiscoveryProvider
{
    private readonly DiscoveryOptions _options = options.Value;

    public string SourceName => "Sitemap";

    public async Task<IReadOnlyList<DiscoveredLocation>> DiscoverAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting sitemap discovery from {BaseUrl}", _options.BaseUrl);

        await progressReporter.ReportAsync(
            runId,
            new DiscoveryProgressUpdate(DiscoveryProgressStage.DownloadingSitemap, "Downloading sitemap"),
            cancellationToken);

        var sitemapUrls = await ResolveSitemapUrlsAsync(runId, cancellationToken);
        var discovered = new Dictionary<string, DiscoveredLocation>(StringComparer.OrdinalIgnoreCase);
        var sitemapsDownloaded = 0;
        var urlsParsed = 0;
        var errorsEncountered = 0;

        foreach (var sitemapUrl in sitemapUrls)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                logger.LogInformation("Downloading sitemap {SitemapUrl}", sitemapUrl);
                var xml = await DownloadAsync(sitemapUrl, runId, cancellationToken);
                sitemapsDownloaded++;

                await progressReporter.ReportAsync(
                    runId,
                    new DiscoveryProgressUpdate(
                        DiscoveryProgressStage.SitemapDownloaded,
                        $"Downloaded sitemap {sitemapsDownloaded} of {sitemapUrls.Count}",
                        SitemapsDownloaded: sitemapsDownloaded),
                    cancellationToken);

                await progressReporter.ReportAsync(
                    runId,
                    new DiscoveryProgressUpdate(
                        DiscoveryProgressStage.ParsingUrls,
                        "Parsing sitemap URLs",
                        SitemapsDownloaded: sitemapsDownloaded),
                    cancellationToken);

                var pageUrls = sitemapParser.ParseLocations(xml);
                urlsParsed += pageUrls.Count;

                await progressReporter.ReportAsync(
                    runId,
                    new DiscoveryProgressUpdate(
                        DiscoveryProgressStage.UrlsParsed,
                        $"Parsed {urlsParsed} URLs",
                        SitemapsDownloaded: sitemapsDownloaded,
                        UrlsParsed: urlsParsed),
                    cancellationToken);

                await progressReporter.ReportAsync(
                    runId,
                    new DiscoveryProgressUpdate(
                        DiscoveryProgressStage.DiscoveringLocations,
                        "Extracting conveyancing locations",
                        SitemapsDownloaded: sitemapsDownloaded,
                        UrlsParsed: urlsParsed),
                    cancellationToken);

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
            catch (Exception ex)
            {
                errorsEncountered++;
                logger.LogWarning(ex, "Failed processing sitemap {SitemapUrl}", sitemapUrl);

                await progressReporter.ReportAsync(
                    runId,
                    new DiscoveryProgressUpdate(
                        DiscoveryProgressStage.DiscoveringLocations,
                        $"Error processing sitemap: {ex.Message}",
                        SitemapsDownloaded: sitemapsDownloaded,
                        UrlsParsed: urlsParsed,
                        LocationsDiscovered: discovered.Count,
                        ErrorsEncountered: errorsEncountered),
                    cancellationToken);
            }
        }

        await progressReporter.ReportAsync(
            runId,
            new DiscoveryProgressUpdate(
                DiscoveryProgressStage.LocationsDiscovered,
                $"Discovered {discovered.Count} conveyancing locations",
                SitemapsDownloaded: sitemapsDownloaded,
                UrlsParsed: urlsParsed,
                LocationsDiscovered: discovered.Count,
                ErrorsEncountered: errorsEncountered),
            cancellationToken);

        logger.LogInformation("Discovered {Count} conveyancing locations from sitemap", discovered.Count);

        return discovered.Values
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<IReadOnlyList<string>> ResolveSitemapUrlsAsync(
        Guid runId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.ConveyancingSitemapPath))
        {
            return [BuildAbsoluteUrl(_options.ConveyancingSitemapPath)];
        }

        var indexXml = await DownloadAsync(BuildAbsoluteUrl(_options.SitemapIndexPath), runId, cancellationToken);

        await progressReporter.ReportAsync(
            runId,
            new DiscoveryProgressUpdate(
                DiscoveryProgressStage.SitemapDownloaded,
                "Sitemap index downloaded",
                SitemapsDownloaded: 1),
            cancellationToken);

        var candidateUrls = sitemapParser.ParseLocations(indexXml);

        return candidateUrls
            .Where(url => url.Contains(_options.ConveyancingUrlSegment, StringComparison.OrdinalIgnoreCase)
                || url.Contains("google-sitemap", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async Task<string> DownloadAsync(string requestUri, Guid runId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Options.Set(HttpRequestContextKeys.ClientName, ResiliencePipelineNames.Discovery);
        request.Options.Set(HttpRequestContextKeys.OperationKind, "discovery");
        request.Options.Set(HttpRequestContextKeys.OperationId, runId);

        using var scope = ResilienceOperationContext.BeginScope(runId);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private string BuildAbsoluteUrl(string path) =>
        new Uri(new Uri(_options.BaseUrl), path).ToString();
}
