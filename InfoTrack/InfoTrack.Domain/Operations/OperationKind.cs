namespace InfoTrack.Domain.Operations;

/// <summary>
/// Discriminator for long-running background operations. Extend for scrape rescans,
/// analytics rebuilds, and other batch workloads without changing the queue contract.
/// </summary>
public enum OperationKind
{
    Discovery = 1,
    Scrape = 2,
    AnalyticsRebuild = 3,
}
