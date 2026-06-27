using System.Xml.Linq;

namespace InfoTrack.Infrastructure.Discovery;

public sealed class SitemapXmlParser
{
    public IReadOnlyList<string> ParseLocations(string xml)
    {
        var document = XDocument.Parse(xml);

        return document
            .Descendants()
            .Where(x => x.Name.LocalName.Equals("loc", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Value.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }
}
