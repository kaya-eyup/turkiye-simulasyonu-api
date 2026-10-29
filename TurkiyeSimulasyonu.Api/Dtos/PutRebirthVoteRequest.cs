using TurkiyeSimulasyonu.Api.Surveys;

namespace TurkiyeSimulasyonu.Api.Dtos;

public record PutRebirthVoteRequest
{
    private readonly string _country = "";

    // " de " → "DE"; doğrulama normalize edilmiş değere yapılır
    [CountryCode]
    public required string Country
    {
        get => _country;
        init => _country = Countries.Normalize(value);
    }
}