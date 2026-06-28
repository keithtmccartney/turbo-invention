using InfoTrack.Application.Features.Discovery.GetDiscoveryHistory;
using InfoTrack.Contracts.Discovery;
using InfoTrack.Domain.Repositories;

namespace InfoTrack.Application.Features.Discovery.GetLatestDiscovery;

public sealed class GetLatestDiscoveryHandler(IDiscoveryRunRepository discoveryRunRepository)
{
    public async Task<DiscoveryRunSummaryDto?> HandleAsync(CancellationToken cancellationToken = default)
    {
        var run = await discoveryRunRepository.GetLatestCompletedAsync(cancellationToken);
        return run is null ? null : GetDiscoveryHistoryHandler.Map(run);
    }
}
