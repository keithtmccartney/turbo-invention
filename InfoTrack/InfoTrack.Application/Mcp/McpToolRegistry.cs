using System.Collections.Frozen;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Application.Mcp;

public interface IMcpToolRegistry
{
    IReadOnlyList<McpToolDefinition> GetDefinitions();

    Task<McpToolExecutionResult> ExecuteAsync(
        string toolName,
        System.Text.Json.JsonElement? arguments,
        CancellationToken cancellationToken = default);
}

public sealed class McpToolRegistry : IMcpToolRegistry
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<McpToolRegistry> _logger;
    private readonly FrozenDictionary<string, McpToolRegistration> _tools;

    public McpToolRegistry(
        IServiceScopeFactory scopeFactory,
        IReadOnlyList<Type> toolTypes,
        ILogger<McpToolRegistry> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _tools = BuildRegistrationMap(toolTypes);
    }

    public IReadOnlyList<McpToolDefinition> GetDefinitions() =>
        _tools.Values
            .Select(x => x.Definition)
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public async Task<McpToolExecutionResult> ExecuteAsync(
        string toolName,
        System.Text.Json.JsonElement? arguments,
        CancellationToken cancellationToken = default)
    {
        if (!_tools.TryGetValue(toolName, out var registration))
        {
            return McpToolExecutionResult.Error($"Unknown tool '{toolName}'.");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var provider = (IMcpToolProvider)scope.ServiceProvider.GetRequiredService(registration.ImplementationType);

        try
        {
            _logger.LogInformation("Executing MCP tool {ToolName}", toolName);
            return await provider.ExecuteAsync(arguments, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation failed for MCP tool {ToolName}", toolName);
            return McpToolExecutionResult.Error(ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "External dependency failed for MCP tool {ToolName}", toolName);
            return McpToolExecutionResult.Error($"Upstream request failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled failure for MCP tool {ToolName}", toolName);
            return McpToolExecutionResult.Error($"Tool execution failed: {ex.Message}");
        }
    }

    private FrozenDictionary<string, McpToolRegistration> BuildRegistrationMap(IReadOnlyList<Type> toolTypes)
    {
        using var scope = _scopeFactory.CreateScope();
        var registrations = new List<McpToolRegistration>(toolTypes.Count);

        foreach (var type in toolTypes)
        {
            var provider = (IMcpToolProvider)scope.ServiceProvider.GetRequiredService(type);
            registrations.Add(new McpToolRegistration(type, provider.Definition));
        }

        return registrations.ToFrozenDictionary(x => x.Definition.Name, StringComparer.OrdinalIgnoreCase);
    }
}

public sealed record McpToolRegistration(Type ImplementationType, McpToolDefinition Definition);

public static class McpServiceCollectionExtensions
{
    public static IServiceCollection AddMcpTools(
        this IServiceCollection services,
        Assembly? assembly = null)
    {
        assembly ??= typeof(McpServiceCollectionExtensions).Assembly;
        var toolTypes = DiscoverToolTypes(assembly);

        foreach (var type in toolTypes)
        {
            services.AddScoped(type);
        }

        services.AddSingleton<IReadOnlyList<Type>>(toolTypes);
        services.AddSingleton<IMcpToolRegistry, McpToolRegistry>();

        return services;
    }

    public static IReadOnlyList<Type> DiscoverToolTypes(Assembly assembly) =>
        assembly.GetTypes()
            .Where(type =>
                !type.IsAbstract
                && typeof(IMcpToolProvider).IsAssignableFrom(type)
                && type.GetCustomAttribute<McpToolAttribute>() is not null)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToList();
}
