namespace InfoTrack.Domain.Operations;

/// <summary>
/// Executes a single kind of long-running operation. Register one processor per <see cref="OperationKind"/>.
/// </summary>
public interface IOperationProcessor
{
    OperationKind Kind { get; }

    Task ProcessAsync(OperationWorkItem workItem, CancellationToken cancellationToken);
}
