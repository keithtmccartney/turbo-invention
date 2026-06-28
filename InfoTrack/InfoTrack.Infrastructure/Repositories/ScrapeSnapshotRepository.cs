using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;
using InfoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InfoTrack.Infrastructure.Repositories;

public sealed class ScrapeSnapshotRepository(InfoTrackDbContext dbContext) : IScrapeSnapshotRepository
{
    public async Task<ScrapeSnapshot?> GetLatestAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ScrapeSnapshots
            .OrderByDescending(x => x.ScrapedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ScrapeSnapshot?> GetPreviousAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ScrapeSnapshots
            .OrderByDescending(x => x.ScrapedAt)
            .Skip(1)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ScrapeSnapshot?> GetByIdWithEntriesAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.ScrapeSnapshots
            .Include(x => x.Entries)
            .ThenInclude(x => x.Solicitor)
            .ThenInclude(x => x!.Location)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Location)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ScrapeSnapshot>> GetHistoryAsync(int take = 20, CancellationToken cancellationToken = default) =>
        await dbContext.ScrapeSnapshots
            .Include(x => x.Entries)
            .ThenInclude(x => x.Location)
            .OrderByDescending(x => x.ScrapedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ScrapeSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await dbContext.ScrapeSnapshots.AddAsync(snapshot, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
