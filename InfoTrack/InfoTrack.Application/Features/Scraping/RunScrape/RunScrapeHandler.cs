using InfoTrack.Contracts.Scraping;
using InfoTrack.Domain.Repositories;
using InfoTrack.Domain.Scraping;

namespace InfoTrack.Application.Features.Scraping.RunScrape;

public sealed class RunScrapeHandler(
    IScrapeOrchestrator scrapeOrchestrator,
    IScrapeSnapshotRepository snapshotRepository,
    IInsightSummaryRepository insightSummaryRepository)
{
    public async Task<ScrapeResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        var snapshotId = await scrapeOrchestrator.RunAsync(cancellationToken);
        var snapshot = await snapshotRepository.GetByIdWithEntriesAsync(snapshotId, cancellationToken)
            ?? throw new InvalidOperationException("Scrape snapshot was not persisted.");

        var insight = await insightSummaryRepository.GetLatestAsync(cancellationToken);

        return new ScrapeResponse(
            snapshot.Id,
            snapshot.ScrapedAt,
            snapshot.TotalFirms,
            snapshot.LocationsSearched,
            insight?.NewFirms ?? 0,
            insight?.RemovedFirms ?? 0);
    }
}
