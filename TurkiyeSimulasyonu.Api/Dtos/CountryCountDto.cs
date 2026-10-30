namespace TurkiyeSimulasyonu.Api.Dtos;

public record CountryCountDto
{
    /// <summary>ISO 3166-1 alfa-2 ülke kodu.</summary>
    public required string Country { get; init; }
    /// <summary>Bu ülkeye verilen oy sayısı.</summary>
    public required int Count { get; init; }
}