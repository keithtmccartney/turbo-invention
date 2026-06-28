using FluentAssertions;
using InfoTrack.Domain.Analytics;
using InfoTrack.Infrastructure.Analytics;

namespace InfoTrack.Tests.Analytics;

public sealed class RankingEngineTests
{
    private readonly RankingEngine _engine = new();

    [Fact]
    public void BuildNationalLeaderboard_OrdersByRatingThenReviews()
    {
        var current =
            new List<SolicitorSnapshotRecord>
            {
                Record("a", "Average Firm", "London", 3.5m, 50, 1),
                Record("b", "Top Firm", "London", 5m, 1000, 2),
                Record("c", "Mid Firm", "Leeds", 4m, 200, 3)
            };

        var leaderboard = _engine.BuildNationalLeaderboard(current, []);

        leaderboard[0].FirmName.Should().Be("Top Firm");
        leaderboard[1].FirmName.Should().Be("Mid Firm");
        leaderboard[2].FirmName.Should().Be("Average Firm");
    }

    [Fact]
    public void BuildNationalLeaderboard_CalculatesRankChange()
    {
        var previous =
            new List<SolicitorSnapshotRecord>
            {
                Record("a", "Alpha", "London", 5m, 100, 1),
                Record("b", "Beta", "London", 4m, 50, 2)
            };

        var current =
            new List<SolicitorSnapshotRecord>
            {
                Record("b", "Beta", "London", 4.8m, 80, 1),
                Record("a", "Alpha", "London", 4.5m, 90, 2)
            };

        var leaderboard = _engine.BuildNationalLeaderboard(current, previous);

        leaderboard.Single(x => x.FirmName == "Beta").RankChange.Should().Be(1);
        leaderboard.Single(x => x.FirmName == "Alpha").RankChange.Should().Be(-1);
    }

    [Fact]
    public void BuildNationalLeaderboard_DeduplicatesFirmAcrossLocations()
    {
        var current =
            new List<SolicitorSnapshotRecord>
            {
                Record("shared", "Gowen & Stevens LLP", "Carshalton", 4.5m, 160, 2),
                Record("shared", "Gowen & Stevens LLP", "Sutton", 4.5m, 160, 5),
                Record("other", "KT Solicitors Limited", "Carshalton", 5m, 0, 1),
            };

        var leaderboard = _engine.BuildNationalLeaderboard(current, []);

        leaderboard.Should().HaveCount(2);
        leaderboard.Should().ContainSingle(x => x.FirmName == "Gowen & Stevens LLP")
            .Which.LocationName.Should().Be("Carshalton");
    }

    private static SolicitorSnapshotRecord Record(
        string key,
        string name,
        string location,
        decimal rating,
        int reviews,
        int rank) =>
        new(key, name, location, Guid.NewGuid(), null, null, null, rating, reviews, rank);
}
