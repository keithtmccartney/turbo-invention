namespace InfoTrack.Contracts.Solicitors;

public sealed record SolicitorDto(
    Guid Id,
    string FirmName,
    string LocationName,
    string? Phone,
    string? Address,
    string? Website,
    string? EmailEnquiryUrl,
    string? Description,
    decimal? Rating,
    int? ReviewCount);

public sealed record LocationResultsDto(string LocationName, IReadOnlyList<SolicitorDto> Solicitors);

public sealed record ResultsResponse(
    DateTimeOffset? LastScrapedAt,
    IReadOnlyList<LocationResultsDto> Results);
