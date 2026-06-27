using System.Globalization;
using System.Text;
using System.Text.Json;
using InfoTrack.Application.Features.Insights.GetDashboard;
using InfoTrack.Application.Features.Scraping.GetResults;
using InfoTrack.Contracts.Insights;

namespace InfoTrack.Application.Mcp.Export;

public sealed class McpResultExporter(
    GetResultsHandler resultsHandler,
    GetDashboardHandler dashboardHandler)
{
    public async Task<string> ExportJsonAsync(CancellationToken cancellationToken = default)
    {
        var results = await resultsHandler.HandleAsync(cancellationToken);
        return JsonSerializer.Serialize(results, McpJsonDefaults.SerializerOptions);
    }

    public async Task<string> ExportCsvAsync(CancellationToken cancellationToken = default)
    {
        var results = await resultsHandler.HandleAsync(cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("Location,FirmName,Phone,Address,Website,EmailEnquiryUrl,Rating,ReviewCount");

        foreach (var location in results.Results)
        {
            foreach (var firm in location.Solicitors)
            {
                builder.Append(Escape(location.LocationName)).Append(',')
                    .Append(Escape(firm.FirmName)).Append(',')
                    .Append(Escape(firm.Phone)).Append(',')
                    .Append(Escape(firm.Address)).Append(',')
                    .Append(Escape(firm.Website)).Append(',')
                    .Append(Escape(firm.EmailEnquiryUrl)).Append(',')
                    .Append(firm.Rating?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append(',')
                    .Append(firm.ReviewCount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)
                    .AppendLine();
            }
        }

        return builder.ToString();
    }

    public async Task<string> ExportExcelAsync(CancellationToken cancellationToken = default)
    {
        var results = await resultsHandler.HandleAsync(cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("Location\tFirmName\tPhone\tAddress\tWebsite\tEmailEnquiryUrl\tRating\tReviewCount");

        foreach (var location in results.Results)
        {
            foreach (var firm in location.Solicitors)
            {
                builder.Append(location.LocationName).Append('\t')
                    .Append(firm.FirmName).Append('\t')
                    .Append(firm.Phone).Append('\t')
                    .Append(firm.Address).Append('\t')
                    .Append(firm.Website).Append('\t')
                    .Append(firm.EmailEnquiryUrl).Append('\t')
                    .Append(firm.Rating?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append('\t')
                    .Append(firm.ReviewCount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)
                    .AppendLine();
            }
        }

        return builder.ToString();
    }

    public async Task<DashboardResponse> GetReportAsync(CancellationToken cancellationToken = default) =>
        await dashboardHandler.HandleAsync(cancellationToken);

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"", StringComparison.Ordinal);
        return escaped.Contains(',', StringComparison.Ordinal) || escaped.Contains('"', StringComparison.Ordinal)
            ? $"\"{escaped}\""
            : escaped;
    }
}
