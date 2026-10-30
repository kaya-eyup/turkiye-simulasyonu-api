namespace TurkiyeSimulasyonu.Api.Dtos;

public record SearchResultDto
{
    /// <summary>Bu sayfadaki öğeler.</summary>
    public required IReadOnlyList<ItemDto> Items { get; init; }
    /// <summary>Eşleşen toplam öğe sayısı (bütün sayfalar).</summary>
    public required int Total { get; init; }
    /// <summary>Toplam sayfa sayısı; sonuç yoksa 0.</summary>
    public required int TotalPages { get; init; }
}