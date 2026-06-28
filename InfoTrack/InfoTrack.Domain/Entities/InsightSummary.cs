namespace InfoTrack.Domain.Entities;

public sealed class InsightSummary
{
    public Guid Id { get; set; }

    public Guid CurrentSnapshotId { get; set; }

    public Guid? PreviousSnapshotId { get; set; }

    public DateTimeOffset GeneratedAt { get; set; }

    public int TotalFirms { get; set; }

    public int LocationsSearched { get; set; }

    public int NewFirms { get; set; }

    public int RemovedFirms { get; set; }

    public decimal? AverageRating { get; set; }

    /// <summary>
    /// Serialised analytics payload (regional stats, leaderboard, deltas).
    /// Stored as JSON to keep the relational model focused while allowing rich analytics evolution.
    /// </summary>
    public required string PayloadJson { get; set; }
}
