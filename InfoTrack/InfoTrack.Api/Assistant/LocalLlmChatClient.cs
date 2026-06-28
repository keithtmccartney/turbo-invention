using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace InfoTrack.Api.Assistant;

public sealed class LocalLlmChatClient(
    HttpClient httpClient,
    IOptions<LocalLlmOptions> options,
    ILogger<LocalLlmChatClient> logger) : ILocalLlmChatClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<OpenAiChatCompletionResponse> CreateChatCompletionAsync(
        OpenAiChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
        {
            throw new InvalidOperationException("Local LLM integration is disabled.");
        }

        logger.LogDebug(
            "Calling local LLM chat completions for model {Model} with {MessageCount} messages",
            request.Model,
            request.Messages.Count);

        using var response = await httpClient.PostAsJsonAsync(
            "v1/chat/completions",
            request,
            JsonOptions,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Local LLM request failed ({(int)response.StatusCode}): {body}");
        }

        return JsonSerializer.Deserialize<OpenAiChatCompletionResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("Local LLM returned an empty response.");
    }
}
