using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InfoTrack.Api.Mcp.JsonRpc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace InfoTrack.Tests.Integration;

public sealed class McpIntegrationTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private const string ApiKey = "integration-test-mcp-key";
    private readonly HttpClient _client;

    public McpIntegrationTests(IsolatedWebApplicationFactory factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Mcp:Enabled"] = "true",
                        ["Mcp:ApiKey"] = ApiKey,
                        ["Mcp:RequireHttps"] = "false",
                        ["Mcp:EnableAssistant"] = "true",
                    });
                });
            })
            .CreateClient();
    }

    [Fact]
    public async Task McpToolsList_WithoutApiKey_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/mcp/tools");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task McpToolsList_WithApiKey_ReturnsTenTools()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/mcp/tools");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("discover_locations");
        json.Should().Contain("get_report");
    }

    [Fact]
    public async Task McpJsonRpc_ToolsList_ReturnsDefinitions()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/mcp")
        {
            Content = JsonContent.Create(new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "tools/list",
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonRpcResponse>();
        payload!.Error.Should().BeNull();
        payload.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task McpJsonRpc_GetStatistics_ReturnsSuccess()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/mcp")
        {
            Content = JsonContent.Create(new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "tools/call",
                @params = new
                {
                    name = "get_statistics",
                    arguments = new { },
                },
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("totalFirms");
    }
}
