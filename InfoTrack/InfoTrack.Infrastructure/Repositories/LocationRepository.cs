using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;
using InfoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InfoTrack.Infrastructure.Repositories;

public sealed class LocationRepository(InfoTrackDbContext dbContext) : ILocationRepository
{
    public async Task<IReadOnlyList<Location>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Locations
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Location>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Locations
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Locations.CountAsync(x => x.IsActive, cancellationToken);

    public async Task ReplaceAllAsync(IReadOnlyList<Location> locations, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Locations.ToListAsync(cancellationToken);
        dbContext.Locations.RemoveRange(existing);
        await dbContext.Locations.AddRangeAsync(locations, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DiscoverySyncOutcome> SyncDiscoveredLocationsAsync(
        IReadOnlyList<DiscoveredLocation> discovered,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var existingLocations = await dbContext.Locations.ToListAsync(cancellationToken);
        var existingBySlug = existingLocations.ToDictionary(x => x.Slug, StringComparer.OrdinalIgnoreCase);

        var discoveredBySlug = new Dictionary<string, DiscoveredLocation>(StringComparer.OrdinalIgnoreCase);
        var skipped = 0;

        foreach (var item in discovered)
        {
            var slug = LocationSlug.Normalize(item.Slug);
            if (!discoveredBySlug.TryAdd(slug, item with { Slug = slug }))
            {
                skipped++;
            }
        }

        var added = 0;
        var updated = 0;
        var unchanged = 0;
        var removed = 0;
        var maxOrder = existingLocations.Where(x => x.IsActive).Select(x => x.DisplayOrder).DefaultIfEmpty(-1).Max();

        foreach (var item in discoveredBySlug.Values)
        {
            if (existingBySlug.TryGetValue(item.Slug, out var location))
            {
                var changed = false;

                if (!string.Equals(location.Name, item.Name, StringComparison.Ordinal))
                {
                    location.Name = item.Name;
                    changed = true;
                }

                if (!location.IsActive)
                {
                    location.IsActive = true;
                    changed = true;
                }

                location.LastDiscoveredAt = now;

                if (changed)
                {
                    updated++;
                }
                else
                {
                    unchanged++;
                }

                continue;
            }

            maxOrder++;
            await dbContext.Locations.AddAsync(
                new Location
                {
                    Id = Guid.NewGuid(),
                    Name = item.Name,
                    Slug = item.Slug,
                    DisplayOrder = maxOrder,
                    IsActive = true,
                    FirstDiscoveredAt = now,
                    LastDiscoveredAt = now
                },
                cancellationToken);
            added++;
        }

        var discoveredSlugs = discoveredBySlug.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var location in existingLocations.Where(x => x.IsActive && !discoveredSlugs.Contains(x.Slug)))
        {
            location.IsActive = false;
            removed++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new DiscoverySyncOutcome(added, updated, removed, skipped, unchanged);
    }
}
