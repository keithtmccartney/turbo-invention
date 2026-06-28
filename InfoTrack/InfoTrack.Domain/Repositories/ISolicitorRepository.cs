using InfoTrack.Domain.Entities;

namespace InfoTrack.Domain.Repositories;

public interface ISolicitorRepository
{
    Task<IReadOnlyList<Solicitor>> GetAllWithLocationsAsync(CancellationToken cancellationToken = default);

    Task UpsertRangeAsync(IReadOnlyList<Solicitor> solicitors, CancellationToken cancellationToken = default);
}
