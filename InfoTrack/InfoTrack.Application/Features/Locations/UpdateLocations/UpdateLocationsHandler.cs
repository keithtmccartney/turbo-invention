using InfoTrack.Contracts.Locations;
using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;

namespace InfoTrack.Application.Features.Locations.UpdateLocations;

public sealed class UpdateLocationsHandler(ILocationRepository locationRepository)
{
    public async Task<LocationsResponse> HandleAsync(UpdateLocationsRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Locations is null)
        {
            throw new ArgumentException("Locations are required.", nameof(request));
        }

        var distinct = request.Locations
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var locations = await locationRepository.SetActiveLocationsAsync(distinct, cancellationToken);

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
