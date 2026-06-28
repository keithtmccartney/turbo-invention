namespace InfoTrack.Infrastructure.Resilience.Options;

/// <summary>
/// Per-client HTTP resilience settings. Bound from <c>Resilience:Defaults</c>,
/// <c>Resilience:Clients:{name}</c>, or legacy nested sections (e.g. <c>Scraping:Resilience</c>).
/// </summary>
public sealed class HttpResilienceClientOptions
{
    /// <summary>Maximum retry attempts after the initial request (not including the first try).</summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>Base delay for exponential backoff when Retry-After is absent.</summary>
    public double BaseDelaySeconds { get; set; } = 2;

    /// <summary>Upper bound for computed retry delays.</summary>
    public double MaxDelaySeconds { get; set; } = 30;

    /// <summary>When true, honour HTTP 429/503 Retry-After headers before falling back to backoff.</summary>
    public bool RespectRetryAfter { get; set; } = true;

    /// <summary>Maximum simultaneous outbound HTTP requests (concurrency limiter / bulkhead).</summary>
    public int MaxConcurrentRequests { get; set; }

    /// <summary>Token-bucket permits replenished each second (0 disables token-bucket rate limiting).</summary>
    public int RequestsPerSecond { get; set; } = 5;

    /// <summary>Queued requests waiting for a concurrency or rate-limit permit.</summary>
    public int RateLimiterQueueLimit { get; set; } = 20;

    /// <summary>
    /// Minimum failures within the sampling window before the breaker can open.
    /// Works together with <see cref="CircuitBreakerFailureRatio"/>.
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>Failure ratio (0–1) that opens the circuit when throughput threshold is met.</summary>
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    /// <summary>Duration the circuit stays open before transitioning to half-open.</summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 60;

    /// <summary>Window used to measure failure ratio for the circuit breaker.</summary>
    public int CircuitBreakerSamplingDurationSeconds { get; set; } = 60;

    /// <summary>TCP/connect phase timeout applied at the <see cref="SocketsHttpHandler"/> level.</summary>
    public int ConnectTimeoutSeconds { get; set; } = 10;

    /// <summary>Per-attempt timeout enforced by Polly (single try including response read).</summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Total end-to-end timeout including all retries. When 0, computed as
    /// <c>RequestTimeoutSeconds * (MaxRetries + 2)</c>.
    /// </summary>
    public int TotalRequestTimeoutSeconds { get; set; }

    public TimeSpan BaseDelay => TimeSpan.FromSeconds(BaseDelaySeconds);

    public TimeSpan MaxDelay => TimeSpan.FromSeconds(MaxDelaySeconds);

    public TimeSpan ConnectTimeout => TimeSpan.FromSeconds(ConnectTimeoutSeconds);

    public TimeSpan AttemptTimeout => TimeSpan.FromSeconds(RequestTimeoutSeconds);

    public TimeSpan TotalTimeout =>
        TotalRequestTimeoutSeconds > 0
            ? TimeSpan.FromSeconds(TotalRequestTimeoutSeconds)
            : TimeSpan.FromSeconds(RequestTimeoutSeconds * (MaxRetries + 2));

    public TimeSpan CircuitBreakerBreakDuration => TimeSpan.FromSeconds(CircuitBreakerDurationSeconds);

    public TimeSpan CircuitBreakerSamplingDuration => TimeSpan.FromSeconds(CircuitBreakerSamplingDurationSeconds);
}
