namespace InfoTrack.Api.Assistant;

public interface ILmStudioChatClient
{
    Task<OpenAiChatCompletionResponse> CreateChatCompletionAsync(
        OpenAiChatCompletionRequest request,
        CancellationToken cancellationToken = default);
}
