using FluentAssertions;
using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Entities;
using InfoTrack.Infrastructure.Persistence;
using InfoTrack.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InfoTrack.Tests.Discovery;

public sealed class LocationDiscoverySyncTests
{
    [Fact]
    public async Task SyncDiscoveredLocations_AddsUpdatesAndArchivesLocations()
    {
        var options = new DbContextOptionsBuilder<InfoTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InfoTrackDbContext(options);
        var repository = new LocationRepository(dbContext);

        await dbContext.Locations.AddAsync(new Location
        {
            Id = Guid.NewGuid(),
            Name = "London",
            Slug = "london",
            DisplayOrder = 0,
            IsActive = true
        });
        await dbContext.Locations.AddAsync(new Location
        {
            Id = Guid.NewGuid(),
            Name = "Old Town",
            Slug = "old-town",
            DisplayOrder = 1,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var discovered = new List<DiscoveredLocation>
        {
            new("london", "London"),
            new("manchester", "Manchester")
        };

        var outcome = await repository.SyncDiscoveredLocationsAsync(discovered);

        outcome.Added.Should().Be(1);
        outcome.Updated.Should().Be(0);
        outcome.Existing.Should().Be(1);
        outcome.Removed.Should().Be(1);

        var active = await dbContext.Locations.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
        active.Select(x => x.Slug).Should().Equal("london", "manchester");

        var archived = await dbContext.Locations.SingleAsync(x => x.Slug == "old-town");
        archived.IsActive.Should().BeFalse();
    }
}
