using Microsoft.Extensions.DependencyInjection;

namespace InfoTrack.Infrastructure.Resilience;

/// <summary>
/// Resolves scoped progress notifiers inside a DI scope because Polly resilience handlers are singletons
/// while progress reporters depend on scoped repositories/DbContext.
/// </summary>
public sealed class ScopedResilienceProgressNotifier(IServiceScopeFactory scopeFactory) : IResilienceProgressNotifier
{
    public async Task NotifyAsync(ResilienceProgressNotification notification, CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DiscoveryResilienceProgressNotifier>()
            .NotifyAsync(notification, cancellationToken);
        await scope.ServiceProvider.GetRequiredService<ScrapeResilienceProgressNotifier>()
            .NotifyAsync(notification, cancellationToken);
    }
}
