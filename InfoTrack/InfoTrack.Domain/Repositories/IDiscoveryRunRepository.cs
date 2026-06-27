using InfoTrack.Domain.Entities;

namespace InfoTrack.Domain.Repositories;

public interface IDiscoveryRunRepository
{
    Task AddAsync(DiscoveryRun run, CancellationToken cancellationToken = default);

    Task UpdateAsync(DiscoveryRun run, CancellationToken cancellationToken = default);

    Task<DiscoveryRun?> GetLatestCompletedAsync(CancellationToken cancellationToken = default);

    Task<DiscoveryRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscoveryRun>> GetHistoryAsync(int take, CancellationToken cancellationToken = default);
}
