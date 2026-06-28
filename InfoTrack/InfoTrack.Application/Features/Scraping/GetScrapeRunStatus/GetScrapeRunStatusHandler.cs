using InfoTrack.Application.Features.Scraping;
using InfoTrack.Contracts.Scraping;
using InfoTrack.Domain.Repositories;

namespace InfoTrack.Application.Features.Scraping.GetScrapeRunStatus;

public sealed class GetScrapeRunStatusHandler(
    IScrapeRunRepository scrapeRunRepository,
    IScrapeSnapshotRepository snapshotRepository)
{
    public async Task<ScrapeRunStatusResponse?> HandleAsync(
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        var run = await scrapeRunRepository.GetByIdAsync(operationId, cancellationToken);
        if (run is null)
        {
            return null;
        }

        var snapshot = run.SnapshotId.HasValue
            ? await snapshotRepository.GetByIdWithEntriesAsync(run.SnapshotId.Value, cancellationToken)
            : null;

        return ScrapeRunMapping.MapStatus(run, snapshot);
    }
}
