using FluentAssertions;
using InfoTrack.Domain.Analytics;
using InfoTrack.Domain.Discovery;
using InfoTrack.Domain.Entities;
using InfoTrack.Domain.Operations;
using InfoTrack.Domain.Repositories;
using InfoTrack.Domain.Scraping;
using InfoTrack.Infrastructure.Options;
using InfoTrack.Infrastructure.Scraping;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InfoTrack.Tests.Operations;

public sealed class ScrapeOrchestratorTests
{
    [Fact]
    public async Task StartAsync_EnqueuesOperationAndReturnsOperationId()
    {
        var queue = new CapturingOperationQueue();
        var repository = new FakeScrapeRunRepository();
        var orchestrator = CreateOrchestrator(queue, repository);

        var operationId = await orchestrator.StartAsync("corr-scrape");

        operationId.Should().NotBeEmpty();
        queue.Items.Should().ContainSingle(x =>
            x.OperationId == operationId &&
            x.Kind == OperationKind.Scrape &&
            x.CorrelationId == "corr-scrape");
        repository.Runs.Should().ContainSingle(x => x.Id == operationId && x.Status == ScrapeRunStatus.Queued);
    }

    [Fact]
    public async Task StartAsync_WhenActiveRunExists_Throws()
    {
        var queue = new CapturingOperationQueue();
        var repository = new FakeScrapeRunRepository { HasActive = true };
        var orchestrator = CreateOrchestrator(queue, repository);

        var act = () => orchestrator.StartAsync("corr-scrape");

        await act.Should().ThrowAsync<InvalidOperationException>();
        queue.Items.Should().BeEmpty();
    }

    private static ScrapeOrchestrator CreateOrchestrator(
        CapturingOperationQueue queue,
        FakeScrapeRunRepository repository) =>
        new(
            new NoOpLocationRepository(),
            new NoOpScrapeClient(),
            new NoOpHtmlParser(),
            new NoOpSolicitorRepository(),
            new NoOpSnapshotRepository(),
            repository,
            new NoOpInsightRepository(),
            new NoOpAnalyticsEngine(),
            new NoOpScrapeProgressReporter(),
            queue,
            null!,
            Options.Create(new ScrapingOptions()),
            NullLogger<ScrapeOrchestrator>.Instance);

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

    private sealed class FakeScrapeRunRepository : IScrapeRunRepository
    {
        public List<ScrapeRun> Runs { get; } = [];

        public bool HasActive { get; set; }

        public Task AddAsync(ScrapeRun run, CancellationToken cancellationToken = default)
        {
            Runs.Add(run);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ScrapeRun run, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<ScrapeRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Runs.FirstOrDefault(x => x.Id == id));

        public Task<bool> HasActiveRunAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(HasActive);
    }

    private sealed class NoOpLocationRepository : ILocationRepository
    {
        public Task<IReadOnlyList<Location>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Location>>([]);

        public Task<IReadOnlyList<Location>> GetActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Location>>([]);

        public Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task ReplaceAllAsync(IReadOnlyList<Location> locations, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<Location>> SetActiveLocationsAsync(
            IReadOnlyList<string> activeNames,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Location>>([]);

        public Task<DiscoverySyncOutcome> SyncDiscoveredLocationsAsync(
            IReadOnlyList<DiscoveredLocation> discovered,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new DiscoverySyncOutcome(0, 0, 0, 0, 0));
    }

    private sealed class NoOpScrapeClient : ISolicitorsScrapeClient
    {
        public Task<string> FetchLocationPageAsync(string locationSlug, CancellationToken cancellationToken = default) =>
            Task.FromResult("<html></html>");
    }

    private sealed class NoOpHtmlParser : ISolicitorsHtmlParser
    {
        public IReadOnlyList<ParsedSolicitorListing> Parse(string html, string locationName) => [];
    }

    private sealed class NoOpSolicitorRepository : ISolicitorRepository
    {
        public Task<IReadOnlyList<Solicitor>> GetAllWithLocationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Solicitor>>([]);

        public Task UpsertRangeAsync(IReadOnlyList<Solicitor> solicitors, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoOpSnapshotRepository : IScrapeSnapshotRepository
    {
        public Task<ScrapeSnapshot?> GetLatestAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ScrapeSnapshot?>(null);

        public Task<ScrapeSnapshot?> GetPreviousAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ScrapeSnapshot?>(null);

        public Task<ScrapeSnapshot?> GetByIdWithEntriesAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ScrapeSnapshot?>(null);

        public Task<IReadOnlyList<ScrapeSnapshot>> GetHistoryAsync(int take = 20, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ScrapeSnapshot>>([]);

        public Task AddAsync(ScrapeSnapshot snapshot, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoOpInsightRepository : IInsightSummaryRepository
    {
        public Task AddAsync(InsightSummary summary, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<InsightSummary?> GetLatestAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<InsightSummary?>(null);
    }

    private sealed class NoOpAnalyticsEngine : IAnalyticsEngine
    {
        public SnapshotComparisonResult CompareSnapshots(AnalyticsContext context) =>
            throw new NotImplementedException();

        public DashboardSummary BuildDashboard(AnalyticsContext context, InsightSummary? cachedSummary = null) =>
            throw new NotImplementedException();

        public InsightSummary PersistSummary(AnalyticsContext context) =>
            new()
            {
                Id = Guid.NewGuid(),
                CurrentSnapshotId = context.Current.SnapshotId,
                GeneratedAt = DateTimeOffset.UtcNow,
                PayloadJson = "{}",
                NewFirms = 0,
                RemovedFirms = 0,
            };
    }

    private sealed class NoOpScrapeProgressReporter : IScrapeProgressReporter
    {
        public Task ReportAsync(Guid runId, ScrapeProgressUpdate update, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
