using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;
using InfoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InfoTrack.Infrastructure.Repositories;

public sealed class SolicitorRepository(InfoTrackDbContext dbContext) : ISolicitorRepository
{
    public async Task<IReadOnlyList<Solicitor>> GetAllWithLocationsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Solicitors
            .Include(x => x.Location)
            .OrderBy(x => x.Location!.DisplayOrder)
            .ThenBy(x => x.FirmName)
            .ToListAsync(cancellationToken);

    public async Task UpsertRangeAsync(IReadOnlyList<Solicitor> solicitors, CancellationToken cancellationToken = default)
    {
        var deduped = solicitors
            .GroupBy(x => x.ExternalKey)
            .Select(group => group.Last())
            .ToList();

        var keys = deduped.Select(x => x.ExternalKey).ToHashSet();
        var existing = await dbContext.Solicitors
            .Where(x => keys.Contains(x.ExternalKey))
            .ToDictionaryAsync(x => x.ExternalKey, cancellationToken);

        foreach (var solicitor in deduped)
        {
            if (existing.TryGetValue(solicitor.ExternalKey, out var current))
            {
                current.FirmName = solicitor.FirmName;
                current.Phone = solicitor.Phone;
                current.Address = solicitor.Address;
                current.Website = solicitor.Website;
                current.EmailEnquiryUrl = solicitor.EmailEnquiryUrl;
                current.Description = solicitor.Description;
                current.Rating = solicitor.Rating;
                current.ReviewCount = solicitor.ReviewCount;
                current.LocationId = solicitor.LocationId;
                current.LastSeenAt = solicitor.LastSeenAt;
            }
            else
            {
                await dbContext.Solicitors.AddAsync(solicitor, cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
