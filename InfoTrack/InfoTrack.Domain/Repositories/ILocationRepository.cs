using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Entities;

namespace InfoTrack.Domain.Repositories;

public interface ILocationRepository
{
    Task<IReadOnlyList<Location>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Location>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default);

    Task ReplaceAllAsync(IReadOnlyList<Location> locations, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Location>> SetActiveLocationsAsync(
        IReadOnlyList<string> activeNames,
        CancellationToken cancellationToken = default);

    Task<DiscoverySyncOutcome> SyncDiscoveredLocationsAsync(
        IReadOnlyList<DiscoveredLocation> discovered,
        CancellationToken cancellationToken = default);
}
