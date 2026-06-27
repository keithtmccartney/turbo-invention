using InfoTrack.Domain.Entities;

namespace InfoTrack.Domain.Repositories;

public interface IInsightSummaryRepository
{
    Task<InsightSummary?> GetLatestAsync(CancellationToken cancellationToken = default);

    Task AddAsync(InsightSummary summary, CancellationToken cancellationToken = default);
}
