using InfoTrack.Api.Assistant;
using InfoTrack.Api.Mcp.Authentication;
using InfoTrack.Api.Mcp.Services;
using Microsoft.Extensions.Options;

namespace InfoTrack.Api.Mcp;

public static class McpDependencyInjection
{
    public static IServiceCollection AddMcpServer(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<McpOptions>(configuration.GetSection(McpOptions.SectionName));
        services.Configure<LmStudioOptions>(configuration.GetSection(LmStudioOptions.SectionName));
        services.AddSingleton<McpApiKeyValidator>();
        services.AddScoped<McpJsonRpcDispatcher>();

        services.AddHttpClient<ILmStudioChatClient, LmStudioChatClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<LmStudioOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        });

        var enableAssistant = configuration.GetValue("Mcp:EnableAssistant", false);
        if (enableAssistant)
        {
            services.AddScoped<IMcpAssistantService, McpAssistantService>();
        }
        else
        {
            services.AddScoped<IMcpAssistantService, DisabledMcpAssistantService>();
        }

        return services;
    }
}
