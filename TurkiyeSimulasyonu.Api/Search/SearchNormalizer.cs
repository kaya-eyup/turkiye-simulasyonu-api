using System.Globalization;
using System.Text;

namespace TurkiyeSimulasyonu.Api.Search;

// Arama metnini sadeleştirir: "Çiğ Köfte" -> "cig kofte".
// Makinenin dil ayarına (culture) hiç bakmaz; Türkçe harfler açık bir tabloyla eşlenir.
public static class SearchNormalizer
{
    private static readonly Dictionary<char, char> TurkishLetters = new()
    {
        ['ı'] = 'i',
        ['I'] = 'i',
        ['İ'] = 'i',
        ['ş'] = 's',
        ['Ş'] = 's',
        ['ğ'] = 'g',
        ['Ğ'] = 'g',
        ['ü'] = 'u',
        ['Ü'] = 'u',
        ['ö'] = 'o',
        ['Ö'] = 'o',
        ['ç'] = 'c',
        ['Ç'] = 'c',
    };

    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "";
        }

        // 1. Türkçe harfler tablodan, geri kalan ToLowerInvariant ile (dil ayarından bağımsız)
        var mapped = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            mapped.Append(TurkishLetters.TryGetValue(ch, out var replacement)
                ? replacement
                : char.ToLowerInvariant(ch));
        }

        // 2. Kalan aksanlar (â, é): harfi ve işaretini ayır, işareti at
        var decomposed = mapped.ToString().Normalize(NormalizationForm.FormD);
        var stripped = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                stripped.Append(ch);
            }
        }

        // 3. Baştaki, sondaki ve aradaki fazla boşlukları tek boşluğa indir
        var parts = stripped.ToString().Split(
            [' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts);
    }
}