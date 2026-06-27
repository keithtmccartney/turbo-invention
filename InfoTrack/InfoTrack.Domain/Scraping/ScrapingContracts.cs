namespace InfoTrack.Domain.Scraping;

public sealed record ParsedSolicitorListing(
    string FirmName,
    string? Phone,
    string? Address,
    string? Website,
    string? EmailEnquiryUrl,
    string? Description,
    decimal? Rating,
    int? ReviewCount,
    int Position);

public interface ISolicitorsHtmlParser
{
    IReadOnlyList<ParsedSolicitorListing> Parse(string html, string locationName);
}

public interface ISolicitorsScrapeClient
{
    Task<string> FetchLocationPageAsync(string locationSlug, CancellationToken cancellationToken = default);
}

public interface IScrapeOrchestrator
{
    Task<Guid> RunAsync(CancellationToken cancellationToken = default);
}
