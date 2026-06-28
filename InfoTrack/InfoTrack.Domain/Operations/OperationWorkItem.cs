namespace InfoTrack.Domain.Operations;

/// <summary>
/// Immutable envelope passed through the operation channel to background workers.
/// </summary>
public sealed record OperationWorkItem(
    Guid OperationId,
    OperationKind Kind,
    string CorrelationId);
