using InfoTrack.Domain.Entities;

namespace InfoTrack.Domain.Repositories;

public interface IScrapeRunRepository
{
    Task AddAsync(ScrapeRun run, CancellationToken cancellationToken = default);

    Task UpdateAsync(ScrapeRun run, CancellationToken cancellationToken = default);

    Task<ScrapeRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> HasActiveRunAsync(CancellationToken cancellationToken = default);
}
