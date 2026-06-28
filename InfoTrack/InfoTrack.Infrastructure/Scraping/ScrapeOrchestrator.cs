using System.Diagnostics;
using InfoTrack.Domain.Analytics;
using InfoTrack.Domain.Common;
using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Operations;
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
    IScrapeRunRepository scrapeRunRepository,
    IInsightSummaryRepository insightSummaryRepository,
    IAnalyticsEngine analyticsEngine,
    IScrapeProgressReporter progressReporter,
    IOperationQueue operationQueue,
    InfoTrackDbContext dbContext,
    IOptions<ScrapingOptions> options,
    ILogger<ScrapeOrchestrator> logger) : IScrapeOrchestrator
{
    private static readonly ActivitySource ActivitySource = new("InfoTrack.Scraping");

    private readonly ScrapingOptions _options = options.Value;

    public async Task<Guid> StartAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        if (await scrapeRunRepository.HasActiveRunAsync(cancellationToken))
        {
            throw new InvalidOperationException("A scrape operation is already queued or running.");
        }

        var run = new ScrapeRun
        {
            Id = Guid.NewGuid(),
            StartedAt = DateTimeOffset.UtcNow,
            Status = ScrapeRunStatus.Queued,
            CorrelationId = correlationId,
            ProgressStage = ScrapeProgressStage.Queued,
            ProgressMessage = "Scrape queued",
        };

        await scrapeRunRepository.AddAsync(run, cancellationToken);

        await progressReporter.ReportAsync(
            run.Id,
            new ScrapeProgressUpdate(ScrapeProgressStage.Queued, "Scrape queued"),
            cancellationToken);

        await operationQueue.EnqueueAsync(
            new OperationWorkItem(run.Id, OperationKind.Scrape, correlationId),
            cancellationToken);

        logger.LogInformation(
            "Scrape operation {OperationId} queued correlationId={CorrelationId}",
            run.Id,
            correlationId);

        return run.Id;
    }

    public async Task ExecuteAsync(
        Guid operationId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("scrape.execute", ActivityKind.Internal);
        activity?.SetTag("operation.id", operationId);
        activity?.SetTag("correlation.id", correlationId);

        var run = await scrapeRunRepository.GetByIdAsync(operationId, cancellationToken)
            ?? throw new InvalidOperationException($"Scrape run {operationId} was not found.");

        if (run.Status is ScrapeRunStatus.Completed or ScrapeRunStatus.Failed)
        {
            logger.LogWarning(
                "Scrape run {OperationId} already terminal with status {Status}",
                operationId,
                run.Status);
            return;
        }

        run.Status = ScrapeRunStatus.Running;
        run.CorrelationId = correlationId;
        await scrapeRunRepository.UpdateAsync(run, cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        var errorsEncountered = 0;

        try
        {
            await progressReporter.ReportAsync(
                operationId,
                new ScrapeProgressUpdate(ScrapeProgressStage.ValidatingLocations, "Validating active locations"),
                cancellationToken);

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
            var locationsCompleted = 0;
            var firmsDiscovered = 0;

            foreach (var location in locations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await progressReporter.ReportAsync(
                    operationId,
                    new ScrapeProgressUpdate(
                        ScrapeProgressStage.ScrapingLocation,
                        $"Scraping {location.Name}",
                        LocationsTotal: locations.Count,
                        LocationsCompleted: locationsCompleted,
                        FirmsDiscovered: firmsDiscovered,
                        ErrorsEncountered: errorsEncountered),
                    cancellationToken);

                try
                {
                    var html = await scrapeClient.FetchLocationPageAsync(location.Slug, operationId, cancellationToken);
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

                    firmsDiscovered = solicitorsByKey.Count;
                }
                catch (Exception ex)
                {
                    errorsEncountered++;
                    logger.LogWarning(ex, "Failed scraping location {LocationName}", location.Name);
                }

                locationsCompleted++;

                await progressReporter.ReportAsync(
                    operationId,
                    new ScrapeProgressUpdate(
                        ScrapeProgressStage.LocationScraped,
                        $"Completed {location.Name}",
                        LocationsTotal: locations.Count,
                        LocationsCompleted: locationsCompleted,
                        FirmsDiscovered: firmsDiscovered,
                        ErrorsEncountered: errorsEncountered),
                    cancellationToken);

                if (_options.RequestDelayMilliseconds > 0)
                {
                    await Task.Delay(_options.RequestDelayMilliseconds, cancellationToken);
                }
            }

            await progressReporter.ReportAsync(
                operationId,
                new ScrapeProgressUpdate(
                    ScrapeProgressStage.PersistingSolicitors,
                    "Persisting solicitor records",
                    LocationsTotal: locations.Count,
                    LocationsCompleted: locationsCompleted,
                    FirmsDiscovered: firmsDiscovered,
                    ErrorsEncountered: errorsEncountered),
                cancellationToken);

            var solicitorsToUpsert = solicitorsByKey.Values.ToList();
            await solicitorRepository.UpsertRangeAsync(solicitorsToUpsert, cancellationToken);

            await progressReporter.ReportAsync(
                operationId,
                new ScrapeProgressUpdate(
                    ScrapeProgressStage.BuildingSnapshot,
                    "Building scrape snapshot",
                    LocationsTotal: locations.Count,
                    LocationsCompleted: locationsCompleted,
                    FirmsDiscovered: firmsDiscovered,
                    ErrorsEncountered: errorsEncountered),
                cancellationToken);

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

            snapshot.TotalFirms = solicitorsByKey.Count;
            snapshot.Entries = resolvedSnapshotEntries;
            await snapshotRepository.AddAsync(snapshot, cancellationToken);

            await progressReporter.ReportAsync(
                operationId,
                new ScrapeProgressUpdate(
                    ScrapeProgressStage.ComputingAnalytics,
                    "Computing analytics",
                    LocationsTotal: locations.Count,
                    LocationsCompleted: locationsCompleted,
                    FirmsDiscovered: snapshot.TotalFirms,
                    ErrorsEncountered: errorsEncountered),
                cancellationToken);

            var previousContext = previousSnapshot is null
                ? null
                : await BuildSnapshotContextAsync(previousSnapshot.Id, cancellationToken);

            var currentContext = await BuildSnapshotContextAsync(snapshot.Id, cancellationToken);
            var analyticsContext = new AnalyticsContext(currentContext, previousContext);
            var insight = analyticsEngine.PersistSummary(analyticsContext);
            await insightSummaryRepository.AddAsync(insight, cancellationToken);

            stopwatch.Stop();
            CompleteRun(
                run,
                snapshot,
                insight.NewFirms,
                insight.RemovedFirms,
                errorsEncountered,
                stopwatch.Elapsed);

            await scrapeRunRepository.UpdateAsync(run, cancellationToken);

            await progressReporter.ReportAsync(
                operationId,
                new ScrapeProgressUpdate(
                    ScrapeProgressStage.Completed,
                    "Scrape complete",
                    LocationsTotal: locations.Count,
                    LocationsCompleted: locationsCompleted,
                    FirmsDiscovered: snapshot.TotalFirms,
                    ErrorsEncountered: errorsEncountered),
                cancellationToken);

            logger.LogInformation(
                "Scrape completed. Snapshot {SnapshotId} captured {TotalFirms} firms across {Locations} locations",
                snapshot.Id,
                snapshot.TotalFirms,
                snapshot.LocationsSearched);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            run.Status = ScrapeRunStatus.Failed;
            run.CompletedAt = DateTimeOffset.UtcNow;
            run.Duration = stopwatch.Elapsed;
            run.ErrorMessage = ex.Message;
            run.ProgressStage = ScrapeProgressStage.Failed;
            run.ProgressMessage = ex.Message;
            run.ErrorsEncountered = errorsEncountered + 1;

            await scrapeRunRepository.UpdateAsync(run, cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Scrape run {OperationId} failed", operationId);
        }
    }

    private static void CompleteRun(
        ScrapeRun run,
        ScrapeSnapshot snapshot,
        int newFirms,
        int removedFirms,
        int errorsEncountered,
        TimeSpan duration)
    {
        run.CompletedAt = DateTimeOffset.UtcNow;
        run.Duration = duration;
        run.Status = ScrapeRunStatus.Completed;
        run.ProgressStage = ScrapeProgressStage.Completed;
        run.ProgressMessage = "Scrape complete";
        run.LocationsTotal = snapshot.LocationsSearched;
        run.LocationsCompleted = snapshot.LocationsSearched;
        run.FirmsDiscovered = snapshot.TotalFirms;
        run.NewFirms = newFirms;
        run.RemovedFirms = removedFirms;
        run.ErrorsEncountered = errorsEncountered;
        run.SnapshotId = snapshot.Id;
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
                    x.Solicitor.Website,
                    x.Solicitor.Rating,
                    x.Solicitor.ReviewCount,
                    x.Rank)));

        return new ScrapeSnapshotContext(snapshot.Id, snapshot.ScrapedAt, records);
    }
}
