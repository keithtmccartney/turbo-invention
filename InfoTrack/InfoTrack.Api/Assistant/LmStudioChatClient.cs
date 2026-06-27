using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace InfoTrack.Api.Assistant;

public sealed class LmStudioChatClient(
    HttpClient httpClient,
    IOptions<LmStudioOptions> options,
    ILogger<LmStudioChatClient> logger) : ILmStudioChatClient
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
            throw new InvalidOperationException("LM Studio integration is disabled.");
        }

        logger.LogDebug(
            "Calling LM Studio chat completions for model {Model} with {MessageCount} messages",
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
                $"LM Studio request failed ({(int)response.StatusCode}): {body}");
        }

        return JsonSerializer.Deserialize<OpenAiChatCompletionResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("LM Studio returned an empty response.");
    }
}
