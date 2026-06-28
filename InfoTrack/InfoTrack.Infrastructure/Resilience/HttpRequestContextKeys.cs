namespace InfoTrack.Infrastructure.Resilience;

/// <summary>
/// Per-request metadata propagated through <see cref="HttpRequestMessage.Options"/>
/// so resilience callbacks can correlate retries with long-running operations.
/// </summary>
public static class HttpRequestContextKeys
{
    public static readonly HttpRequestOptionsKey<Guid?> OperationId =
        new("InfoTrack.Resilience.OperationId");

    public static readonly HttpRequestOptionsKey<string?> ClientName =
        new("InfoTrack.Resilience.ClientName");

    public static readonly HttpRequestOptionsKey<string?> OperationKind =
        new("InfoTrack.Resilience.OperationKind");
}
