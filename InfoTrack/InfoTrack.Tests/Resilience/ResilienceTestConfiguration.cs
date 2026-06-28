namespace InfoTrack.Tests.Resilience;

internal static class ResilienceTestConfiguration
{
    public const string TestBaseUrl = "https://resilience.test/";

    /// <summary>
    /// Fast retries so integration tests finish quickly while still exercising Polly backoff.
    /// </summary>
    public static IReadOnlyDictionary<string, string?> FastRetries => new Dictionary<string, string?>
    {
        ["Resilience:Defaults:MaxRetries"] = "5",
        ["Resilience:Defaults:BaseDelaySeconds"] = "0",
        ["Resilience:Defaults:MaxDelaySeconds"] = "1",
        ["Resilience:Defaults:RespectRetryAfter"] = "true",
        ["Resilience:Clients:Scraping:RequestsPerSecond"] = "100",
        ["Resilience:Clients:Discovery:RequestsPerSecond"] = "100",
        ["Scraping:BaseUrl"] = TestBaseUrl,
        ["Scraping:UserAgent"] = "InfoTrack-Tests/1.0",
        ["Scraping:RequestTimeoutSeconds"] = "30",
        ["Discovery:BaseUrl"] = TestBaseUrl,
        ["Discovery:ConveyancingSitemapPath"] = "/google-sitemap4.xml",
        ["Discovery:UserAgent"] = "InfoTrack-Tests/1.0",
        ["Discovery:RequestTimeoutSeconds"] = "30",
    };
}
