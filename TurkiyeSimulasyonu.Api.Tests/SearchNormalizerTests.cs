using System.Globalization;
using TurkiyeSimulasyonu.Api.Search;
namespace TurkiyeSimulasyonu.Api.Tests;

public class SearchNormalizerTests
{
    [Theory]
    [InlineData("İSTANBUL", "istanbul")]
    [InlineData("ığdır", "igdir")]
    [InlineData("Çiğ Köfte", "cig kofte")]
    [InlineData("ŞANLIURFA", "sanliurfa")]
    [InlineData("Güllaç", "gullac")]
    [InlineData("  Kebap  ", "kebap")]
    [InlineData("Çiğ    Köfte", "cig kofte")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Normalize_ReturnsExpected(string? input, string expected)
    {
        Assert.Equal(expected, SearchNormalizer.Normalize(input));
    }

    // Turkish I problem: ToLower makinenin dil ayarına bakar, Normalize bakmaz
    [Fact]
    public void Normalize_DoesNotDependOnCulture()
    {
        Assert.Equal("ıstanbul", "ISTANBUL".ToLower(new CultureInfo("tr-TR")));
        Assert.Equal("istanbul", "ISTANBUL".ToLower(CultureInfo.InvariantCulture));

        Assert.Equal("istanbul", SearchNormalizer.Normalize("ISTANBUL"));
    }
}