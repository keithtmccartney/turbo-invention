using InfoTrack.Contracts.Scraping;
using InfoTrack.Domain.Repositories;
using InfoTrack.Domain.Scraping;

namespace InfoTrack.Application.Features.Scraping.StartScrape;

public sealed class StartScrapeHandler(
    IScrapeOrchestrator scrapeOrchestrator,
    ILocationRepository locationRepository)
{
    public async Task<StartScrapeResponse> HandleAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (await locationRepository.GetActiveCountAsync(cancellationToken) == 0)
        {
            throw new ArgumentException(
                "No active locations configured. Add locations on the Locations page before running a scrape.");
        }

        var operationId = await scrapeOrchestrator.StartAsync(correlationId, cancellationToken);
        return new StartScrapeResponse(operationId, correlationId, "Queued");
    }
}
