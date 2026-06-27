using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InfoTrack.Contracts.Discovery;
using InfoTrack.Contracts.Locations;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InfoTrack.Tests.Integration;

public sealed class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GetLocations_WhenFresh_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/locations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LocationsResponse>();
        payload!.Locations.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateLocations_ActivatesSelectionAndDeactivatesOthers()
    {
        await _client.PostAsJsonAsync("/api/locations", new UpdateLocationsRequest(["Cardiff", "London", "Manchester"]));

        var response = await _client.PostAsJsonAsync("/api/locations", new UpdateLocationsRequest(["Cardiff", "London"]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LocationsResponse>();
        payload!.Locations.Should().HaveCount(3);
        payload.Locations.Where(x => x.IsActive).Select(x => x.Name).Should().BeEquivalentTo(["Cardiff", "London"]);
        payload.Locations.Single(x => x.Name == "Manchester").IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateLocations_WithEmptySelection_DeactivatesAllLocations()
    {
        await _client.PostAsJsonAsync("/api/locations", new UpdateLocationsRequest(["Cardiff", "London"]));

        var response = await _client.PostAsJsonAsync("/api/locations", new UpdateLocationsRequest([]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LocationsResponse>();
        payload!.Locations.Should().HaveCount(2);
        payload.Locations.Should().OnlyContain(x => !x.IsActive);
    }

    [Fact]
    public async Task RunScrape_WithNoActiveLocations_ReturnsBadRequest()
    {
        var response = await _client.PostAsync("/api/scrape", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("No active locations configured");
    }

    [Fact]
    public async Task GetDiscoverySummary_ReturnsActiveLocationCount()
    {
        var response = await _client.GetAsync("/api/discovery/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<DiscoverySummaryDto>();
        payload!.ActiveLocationCount.Should().Be(0);
    }

    [Fact]
    public async Task GetLatestDiscoveryRun_WithNoRuns_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/discovery/runs/latest");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetInsights_WithNoScrape_ReturnsEmptyDashboard()
    {
        var response = await _client.GetAsync("/api/insights");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("\"totalFirms\":0");
        json.Should().Contain("\"discovery\"");
    }
}
