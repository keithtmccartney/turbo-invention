namespace InfoTrack.Infrastructure.Options;

public sealed class DiscoveryOptions
{
    public const string SectionName = "Discovery";

    public string BaseUrl { get; set; } = "https://www.solicitors.com";

    public string SitemapIndexPath { get; set; } = "/sitemap.xml";

    public string ConveyancingSitemapPath { get; set; } = "/google-sitemap4.xml";

    public string ConveyancingUrlSegment { get; set; } = "conveyancing+";

    public string UserAgent { get; set; } = "InfoTrack-Assessment/1.0 (+https://github.com/infotrack)";

    public int RequestTimeoutSeconds { get; set; } = 30;

    public DiscoveryResilienceOptions Resilience { get; set; } = new();
}

public sealed class DiscoveryResilienceOptions
{
    public int MaxRetryAttempts { get; set; } = 3;

    public int RetryDelayMilliseconds { get; set; } = 500;

    public int MaxRetryDelayMilliseconds { get; set; } = 5000;

    public int CircuitBreakerFailureRatio { get; set; } = 50;

    public int CircuitBreakerMinimumThroughput { get; set; } = 5;

    public int CircuitBreakerBreakDurationSeconds { get; set; } = 30;

    public int RateLimiterPermitLimit { get; set; } = 10;

    public int RateLimiterQueueLimit { get; set; } = 20;

    public int BulkheadMaxParallelization { get; set; } = 2;

    public int BulkheadMaxQueuedActions { get; set; } = 10;
}

public sealed class OperationWorkerOptions
{
    public const string SectionName = "Operations";

    public int ChannelCapacity { get; set; } = 100;
}
