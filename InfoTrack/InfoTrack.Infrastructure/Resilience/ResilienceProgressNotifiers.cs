using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Scraping;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Infrastructure.Resilience;

/// <summary>
/// Maps generic resilience events to discovery run progress messages shown in the UI.
/// </summary>
public sealed class DiscoveryResilienceProgressNotifier(
    IDiscoveryProgressReporter progressReporter,
    ILogger<DiscoveryResilienceProgressNotifier> logger) : IResilienceProgressNotifier
{
    public async Task NotifyAsync(ResilienceProgressNotification notification, CancellationToken cancellationToken = default)
    {
        if (notification.OperationId is not Guid runId)
        {
            return;
        }

        if (!string.Equals(notification.PipelineName, ResiliencePipelineNames.Discovery, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var (stage, message) = notification.Kind switch
        {
            ResilienceProgressKind.RateLimitWait =>
                (DiscoveryProgressStage.WaitingForRateLimit,
                    $"Waiting due to remote rate limiting ({notification.RetryAfterSeconds ?? notification.Delay.TotalSeconds:0}s)…"),

            ResilienceProgressKind.Retrying =>
                (DiscoveryProgressStage.RetryingRequest,
                    $"Retrying request (attempt {notification.AttemptNumber})…"),

            ResilienceProgressKind.Continuing =>
                (DiscoveryProgressStage.ContinuingAfterRetry,
                    "Continuing discovery…"),

            ResilienceProgressKind.CircuitBreakerOpen =>
                (DiscoveryProgressStage.RetryingRequest,
                    "Upstream circuit breaker open — waiting before retry…"),

            ResilienceProgressKind.Timeout =>
                (DiscoveryProgressStage.RetryingRequest,
                    "Request timed out — retrying…"),

            _ => ((DiscoveryProgressStage?)null, (string?)null),
        };

        if (stage is null || message is null)
        {
            return;
        }

        logger.LogDebug(
            "Discovery progress from resilience runId={RunId} stage={Stage} message={Message}",
            runId,
            stage,
            message);

        await progressReporter.ReportAsync(
            runId,
            new DiscoveryProgressUpdate(stage.Value, message),
            cancellationToken);
    }
}

/// <summary>
/// Maps resilience events to scrape run progress (location fetch retries).
/// </summary>
public sealed class ScrapeResilienceProgressNotifier(
    IScrapeProgressReporter progressReporter,
    ILogger<ScrapeResilienceProgressNotifier> logger) : IResilienceProgressNotifier
{
    public async Task NotifyAsync(ResilienceProgressNotification notification, CancellationToken cancellationToken = default)
    {
        if (notification.OperationId is not Guid runId)
        {
            return;
        }

        if (!string.Equals(notification.PipelineName, ResiliencePipelineNames.Scraping, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var (stage, message) = notification.Kind switch
        {
            ResilienceProgressKind.RateLimitWait =>
                (ScrapeProgressStage.WaitingForRateLimit,
                    $"Waiting due to remote rate limiting ({notification.RetryAfterSeconds ?? notification.Delay.TotalSeconds:0}s)…"),

            ResilienceProgressKind.Retrying or ResilienceProgressKind.Timeout or ResilienceProgressKind.CircuitBreakerOpen =>
                (ScrapeProgressStage.RetryingRequest,
                    $"Retrying location request (attempt {notification.AttemptNumber})…"),

            ResilienceProgressKind.Continuing =>
                (ScrapeProgressStage.ScrapingLocation,
                    "Continuing scrape…"),

            _ => ((ScrapeProgressStage?)null, (string?)null),
        };

        if (stage is null || message is null)
        {
            return;
        }

        logger.LogDebug(
            "Scrape progress from resilience runId={RunId} stage={Stage} message={Message}",
            runId,
            stage,
            message);

        await progressReporter.ReportAsync(
            runId,
            new ScrapeProgressUpdate(stage.Value, message),
            cancellationToken);
    }
}

/// <summary>
/// Fan-out notifier so multiple domain-specific bridges can react to the same resilience event.
/// </summary>
public sealed class CompositeResilienceProgressNotifier(IEnumerable<IResilienceProgressNotifier> notifiers) : IResilienceProgressNotifier
{
    public async Task NotifyAsync(ResilienceProgressNotification notification, CancellationToken cancellationToken = default)
    {
        foreach (var notifier in notifiers)
        {
            await notifier.NotifyAsync(notification, cancellationToken);
        }
    }
}
