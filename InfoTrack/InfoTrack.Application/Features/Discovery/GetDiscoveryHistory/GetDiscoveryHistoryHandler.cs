using InfoTrack.Contracts.Discovery;
using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;

namespace InfoTrack.Application.Features.Discovery.GetDiscoveryHistory;

public sealed class GetDiscoveryHistoryHandler(IDiscoveryRunRepository discoveryRunRepository)
{
    public async Task<DiscoveryHistoryResponse> HandleAsync(int take = 20, CancellationToken cancellationToken = default)
    {
        var runs = await discoveryRunRepository.GetHistoryAsync(take, cancellationToken);
        return new DiscoveryHistoryResponse(runs.Select(Map).ToList());
    }

    internal static DiscoveryRunSummaryDto Map(DiscoveryRun run) =>
        new(
            run.Id,
            run.Source,
            run.StartedAt,
            run.CompletedAt,
            run.Duration.HasValue ? (long)run.Duration.Value.TotalMilliseconds : null,
            run.Status.ToString(),
            run.LocationsFound,
            run.NewLocations,
            run.UpdatedLocations,
            run.RemovedLocations,
            run.SkippedLocations,
            run.ErrorMessage);
}
