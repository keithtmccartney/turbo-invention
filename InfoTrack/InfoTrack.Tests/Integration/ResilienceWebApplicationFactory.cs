using InfoTrack.Infrastructure.Persistence;
using InfoTrack.Tests.Resilience;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;

namespace InfoTrack.Tests.Integration;

public sealed class ResilienceWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"InfoTrackResilienceTests-{Guid.NewGuid():N}";

    public StubHttpMessageHandlerBuilderFilter HandlerFilter { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(ResilienceTestConfiguration.FastRetries);
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Enabled"] = "false",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<InfoTrackDbContext>>();
            services.RemoveAll<InfoTrackDbContext>();

            services.AddDbContext<InfoTrackDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.AddSingleton(HandlerFilter);
            services.AddSingleton<IHttpMessageHandlerBuilderFilter>(sp =>
                sp.GetRequiredService<StubHttpMessageHandlerBuilderFilter>());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<InfoTrackDbContext>().Database.EnsureCreated();

        return host;
    }
}
