using InfoTrack.Contracts.Discovery;
using InfoTrack.Domain.Discovery;

namespace InfoTrack.Application.Features.Discovery.RunDiscovery;

public sealed class RunDiscoveryHandler(IDiscoveryOrchestrator discoveryOrchestrator)
{
    public async Task<DiscoveryRunResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        var result = await discoveryOrchestrator.RunAsync(cancellationToken);
        return Map(result);
    }

    private static DiscoveryRunResponse Map(DiscoveryResult result) =>
        new(
            result.RunId,
            result.Source,
            result.CompletedAt - result.Statistics.Duration,
            result.CompletedAt,
            (long)result.Statistics.Duration.TotalMilliseconds,
            new DiscoveryStatisticsDto(
                result.Statistics.TotalDiscovered,
                result.Statistics.Added,
                result.Statistics.Updated,
                result.Statistics.Removed,
                result.Statistics.Skipped,
                result.Statistics.Existing,
                (long)result.Statistics.Duration.TotalMilliseconds),
            result.Locations
                .Select(x => new DiscoveredLocationDto(x.Slug, x.Name))
                .ToList());
}
