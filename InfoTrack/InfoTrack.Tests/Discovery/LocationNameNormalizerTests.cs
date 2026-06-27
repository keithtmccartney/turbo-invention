using FluentAssertions;
using InfoTrack.Domain.Discovery;

namespace InfoTrack.Tests.Discovery;

public sealed class LocationNameNormalizerTests
{
    [Theory]
    [InlineData("london", "London")]
    [InlineData("leamington-spa", "Leamington Spa")]
    [InlineData("st-albans", "St Albans")]
    [InlineData("chesterfield", "Chesterfield")]
    public void FromSlug_ConvertsSlugToReadableName(string slug, string expected)
    {
        LocationNameNormalizer.FromSlug(slug).Should().Be(expected);
    }
}
