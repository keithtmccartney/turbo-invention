using FluentAssertions;
using InfoTrack.Application.Features.Scraping.GetResults;
using InfoTrack.Domain.Entities;
using InfoTrack.Infrastructure.Persistence;
using InfoTrack.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InfoTrack.Tests.Scraping;

public sealed class GetResultsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsOneRowPerFirm_WhenFirmAppearsInMultipleSnapshotLocations()
    {
        var options = new DbContextOptionsBuilder<InfoTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InfoTrackDbContext(options);

        var carshalton = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Carshalton",
            Slug = "carshalton",
            DisplayOrder = 0,
            IsActive = true,
        };
        var sutton = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Sutton",
            Slug = "sutton",
            DisplayOrder = 1,
            IsActive = true,
        };

        var sharedSolicitor = new Solicitor
        {
            Id = Guid.NewGuid(),
            ExternalKey = "shared-firm",
            FirmName = "Shared Firm LLP",
            LocationId = sutton.Id,
            FirstSeenAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
        };

        dbContext.Locations.AddRange(carshalton, sutton);
        dbContext.Solicitors.Add(sharedSolicitor);

        var snapshot = new ScrapeSnapshot
        {
            Id = Guid.NewGuid(),
            ScrapedAt = DateTimeOffset.UtcNow,
            LocationsSearched = 2,
            TotalFirms = 1,
            Entries =
            [
                new SnapshotEntry
                {
                    Id = Guid.NewGuid(),
                    SolicitorId = sharedSolicitor.Id,
                    LocationId = carshalton.Id,
                    Rank = 1,
                    Solicitor = sharedSolicitor,
                    Location = carshalton,
                },
                new SnapshotEntry
                {
                    Id = Guid.NewGuid(),
                    SolicitorId = sharedSolicitor.Id,
                    LocationId = sutton.Id,
                    Rank = 2,
                    Solicitor = sharedSolicitor,
                    Location = sutton,
                },
            ],
        };

        dbContext.ScrapeSnapshots.Add(snapshot);
        await dbContext.SaveChangesAsync();

        var handler = new GetResultsHandler(
            new SolicitorRepository(dbContext),
            new ScrapeSnapshotRepository(dbContext));

        var response = await handler.HandleAsync();

        response.Results.SelectMany(x => x.Solicitors).Should().HaveCount(1);
        response.Results.Single().LocationName.Should().Be("Sutton");
    }
}
