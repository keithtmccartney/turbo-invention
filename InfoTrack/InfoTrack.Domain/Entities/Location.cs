namespace InfoTrack.Domain.Entities;

public sealed class Location
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Slug { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? FirstDiscoveredAt { get; set; }

    public DateTimeOffset? LastDiscoveredAt { get; set; }

    public ICollection<Solicitor> Solicitors { get; set; } = [];
}
