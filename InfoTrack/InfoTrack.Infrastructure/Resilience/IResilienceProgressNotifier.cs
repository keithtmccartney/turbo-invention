namespace InfoTrack.Infrastructure.Resilience;

public interface IResilienceProgressNotifier
{
    Task NotifyAsync(ResilienceProgressNotification notification, CancellationToken cancellationToken = default);
}
