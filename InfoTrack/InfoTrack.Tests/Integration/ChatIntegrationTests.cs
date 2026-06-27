using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace InfoTrack.Tests.Integration;

public sealed class ChatIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ChatIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Mcp:EnableAssistant"] = "true",
                        ["LmStudio:Enabled"] = "false",
                    });
                });
            })
            .CreateClient();
    }

    [Fact]
    public async Task Chat_WhenLmStudioDisabled_ReturnsDisabledMessage()
    {
        var response = await _client.PostAsJsonAsync("/api/chat", new
        {
            messages = new[]
            {
                new { role = "user", content = "How many firms do we have?" },
            },
        });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var payload = await response.Content.ReadFromJsonAsync<ChatPayload>();
        payload!.IsError.Should().BeTrue();
        payload.Reply.Should().Contain("LM Studio integration is disabled");
    }

    private sealed record ChatPayload(string Reply, IReadOnlyList<string> ToolsInvoked, bool IsError);
}
