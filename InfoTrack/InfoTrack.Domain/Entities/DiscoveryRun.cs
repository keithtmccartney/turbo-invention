namespace InfoTrack.Domain.Entities;

public enum DiscoveryRunStatus
{
    Running,
    Completed,
    Failed
}

public sealed class DiscoveryRun
{
    public Guid Id { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public TimeSpan? Duration { get; set; }

    public required string Source { get; set; }

    public int LocationsFound { get; set; }

    public int NewLocations { get; set; }

    public int ExistingLocations { get; set; }

    public int UpdatedLocations { get; set; }

    public int RemovedLocations { get; set; }

    public int SkippedLocations { get; set; }

    public DiscoveryRunStatus Status { get; set; }

    public string? ErrorMessage { get; set; }
}
