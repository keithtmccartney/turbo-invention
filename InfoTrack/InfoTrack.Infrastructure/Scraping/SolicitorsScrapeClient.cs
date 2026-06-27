using InfoTrack.Domain.Scraping;
using InfoTrack.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InfoTrack.Infrastructure.Scraping;

public sealed class SolicitorsScrapeClient(
    HttpClient httpClient,
    IOptions<ScrapingOptions> options,
    ILogger<SolicitorsScrapeClient> logger) : ISolicitorsScrapeClient
{
    private readonly ScrapingOptions _options = options.Value;

    public async Task<string> FetchLocationPageAsync(string locationSlug, CancellationToken cancellationToken = default)
    {
        var slug = locationSlug.Trim().ToLowerInvariant();
        var path = _options.LocationPathTemplate.Replace("{location}", slug, StringComparison.OrdinalIgnoreCase);
        var requestUri = new Uri(new Uri(_options.BaseUrl), path);

        logger.LogInformation("Fetching solicitor listings from {Uri}", requestUri);

        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
