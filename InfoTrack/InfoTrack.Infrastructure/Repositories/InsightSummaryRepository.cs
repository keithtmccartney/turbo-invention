using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;
using InfoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InfoTrack.Infrastructure.Repositories;

public sealed class InsightSummaryRepository(InfoTrackDbContext dbContext) : IInsightSummaryRepository
{
    public async Task<InsightSummary?> GetLatestAsync(CancellationToken cancellationToken = default) =>
        await dbContext.InsightSummaries
            .OrderByDescending(x => x.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(InsightSummary summary, CancellationToken cancellationToken = default)
    {
        await dbContext.InsightSummaries.AddAsync(summary, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
