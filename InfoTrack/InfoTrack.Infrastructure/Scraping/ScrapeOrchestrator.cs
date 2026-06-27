using InfoTrack.Domain.Analytics;
using InfoTrack.Domain.Common;
using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;
using InfoTrack.Domain.Scraping;
using InfoTrack.Infrastructure.Options;
using InfoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InfoTrack.Infrastructure.Scraping;

public sealed class ScrapeOrchestrator(
    ILocationRepository locationRepository,
    ISolicitorsScrapeClient scrapeClient,
    ISolicitorsHtmlParser htmlParser,
    ISolicitorRepository solicitorRepository,
    IScrapeSnapshotRepository snapshotRepository,
    IInsightSummaryRepository insightSummaryRepository,
    IAnalyticsEngine analyticsEngine,
    InfoTrackDbContext dbContext,
    IOptions<ScrapingOptions> options,
    ILogger<ScrapeOrchestrator> logger) : IScrapeOrchestrator
{
    private readonly ScrapingOptions _options = options.Value;

    public async Task<Guid> RunAsync(CancellationToken cancellationToken = default)
    {
        var locations = await locationRepository.GetActiveAsync(cancellationToken);
        if (locations.Count == 0)
        {
            throw new InvalidOperationException("No active locations configured.");
        }

        var previousSnapshot = await snapshotRepository.GetLatestAsync(cancellationToken);
        var scrapedAt = DateTimeOffset.UtcNow;
        var snapshot = new ScrapeSnapshot
        {
            Id = Guid.NewGuid(),
            ScrapedAt = scrapedAt,
            LocationsSearched = locations.Count
        };

        var solicitorsByKey = new Dictionary<string, Solicitor>();
        var snapshotEntriesByFirmLocation = new Dictionary<(string ExternalKey, Guid LocationId), SnapshotEntry>();

        foreach (var location in locations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var html = await scrapeClient.FetchLocationPageAsync(location.Slug, cancellationToken);
            var parsed = htmlParser.Parse(html, location.Name);

            foreach (var listing in parsed)
            {
                var externalKey = SolicitorIdentity.CreateKey(listing.FirmName, listing.Address, listing.Phone);
                if (!solicitorsByKey.TryGetValue(externalKey, out var solicitor))
                {
                    solicitor = new Solicitor
                    {
                        Id = Guid.NewGuid(),
                        ExternalKey = externalKey,
                        FirmName = listing.FirmName,
                        Phone = listing.Phone,
                        Address = listing.Address,
                        Website = listing.Website,
                        EmailEnquiryUrl = listing.EmailEnquiryUrl,
                        Description = listing.Description,
                        Rating = listing.Rating,
                        ReviewCount = listing.ReviewCount,
                        LocationId = location.Id,
                        FirstSeenAt = scrapedAt,
                        LastSeenAt = scrapedAt
                    };
                    solicitorsByKey[externalKey] = solicitor;
                }
                else
                {
                    solicitor.FirmName = listing.FirmName;
                    solicitor.Phone = listing.Phone;
                    solicitor.Address = listing.Address;
                    solicitor.Website = listing.Website;
                    solicitor.EmailEnquiryUrl = listing.EmailEnquiryUrl;
                    solicitor.Description = listing.Description;
                    solicitor.Rating = listing.Rating;
                    solicitor.ReviewCount = listing.ReviewCount;
                    solicitor.LocationId = location.Id;
                    solicitor.LastSeenAt = scrapedAt;
                }

                var firmLocationKey = (externalKey, location.Id);
                if (snapshotEntriesByFirmLocation.TryGetValue(firmLocationKey, out var snapshotEntry))
                {
                    snapshotEntry.Rank = Math.Min(snapshotEntry.Rank, listing.Position);
                }
                else
                {
                    snapshotEntriesByFirmLocation[firmLocationKey] = new SnapshotEntry
                    {
                        Id = Guid.NewGuid(),
                        ScrapeSnapshotId = snapshot.Id,
                        SolicitorId = solicitor.Id,
                        LocationId = location.Id,
                        Rank = listing.Position
                    };
                }
            }

            if (_options.RequestDelayMilliseconds > 0)
            {
                await Task.Delay(_options.RequestDelayMilliseconds, cancellationToken);
            }
        }

        var solicitorsToUpsert = solicitorsByKey.Values.ToList();
        await solicitorRepository.UpsertRangeAsync(solicitorsToUpsert, cancellationToken);

        var externalKeys = solicitorsToUpsert.Select(x => x.ExternalKey).ToHashSet();
        var persistedRows = await dbContext.Solicitors
            .Where(x => externalKeys.Contains(x.ExternalKey))
            .ToListAsync(cancellationToken);
        var persisted = persistedRows
            .GroupBy(x => x.ExternalKey)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(x => x.LastSeenAt).First());

        var resolvedSnapshotEntries = snapshotEntriesByFirmLocation
            .Select(pair =>
            {
                var entry = pair.Value;
                entry.SolicitorId = persisted[pair.Key.ExternalKey].Id;
                return entry;
            })
            .ToList();

        snapshot.TotalFirms = resolvedSnapshotEntries.Count;
        snapshot.Entries = resolvedSnapshotEntries;
        await snapshotRepository.AddAsync(snapshot, cancellationToken);

        var previousContext = previousSnapshot is null
            ? null
            : await BuildSnapshotContextAsync(previousSnapshot.Id, cancellationToken);

        var currentContext = await BuildSnapshotContextAsync(snapshot.Id, cancellationToken);
        var analyticsContext = new AnalyticsContext(currentContext, previousContext);
        await insightSummaryRepository.AddAsync(analyticsEngine.PersistSummary(analyticsContext), cancellationToken);

        logger.LogInformation(
            "Scrape completed. Snapshot {SnapshotId} captured {TotalFirms} firms across {Locations} locations",
            snapshot.Id,
            snapshot.TotalFirms,
            snapshot.LocationsSearched);

        return snapshot.Id;
    }

    private async Task<ScrapeSnapshotContext> BuildSnapshotContextAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        var snapshot = await snapshotRepository.GetByIdWithEntriesAsync(snapshotId, cancellationToken)
            ?? throw new InvalidOperationException($"Snapshot {snapshotId} was not found.");

        var records = SolicitorSnapshotRecords.DeduplicateByFirmAndLocation(
            snapshot.Entries
                .Where(x => x.Solicitor is not null && x.Location is not null)
                .Select(x => new SolicitorSnapshotRecord(
                    x.Solicitor!.ExternalKey,
                    x.Solicitor.FirmName,
                    x.Location!.Name,
                    x.LocationId,
                    x.Solicitor.Phone,
                    x.Solicitor.Address,
                    x.Solicitor.Rating,
                    x.Solicitor.ReviewCount,
                    x.Rank)));

        return new ScrapeSnapshotContext(snapshot.Id, snapshot.ScrapedAt, records);
    }
}
