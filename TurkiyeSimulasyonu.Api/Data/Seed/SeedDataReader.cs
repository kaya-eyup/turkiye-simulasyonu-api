using System.Text.Json;

namespace TurkiyeSimulasyonu.Api.Data.Seed;

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

        foreach (var item in data.Items)
        {
            // zod'daki tuple kuralı: tam 5 eleman, hiçbiri negatif değil
            if (item.Distribution.Count != 5 || item.Distribution.Any(n => n < 0))
            {
                throw new InvalidDataException(
                    $"Item '{item.Id}' must have exactly 5 non-negative vote counts.");
            }

            EnsureUtc(item.CreatedAt, $"Item '{item.Id}'");
        }

        foreach (var comment in data.Comments)
        {
            EnsureUtc(comment.CreatedAt, $"Comment '{comment.Id}'");
        }

        return data;
    }

    // "…Z" ile biten tarih Utc olarak okunur. Z yoksa saat dilimi bilinmez (Unspecified) ve
    // DateTimeOffset'e çevrilirken bilgisayarın saat dilimi varsayılır: yerelde +03:00, Azure'da +00:00.
    private static void EnsureUtc(DateTime value, string owner)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new InvalidDataException(
                $"{owner} has a non-UTC createdAt; seed dates must end with 'Z'.");
        }
    }
}