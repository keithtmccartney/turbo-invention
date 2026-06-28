namespace InfoTrack.Infrastructure.Options;

public sealed class ScrapingOptions
{
    public const string SectionName = "Scraping";

    public string BaseUrl { get; set; } = "https://www.solicitors.com";

    public string EntryPath { get; set; } = "/conveyancing.html";

    public string LocationPathTemplate { get; set; } = "/conveyancing+{location}.html";

    public string UserAgent { get; set; } = "InfoTrack-Assessment/1.0 (+https://github.com/infotrack)";

    public int RequestDelayMilliseconds { get; set; } = 250;

    public int RequestTimeoutSeconds { get; set; } = 30;

    public ScrapingResilienceOptions Resilience { get; set; } = new();
}

public sealed class ScrapingResilienceOptions
{
    public int MaxRetryAttempts { get; set; } = 3;

    public int RetryDelayMilliseconds { get; set; } = 500;

    public int MaxRetryDelayMilliseconds { get; set; } = 5000;

    public int CircuitBreakerFailureRatio { get; set; } = 50;

    public int CircuitBreakerMinimumThroughput { get; set; } = 5;

    public int CircuitBreakerBreakDurationSeconds { get; set; } = 30;
}
