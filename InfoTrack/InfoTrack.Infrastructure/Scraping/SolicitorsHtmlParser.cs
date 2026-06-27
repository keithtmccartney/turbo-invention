using System.Text.RegularExpressions;
using InfoTrack.Domain.Scraping;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Infrastructure.Scraping;

public sealed partial class SolicitorsHtmlParser(ILogger<SolicitorsHtmlParser> logger) : ISolicitorsHtmlParser
{
    public IReadOnlyList<ParsedSolicitorListing> Parse(string html, string locationName)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        var blocks = ExtractResultBlocks(html);
        var results = new List<ParsedSolicitorListing>();

        foreach (var block in blocks)
        {
            try
            {
                var listing = ParseBlock(block);
                if (listing is not null)
                {
                    results.Add(listing with { Position = results.Count + 1 });
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse solicitor block for {Location}", locationName);
            }
        }

        logger.LogInformation("Parsed {Count} solicitors for {Location}", results.Count, locationName);
        return results;
    }

    private static IEnumerable<string> ExtractResultBlocks(string html)
    {
        var matches = ResultItemOpenTagRegex().Matches(html);
        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index + matches[i].Length;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : html.Length;
            yield return html[start..end];
        }
    }

    private static ParsedSolicitorListing? ParseBlock(string block)
    {
        var firmName = ExtractFirmName(block);
        if (string.IsNullOrWhiteSpace(firmName))
        {
            return null;
        }

        var phone = ExtractPhone(block);
        var address = ExtractAddress(block);
        var website = ExtractWebsite(block);
        var emailUrl = ExtractEmailEnquiryUrl(block);
        var description = ExtractDescription(block);
        var (rating, reviewCount) = ExtractRating(block);

        return new ParsedSolicitorListing(
            firmName,
            phone,
            address,
            website,
            emailUrl,
            description,
            rating,
            reviewCount,
            0);
    }

    private static string? ExtractFirmName(string block)
    {
        var match = FirmNameRegex().Match(block);
        if (!match.Success)
        {
            return null;
        }

        return HtmlDecode(StripTags(match.Groups["name"].Value)).Trim();
    }

    private static string? ExtractPhone(string block)
    {
        var match = PhoneRegex().Match(block);
        if (!match.Success)
        {
            return null;
        }

        var displayText = CleanWhitespace(HtmlDecode(StripTags(match.Groups["display"].Value)));
        if (!string.IsNullOrWhiteSpace(displayText))
        {
            return displayText;
        }

        return CleanWhitespace(match.Groups["phone"].Value);
    }

    private static string? ExtractAddress(string block)
    {
        var match = AddressRegex().Match(block);
        return match.Success ? CleanWhitespace(HtmlDecode(match.Groups["address"].Value)) : null;
    }

    private static string? ExtractWebsite(string block) =>
        ExtractLinkByIcon(block, "fa-globe");

    private static string? ExtractEmailEnquiryUrl(string block) =>
        ExtractLinkByIcon(block, "fa-envelope");

    private static string? ExtractLinkByIcon(string block, string iconFragment)
    {
        var searchStart = 0;
        while (searchStart < block.Length)
        {
            var iconIndex = block.IndexOf(iconFragment, searchStart, StringComparison.OrdinalIgnoreCase);
            if (iconIndex < 0)
            {
                return null;
            }

            var anchorStart = block.LastIndexOf("<a", iconIndex, StringComparison.OrdinalIgnoreCase);
            if (anchorStart >= 0)
            {
                var anchorEnd = block.IndexOf("</a>", iconIndex, StringComparison.OrdinalIgnoreCase);
                if (anchorEnd > iconIndex)
                {
                    var anchorTag = block[anchorStart..(anchorEnd + 4)];
                    var hrefMatch = HrefRegex().Match(anchorTag);
                    if (hrefMatch.Success)
                    {
                        return hrefMatch.Groups["url"].Value.Trim();
                    }
                }
            }

            searchStart = iconIndex + iconFragment.Length;
        }

        return null;
    }

    private static string? ExtractDescription(string block)
    {
        var addressEnd = block.IndexOf("</address>", StringComparison.OrdinalIgnoreCase);
        if (addressEnd < 0)
        {
            return null;
        }

        var paragraphStart = block.IndexOf("<p>", addressEnd, StringComparison.OrdinalIgnoreCase);
        if (paragraphStart < 0)
        {
            return null;
        }

        var paragraphEnd = block.IndexOf("</p>", paragraphStart, StringComparison.OrdinalIgnoreCase);
        if (paragraphEnd < 0)
        {
            return null;
        }

        var content = block[(paragraphStart + 3)..paragraphEnd];
        return CleanWhitespace(HtmlDecode(StripTags(content)));
    }

    private static (decimal? Rating, int? ReviewCount) ExtractRating(string block)
    {
        var match = ReviewCountRegex().Match(block);
        int? reviewCount = match.Success && int.TryParse(match.Groups["count"].Value, out var count) ? count : null;

        var fullStars = StarFullRegex().Matches(block).Count;
        var halfStars = StarHalfRegex().Matches(block).Count;
        var noneStars = StarNoneRegex().Matches(block).Count;

        if (fullStars + halfStars + noneStars == 0)
        {
            return (null, reviewCount);
        }

        var rating = fullStars + (halfStars * 0.5m);
        return (rating, reviewCount);
    }

    private static string StripTags(string input) => TagRegex().Replace(input, string.Empty);

    private static string HtmlDecode(string input) =>
        input
            .Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase)
            .Replace("&quot;", "\"", StringComparison.OrdinalIgnoreCase)
            .Replace("&#39;", "'", StringComparison.OrdinalIgnoreCase)
            .Replace("&lt;", "<", StringComparison.OrdinalIgnoreCase)
            .Replace("&gt;", ">", StringComparison.OrdinalIgnoreCase);

    private static string CleanWhitespace(string input) =>
        WhitespaceRegex().Replace(input, " ").Trim();

    [GeneratedRegex(@"<div class=""result-item(?:\s[^""]*)?"">", RegexOptions.IgnoreCase)]
    private static partial Regex ResultItemOpenTagRegex();

    [GeneratedRegex(@"<span class=""h2"">(?<name>.*?)(?:<div class=""greentick(?:-small)?""|<span class=""rev-results"")", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex FirmNameRegex();

    [GeneratedRegex(@"href=""tel:(?<phone>[^""]+)"">(?<display>[^<]*)</a>", RegexOptions.IgnoreCase)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"<address>(?<address>.*?)</address>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex AddressRegex();

    [GeneratedRegex(@"href=""(?<url>[^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex HrefRegex();

    [GeneratedRegex(@"rev-results"">.*?>\s*\((?<count>\d+)\)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ReviewCountRegex();

    [GeneratedRegex(@"star-full", RegexOptions.IgnoreCase)]
    private static partial Regex StarFullRegex();

    [GeneratedRegex(@"star-half", RegexOptions.IgnoreCase)]
    private static partial Regex StarHalfRegex();

    [GeneratedRegex(@"star-none", RegexOptions.IgnoreCase)]
    private static partial Regex StarNoneRegex();

    [GeneratedRegex("<.*?>", RegexOptions.Singleline)]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
