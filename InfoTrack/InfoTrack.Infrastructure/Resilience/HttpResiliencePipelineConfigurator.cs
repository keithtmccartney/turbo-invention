using System.Net;
using System.Threading.RateLimiting;
using InfoTrack.Infrastructure.Resilience.Options;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Retry;
using Polly.Timeout;

namespace InfoTrack.Infrastructure.Resilience;

/// <summary>
/// Builds Polly v8 <see cref="ResiliencePipeline{HttpResponseMessage}"/> instances for typed HttpClients.
/// Strategy order matches Microsoft's standard HTTP handler: rate limit → total timeout → retry →
/// circuit breaker → attempt timeout (innermost).
/// </summary>
public static class HttpResiliencePipelineConfigurator
{
    public static void Configure(
        ResiliencePipelineBuilder<HttpResponseMessage> builder,
        HttpResilienceClientOptions options,
        string pipelineName,
        IHttpResilienceTelemetry telemetry,
        IResilienceProgressNotifier progressNotifier)
    {
        builder.AddConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = Math.Max(1, options.MaxConcurrentRequests),
            QueueLimit = Math.Max(0, options.RateLimiterQueueLimit),
        });

        builder.AddTimeout(new HttpTimeoutStrategyOptions
        {
            Name = "TotalRequestTimeout",
            Timeout = options.TotalTimeout,
            OnTimeout = args =>
            {
                telemetry.RecordTimeout(pipelineName, "total", GetOperationId(args.Context));
                return default;
            },
        });

        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = Math.Max(0, options.MaxRetries),
            Delay = options.BaseDelay,
            MaxDelay = options.MaxDelay,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldRetryAfterHeader = options.RespectRetryAfter,
            ShouldHandle = ShouldRetryPredicate,
            OnRetry = args => OnRetryAsync(args, pipelineName, options, telemetry, progressNotifier),
        });

        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = options.CircuitBreakerFailureRatio,
            MinimumThroughput = Math.Max(1, options.CircuitBreakerThreshold),
            BreakDuration = options.CircuitBreakerBreakDuration,
            SamplingDuration = options.CircuitBreakerSamplingDuration,
            ShouldHandle = ShouldRetryPredicate,
            OnOpened = args =>
            {
                telemetry.RecordCircuitBreakerStateChanged(pipelineName, "open", args.BreakDuration.ToString());
                return NotifyCircuitAsync(
                    progressNotifier,
                    pipelineName,
                    ResilienceProgressKind.CircuitBreakerOpen,
                    args.Context,
                    "Circuit breaker opened due to sustained upstream failures");
            },
            OnClosed = args =>
            {
                telemetry.RecordCircuitBreakerStateChanged(pipelineName, "closed", null);
                return NotifyCircuitAsync(
                    progressNotifier,
                    pipelineName,
                    ResilienceProgressKind.CircuitBreakerClosed,
                    args.Context,
                    "Circuit breaker closed — upstream healthy");
            },
            OnHalfOpened = args =>
            {
                telemetry.RecordCircuitBreakerStateChanged(pipelineName, "half-open", null);
                return NotifyCircuitAsync(
                    progressNotifier,
                    pipelineName,
                    ResilienceProgressKind.CircuitBreakerHalfOpen,
                    args.Context,
                    "Circuit breaker half-open — probing upstream");
            },
        });

        builder.AddTimeout(new HttpTimeoutStrategyOptions
        {
            Name = "AttemptTimeout",
            Timeout = options.AttemptTimeout,
            OnTimeout = args =>
            {
                telemetry.RecordTimeout(pipelineName, "attempt", GetOperationId(args.Context));
                return default;
            },
        });
    }

    private static ValueTask<bool> ShouldRetryPredicate(CircuitBreakerPredicateArguments<HttpResponseMessage> args) =>
        ShouldRetryCore(args.Outcome, args.Context.CancellationToken);

    private static ValueTask<bool> ShouldRetryPredicate(RetryPredicateArguments<HttpResponseMessage> args) =>
        ShouldRetryCore(args.Outcome, args.Context.CancellationToken);

    private static ValueTask<bool> ShouldRetryCore(Outcome<HttpResponseMessage> outcome, CancellationToken cancellationToken)
    {
        if (outcome.Result is { } response)
        {
            return ValueTask.FromResult(HttpTransientFailureClassifier.ShouldRetryHttpResponse(response));
        }

        if (outcome.Exception is { } exception)
        {
            return ValueTask.FromResult(HttpTransientFailureClassifier.ShouldRetryException(exception, cancellationToken));
        }

        return ValueTask.FromResult(false);
    }

    private static async ValueTask OnRetryAsync(
        OnRetryArguments<HttpResponseMessage> args,
        string pipelineName,
        HttpResilienceClientOptions options,
        IHttpResilienceTelemetry telemetry,
        IResilienceProgressNotifier progressNotifier)
    {
        var operationId = ResilienceOperationContext.Current;
        HttpStatusCode? statusCode = args.Outcome.Result?.StatusCode;
        TimeSpan? retryAfter = null;

        if (args.Outcome.Result is { } response)
        {
            retryAfter = RetryAfterHeaderParser.TryGetDelay(response.Headers);
        }

        var reason = args.Outcome.Exception is { } exception
            ? HttpTransientFailureClassifier.DescribeException(exception)
            : statusCode.HasValue
                ? HttpTransientFailureClassifier.DescribeHttpStatus(statusCode.Value)
                : "transient failure";

        telemetry.RecordRetryScheduled(
            pipelineName,
            args.AttemptNumber + 1,
            args.RetryDelay,
            reason,
            statusCode,
            retryAfter,
            operationId);

        var kind = statusCode == HttpStatusCode.TooManyRequests
            ? ResilienceProgressKind.RateLimitWait
            : ResilienceProgressKind.Retrying;

        if (kind == ResilienceProgressKind.RateLimitWait)
        {
            telemetry.RecordRateLimitWait(pipelineName, args.RetryDelay, operationId);
        }

        await progressNotifier.NotifyAsync(
            new ResilienceProgressNotification(
                pipelineName,
                kind,
                operationId,
                args.AttemptNumber + 1,
                args.RetryDelay,
                reason,
                statusCode.HasValue ? (int)statusCode.Value : null,
                retryAfter?.TotalSeconds ?? (options.RespectRetryAfter ? null : args.RetryDelay.TotalSeconds)),
            args.Context.CancellationToken);
    }

    private static async ValueTask NotifyCircuitAsync(
        IResilienceProgressNotifier progressNotifier,
        string pipelineName,
        ResilienceProgressKind kind,
        ResilienceContext context,
        string reason)
    {
        await progressNotifier.NotifyAsync(
            new ResilienceProgressNotification(
                pipelineName,
                kind,
                GetOperationId(context),
                0,
                TimeSpan.Zero,
                reason,
                null,
                null),
            context.CancellationToken);
    }

    private static Guid? GetOperationId(ResilienceContext context) =>
        ResilienceOperationContext.Current;
}

/// <summary>
/// Outermost handler that records request duration and outcome metrics after the resilience pipeline executes.
/// </summary>
public sealed class HttpResilienceTelemetryHandler(
    IHttpResilienceTelemetry telemetry,
    string pipelineName) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Guid? operationId = request.Options.TryGetValue(HttpRequestContextKeys.OperationId, out var id) ? id : null;

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            telemetry.RecordRequestCompleted(
                pipelineName,
                request.RequestUri?.ToString(),
                response.StatusCode,
                response.IsSuccessStatusCode,
                sw.Elapsed,
                operationId);

            return response;
        }
        catch (Exception)
        {
            sw.Stop();
            telemetry.RecordRequestCompleted(
                pipelineName,
                request.RequestUri?.ToString(),
                null,
                false,
                sw.Elapsed,
                operationId);
            throw;
        }
    }
}
