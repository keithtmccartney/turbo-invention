using System.Diagnostics;
using InfoTrack.Domain.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Infrastructure.Operations;

/// <summary>
/// Single consumer background worker that dispatches queued operations to registered processors.
/// Designed for extension: add <see cref="IOperationProcessor"/> implementations for new job kinds.
/// </summary>
public sealed class OperationWorkerBackgroundService(
    IOperationQueue operationQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<OperationWorkerBackgroundService> logger) : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("InfoTrack.Operations");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Operation worker started");

        await foreach (var workItem in operationQueue.ReadAllAsync(stoppingToken))
        {
            using var activity = ActivitySource.StartActivity(
                $"operation.{workItem.Kind}",
                ActivityKind.Internal);

            activity?.SetTag("operation.id", workItem.OperationId);
            activity?.SetTag("operation.kind", workItem.Kind.ToString());
            activity?.SetTag("correlation.id", workItem.CorrelationId);

            using var scope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["OperationId"] = workItem.OperationId,
                ["OperationKind"] = workItem.Kind.ToString(),
                ["CorrelationId"] = workItem.CorrelationId,
            });

            try
            {
                logger.LogInformation(
                    "Processing operation {OperationId} kind={Kind}",
                    workItem.OperationId,
                    workItem.Kind);

                await using var serviceScope = scopeFactory.CreateAsyncScope();
                var processors = serviceScope.ServiceProvider.GetServices<IOperationProcessor>();
                var processor = processors.SingleOrDefault(x => x.Kind == workItem.Kind);
                if (processor is null)
                {
                    logger.LogError(
                        "No processor registered for operation {OperationId} kind={Kind}",
                        workItem.OperationId,
                        workItem.Kind);
                    continue;
                }

                await processor.ProcessAsync(workItem, stoppingToken);

                logger.LogInformation(
                    "Completed operation {OperationId} kind={Kind}",
                    workItem.OperationId,
                    workItem.Kind);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Operation worker shutting down");
                throw;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                logger.LogError(
                    ex,
                    "Operation {OperationId} kind={Kind} failed in worker",
                    workItem.OperationId,
                    workItem.Kind);
            }
        }
    }
}
