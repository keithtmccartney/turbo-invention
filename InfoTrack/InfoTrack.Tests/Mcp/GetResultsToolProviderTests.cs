using System.Text.Json;
using FluentAssertions;
using InfoTrack.Application.Features.Scraping.GetResults;
using InfoTrack.Application.Mcp.ToolProviders;
using InfoTrack.Domain.Entities;
using InfoTrack.Infrastructure.Persistence;
using InfoTrack.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InfoTrack.Tests.Mcp;

public sealed class GetResultsToolProviderTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsFullResultsSchema()
    {
        var options = new DbContextOptionsBuilder<InfoTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InfoTrackDbContext(options);
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Aberdeen",
            Slug = "aberdeen",
            DisplayOrder = 0,
            IsActive = true,
        };
        dbContext.Locations.Add(location);
        dbContext.Solicitors.Add(new Solicitor
        {
            Id = Guid.NewGuid(),
            ExternalKey = "firm-1",
            FirmName = "Blackadders Solicitors",
            Address = "6 Bon Accord Square, Aberdeen, AB11 6XU",
            Website = "https://example.com",
            EmailEnquiryUrl = "mailto:test@example.com",
            Description = "Conveyancing specialists",
            LocationId = location.Id,
            FirstSeenAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
        });
        dbContext.ScrapeSnapshots.Add(new ScrapeSnapshot
        {
            Id = Guid.NewGuid(),
            ScrapedAt = DateTimeOffset.UtcNow,
            LocationsSearched = 1,
            TotalFirms = 1,
        });
        await dbContext.SaveChangesAsync();

        var handler = new GetResultsHandler(new SolicitorRepository(dbContext), new ScrapeSnapshotRepository(dbContext));
        var provider = new GetResultsToolProvider(handler);

        var result = await provider.ExecuteAsync(null);

        result.IsError.Should().BeFalse();
        result.Content.First().Text.Should().Contain("lastScrapedAt");
        result.Content.First().Text.Should().Contain("Blackadders Solicitors");
        result.Content.First().Text.Should().Contain("emailEnquiryUrl");
        result.Content.First().Text.Should().Contain("description");
    }

    [Fact]
    public async Task ExecuteAsync_WithLocationFilter_ReturnsSingleLocationGroup()
    {
        var options = new DbContextOptionsBuilder<InfoTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InfoTrackDbContext(options);
        var aberdeen = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Aberdeen",
            Slug = "aberdeen",
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
        dbContext.Locations.AddRange(aberdeen, sutton);
        dbContext.Solicitors.AddRange(
            new Solicitor
            {
                Id = Guid.NewGuid(),
                ExternalKey = "aberdeen-firm",
                FirmName = "Aberdeen Firm",
                LocationId = aberdeen.Id,
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow,
            },
            new Solicitor
            {
                Id = Guid.NewGuid(),
                ExternalKey = "sutton-firm",
                FirmName = "Sutton Firm",
                LocationId = sutton.Id,
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow,
            });
        await dbContext.SaveChangesAsync();

        var handler = new GetResultsHandler(new SolicitorRepository(dbContext), new ScrapeSnapshotRepository(dbContext));
        var provider = new GetResultsToolProvider(handler);

        using var arguments = JsonDocument.Parse("""{"location":"Aberdeen"}""");
        var result = await provider.ExecuteAsync(arguments.RootElement);

        result.IsError.Should().BeFalse();
        result.Content.First().Text.Should().Contain("Aberdeen Firm");
        result.Content.First().Text.Should().NotContain("Sutton Firm");
    }
}
