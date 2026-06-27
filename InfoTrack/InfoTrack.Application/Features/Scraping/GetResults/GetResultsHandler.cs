using InfoTrack.Contracts.Solicitors;
using InfoTrack.Domain.Repositories;

namespace InfoTrack.Application.Features.Scraping.GetResults;

public sealed class GetResultsHandler(
    ISolicitorRepository solicitorRepository,
    IScrapeSnapshotRepository snapshotRepository)
{
    public async Task<ResultsResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        var latestSnapshot = await snapshotRepository.GetLatestAsync(cancellationToken);
        var solicitors = await solicitorRepository.GetAllWithLocationsAsync(cancellationToken);

        var grouped = solicitors
            .GroupBy(x => x.Location?.Name ?? "Unknown")
            .OrderBy(x => x.First().Location?.DisplayOrder ?? int.MaxValue)
            .Select(group => new LocationResultsDto(
                group.Key,
                group.Select(Map).OrderBy(x => x.FirmName).ToList()))
            .ToList();

        return new ResultsResponse(latestSnapshot?.ScrapedAt, grouped);
    }

    private static SolicitorDto Map(Domain.Entities.Solicitor solicitor) =>
        new(
            solicitor.Id,
            solicitor.FirmName,
            solicitor.Location?.Name ?? "Unknown",
            solicitor.Phone,
            solicitor.Address,
            solicitor.Website,
            solicitor.EmailEnquiryUrl,
            solicitor.Description,
            solicitor.Rating,
            solicitor.ReviewCount);
}
