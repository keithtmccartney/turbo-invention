namespace InfoTrack.Domain.Discovery;

public interface IDiscoveryOrchestrator
{
    Task<DiscoveryResult> RunAsync(CancellationToken cancellationToken = default);
}
