using InfoTrack.Application.Features.Scraping.GetScrapeRunStatus;
using InfoTrack.Application.Features.Scraping.StartScrape;
using InfoTrack.Contracts.Scraping;
using InfoTrack.Domain.Entities;

namespace InfoTrack.Application.Features.Scraping.RunScrape;

/// <summary>
/// Waits for an asynchronous scrape operation to complete. Used by MCP tools that require a synchronous result.
/// </summary>
public sealed class RunScrapeHandler(
    StartScrapeHandler startScrapeHandler,
    GetScrapeRunStatusHandler getScrapeRunStatusHandler)
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);

    public async Task<ScrapeResponse> HandleAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var started = await startScrapeHandler.HandleAsync(correlationId, cancellationToken);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(DefaultTimeout);

        while (!timeoutCts.IsCancellationRequested)
        {
            var status = await getScrapeRunStatusHandler.HandleAsync(started.OperationId, cancellationToken);
            if (status is null)
            {
                throw new InvalidOperationException($"Scrape operation {started.OperationId} was not found.");
            }

            if (status.Status == nameof(ScrapeRunStatus.Completed))
            {
                return status.Result
                    ?? throw new InvalidOperationException("Completed scrape run is missing result.");
            }

            if (status.Status == nameof(ScrapeRunStatus.Failed))
            {
                throw new InvalidOperationException(status.ErrorMessage ?? "Scrape failed.");
            }

            await Task.Delay(PollInterval, timeoutCts.Token);
        }

        throw new TimeoutException($"Scrape operation {started.OperationId} did not complete in time.");
    }
}
