namespace InfoTrack.Domain.Discovery;

public interface IDiscoveryOrchestrator
{
    /// <summary>
    /// Creates a queued discovery run and returns its operation identifier.
    /// </summary>
    Task<Guid> StartAsync(string correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes discovery for an existing run. Invoked by the background operation worker.
    /// </summary>
    Task ExecuteAsync(Guid operationId, string correlationId, CancellationToken cancellationToken = default);
}
