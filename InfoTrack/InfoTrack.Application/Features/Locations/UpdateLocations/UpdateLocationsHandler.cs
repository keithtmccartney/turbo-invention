using InfoTrack.Contracts.Locations;
using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;

namespace InfoTrack.Application.Features.Locations.UpdateLocations;

public sealed class UpdateLocationsHandler(ILocationRepository locationRepository)
{
    public async Task<LocationsResponse> HandleAsync(UpdateLocationsRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Locations is null || request.Locations.Count == 0)
        {
            throw new ArgumentException("At least one location is required.", nameof(request));
        }

        var distinct = request.Locations
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinct.Count == 0)
        {
            throw new ArgumentException("At least one valid location name is required.", nameof(request));
        }

        var now = DateTimeOffset.UtcNow;
        var locations = distinct
            .Select((name, index) => new Location
            {
                Id = Guid.NewGuid(),
                Name = name,
                Slug = LocationSlug.FromName(name),
                DisplayOrder = index,
                IsActive = true,
                FirstDiscoveredAt = now,
                LastDiscoveredAt = now
            })
            .ToList();

        await locationRepository.ReplaceAllAsync(locations, cancellationToken);

        return new LocationsResponse(
            locations.Select(Map).ToList());
    }

    internal static LocationDto Map(Location location) =>
        new(
            location.Id,
            location.Name,
            location.Slug,
            location.DisplayOrder,
            location.IsActive,
            location.FirstDiscoveredAt,
            location.LastDiscoveredAt);
}
