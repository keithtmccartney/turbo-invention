using System.Diagnostics;
using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Operations;
using InfoTrack.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Infrastructure.Discovery;

public sealed class DiscoveryOrchestrator(
    IDiscoveryProvider discoveryProvider,
    ILocationRepository locationRepository,
    IDiscoveryRunRepository discoveryRunRepository,
    IDiscoveryProgressReporter progressReporter,
    IOperationQueue operationQueue,
    ILogger<DiscoveryOrchestrator> logger) : IDiscoveryOrchestrator
{
    private static readonly ActivitySource ActivitySource = new("InfoTrack.Discovery");

    public async Task<Guid> StartAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        if (await discoveryRunRepository.HasActiveRunAsync(cancellationToken))
        {
            throw new InvalidOperationException("A discovery operation is already queued or running.");
        }

        var run = new DiscoveryRun
        {
            Id = Guid.NewGuid(),
            StartedAt = DateTimeOffset.UtcNow,
            Source = discoveryProvider.SourceName,
            Status = DiscoveryRunStatus.Queued,
            CorrelationId = correlationId,
            ProgressStage = DiscoveryProgressStage.Queued,
            ProgressMessage = "Discovery queued",
        };

        await discoveryRunRepository.AddAsync(run, cancellationToken);

        await progressReporter.ReportAsync(
            run.Id,
            new DiscoveryProgressUpdate(DiscoveryProgressStage.Queued, "Discovery queued"),
            cancellationToken);

        await operationQueue.EnqueueAsync(
            new OperationWorkItem(run.Id, OperationKind.Discovery, correlationId),
            cancellationToken);

        logger.LogInformation(
            "Discovery operation {OperationId} queued correlationId={CorrelationId}",
            run.Id,
            correlationId);

        return run.Id;
    }

    public async Task ExecuteAsync(
        Guid operationId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("discovery.execute", ActivityKind.Internal);
        activity?.SetTag("operation.id", operationId);
        activity?.SetTag("correlation.id", correlationId);

        var run = await discoveryRunRepository.GetByIdAsync(operationId, cancellationToken)
            ?? throw new InvalidOperationException($"Discovery run {operationId} was not found.");

        if (run.Status is DiscoveryRunStatus.Completed or DiscoveryRunStatus.Failed)
        {
            logger.LogWarning(
                "Discovery run {OperationId} already terminal with status {Status}",
                operationId,
                run.Status);
            return;
        }

        run.Status = DiscoveryRunStatus.Running;
        run.CorrelationId = correlationId;
        await discoveryRunRepository.UpdateAsync(run, cancellationToken);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation(
                "Discovery run {OperationId} started using provider {Source}",
                operationId,
                discoveryProvider.SourceName);

            var discovered = await discoveryProvider.DiscoverAsync(operationId, cancellationToken);

            await progressReporter.ReportAsync(
                operationId,
                new DiscoveryProgressUpdate(
                    DiscoveryProgressStage.SyncingLocations,
                    "Synchronising discovered locations",
                    LocationsDiscovered: discovered.Count),
                cancellationToken);

            var syncOutcome = await locationRepository.SyncDiscoveredLocationsAsync(discovered, cancellationToken);

            await progressReporter.ReportAsync(
                operationId,
                new DiscoveryProgressUpdate(
                    DiscoveryProgressStage.NewLocationsAdded,
                    "New locations added",
                    LocationsDiscovered: discovered.Count,
                    NewLocationsAdded: syncOutcome.Added,
                    ExistingLocationsUpdated: syncOutcome.Existing),
                cancellationToken);

            await progressReporter.ReportAsync(
                operationId,
                new DiscoveryProgressUpdate(
                    DiscoveryProgressStage.ExistingLocationsUpdated,
                    "Existing locations updated",
                    LocationsDiscovered: discovered.Count,
                    NewLocationsAdded: syncOutcome.Added,
                    ExistingLocationsUpdated: syncOutcome.Existing + syncOutcome.Updated),
                cancellationToken);

            stopwatch.Stop();
            CompleteRun(run, discovered.Count, syncOutcome, stopwatch.Elapsed);
            await discoveryRunRepository.UpdateAsync(run, cancellationToken);

            await progressReporter.ReportAsync(
                operationId,
                new DiscoveryProgressUpdate(
                    DiscoveryProgressStage.Completed,
                    "Discovery complete",
                    LocationsDiscovered: discovered.Count,
                    NewLocationsAdded: syncOutcome.Added,
                    ExistingLocationsUpdated: syncOutcome.Existing + syncOutcome.Updated),
                cancellationToken);

            logger.LogInformation(
                "Discovery run {OperationId} completed. Found={Found} Added={Added} Updated={Updated} Removed={Removed}",
                operationId,
                run.LocationsFound,
                run.NewLocations,
                run.UpdatedLocations,
                run.RemovedLocations);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            run.Status = DiscoveryRunStatus.Failed;
            run.CompletedAt = DateTimeOffset.UtcNow;
            run.Duration = stopwatch.Elapsed;
            run.ErrorMessage = ex.Message;
            run.ProgressStage = DiscoveryProgressStage.Failed;
            run.ProgressMessage = ex.Message;
            run.ErrorsEncountered += 1;

            await discoveryRunRepository.UpdateAsync(run, cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Discovery run {OperationId} failed", operationId);
        }
    }

    private static void CompleteRun(
        DiscoveryRun run,
        int locationsFound,
        DiscoverySyncOutcome syncOutcome,
        TimeSpan duration)
    {
        run.CompletedAt = DateTimeOffset.UtcNow;
        run.Duration = duration;
        run.LocationsFound = locationsFound;
        run.NewLocations = syncOutcome.Added;
        run.ExistingLocations = syncOutcome.Existing;
        run.UpdatedLocations = syncOutcome.Updated;
        run.RemovedLocations = syncOutcome.Removed;
        run.SkippedLocations = syncOutcome.Skipped;
        run.Status = DiscoveryRunStatus.Completed;
        run.ProgressStage = DiscoveryProgressStage.Completed;
        run.ProgressMessage = "Discovery complete";
    }
}
