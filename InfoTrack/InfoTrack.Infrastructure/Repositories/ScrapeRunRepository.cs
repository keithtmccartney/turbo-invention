using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;
using InfoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InfoTrack.Infrastructure.Repositories;

public sealed class ScrapeRunRepository(InfoTrackDbContext dbContext) : IScrapeRunRepository
{
    public async Task AddAsync(ScrapeRun run, CancellationToken cancellationToken = default)
    {
        await dbContext.ScrapeRuns.AddAsync(run, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ScrapeRun run, CancellationToken cancellationToken = default)
    {
        dbContext.ScrapeRuns.Update(run);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ScrapeRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.ScrapeRuns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> HasActiveRunAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ScrapeRuns.AnyAsync(
            x => x.Status == ScrapeRunStatus.Queued || x.Status == ScrapeRunStatus.Running,
            cancellationToken);
}
