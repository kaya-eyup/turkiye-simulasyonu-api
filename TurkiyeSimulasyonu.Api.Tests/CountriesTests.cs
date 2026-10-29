using System.Globalization;
using TurkiyeSimulasyonu.Api.Surveys;

namespace TurkiyeSimulasyonu.Api.Tests;

public class CountriesTests
{
    [Fact]
    public void List_HasAllOfficialCodes()
    {
        Assert.Equal(249, Countries.Count);
    }

    [Theory]
    [InlineData("DE", "DE")]
    [InlineData("de", "DE")]
    [InlineData("  jp ", "JP")]
    [InlineData(null, "")]
    public void Normalize_TrimsAndUppercases(string? input, string expected)
    {
        Assert.Equal(expected, Countries.Normalize(input));
    }

    [Theory]
    [InlineData("DE")]
    [InlineData("TR")]
    [InlineData("IT")]
    public void IsValid_OfficialCode_ReturnsTrue(string code)
    {
        Assert.True(Countries.IsValid(code));
    }

    [Theory]
    [InlineData("XX")]  // atanmamış
    [InlineData("XK")]  // Kosova: yaygın ama ISO'nun resmi listesinde değil
    [InlineData("TUR")] // alfa-3
    [InlineData("de")]  // IsValid normalize etmez
    [InlineData("")]
    public void IsValid_OtherCode_ReturnsFalse(string code)
    {
        Assert.False(Countries.IsValid(code));
    }

    // Turkish I: Türkçe ayarlı makinede "it".ToUpper() → "İT" ve İtalya geçersiz sayılırdı
    [Fact]
    public void Normalize_DoesNotDependOnCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            Assert.Equal("İT", "it".ToUpper(CultureInfo.CurrentCulture));
            Assert.Equal("IT", Countries.Normalize("it"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}