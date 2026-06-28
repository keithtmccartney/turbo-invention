using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Infrastructure.Discovery;

public sealed class DiscoveryProgressReporter(
    IDiscoveryRunRepository discoveryRunRepository,
    ILogger<DiscoveryProgressReporter> logger) : IDiscoveryProgressReporter
{
    public async Task ReportAsync(
        Guid runId,
        DiscoveryProgressUpdate update,
        CancellationToken cancellationToken = default)
    {
        var run = await discoveryRunRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            logger.LogWarning("Progress update ignored because run {RunId} was not found", runId);
            return;
        }

        run.ProgressStage = update.Stage;
        run.ProgressMessage = update.Message;
        run.SitemapsDownloaded = update.SitemapsDownloaded;
        run.UrlsParsed = update.UrlsParsed;
        run.LocationsFound = update.LocationsDiscovered;
        run.NewLocations = update.NewLocationsAdded;
        run.ExistingLocations = update.ExistingLocationsUpdated;
        run.ErrorsEncountered = update.ErrorsEncountered;

        await discoveryRunRepository.UpdateAsync(run, cancellationToken);

        logger.LogInformation(
            "Discovery run {RunId} progress stage={Stage} message={Message}",
            runId,
            update.Stage,
            update.Message);
    }
}
