using InfoTrack.Contracts.Solicitors;

namespace InfoTrack.Application.Mcp;

internal static class FirmSearchMatcher
{
    public static IEnumerable<SolicitorDto> Filter(
        ResultsResponse results,
        string? locationFilter,
        string? query)
    {
        return results.Results
            .Where(location => locationFilter is null
                || location.LocationName.Equals(locationFilter, StringComparison.OrdinalIgnoreCase))
            .SelectMany(location => location.Solicitors)
            .Where(firm => query is null || Matches(firm, query))
            .OrderBy(firm => firm.FirmName, StringComparer.OrdinalIgnoreCase);
    }

    public static bool Matches(SolicitorDto firm, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return Contains(firm.FirmName, query)
            || Contains(firm.LocationName, query)
            || Contains(firm.Phone, query)
            || Contains(firm.Address, query)
            || Contains(firm.Website, query)
            || Contains(firm.EmailEnquiryUrl, query)
            || Contains(firm.Description, query)
            || (firm.Rating?.ToString(System.Globalization.CultureInfo.InvariantCulture).Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            || (firm.ReviewCount?.ToString(System.Globalization.CultureInfo.InvariantCulture).Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static bool Contains(string? value, string query) =>
        !string.IsNullOrEmpty(value)
        && value.Contains(query, StringComparison.OrdinalIgnoreCase);
}
