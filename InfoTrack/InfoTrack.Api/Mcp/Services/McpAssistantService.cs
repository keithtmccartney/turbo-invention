using System.Text.Json;
using InfoTrack.Api.Assistant;
using InfoTrack.Application.Mcp;
using InfoTrack.Domain.Scraping;
using Microsoft.Extensions.Options;

namespace InfoTrack.Api.Mcp.Services;

/// <summary>
/// Orchestrates natural-language queries via a local OpenAI-compatible LLM, using MCP tools to fetch live solicitor data.
/// </summary>
public sealed class McpAssistantService(
    ILocalLlmChatClient localLlmChatClient,
    IMcpToolRegistry toolRegistry,
    IOptions<LocalLlmOptions> localLlmOptions,
    ILogger<McpAssistantService> logger) : IMcpAssistantService
{
    public const string SystemPrompt =
        """
        You are the InfoTrack Solicitor Intelligence assistant.
        You help users understand UK conveyancing solicitor market data discovered and scraped by InfoTrack.

        Rules:
        - Always use the provided tools to retrieve current data. Never invent firm names, counts, or statistics.
        - Prefer get_statistics for overview and location firm-count questions; use search_firms to list firms in a location or find firms by name.
        - search_firms accepts location without a firm name query. get_statistics accepts an optional location argument.
        - Use discover_locations before scraping when the catalogue is empty or the user asks to refresh locations.
        - Use scrape_location or scrape_multiple_locations when the user wants fresh scrape data.
        - Summarise tool results clearly in plain English. Use the term "firms" consistently.
        - When totalFirms is 0, explain that no scrape has run yet and suggest discover_locations then scrape_location — do not offer to scrape unless the user asks.
        """;

    public async Task<McpAssistantResponse> CompleteAsync(
        McpAssistantRequest request,
        CancellationToken cancellationToken = default)
    {
        var options = localLlmOptions.Value;
        if (!options.Enabled)
        {
            return new McpAssistantResponse(
                "Local LLM integration is disabled. Set LocalLlm:Enabled to true and ensure your model server is running.",
                [],
                IsError: true);
        }

        var allowedTools = request.AllowedTools?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tools = McpOpenAiToolMapper.MapTools(toolRegistry.GetDefinitions(), allowedTools);
        if (tools.Count == 0)
        {
            return new McpAssistantResponse("No MCP tools are available for this request.", [], IsError: true);
        }

        var conversation = BuildConversation(request.Messages);
        var toolsInvoked = new List<string>();

        try
        {
            for (var round = 0; round < options.MaxToolRounds; round++)
            {
                var completion = await localLlmChatClient.CreateChatCompletionAsync(
                    new OpenAiChatCompletionRequest
                    {
                        Model = options.Model,
                        Messages = conversation,
                        Tools = tools,
                        ToolChoice = "auto",
                    },
                    cancellationToken);

                var choice = completion.Choices.FirstOrDefault()?.Message;
                if (choice is null)
                {
                    return new McpAssistantResponse("The local LLM returned no assistant message.", toolsInvoked, IsError: true);
                }

                if (choice.ToolCalls is not { Count: > 0 })
                {
                    var reply = string.IsNullOrWhiteSpace(choice.Content)
                        ? "The model returned an empty response."
                        : ScrapedTextNormalizer.Normalize(choice.Content.Trim()) ?? choice.Content.Trim();

                    return new McpAssistantResponse(
                        reply,
                        toolsInvoked,
                        IsError: string.IsNullOrWhiteSpace(choice.Content));
                }

                conversation.Add(new OpenAiChatMessage
                {
                    Role = "assistant",
                    Content = choice.Content,
                    ToolCalls = choice.ToolCalls,
                });

                foreach (var toolCall in choice.ToolCalls)
                {
                    var toolName = toolCall.Function.Name;
                    toolsInvoked.Add(toolName);

                    logger.LogInformation(
                        "Local LLM requested MCP tool {ToolName} (round {Round})",
                        toolName,
                        round + 1);

                    var arguments = ParseToolArguments(toolCall.Function.Arguments);
                    var execution = await toolRegistry.ExecuteAsync(toolName, arguments, cancellationToken);
                    var toolText = execution.Content.FirstOrDefault()?.Text ?? string.Empty;
                    toolText = TruncateToolResult(toolText, options.MaxToolResultCharacters);

                    if (execution.IsError)
                    {
                        toolText = $"Tool error: {toolText}";
                    }

                    conversation.Add(new OpenAiChatMessage
                    {
                        Role = "tool",
                        ToolCallId = toolCall.Id,
                        Content = toolText,
                    });
                }
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Local LLM request failed");
            return new McpAssistantResponse(
                $"Unable to reach the local LLM server at {options.BaseUrl}. Start your model server, load the configured model ({options.Model}), and try again.",
                toolsInvoked,
                IsError: true);
        }

        return new McpAssistantResponse(
            "I reached the maximum number of tool calls for this question. Please try a simpler query or run discovery/scrape first.",
            toolsInvoked);
    }

    private static List<OpenAiChatMessage> BuildConversation(IReadOnlyList<McpAssistantMessage> messages)
    {
        var conversation = new List<OpenAiChatMessage>
        {
            new() { Role = "system", Content = SystemPrompt },
        };

        var sanitized = messages
            .Where(message => !string.IsNullOrWhiteSpace(message.Content))
            .Where(message => !message.Content.StartsWith("Tools used:", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var firstUserIndex = sanitized.FindIndex(message =>
            message.Role.Equals("user", StringComparison.OrdinalIgnoreCase));

        if (firstUserIndex > 0)
        {
            sanitized = sanitized.Skip(firstUserIndex).ToList();
        }

        foreach (var message in sanitized)
        {
            if (!message.Role.Equals("user", StringComparison.OrdinalIgnoreCase)
                && !message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            conversation.Add(new OpenAiChatMessage
            {
                Role = message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user",
                Content = message.Content.Trim(),
            });
        }

        return conversation;
    }

    private static JsonElement? ParseToolArguments(string? rawArguments)
    {
        if (string.IsNullOrWhiteSpace(rawArguments) || rawArguments.Trim() == "{}")
        {
            return null;
        }

        using var document = JsonDocument.Parse(rawArguments);
        return document.RootElement.Clone();
    }

    private static string TruncateToolResult(string text, int maxCharacters)
    {
        if (text.Length <= maxCharacters)
        {
            return text;
        }

        return text[..maxCharacters] + "\n\n[Tool output truncated for the model context window.]";
    }
}
