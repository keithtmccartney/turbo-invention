using InfoTrack.Domain.Analytics;

namespace InfoTrack.Infrastructure.Analytics;

public sealed class GrowthDetector
{
    public IReadOnlyList<GrowthSignal> Detect(
        IReadOnlyList<SolicitorSnapshotRecord> current,
        IReadOnlyList<SolicitorSnapshotRecord> previous,
        IReadOnlyList<SolicitorSnapshotRecord> newSolicitors)
    {
        var signals = new List<GrowthSignal>();

        foreach (var firm in newSolicitors.Take(20))
        {
            signals.Add(new GrowthSignal(
                firm.FirmName,
                firm.LocationName,
                "NewEntrant",
                $"{firm.FirmName} appeared in {firm.LocationName} for the first time."));
        }

        var previousLookup = previous
            .GroupBy(SolicitorSnapshotRecords.FirmLocationKey)
            .ToDictionary(group => group.Key, group => group.MinBy(record => record.Rank)!);

        foreach (var firm in SolicitorSnapshotRecords.DeduplicateByFirmAndLocation(current))
        {
            if (!previousLookup.TryGetValue(SolicitorSnapshotRecords.FirmLocationKey(firm), out var prior))
            {
                continue;
            }

            if (firm.ReviewCount.HasValue && prior.ReviewCount.HasValue)
            {
                var delta = firm.ReviewCount.Value - prior.ReviewCount.Value;
                if (delta >= 50)
                {
                    signals.Add(new GrowthSignal(
                        firm.FirmName,
                        firm.LocationName,
                        "ReviewGrowth",
                        $"Review count increased by {delta} since last scrape."));
                }
            }

            if (firm.Rating.HasValue && prior.Rating.HasValue)
            {
                var ratingDelta = firm.Rating.Value - prior.Rating.Value;
                if (ratingDelta >= 0.5m)
                {
                    signals.Add(new GrowthSignal(
                        firm.FirmName,
                        firm.LocationName,
                        "RatingImprovement",
                        $"Rating improved by {ratingDelta:0.0} stars."));
                }
            }
        }

        return signals
            .OrderByDescending(x => x.SignalType == "NewEntrant")
            .ThenBy(x => x.LocationName)
            .ThenBy(x => x.FirmName)
            .Take(50)
            .ToList();
    }
}
