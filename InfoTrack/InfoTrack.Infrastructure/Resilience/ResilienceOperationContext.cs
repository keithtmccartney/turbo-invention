namespace InfoTrack.Infrastructure.Resilience;

/// <summary>
/// Correlates in-flight HTTP calls with long-running operations for progress reporting.
/// Scrape and discovery orchestrators run sequentially; AsyncLocal is sufficient without threading run ids through Polly internals.
/// </summary>
public static class ResilienceOperationContext
{
    private static readonly AsyncLocal<Guid?> CurrentOperationId = new();

    public static Guid? Current => CurrentOperationId.Value;

    public static IDisposable BeginScope(Guid? operationId)
    {
        CurrentOperationId.Value = operationId;
        return new Scope();
    }

    private sealed class Scope : IDisposable
    {
        public void Dispose() => CurrentOperationId.Value = null;
    }
}
