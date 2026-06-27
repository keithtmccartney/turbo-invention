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
    public async Task UpdateLocations_ReplacesLocationList()
    {
        var request = new UpdateLocationsRequest(["Cardiff", "London"]);

        var response = await _client.PostAsJsonAsync("/api/locations", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LocationsResponse>();
        payload!.Locations.Should().HaveCount(2);
        payload.Locations.Select(x => x.Name).Should().BeEquivalentTo(["Cardiff", "London"]);
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
