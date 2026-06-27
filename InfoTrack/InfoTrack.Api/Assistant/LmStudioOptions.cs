namespace InfoTrack.Api.Assistant;

public sealed class LmStudioOptions
{
    public const string SectionName = "LmStudio";

    public bool Enabled { get; init; } = true;

    public string BaseUrl { get; init; } = "http://localhost:1234";

    public string Model { get; init; } = "qwen2.5-3b-instruct";

    public int MaxToolRounds { get; init; } = 5;

    public int MaxToolResultCharacters { get; init; } = 12_000;

    public int RequestTimeoutSeconds { get; init; } = 120;
}
