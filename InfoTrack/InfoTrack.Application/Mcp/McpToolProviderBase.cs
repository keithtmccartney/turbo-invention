using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace InfoTrack.Application.Mcp;

public abstract class McpToolProviderBase : IMcpToolProvider
{
    private readonly McpToolDefinition _definition;

    protected McpToolProviderBase()
    {
        var attribute = GetType().GetCustomAttribute<McpToolAttribute>()
            ?? throw new InvalidOperationException(
                $"{GetType().Name} must be decorated with [{nameof(McpToolAttribute)}].");

        _definition = new McpToolDefinition(
            attribute.Name,
            attribute.Description,
            BuildInputSchema());
    }

    public McpToolDefinition Definition => _definition;

    public abstract Task<McpToolExecutionResult> ExecuteAsync(
        JsonElement? arguments,
        CancellationToken cancellationToken = default);

    protected abstract JsonObject BuildInputSchema();

    protected static JsonElement? GetArguments(JsonElement? arguments) =>
        arguments is { ValueKind: JsonValueKind.Object } ? arguments : null;

    protected static string? GetOptionalString(JsonElement arguments, string propertyName) =>
        arguments.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    protected static string GetRequiredString(JsonElement arguments, string propertyName)
    {
        var value = GetOptionalString(arguments, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Argument '{propertyName}' is required.", propertyName);
        }

        return value.Trim();
    }

    protected static IReadOnlyList<string> GetRequiredStringArray(JsonElement arguments, string propertyName)
    {
        if (!arguments.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException($"Argument '{propertyName}' must be a non-empty array.", propertyName);
        }

        var items = value.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString()?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (items.Count == 0)
        {
            throw new ArgumentException($"Argument '{propertyName}' must contain at least one value.", propertyName);
        }

        return items;
    }

    protected static Guid? GetOptionalGuid(JsonElement arguments, string propertyName)
    {
        var raw = GetOptionalString(arguments, propertyName);
        return Guid.TryParse(raw, out var guid) ? guid : null;
    }
}
