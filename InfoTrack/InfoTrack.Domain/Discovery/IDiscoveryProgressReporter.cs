namespace InfoTrack.Domain.Discovery;

public interface IDiscoveryProgressReporter
{
    Task ReportAsync(Guid runId, DiscoveryProgressUpdate update, CancellationToken cancellationToken = default);
}
