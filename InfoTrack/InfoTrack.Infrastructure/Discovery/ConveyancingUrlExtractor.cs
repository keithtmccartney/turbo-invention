using System.Text.RegularExpressions;

namespace InfoTrack.Infrastructure.Discovery;

public sealed partial class ConveyancingUrlExtractor
{
    [GeneratedRegex(@"conveyancing\+([a-z0-9-]+)\.html", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ConveyancingLocationPattern();

    public IReadOnlyList<string> ExtractSlugs(IEnumerable<string> urls)
    {
        var slugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var url in urls)
        {
            var match = ConveyancingLocationPattern().Match(url);
            if (match.Success)
            {
                slugs.Add(match.Groups[1].Value.ToLowerInvariant());
            }
        }

        return slugs.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
