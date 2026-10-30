namespace TurkiyeSimulasyonu.Api.Dtos;

public record RebirthResultsDto
{
    /// <summary>Toplam oy sayısı.</summary>
    public required int Total { get; init; }
    /// <summary>Ülke başına oy sayıları, çoktan aza.</summary>
    public required IReadOnlyList<CountryCountDto> Results { get; init; }
}