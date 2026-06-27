namespace InfoTrack.Domain.Entities;

public sealed class ScrapeSnapshot
{
    public Guid Id { get; set; }

    public DateTimeOffset ScrapedAt { get; set; }

    public int TotalFirms { get; set; }

    public int LocationsSearched { get; set; }

    public ICollection<SnapshotEntry> Entries { get; set; } = [];
}

public sealed class SnapshotEntry
{
    public Guid Id { get; set; }

    public Guid ScrapeSnapshotId { get; set; }

    public ScrapeSnapshot? ScrapeSnapshot { get; set; }

    public Guid SolicitorId { get; set; }

    public Solicitor? Solicitor { get; set; }

    public Guid LocationId { get; set; }

    public Location? Location { get; set; }

    public int Rank { get; set; }
}
