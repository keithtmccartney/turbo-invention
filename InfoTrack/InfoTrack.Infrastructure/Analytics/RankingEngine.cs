using InfoTrack.Domain.Analytics;

namespace InfoTrack.Infrastructure.Analytics;

public sealed class RankingEngine
{
    public IReadOnlyList<FirmRanking> BuildNationalLeaderboard(
        IReadOnlyList<SolicitorSnapshotRecord> current,
        IReadOnlyList<SolicitorSnapshotRecord> previous)
    {
        var previousRankLookup = previous
            .OrderByDescending(x => x.Rating ?? 0)
            .ThenByDescending(x => x.ReviewCount ?? 0)
            .Select((record, index) => (Key: Key(record), Rank: index + 1))
            .ToDictionary(x => x.Key, x => x.Rank);

        return current
            .OrderByDescending(x => x.Rating ?? 0)
            .ThenByDescending(x => x.ReviewCount ?? 0)
            .ThenBy(x => x.FirmName, StringComparer.OrdinalIgnoreCase)
            .Select((record, index) =>
            {
                var rank = index + 1;
                var key = Key(record);
                var previousRank = previousRankLookup.TryGetValue(key, out var prev) ? prev : (int?)null;
                var rankChange = previousRank.HasValue ? previousRank.Value - rank : 0;

                return new FirmRanking(
                    rank,
                    record.FirmName,
                    record.LocationName,
                    record.Rating,
                    record.ReviewCount,
                    rankChange);
            })
            .ToList();
    }

    public IReadOnlyList<FirmRanking> BuildTopFirms(IReadOnlyList<FirmRanking> leaderboard, int count = 10) =>
        leaderboard.Take(count).ToList();

    private static string Key(SolicitorSnapshotRecord record) =>
        SolicitorSnapshotRecords.FirmLocationKey(record);
}
