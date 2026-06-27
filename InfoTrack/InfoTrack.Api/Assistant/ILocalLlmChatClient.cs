namespace InfoTrack.Api.Assistant;

public interface ILocalLlmChatClient
{
    Task<OpenAiChatCompletionResponse> CreateChatCompletionAsync(
        OpenAiChatCompletionRequest request,
        CancellationToken cancellationToken = default);
}
