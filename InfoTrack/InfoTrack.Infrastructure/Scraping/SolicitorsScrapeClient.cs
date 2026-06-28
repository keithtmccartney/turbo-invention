using InfoTrack.Domain.Scraping;
using InfoTrack.Infrastructure.Options;
using InfoTrack.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InfoTrack.Infrastructure.Scraping;

public sealed class SolicitorsScrapeClient(
    HttpClient httpClient,
    IOptions<ScrapingOptions> options,
    ILogger<SolicitorsScrapeClient> logger) : ISolicitorsScrapeClient
{
    private readonly ScrapingOptions _options = options.Value;

    public async Task<string> FetchLocationPageAsync(
        string locationSlug,
        Guid? operationId = null,
        CancellationToken cancellationToken = default)
    {
        var slug = locationSlug.Trim().ToLowerInvariant();
        var path = _options.LocationPathTemplate.Replace("{location}", slug, StringComparison.OrdinalIgnoreCase);
        var requestUri = new Uri(new Uri(_options.BaseUrl), path);

        logger.LogInformation("Fetching solicitor listings from {Uri}", requestUri);

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        ApplyResilienceContext(request, operationId);

        using var scope = ResilienceOperationContext.BeginScope(operationId);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    internal static void ApplyResilienceContext(HttpRequestMessage request, Guid? operationId)
    {
        request.Options.Set(HttpRequestContextKeys.ClientName, ResiliencePipelineNames.Scraping);
        request.Options.Set(HttpRequestContextKeys.OperationKind, "scrape");

        if (operationId.HasValue)
        {
            request.Options.Set(HttpRequestContextKeys.OperationId, operationId);
        }
    }
}
