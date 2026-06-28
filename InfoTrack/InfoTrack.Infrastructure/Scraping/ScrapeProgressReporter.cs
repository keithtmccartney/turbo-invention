using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Repositories;
using InfoTrack.Domain.Scraping;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Infrastructure.Scraping;

public sealed class ScrapeProgressReporter(
    IScrapeRunRepository scrapeRunRepository,
    ILogger<ScrapeProgressReporter> logger) : IScrapeProgressReporter
{
    public async Task ReportAsync(
        Guid runId,
        ScrapeProgressUpdate update,
        CancellationToken cancellationToken = default)
    {
        var run = await scrapeRunRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            logger.LogWarning("Scrape progress update ignored because run {RunId} was not found", runId);
            return;
        }

        run.ProgressStage = update.Stage;
        run.ProgressMessage = update.Message;
        run.LocationsTotal = update.LocationsTotal;
        run.LocationsCompleted = update.LocationsCompleted;
        run.FirmsDiscovered = update.FirmsDiscovered;
        run.ErrorsEncountered = update.ErrorsEncountered;

        await scrapeRunRepository.UpdateAsync(run, cancellationToken);

        logger.LogInformation(
            "Scrape run {RunId} progress stage={Stage} message={Message}",
            runId,
            update.Stage,
            update.Message);
    }
}
