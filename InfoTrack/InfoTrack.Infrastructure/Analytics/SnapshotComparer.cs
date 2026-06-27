using InfoTrack.Domain.Analytics;

namespace InfoTrack.Infrastructure.Analytics;

public sealed class SnapshotComparer
{
    public SnapshotComparisonResult Compare(AnalyticsContext context)
    {
        var current = SolicitorSnapshotRecords.DeduplicateByFirmAndLocation(context.Current.Solicitors);
        var previous = SolicitorSnapshotRecords.DeduplicateByFirmAndLocation(context.Previous?.Solicitors ?? []);

        var currentKeys = current.ToDictionary(x => SolicitorSnapshotRecords.FirmLocationKey(x));
        var previousKeys = previous.ToDictionary(x => SolicitorSnapshotRecords.FirmLocationKey(x));

        var newSolicitors = current
            .Where(x => !previousKeys.ContainsKey(SolicitorSnapshotRecords.FirmLocationKey(x)))
            .OrderBy(x => x.LocationName)
            .ThenBy(x => x.FirmName)
            .ToList();

        var removedSolicitors = previous
            .Where(x => !currentKeys.ContainsKey(SolicitorSnapshotRecords.FirmLocationKey(x)))
            .OrderBy(x => x.LocationName)
            .ThenBy(x => x.FirmName)
            .ToList();

        var regionalDeltas = BuildRegionalDeltas(current, previous, newSolicitors, removedSolicitors);
        var leaderboard = new RankingEngine().BuildNationalLeaderboard(current, previous);
        var growthSignals = new GrowthDetector().Detect(current, previous, newSolicitors);

        return new SnapshotComparisonResult(
            context.Current.SnapshotId,
            context.Previous?.SnapshotId,
            newSolicitors,
            removedSolicitors,
            regionalDeltas,
            leaderboard,
            growthSignals);
    }

    private static IReadOnlyList<RegionalDelta> BuildRegionalDeltas(
        IReadOnlyList<SolicitorSnapshotRecord> current,
        IReadOnlyList<SolicitorSnapshotRecord> previous,
        IReadOnlyList<SolicitorSnapshotRecord> newSolicitors,
        IReadOnlyList<SolicitorSnapshotRecord> removedSolicitors)
    {
        var locations = current.Select(x => x.LocationName)
            .Concat(previous.Select(x => x.LocationName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x);

        return locations.Select(location =>
        {
            var currentLocation = current.Where(x => x.LocationName.Equals(location, StringComparison.OrdinalIgnoreCase)).ToList();
            var previousLocation = previous.Where(x => x.LocationName.Equals(location, StringComparison.OrdinalIgnoreCase)).ToList();
            var newCount = newSolicitors.Count(x => x.LocationName.Equals(location, StringComparison.OrdinalIgnoreCase));
            var removedCount = removedSolicitors.Count(x => x.LocationName.Equals(location, StringComparison.OrdinalIgnoreCase));

            return new RegionalDelta(
                location,
                previousLocation.Count,
                currentLocation.Count,
                currentLocation.Count - previousLocation.Count,
                newCount,
                removedCount,
                AverageRating(currentLocation));
        }).ToList();
    }

    private static decimal? AverageRating(IReadOnlyList<SolicitorSnapshotRecord> records)
    {
        var rated = records.Where(x => x.Rating.HasValue).ToList();
        return rated.Count == 0 ? null : Math.Round(rated.Average(x => x.Rating!.Value), 2);
    }
}
