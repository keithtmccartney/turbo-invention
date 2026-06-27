using System.Net;
using System.Text.RegularExpressions;

namespace InfoTrack.Domain.Scraping;

public static partial class ScrapedTextNormalizer
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var decoded = UnicodeEscapeRegex().Replace(value, match =>
            ((char)Convert.ToInt32(match.Groups[1].Value, 16)).ToString());

        decoded = WebUtility.HtmlDecode(decoded)
            .Replace('\u00A0', ' ')
            .Replace("&nbsp", " ", StringComparison.OrdinalIgnoreCase);

        return WhitespaceRegex().Replace(decoded, " ").Trim();
    }

    [GeneratedRegex(@"\\u([0-9a-fA-F]{4})", RegexOptions.CultureInvariant)]
    private static partial Regex UnicodeEscapeRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
