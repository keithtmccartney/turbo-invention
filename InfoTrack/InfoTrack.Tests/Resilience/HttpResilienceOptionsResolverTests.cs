using FluentAssertions;
using InfoTrack.Infrastructure.Resilience;
using InfoTrack.Infrastructure.Resilience.Options;
using Microsoft.Extensions.Options;

namespace InfoTrack.Tests.Resilience;

public sealed class HttpResilienceOptionsResolverTests
{
    [Fact]
    public void Resolve_MergesDefaultsClientOverrideAndLegacy()
    {
        var resolver = new HttpResilienceOptionsResolver(Options.Create(new ResilienceOptions
        {
            Defaults = new HttpResilienceClientOptions
            {
                MaxRetries = 5,
                BaseDelaySeconds = 2,
                RequestTimeoutSeconds = 30,
                MaxConcurrentRequests = 3,
            },
            Clients =
            {
                ["Discovery"] = new HttpResilienceClientOptions
                {
                    MaxConcurrentRequests = 2,
                    RequestsPerSecond = 8,
                },
            },
        }));

        var discovery = resolver.Resolve(
            "Discovery",
            new HttpResilienceClientOptions
            {
                MaxRetries = 3,
                CircuitBreakerDurationSeconds = 45,
            });

        discovery.MaxRetries.Should().Be(3);
        discovery.BaseDelaySeconds.Should().Be(2);
        discovery.MaxConcurrentRequests.Should().Be(2, "client override should win over defaults");
        discovery.CircuitBreakerDurationSeconds.Should().Be(45);
    }
}
