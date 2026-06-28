namespace InfoTrack.Infrastructure.Resilience;

/// <summary>Named Polly resilience pipelines registered with <see cref="Microsoft.Extensions.DependencyInjection.IHttpClientBuilder"/>.</summary>
public static class ResiliencePipelineNames
{
    public const string Scraping = "Scraping";

    public const string Discovery = "Discovery";
}
