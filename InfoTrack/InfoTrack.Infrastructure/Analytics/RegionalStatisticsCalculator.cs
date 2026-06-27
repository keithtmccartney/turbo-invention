using InfoTrack.Domain.Analytics;

namespace InfoTrack.Infrastructure.Analytics;

public sealed class RegionalStatisticsCalculator
{
    public IReadOnlyList<RegionalStatistic> Calculate(IReadOnlyList<SolicitorSnapshotRecord> current) =>
        current
            .GroupBy(x => x.LocationName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key)
            .Select(group =>
            {
                var rated = group.Where(x => x.Rating.HasValue).ToList();
                return new RegionalStatistic(
                    group.Key,
                    group.Count(),
                    rated.Count == 0 ? null : Math.Round(rated.Average(x => x.Rating!.Value), 2),
                    group.Sum(x => x.ReviewCount ?? 0));
            })
            .ToList();
}
