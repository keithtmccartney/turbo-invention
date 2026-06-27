using InfoTrack.Application.Features.Locations.UpdateLocations;
using InfoTrack.Contracts.Locations;
using InfoTrack.Domain.Repositories;

namespace InfoTrack.Application.Features.Locations.GetLocations;

public sealed class GetLocationsHandler(ILocationRepository locationRepository)
{
    public async Task<LocationsResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        var locations = await locationRepository.GetAllAsync(cancellationToken);
        var dtos = locations
            .Select(UpdateLocationsHandler.Map)
            .ToList();

        return new LocationsResponse(dtos);
    }
}
