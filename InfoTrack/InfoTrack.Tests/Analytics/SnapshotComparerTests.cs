using FluentAssertions;
using InfoTrack.Domain.Analytics;
using InfoTrack.Infrastructure.Analytics;

namespace InfoTrack.Tests.Analytics;

public sealed class SnapshotComparerTests
{
    private readonly SnapshotComparer _comparer = new();

    [Fact]
    public void Compare_DetectsNewAndRemovedSolicitors()
    {
        var previous = new ScrapeSnapshotContext(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(-1),
            [
                Record("a", "Alpha LLP", "London", 1),
                Record("b", "Beta LLP", "London", 2)
            ]);

        var current = new ScrapeSnapshotContext(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [
                Record("a", "Alpha LLP", "London", 1),
                Record("c", "Gamma LLP", "London", 3)
            ]);

        var result = _comparer.Compare(new AnalyticsContext(current, previous));

        result.NewSolicitors.Should().ContainSingle(x => x.FirmName == "Gamma LLP");
        result.RemovedSolicitors.Should().ContainSingle(x => x.FirmName == "Beta LLP");
        result.RegionalDeltas.Should().Contain(x => x.LocationName == "London" && x.NewCount == 1 && x.RemovedCount == 1);
    }

    [Fact]
    public void Compare_FirstSnapshot_TreatsAllSolicitorsAsNew()
    {
        var current = new ScrapeSnapshotContext(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [Record("a", "Alpha LLP", "London", 1)]);

        var result = _comparer.Compare(new AnalyticsContext(current, null));

        result.NewSolicitors.Should().HaveCount(1);
        result.RemovedSolicitors.Should().BeEmpty();
    }

    [Fact]
    public void Compare_DeduplicatesRepeatedFirmInSameLocation()
    {
        var current = new ScrapeSnapshotContext(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [
                Record("dup", "Alpha LLP", "Birmingham", 3),
                Record("dup", "Alpha LLP", "Birmingham", 1)
            ]);

        var result = _comparer.Compare(new AnalyticsContext(current, null));

        result.NewSolicitors.Should().ContainSingle(x => x.FirmName == "Alpha LLP");
        result.NewSolicitors.Single().Rank.Should().Be(1);
    }

    [Fact]
    public void Compare_DeduplicatesRepeatedFirmAcrossLocations()
    {
        var current = new ScrapeSnapshotContext(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [
                Record("shared", "Gowen & Stevens LLP", "Carshalton", 2),
                Record("shared", "Gowen & Stevens LLP", "Sutton", 5),
                Record("other", "KT Solicitors Limited", "Carshalton", 1),
            ]);

        var result = _comparer.Compare(new AnalyticsContext(current, null));

        result.NewSolicitors.Should().HaveCount(2);
        result.NewSolicitors.Should().ContainSingle(x => x.FirmName == "Gowen & Stevens LLP");
        result.NewSolicitors.Single(x => x.FirmName == "Gowen & Stevens LLP").LocationName.Should().Be("Carshalton");
        result.RegionalDeltas.Should().ContainSingle(x =>
            x.LocationName == "Carshalton" && x.CurrentCount == 2 && x.NewCount == 2);
    }

    private static SolicitorSnapshotRecord Record(string key, string name, string location, int rank) =>
        new(key, name, location, Guid.NewGuid(), "0123", "Address", null, 4.5m, 100, rank);
}
