using InfoTrack.Contracts.Solicitors;
using InfoTrack.Domain.Repositories;
using InfoTrack.Domain.Scraping;

namespace InfoTrack.Application.Features.Scraping.GetResults;

public sealed class GetResultsHandler(
    ISolicitorRepository solicitorRepository,
    IScrapeSnapshotRepository snapshotRepository)
{
    public async Task<ResultsResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        var latestSnapshot = await snapshotRepository.GetLatestAsync(cancellationToken);
        return await MapFromSolicitorsAsync(latestSnapshot?.ScrapedAt, cancellationToken);
    }

    private async Task<ResultsResponse> MapFromSolicitorsAsync(
        DateTimeOffset? scrapedAt,
        CancellationToken cancellationToken)
    {
        var solicitors = await solicitorRepository.GetAllWithLocationsAsync(cancellationToken);

        var grouped = solicitors
            .GroupBy(x => x.Location?.Name ?? "Unknown")
            .OrderBy(x => x.First().Location?.DisplayOrder ?? int.MaxValue)
            .Select(group => new LocationResultsDto(
                group.Key,
                group.Select(MapSolicitor).OrderBy(x => x.FirmName).ToList()))
            .ToList();

        return new ResultsResponse(scrapedAt, grouped);
    }

    private static SolicitorDto MapSolicitor(Domain.Entities.Solicitor solicitor) =>
        new(
            solicitor.Id,
            ScrapedTextNormalizer.Normalize(solicitor.FirmName) ?? solicitor.FirmName,
            solicitor.Location?.Name ?? "Unknown",
            ScrapedTextNormalizer.Normalize(solicitor.Phone),
            ScrapedTextNormalizer.Normalize(solicitor.Address),
            solicitor.Website,
            solicitor.EmailEnquiryUrl,
            ScrapedTextNormalizer.Normalize(solicitor.Description),
            solicitor.Rating,
            solicitor.ReviewCount);
}
