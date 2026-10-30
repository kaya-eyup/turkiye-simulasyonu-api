using TurkiyeSimulasyonu.Api.Surveys;

namespace TurkiyeSimulasyonu.Api.Dtos;

public record PutRebirthVoteRequest
{
    private readonly string _country = "";

    // " de " → "DE"; doğrulama normalize edilmiş değere yapılır
    /// <summary>Ülke kodu, ISO 3166-1 alfa-2 (ör. `DE`); büyük/küçük harf fark etmez.</summary>
    [CountryCode]
    public required string Country
    {
        get => _country;
        init => _country = Countries.Normalize(value);
    }
}