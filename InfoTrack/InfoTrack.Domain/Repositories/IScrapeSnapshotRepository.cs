using InfoTrack.Domain.Entities;

namespace InfoTrack.Domain.Repositories;

public interface IScrapeSnapshotRepository
{
    Task<ScrapeSnapshot?> GetLatestAsync(CancellationToken cancellationToken = default);

    Task<ScrapeSnapshot?> GetPreviousAsync(CancellationToken cancellationToken = default);

    Task<ScrapeSnapshot?> GetByIdWithEntriesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScrapeSnapshot>> GetHistoryAsync(int take = 20, CancellationToken cancellationToken = default);

    Task AddAsync(ScrapeSnapshot snapshot, CancellationToken cancellationToken = default);
}
