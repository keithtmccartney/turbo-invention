using System.Threading.Channels;
using InfoTrack.Domain.Operations;
using InfoTrack.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InfoTrack.Infrastructure.Operations;

/// <summary>
/// Bounded channel-backed queue isolates API request threads from long-running work.
/// </summary>
public sealed class OperationQueue : IOperationQueue
{
    private readonly Channel<OperationWorkItem> _channel;
    private readonly ILogger<OperationQueue> _logger;

    public OperationQueue(IOptions<OperationWorkerOptions> options, ILogger<OperationQueue> logger)
    {
        _logger = logger;
        var capacity = Math.Max(1, options.Value.ChannelCapacity);
        _channel = Channel.CreateBounded<OperationWorkItem>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public async ValueTask EnqueueAsync(OperationWorkItem workItem, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Enqueued operation {OperationId} kind={Kind} correlationId={CorrelationId}",
            workItem.OperationId,
            workItem.Kind,
            workItem.CorrelationId);

        await _channel.Writer.WriteAsync(workItem, cancellationToken);
    }

    public async IAsyncEnumerable<OperationWorkItem> ReadAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }
    }
}
