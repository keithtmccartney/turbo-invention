namespace InfoTrack.Domain.Scraping;

public interface IScrapeProgressReporter
{
    Task ReportAsync(Guid runId, ScrapeProgressUpdate update, CancellationToken cancellationToken = default);
}
