using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Infrastructure.Resilience;

public interface IHttpResilienceTelemetry
{
    void RecordRequestCompleted(
        string pipelineName,
        string? requestUri,
        HttpStatusCode? statusCode,
        bool success,
        TimeSpan duration,
        Guid? operationId);

    void RecordRetryScheduled(
        string pipelineName,
        int attemptNumber,
        TimeSpan delay,
        string reason,
        HttpStatusCode? statusCode,
        TimeSpan? retryAfter,
        Guid? operationId);

    void RecordCircuitBreakerStateChanged(string pipelineName, string state, string? reason);

    void RecordTimeout(string pipelineName, string timeoutKind, Guid? operationId);

    void RecordRateLimitWait(string pipelineName, TimeSpan delay, Guid? operationId);
}

/// <summary>
/// Structured logging and <see cref="System.Diagnostics.Metrics"/> counters designed for
/// OpenTelemetry/Prometheus export via the .NET metrics API.
/// </summary>
public sealed class HttpResilienceTelemetry(ILogger<HttpResilienceTelemetry> logger) : IHttpResilienceTelemetry
{
    private static readonly Meter Meter = new("InfoTrack.Http.Resilience", "1.0.0");

    private readonly Counter<long> _requestsTotal = Meter.CreateCounter<long>(
        "http.client.requests.total",
        description: "Total outbound HTTP requests passing through resilience pipelines");

    private readonly Counter<long> _retriesTotal = Meter.CreateCounter<long>(
        "http.client.retries.total",
        description: "Retry attempts scheduled by resilience pipelines");

    private readonly Counter<long> _rateLimitTotal = Meter.CreateCounter<long>(
        "http.client.rate_limit.total",
        description: "HTTP 429 responses observed");

    private readonly Counter<long> _circuitBreakerTransitions = Meter.CreateCounter<long>(
        "http.client.circuit_breaker.transitions",
        description: "Circuit breaker state transitions");

    private readonly Histogram<double> _requestDurationMs = Meter.CreateHistogram<double>(
        "http.client.request.duration",
        unit: "ms",
        description: "End-to-end outbound HTTP request duration including retries");

    public void RecordRequestCompleted(
        string pipelineName,
        string? requestUri,
        HttpStatusCode? statusCode,
        bool success,
        TimeSpan duration,
        Guid? operationId)
    {
        var tags = new TagList
        {
            { "pipeline", pipelineName },
            { "success", success },
            { "status_code", statusCode.HasValue ? (int)statusCode.Value : 0 },
        };

        _requestsTotal.Add(1, tags);
        _requestDurationMs.Record(duration.TotalMilliseconds, tags);

        logger.LogInformation(
            "HTTP resilience request completed pipeline={Pipeline} uri={RequestUri} statusCode={StatusCode} success={Success} durationMs={DurationMs} operationId={OperationId}",
            pipelineName,
            requestUri,
            statusCode.HasValue ? (int)statusCode.Value : null,
            success,
            duration.TotalMilliseconds,
            operationId);
    }

    public void RecordRetryScheduled(
        string pipelineName,
        int attemptNumber,
        TimeSpan delay,
        string reason,
        HttpStatusCode? statusCode,
        TimeSpan? retryAfter,
        Guid? operationId)
    {
        var tags = new TagList
        {
            { "pipeline", pipelineName },
            { "reason", reason },
            { "status_code", statusCode.HasValue ? (int)statusCode.Value : 0 },
        };

        _retriesTotal.Add(1, tags);

        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            _rateLimitTotal.Add(1, new TagList { { "pipeline", pipelineName } });
        }

        logger.LogWarning(
            "HTTP resilience retry scheduled pipeline={Pipeline} attempt={Attempt} delayMs={DelayMs} reason={Reason} statusCode={StatusCode} retryAfterSeconds={RetryAfterSeconds} operationId={OperationId}",
            pipelineName,
            attemptNumber,
            delay.TotalMilliseconds,
            reason,
            statusCode.HasValue ? (int)statusCode.Value : null,
            retryAfter?.TotalSeconds,
            operationId);
    }

    public void RecordCircuitBreakerStateChanged(string pipelineName, string state, string? reason)
    {
        _circuitBreakerTransitions.Add(
            1,
            new TagList
            {
                { "pipeline", pipelineName },
                { "state", state },
            });

        logger.LogWarning(
            "HTTP resilience circuit breaker pipeline={Pipeline} state={State} reason={Reason}",
            pipelineName,
            state,
            reason);
    }

    public void RecordTimeout(string pipelineName, string timeoutKind, Guid? operationId)
    {
        logger.LogWarning(
            "HTTP resilience timeout pipeline={Pipeline} timeoutKind={TimeoutKind} operationId={OperationId}",
            pipelineName,
            timeoutKind,
            operationId);
    }

    public void RecordRateLimitWait(string pipelineName, TimeSpan delay, Guid? operationId)
    {
        logger.LogInformation(
            "HTTP resilience rate limit wait pipeline={Pipeline} delayMs={DelayMs} operationId={OperationId}",
            pipelineName,
            delay.TotalMilliseconds,
            operationId);
    }
}
