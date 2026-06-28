using InfoTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace InfoTrack.Tests.Integration;

public sealed class RateLimitWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"InfoTrackRateLimitTests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Enabled"] = "true",
                ["RateLimiting:ReadPermitLimit"] = "5",
                ["RateLimiting:ReadWindowSeconds"] = "60",
                ["RateLimiting:WritePermitLimit"] = "3",
                ["RateLimiting:WriteWindowSeconds"] = "60",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<InfoTrackDbContext>>();
            services.RemoveAll<InfoTrackDbContext>();

            services.AddDbContext<InfoTrackDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
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
