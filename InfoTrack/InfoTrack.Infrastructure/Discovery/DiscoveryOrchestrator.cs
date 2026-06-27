using System.Diagnostics;
using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Infrastructure.Discovery;

public sealed class DiscoveryOrchestrator(
    IDiscoveryProvider discoveryProvider,
    ILocationRepository locationRepository,
    IDiscoveryRunRepository discoveryRunRepository,
    ILogger<DiscoveryOrchestrator> logger) : IDiscoveryOrchestrator
{
    public async Task<DiscoveryResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var run = new DiscoveryRun
        {
            Id = Guid.NewGuid(),
            StartedAt = DateTimeOffset.UtcNow,
            Source = discoveryProvider.SourceName,
            Status = DiscoveryRunStatus.Running
        };

        await discoveryRunRepository.AddAsync(run, cancellationToken);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation(
                "Discovery run {RunId} started using provider {Source}",
                run.Id,
                discoveryProvider.SourceName);

            var discovered = await discoveryProvider.DiscoverAsync(cancellationToken);
            var syncOutcome = await locationRepository.SyncDiscoveredLocationsAsync(discovered, cancellationToken);

            stopwatch.Stop();
            CompleteRun(run, discovered.Count, syncOutcome, stopwatch.Elapsed);

            await discoveryRunRepository.UpdateAsync(run, cancellationToken);

            logger.LogInformation(
                "Discovery run {RunId} completed. Found={Found} Added={Added} Updated={Updated} Removed={Removed}",
                run.Id,
                run.LocationsFound,
                run.NewLocations,
                run.UpdatedLocations,
                run.RemovedLocations);

            var statistics = new DiscoveryStatistics(
                discovered.Count,
                syncOutcome.Added,
                syncOutcome.Updated,
                syncOutcome.Removed,
                syncOutcome.Skipped,
                syncOutcome.Existing,
                stopwatch.Elapsed);

            return new DiscoveryResult(
                discovered,
                statistics,
                run.Id,
                run.Source,
                run.CompletedAt!.Value);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            run.Status = DiscoveryRunStatus.Failed;
            run.CompletedAt = DateTimeOffset.UtcNow;
            run.Duration = stopwatch.Elapsed;
            run.ErrorMessage = ex.Message;

            await discoveryRunRepository.UpdateAsync(run, cancellationToken);

            logger.LogError(ex, "Discovery run {RunId} failed", run.Id);
            throw;
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
    }
}
