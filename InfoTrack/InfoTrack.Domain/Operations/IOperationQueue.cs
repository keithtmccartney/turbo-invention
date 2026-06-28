namespace InfoTrack.Domain.Operations;

public interface IOperationQueue
{
    ValueTask EnqueueAsync(OperationWorkItem workItem, CancellationToken cancellationToken = default);

    IAsyncEnumerable<OperationWorkItem> ReadAllAsync(CancellationToken cancellationToken);
}
