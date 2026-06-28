using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;
using InfoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InfoTrack.Infrastructure.Repositories;

public sealed class DiscoveryRunRepository(InfoTrackDbContext dbContext) : IDiscoveryRunRepository
{
    public async Task AddAsync(DiscoveryRun run, CancellationToken cancellationToken = default)
    {
        await dbContext.DiscoveryRuns.AddAsync(run, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DiscoveryRun run, CancellationToken cancellationToken = default)
    {
        dbContext.DiscoveryRuns.Update(run);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DiscoveryRun?> GetLatestCompletedAsync(CancellationToken cancellationToken = default) =>
        await dbContext.DiscoveryRuns
            .Where(x => x.Status == DiscoveryRunStatus.Completed)
            .OrderByDescending(x => x.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<DiscoveryRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.DiscoveryRuns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> HasActiveRunAsync(CancellationToken cancellationToken = default) =>
        await dbContext.DiscoveryRuns.AnyAsync(
            x => x.Status == DiscoveryRunStatus.Queued || x.Status == DiscoveryRunStatus.Running,
            cancellationToken);

    public async Task<IReadOnlyList<DiscoveryRun>> GetHistoryAsync(int take, CancellationToken cancellationToken = default) =>
        await dbContext.DiscoveryRuns
            .OrderByDescending(x => x.StartedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
}
