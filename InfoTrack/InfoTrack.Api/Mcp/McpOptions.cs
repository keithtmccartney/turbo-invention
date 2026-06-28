namespace InfoTrack.Api.Mcp;

public sealed class McpOptions
{
    public const string SectionName = "Mcp";

    public bool Enabled { get; init; } = true;

    public string ApiKey { get; init; } = string.Empty;

    public string ServerName { get; init; } = "InfoTrack Solicitor Intelligence";

    public string ServerVersion { get; init; } = "1.0";

    public bool RequireHttps { get; init; } = true;

    public bool EnableAssistant { get; init; }
}
