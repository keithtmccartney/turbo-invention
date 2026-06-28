using InfoTrack.Domain.Operations;
using InfoTrack.Domain.Scraping;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Infrastructure.Operations;

public sealed class ScrapeOperationProcessor(
    IScrapeOrchestrator scrapeOrchestrator,
    ILogger<ScrapeOperationProcessor> logger) : IOperationProcessor
{
    public OperationKind Kind => OperationKind.Scrape;

    public async Task ProcessAsync(OperationWorkItem workItem, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Scrape processor starting operation {OperationId}",
            workItem.OperationId);

        await scrapeOrchestrator.ExecuteAsync(
            workItem.OperationId,
            workItem.CorrelationId,
            cancellationToken);
    }
}
