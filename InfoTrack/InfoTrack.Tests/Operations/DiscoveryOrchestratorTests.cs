using FluentAssertions;
using InfoTrack.Contracts.Discovery;
using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Operations;
using InfoTrack.Domain.Repositories;
using InfoTrack.Infrastructure.Discovery;
using InfoTrack.Infrastructure.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Tests.Operations;

public sealed class DiscoveryOrchestratorTests
{
    [Fact]
    public async Task StartAsync_EnqueuesOperationAndReturnsOperationId()
    {
        var queue = new CapturingOperationQueue();
        var repository = new FakeDiscoveryRunRepository();
        var orchestrator = CreateOrchestrator(queue, repository);

        var operationId = await orchestrator.StartAsync("corr-123");

        operationId.Should().NotBeEmpty();
        queue.Items.Should().ContainSingle(x =>
            x.OperationId == operationId &&
            x.Kind == OperationKind.Discovery &&
            x.CorrelationId == "corr-123");
        repository.Runs.Should().ContainSingle(x => x.Id == operationId && x.Status == DiscoveryRunStatus.Queued);
    }

    [Fact]
    public async Task StartAsync_WhenActiveRunExists_Throws()
    {
        var queue = new CapturingOperationQueue();
        var repository = new FakeDiscoveryRunRepository
        {
            HasActive = true,
        };
        var orchestrator = CreateOrchestrator(queue, repository);

        var act = () => orchestrator.StartAsync("corr-123");

        await act.Should().ThrowAsync<InvalidOperationException>();
        queue.Items.Should().BeEmpty();
    }

    private static DiscoveryOrchestrator CreateOrchestrator(
        CapturingOperationQueue queue,
        FakeDiscoveryRunRepository repository) =>
        new(
            new FakeDiscoveryProvider(),
            new NoOpLocationRepository(),
            repository,
            new NoOpProgressReporter(),
            queue,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DiscoveryOrchestrator>.Instance);

    private sealed class CapturingOperationQueue : IOperationQueue
    {
        public List<OperationWorkItem> Items { get; } = [];

        public ValueTask EnqueueAsync(OperationWorkItem workItem, CancellationToken cancellationToken = default)
        {
            Items.Add(workItem);
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<OperationWorkItem> ReadAllAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class FakeDiscoveryRunRepository : IDiscoveryRunRepository
    {
        public List<DiscoveryRun> Runs { get; } = [];

        public bool HasActive { get; set; }

        public Task AddAsync(DiscoveryRun run, CancellationToken cancellationToken = default)
        {
            Runs.Add(run);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(DiscoveryRun run, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<DiscoveryRun?> GetLatestCompletedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<DiscoveryRun?>(null);

        public Task<DiscoveryRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Runs.FirstOrDefault(x => x.Id == id));

        public Task<bool> HasActiveRunAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(HasActive);

        public Task<IReadOnlyList<DiscoveryRun>> GetHistoryAsync(int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DiscoveryRun>>(Runs);
    }

    private sealed class FakeDiscoveryProvider : IDiscoveryProvider
    {
        public string SourceName => "Test";

        public Task<IReadOnlyList<DiscoveredLocation>> DiscoverAsync(
            Guid runId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DiscoveredLocation>>([]);
    }

    private sealed class NoOpLocationRepository : ILocationRepository
    {
        public Task<IReadOnlyList<Location>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Location>>([]);

        public Task<IReadOnlyList<Location>> GetActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Location>>([]);

        public Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task ReplaceAllAsync(IReadOnlyList<Location> locations, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<Location>> SetActiveLocationsAsync(
            IReadOnlyList<string> activeNames,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Location>>([]);

        public Task<DiscoverySyncOutcome> SyncDiscoveredLocationsAsync(
            IReadOnlyList<DiscoveredLocation> discoveredLocations,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new DiscoverySyncOutcome(0, 0, 0, 0, 0));
    }

    private sealed class NoOpProgressReporter : IDiscoveryProgressReporter
    {
        public Task ReportAsync(Guid runId, DiscoveryProgressUpdate update, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
