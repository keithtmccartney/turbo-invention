using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InfoTrack.Contracts.Locations;
using InfoTrack.Contracts.Scraping;
using InfoTrack.Tests.Resilience;

namespace InfoTrack.Tests.Integration;

/// <summary>
/// End-to-end proof that scrape operations survive upstream HTTP 429 via outbound Polly retries.
/// </summary>
public sealed class OutboundResilienceApiIntegrationTests : IClassFixture<ResilienceWebApplicationFactory>
{
    private readonly ResilienceWebApplicationFactory _factory;

    public OutboundResilienceApiIntegrationTests(ResilienceWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task StartScrape_WhenUpstreamReturns429Then200_CompletesSuccessfully()
    {
        var londonHtml = await File.ReadAllTextAsync(Path.Combine("TestData", "london-sample.html"));
        var scrapeHandler = new SequenceResponseHandler(
            () => SequenceResponseHandler.TooManyRequests(retryAfterSeconds: 1),
            () => SequenceResponseHandler.Ok(londonHtml));

        _factory.HandlerFilter.RegisterHandler("SolicitorsScrapeClient", scrapeHandler);

        using var client = _factory.CreateClient();

        var locationsResponse = await client.PostAsJsonAsync("/api/locations", new UpdateLocationsRequest(["London"]));
        locationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var startResponse = await client.PostAsync("/api/scrape", null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var started = await startResponse.Content.ReadFromJsonAsync<StartScrapeResponse>();
        started.Should().NotBeNull();

        var status = await WaitForScrapeCompletionAsync(client, started!.OperationId);
        status.Status.Should().Be("Completed");
        status.Result.Should().NotBeNull();
        status.Result!.TotalFirms.Should().BeGreaterThan(0);

        scrapeHandler.RequestCount.Should().BeGreaterThanOrEqualTo(2);
    }

    private static async Task<ScrapeRunStatusResponse> WaitForScrapeCompletionAsync(
        HttpClient client,
        Guid operationId,
        TimeSpan? timeout = null)
    {
        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));

        while (DateTimeOffset.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/api/scrape/runs/{operationId}/status");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var status = await response.Content.ReadFromJsonAsync<ScrapeRunStatusResponse>();
            status.Should().NotBeNull();

            if (status!.Status == "Completed")
            {
                return status;
            }

            if (status.Status == "Failed")
            {
                throw new InvalidOperationException(status.ErrorMessage ?? "Scrape failed.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        throw new TimeoutException($"Scrape operation {operationId} did not complete in time.");
    }
}
