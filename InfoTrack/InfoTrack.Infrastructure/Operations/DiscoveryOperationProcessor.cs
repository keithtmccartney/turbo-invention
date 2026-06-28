using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Operations;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Infrastructure.Operations;

public sealed class DiscoveryOperationProcessor(
    IDiscoveryOrchestrator discoveryOrchestrator,
    ILogger<DiscoveryOperationProcessor> logger) : IOperationProcessor
{
    public OperationKind Kind => OperationKind.Discovery;

    public async Task ProcessAsync(OperationWorkItem workItem, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Discovery processor starting operation {OperationId}",
            workItem.OperationId);

        await discoveryOrchestrator.ExecuteAsync(
            workItem.OperationId,
            workItem.CorrelationId,
            cancellationToken);
    }
}
