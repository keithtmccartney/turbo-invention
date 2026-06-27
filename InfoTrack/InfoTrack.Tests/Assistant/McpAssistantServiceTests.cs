using FluentAssertions;
using InfoTrack.Api.Assistant;
using InfoTrack.Api.Mcp.Services;
using InfoTrack.Application.Mcp;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InfoTrack.Tests.Assistant;

public sealed class McpAssistantServiceTests
{
    [Fact]
    public async Task CompleteAsync_ExecutesToolThenReturnsModelSummary()
    {
        var registry = new FakeToolRegistry();
        var lmStudio = new FakeLmStudioChatClient([
            new OpenAiChatCompletionResponse
            {
                Choices =
                [
                    new OpenAiChatChoice
                    {
                        FinishReason = "tool_calls",
                        Message = new OpenAiChatMessage
                        {
                            Role = "assistant",
                            ToolCalls =
                            [
                                new OpenAiToolCall
                                {
                                    Id = "call-1",
                                    Function = new OpenAiToolCallFunction
                                    {
                                        Name = "get_statistics",
                                        Arguments = "{}",
                                    },
                                },
                            ],
                        },
                    },
                ],
            },
            new OpenAiChatCompletionResponse
            {
                Choices =
                [
                    new OpenAiChatChoice
                    {
                        FinishReason = "stop",
                        Message = new OpenAiChatMessage
                        {
                            Role = "assistant",
                            Content = "You currently have 12 firms across 2 locations.",
                        },
                    },
                ],
            },
        ]);

        var service = new McpAssistantService(
            lmStudio,
            registry,
            Options.Create(new LmStudioOptions { Enabled = true, Model = "test-model" }),
            NullLogger<McpAssistantService>.Instance);

        var response = await service.CompleteAsync(
            new McpAssistantRequest([new McpAssistantMessage("user", "How many firms do we have?")]));

        response.Reply.Should().Be("You currently have 12 firms across 2 locations.");
        response.ToolsInvoked.Should().Equal("get_statistics");
        response.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task CompleteAsync_WhenLmStudioUnreachable_ReturnsErrorResponse()
    {
        var registry = new FakeToolRegistry();
        var lmStudio = new ThrowingLmStudioChatClient(new HttpRequestException("Connection refused"));

        var service = new McpAssistantService(
            lmStudio,
            registry,
            Options.Create(new LmStudioOptions
            {
                Enabled = true,
                Model = "test-model",
                BaseUrl = "http://localhost:1234",
            }),
            NullLogger<McpAssistantService>.Instance);

        var response = await service.CompleteAsync(
            new McpAssistantRequest([new McpAssistantMessage("user", "Search for Smith")]));

        response.IsError.Should().BeTrue();
        response.Reply.Should().Contain("http://localhost:1234");
        response.ToolsInvoked.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteAsync_IgnoresUiIntroBeforeFirstUserMessage()
    {
        var registry = new FakeToolRegistry();
        var lmStudio = new CapturingLmStudioChatClient(
            new OpenAiChatCompletionResponse
            {
                Choices =
                [
                    new OpenAiChatChoice
                    {
                        FinishReason = "stop",
                        Message = new OpenAiChatMessage { Role = "assistant", Content = "Zero firms." },
                    },
                ],
            });

        var service = new McpAssistantService(
            lmStudio,
            registry,
            Options.Create(new LmStudioOptions { Enabled = true, Model = "test-model" }),
            NullLogger<McpAssistantService>.Instance);

        await service.CompleteAsync(new McpAssistantRequest(
        [
            new McpAssistantMessage("assistant", "Welcome! Ask me anything."),
            new McpAssistantMessage("user", "How many firms?"),
        ]));

        lmStudio.LastRequest!.Messages.Should().NotContain(message =>
            message.Role == "assistant" && message.Content!.Contains("Welcome"));
        lmStudio.LastRequest.Messages.Should().Contain(message =>
            message.Role == "user" && message.Content == "How many firms?");
    }

    private sealed class CapturingLmStudioChatClient(OpenAiChatCompletionResponse response) : ILmStudioChatClient
    {
        public OpenAiChatCompletionRequest? LastRequest { get; private set; }

        public Task<OpenAiChatCompletionResponse> CreateChatCompletionAsync(
            OpenAiChatCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(response);
        }
    }

    private sealed class FakeToolRegistry : IMcpToolRegistry
    {
        public IReadOnlyList<McpToolDefinition> GetDefinitions() =>
        [
            new("get_statistics", "Stats", McpToolSchemaBuilder.EmptyObject()),
        ];

        public Task<McpToolExecutionResult> ExecuteAsync(
            string toolName,
            System.Text.Json.JsonElement? arguments,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(McpToolExecutionResult.Success("{\"totalFirms\":12,\"locationsSearched\":2}"));
    }

    private sealed class ThrowingLmStudioChatClient(Exception exception) : ILmStudioChatClient
    {
        public Task<OpenAiChatCompletionResponse> CreateChatCompletionAsync(
            OpenAiChatCompletionRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromException<OpenAiChatCompletionResponse>(exception);
    }

    private sealed class FakeLmStudioChatClient(IReadOnlyList<OpenAiChatCompletionResponse> responses) : ILmStudioChatClient
    {
        private int _index;

        public Task<OpenAiChatCompletionResponse> CreateChatCompletionAsync(
            OpenAiChatCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (_index >= responses.Count)
            {
                throw new InvalidOperationException("No more fake LM Studio responses configured.");
            }

            return Task.FromResult(responses[_index++]);
        }
    }
}
