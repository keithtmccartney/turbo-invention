namespace InfoTrack.Domain.Analytics;

public static class SolicitorSnapshotRecords
{
    public static string FirmLocationKey(SolicitorSnapshotRecord record) =>
        FirmLocationKey(record.ExternalKey, record.LocationName);

    public static string FirmLocationKey(string externalKey, string locationName) =>
        $"{externalKey}:{locationName}".ToLowerInvariant();

    /// <summary>
    /// Keeps one record per firm/location, preferring the best (lowest) search rank.
    /// </summary>
    public static IReadOnlyList<SolicitorSnapshotRecord> DeduplicateByFirmAndLocation(
        IEnumerable<SolicitorSnapshotRecord> records) =>
        records
            .GroupBy(FirmLocationKey)
            .Select(group => group.MinBy(record => record.Rank)!)
            .ToList();

    /// <summary>
    /// Keeps one record per firm, preferring the best (lowest) search rank across locations.
    /// </summary>
    public static IReadOnlyList<SolicitorSnapshotRecord> DeduplicateByFirm(
        IEnumerable<SolicitorSnapshotRecord> records) =>
        records
            .GroupBy(record => record.ExternalKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.MinBy(record => record.Rank)!)
            .ToList();

    /// <summary>
    /// Normalizes snapshot records for analytics: one row per firm with the best rank in its primary location.
    /// </summary>
    public static IReadOnlyList<SolicitorSnapshotRecord> NormalizeForAnalytics(
        IEnumerable<SolicitorSnapshotRecord> records) =>
        DeduplicateByFirm(DeduplicateByFirmAndLocation(records));
}
