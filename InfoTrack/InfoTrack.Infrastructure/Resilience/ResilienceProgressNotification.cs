namespace InfoTrack.Infrastructure.Resilience;

public enum ResilienceProgressKind
{
    RateLimitWait,
    Retrying,
    Continuing,
    CircuitBreakerOpen,
    CircuitBreakerClosed,
    CircuitBreakerHalfOpen,
    Timeout,
}

public sealed record ResilienceProgressNotification(
    string PipelineName,
    ResilienceProgressKind Kind,
    Guid? OperationId,
    int AttemptNumber,
    TimeSpan Delay,
    string Reason,
    int? HttpStatusCode,
    double? RetryAfterSeconds);
