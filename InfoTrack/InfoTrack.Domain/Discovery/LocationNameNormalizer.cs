using System.Globalization;

namespace InfoTrack.Domain.Discovery;

public static class LocationNameNormalizer
{
    public static string FromSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return string.Empty;
        }

        var parts = LocationSlug.Normalize(slug)
            .Split(['-', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join(' ', parts.Select(ToTitleCase));
    }

    private static string ToTitleCase(string value) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value);
}
