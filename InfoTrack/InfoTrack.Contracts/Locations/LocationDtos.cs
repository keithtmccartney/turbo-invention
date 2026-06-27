namespace InfoTrack.Contracts.Locations;

public sealed record LocationDto(
    Guid Id,
    string Name,
    string Slug,
    int DisplayOrder,
    bool IsActive,
    DateTimeOffset? FirstDiscoveredAt,
    DateTimeOffset? LastDiscoveredAt);

public sealed record UpdateLocationsRequest(IReadOnlyList<string> Locations);

public sealed record LocationsResponse(IReadOnlyList<LocationDto> Locations);
