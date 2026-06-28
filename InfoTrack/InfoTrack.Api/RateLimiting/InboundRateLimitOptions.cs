namespace InfoTrack.Api.RateLimiting;

public sealed class InboundRateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    /// <summary>GET/read endpoints — permits per client IP per window.</summary>
    public int ReadPermitLimit { get; set; } = 600;

    public int ReadWindowSeconds { get; set; } = 60;

    public int ReadQueueLimit { get; set; }

    /// <summary>POST/write endpoints — tighter cap for scrape/discovery/location mutations.</summary>
    public int WritePermitLimit { get; set; } = 30;

    public int WriteWindowSeconds { get; set; } = 60;

    public int WriteQueueLimit { get; set; }
}

public static class InboundRateLimitPolicies
{
    public const string ApiReads = "api-reads";

    public const string ApiWrites = "api-writes";
}
