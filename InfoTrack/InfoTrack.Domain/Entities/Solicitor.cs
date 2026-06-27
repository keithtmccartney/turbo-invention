namespace InfoTrack.Domain.Entities;

public sealed class Solicitor
{
    public Guid Id { get; set; }

    /// <summary>
    /// Stable identity derived from firm name, address, and phone for deduplication across scrapes.
    /// </summary>
    public required string ExternalKey { get; set; }

    public required string FirmName { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? Website { get; set; }

    public string? EmailEnquiryUrl { get; set; }

    public string? Description { get; set; }

    public decimal? Rating { get; set; }

    public int? ReviewCount { get; set; }

    public Guid LocationId { get; set; }

    public Location? Location { get; set; }

    public DateTimeOffset FirstSeenAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }
}
