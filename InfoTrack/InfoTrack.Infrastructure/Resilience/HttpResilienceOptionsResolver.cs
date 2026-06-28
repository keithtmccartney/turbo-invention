using InfoTrack.Infrastructure.Options;
using InfoTrack.Infrastructure.Resilience.Options;
using Microsoft.Extensions.Options;

namespace InfoTrack.Infrastructure.Resilience;

/// <summary>
/// Merges <c>Resilience:Defaults</c>, named client overrides, and legacy nested sections.
/// </summary>
public sealed class HttpResilienceOptionsResolver(IOptions<ResilienceOptions> resilienceOptions)
{
    public HttpResilienceClientOptions Resolve(string pipelineName, HttpResilienceClientOptions? legacyOverride = null)
    {
        var merged = Clone(resilienceOptions.Value.Defaults);

        if (resilienceOptions.Value.Clients.TryGetValue(pipelineName, out var clientOverride))
        {
            Apply(merged, clientOverride);
        }

        if (legacyOverride is not null)
        {
            Apply(merged, legacyOverride);
        }

        return merged;
    }

    public HttpResilienceClientOptions ResolveScraping(IOptions<ScrapingOptions> scrapingOptions) =>
        Resolve(ResiliencePipelineNames.Scraping, MapLegacy(scrapingOptions.Value.Resilience, scrapingOptions.Value.RequestTimeoutSeconds));

    public HttpResilienceClientOptions ResolveDiscovery(IOptions<DiscoveryOptions> discoveryOptions) =>
        Resolve(ResiliencePipelineNames.Discovery, MapLegacy(discoveryOptions.Value.Resilience, discoveryOptions.Value.RequestTimeoutSeconds));

    private static HttpResilienceClientOptions MapLegacy(DiscoveryResilienceOptions legacy, int requestTimeoutSeconds) =>
        new()
        {
            MaxRetries = legacy.MaxRetryAttempts,
            BaseDelaySeconds = legacy.RetryDelayMilliseconds / 1000d,
            MaxDelaySeconds = legacy.MaxRetryDelayMilliseconds / 1000d,
            MaxConcurrentRequests = legacy.BulkheadMaxParallelization,
            RateLimiterQueueLimit = legacy.RateLimiterQueueLimit,
            RequestsPerSecond = legacy.RateLimiterPermitLimit,
            CircuitBreakerFailureRatio = legacy.CircuitBreakerFailureRatio / 100d,
            CircuitBreakerThreshold = legacy.CircuitBreakerMinimumThroughput,
            CircuitBreakerDurationSeconds = legacy.CircuitBreakerBreakDurationSeconds,
            RequestTimeoutSeconds = requestTimeoutSeconds,
        };

    private static HttpResilienceClientOptions MapLegacy(ScrapingResilienceOptions legacy, int requestTimeoutSeconds) =>
        new()
        {
            MaxRetries = legacy.MaxRetryAttempts,
            BaseDelaySeconds = legacy.RetryDelayMilliseconds / 1000d,
            MaxDelaySeconds = legacy.MaxRetryDelayMilliseconds / 1000d,
            CircuitBreakerFailureRatio = legacy.CircuitBreakerFailureRatio / 100d,
            CircuitBreakerThreshold = legacy.CircuitBreakerMinimumThroughput,
            CircuitBreakerDurationSeconds = legacy.CircuitBreakerBreakDurationSeconds,
            RequestTimeoutSeconds = requestTimeoutSeconds,
        };

    private static HttpResilienceClientOptions Clone(HttpResilienceClientOptions source) =>
        new()
        {
            MaxRetries = source.MaxRetries,
            BaseDelaySeconds = source.BaseDelaySeconds,
            MaxDelaySeconds = source.MaxDelaySeconds,
            RespectRetryAfter = source.RespectRetryAfter,
            MaxConcurrentRequests = source.MaxConcurrentRequests,
            RequestsPerSecond = source.RequestsPerSecond,
            RateLimiterQueueLimit = source.RateLimiterQueueLimit,
            CircuitBreakerThreshold = source.CircuitBreakerThreshold,
            CircuitBreakerFailureRatio = source.CircuitBreakerFailureRatio,
            CircuitBreakerDurationSeconds = source.CircuitBreakerDurationSeconds,
            CircuitBreakerSamplingDurationSeconds = source.CircuitBreakerSamplingDurationSeconds,
            ConnectTimeoutSeconds = source.ConnectTimeoutSeconds,
            RequestTimeoutSeconds = source.RequestTimeoutSeconds,
            TotalRequestTimeoutSeconds = source.TotalRequestTimeoutSeconds,
        };

    private static void Apply(HttpResilienceClientOptions target, HttpResilienceClientOptions overrideOptions)
    {
        if (overrideOptions.MaxRetries != default)
        {
            target.MaxRetries = overrideOptions.MaxRetries;
        }

        if (overrideOptions.BaseDelaySeconds != default)
        {
            target.BaseDelaySeconds = overrideOptions.BaseDelaySeconds;
        }

        if (overrideOptions.MaxDelaySeconds != default)
        {
            target.MaxDelaySeconds = overrideOptions.MaxDelaySeconds;
        }

        target.RespectRetryAfter = overrideOptions.RespectRetryAfter;

        if (overrideOptions.MaxConcurrentRequests != default)
        {
            target.MaxConcurrentRequests = overrideOptions.MaxConcurrentRequests;
        }

        if (overrideOptions.RequestsPerSecond != default)
        {
            target.RequestsPerSecond = overrideOptions.RequestsPerSecond;
        }

        if (overrideOptions.RateLimiterQueueLimit != default)
        {
            target.RateLimiterQueueLimit = overrideOptions.RateLimiterQueueLimit;
        }

        if (overrideOptions.CircuitBreakerThreshold != default)
        {
            target.CircuitBreakerThreshold = overrideOptions.CircuitBreakerThreshold;
        }

        if (overrideOptions.CircuitBreakerFailureRatio != default)
        {
            target.CircuitBreakerFailureRatio = overrideOptions.CircuitBreakerFailureRatio;
        }

        if (overrideOptions.CircuitBreakerDurationSeconds != default)
        {
            target.CircuitBreakerDurationSeconds = overrideOptions.CircuitBreakerDurationSeconds;
        }

        if (overrideOptions.CircuitBreakerSamplingDurationSeconds != default)
        {
            target.CircuitBreakerSamplingDurationSeconds = overrideOptions.CircuitBreakerSamplingDurationSeconds;
        }

        if (overrideOptions.ConnectTimeoutSeconds != default)
        {
            target.ConnectTimeoutSeconds = overrideOptions.ConnectTimeoutSeconds;
        }

        if (overrideOptions.RequestTimeoutSeconds != default)
        {
            target.RequestTimeoutSeconds = overrideOptions.RequestTimeoutSeconds;
        }

        if (overrideOptions.TotalRequestTimeoutSeconds != default)
        {
            target.TotalRequestTimeoutSeconds = overrideOptions.TotalRequestTimeoutSeconds;
        }
    }
}
