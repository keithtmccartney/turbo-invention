using InfoTrack.Application.Features.Discovery;
using InfoTrack.Contracts.Discovery;
using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;

namespace InfoTrack.Application.Features.Discovery.GetDiscoveryRunStatus;

public sealed class GetDiscoveryRunStatusHandler(IDiscoveryRunRepository discoveryRunRepository)
{
    public async Task<DiscoveryRunStatusResponse?> HandleAsync(
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        var run = await discoveryRunRepository.GetByIdAsync(operationId, cancellationToken);
        return run is null ? null : DiscoveryRunMapping.MapStatus(run);
    }

    public static bool IsTerminal(DiscoveryRunStatusResponse status) =>
        status.Status is nameof(DiscoveryRunStatus.Completed) or nameof(DiscoveryRunStatus.Failed);
}
