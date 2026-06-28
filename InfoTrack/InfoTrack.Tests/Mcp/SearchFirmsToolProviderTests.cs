using System.Text.Json;
using FluentAssertions;
using InfoTrack.Application.Features.Scraping.GetResults;
using InfoTrack.Application.Mcp.ToolProviders;
using InfoTrack.Contracts.Solicitors;
using InfoTrack.Infrastructure.Persistence;
using InfoTrack.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InfoTrack.Tests.Mcp;

public sealed class SearchFirmsToolProviderTests
{
    [Fact]
    public async Task ExecuteAsync_WithLocationOnly_ReturnsAllFirmsInLocation()
    {
        var options = new DbContextOptionsBuilder<InfoTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InfoTrackDbContext(options);
        var location = new Domain.Entities.Location
        {
            Id = Guid.NewGuid(),
            Name = "Aberdeen",
            Slug = "aberdeen",
            DisplayOrder = 0,
            IsActive = true,
        };
        dbContext.Locations.Add(location);
        dbContext.Solicitors.Add(new Domain.Entities.Solicitor
        {
            Id = Guid.NewGuid(),
            ExternalKey = "firm-1",
            FirmName = "Blackadders Solicitors",
            LocationId = location.Id,
            FirstSeenAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
        });
        dbContext.Solicitors.Add(new Domain.Entities.Solicitor
        {
            Id = Guid.NewGuid(),
            ExternalKey = "firm-2",
            FirmName = "London Only Firm",
            LocationId = Guid.NewGuid(),
            FirstSeenAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        var handler = new GetResultsHandler(new SolicitorRepository(dbContext), new ScrapeSnapshotRepository(dbContext));
        var provider = new SearchFirmsToolProvider(handler);

        using var arguments = JsonDocument.Parse("""{"location":"Aberdeen"}""");
        var result = await provider.ExecuteAsync(arguments.RootElement);

        result.IsError.Should().BeFalse();
        result.Content.First().Text.Should().Contain("\"matchCount\":1");
        result.Content.First().Text.Should().Contain("Blackadders Solicitors");
    }

    [Fact]
    public async Task ExecuteAsync_WithAddressQuery_ReturnsMatchingFirmsInLocation()
    {
        var options = new DbContextOptionsBuilder<InfoTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InfoTrackDbContext(options);
        var location = new Domain.Entities.Location
        {
            Id = Guid.NewGuid(),
            Name = "Aberdeen",
            Slug = "aberdeen",
            DisplayOrder = 0,
            IsActive = true,
        };
        dbContext.Locations.Add(location);
        dbContext.Solicitors.AddRange(
            new Domain.Entities.Solicitor
            {
                Id = Guid.NewGuid(),
                ExternalKey = "bon-accord-firm",
                FirmName = "Blackadders Solicitors",
                Address = "6 Bon Accord Square, Aberdeen, AB11 6XU",
                LocationId = location.Id,
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow,
            },
            new Domain.Entities.Solicitor
            {
                Id = Guid.NewGuid(),
                ExternalKey = "union-street-firm",
                FirmName = "Grant Smith Law Practice",
                Address = "252 Union Street, Aberdeen, AB10 1TN",
                LocationId = location.Id,
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow,
            });
        await dbContext.SaveChangesAsync();

        var handler = new GetResultsHandler(new SolicitorRepository(dbContext), new ScrapeSnapshotRepository(dbContext));
        var provider = new SearchFirmsToolProvider(handler);

        using var arguments = JsonDocument.Parse("""{"location":"Aberdeen","query":"Bon Accord"}""");
        var result = await provider.ExecuteAsync(arguments.RootElement);

        result.IsError.Should().BeFalse();
        result.Content.First().Text.Should().Contain("\"matchCount\":1");
        result.Content.First().Text.Should().Contain("Blackadders Solicitors");
        result.Content.First().Text.Should().Contain("\"id\":");
        result.Content.First().Text.Should().NotContain("Grant Smith Law Practice");
    }

    [Fact]
    public async Task ExecuteAsync_WithPhoneQuery_ReturnsMatchingFirm()
    {
        var options = new DbContextOptionsBuilder<InfoTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InfoTrackDbContext(options);
        var location = new Domain.Entities.Location
        {
            Id = Guid.NewGuid(),
            Name = "Aberdeen",
            Slug = "aberdeen",
            DisplayOrder = 0,
            IsActive = true,
        };
        dbContext.Locations.Add(location);
        dbContext.Solicitors.AddRange(
            new Domain.Entities.Solicitor
            {
                Id = Guid.NewGuid(),
                ExternalKey = "phone-match",
                FirmName = "Matched Firm",
                Phone = "01224 588913",
                LocationId = location.Id,
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow,
            },
            new Domain.Entities.Solicitor
            {
                Id = Guid.NewGuid(),
                ExternalKey = "other-firm",
                FirmName = "Other Firm",
                Phone = "01224 621620",
                LocationId = location.Id,
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow,
            });
        await dbContext.SaveChangesAsync();

        var handler = new GetResultsHandler(new SolicitorRepository(dbContext), new ScrapeSnapshotRepository(dbContext));
        var provider = new SearchFirmsToolProvider(handler);

        using var arguments = JsonDocument.Parse("""{"location":"Aberdeen","query":"588913"}""");
        var result = await provider.ExecuteAsync(arguments.RootElement);

        result.IsError.Should().BeFalse();
        result.Content.First().Text.Should().Contain("Matched Firm");
        result.Content.First().Text.Should().NotContain("Other Firm");
    }
}
