using FluentAssertions;
using InfoTrack.Infrastructure.Persistence;
using InfoTrack.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InfoTrack.Tests.Discovery;

public sealed class LocationActiveSelectionTests
{
    [Fact]
    public async Task SetActiveLocations_PreservesCatalogAndDeactivatesUnselected()
    {
        var options = new DbContextOptionsBuilder<InfoTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InfoTrackDbContext(options);
        var repository = new LocationRepository(dbContext);

        await repository.SetActiveLocationsAsync(["London", "Manchester", "Cardiff"]);

        var locations = await repository.SetActiveLocationsAsync(["London", "Cardiff"]);

        locations.Should().HaveCount(3);
        locations.Where(x => x.IsActive).Select(x => x.Name).Should().BeEquivalentTo(["London", "Cardiff"]);

        var deactivated = await dbContext.Locations.SingleAsync(x => x.Name == "Manchester");
        deactivated.IsActive.Should().BeFalse();

        var london = await dbContext.Locations.SingleAsync(x => x.Name == "London");
        var cardiff = await dbContext.Locations.SingleAsync(x => x.Name == "Cardiff");
        london.Id.Should().NotBeEmpty();
        cardiff.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SetActiveLocations_WithEmptySelection_DeactivatesAllLocations()
    {
        var options = new DbContextOptionsBuilder<InfoTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InfoTrackDbContext(options);
        var repository = new LocationRepository(dbContext);

        await repository.SetActiveLocationsAsync(["London", "Manchester"]);

        var locations = await repository.SetActiveLocationsAsync([]);

        locations.Should().HaveCount(2);
        locations.Should().OnlyContain(x => !x.IsActive);
    }

    [Fact]
    public async Task SetActiveLocations_PreservesAlphabeticalCatalogOrder()
    {
        var options = new DbContextOptionsBuilder<InfoTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InfoTrackDbContext(options);
        var repository = new LocationRepository(dbContext);

        await repository.SetActiveLocationsAsync(["London", "Manchester", "Cardiff"]);

        var locations = await repository.SetActiveLocationsAsync(["Airdrie", "Aldgate"]);

        locations.Select(x => x.Name).Should().Equal(["Airdrie", "Aldgate", "Cardiff", "London", "Manchester"]);
        locations.Where(x => x.IsActive).Select(x => x.Name).Should().BeEquivalentTo(["Airdrie", "Aldgate"]);
    }
}
