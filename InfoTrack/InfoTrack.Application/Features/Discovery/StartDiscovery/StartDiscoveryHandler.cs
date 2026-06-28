using InfoTrack.Contracts.Discovery;
using InfoTrack.Domain.Discovery;

namespace InfoTrack.Application.Features.Discovery.StartDiscovery;

public sealed class StartDiscoveryHandler(IDiscoveryOrchestrator discoveryOrchestrator)
{
    public async Task<StartDiscoveryResponse> HandleAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var operationId = await discoveryOrchestrator.StartAsync(correlationId, cancellationToken);
        return new StartDiscoveryResponse(operationId, correlationId, "Queued");
    }
}
