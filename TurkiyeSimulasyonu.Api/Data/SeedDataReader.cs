using System.Text.Json;

namespace TurkiyeSimulasyonu.Api.Data;

public static class SeedDataReader
{
    // ASP.NET Core'un cevap yazarken kullandığı kurallar: camelCase adlar, büyük-küçük harf duyarsız eşleşme.
    // Bir kez oluşturulup her okumada tekrar kullanılır.
    private static readonly JsonSerializerOptions s_options = new(JsonSerializerDefaults.Web);

    public static SeedData Read(string path)
    {
        var json = File.ReadAllText(path);

        var data = JsonSerializer.Deserialize<SeedData>(json, s_options)
            ?? throw new InvalidDataException($"Seed file '{path}' contains null.");
        // zod'daki tuple kuralı: tam 5 eleman, hiçbiri negatif değil
        foreach (var item in data.Items)
        {
            if (item.Distribution.Count != 5 || item.Distribution.Any(n => n < 0))
            {
                throw new InvalidDataException(
                    $"Item '{item.Id}' must have exactly 5 non-negative vote counts.");
            }
        }

        return data;
    }
}