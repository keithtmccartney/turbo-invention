using InfoTrack.Domain.Scraping;

namespace InfoTrack.Domain.Entities;

public enum ScrapeRunStatus
{
    Queued,
    Running,
    Completed,
    Failed
}

public sealed class ScrapeRun
{
    public Guid Id { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public TimeSpan? Duration { get; set; }

    public ScrapeRunStatus Status { get; set; }

    public string? CorrelationId { get; set; }

    public ScrapeProgressStage ProgressStage { get; set; } = ScrapeProgressStage.Queued;

    public string? ProgressMessage { get; set; }

    public int LocationsTotal { get; set; }

    public int LocationsCompleted { get; set; }

    public int FirmsDiscovered { get; set; }

    public int NewFirms { get; set; }

    public int RemovedFirms { get; set; }

    public int ErrorsEncountered { get; set; }

    public Guid? SnapshotId { get; set; }

    public string? ErrorMessage { get; set; }
}
