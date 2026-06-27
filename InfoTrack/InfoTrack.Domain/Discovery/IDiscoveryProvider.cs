namespace InfoTrack.Domain.Discovery;

public interface IDiscoveryProvider
{
    string SourceName { get; }

    Task<IReadOnlyList<DiscoveredLocation>> DiscoverAsync(CancellationToken cancellationToken = default);
}
