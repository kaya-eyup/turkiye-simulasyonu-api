namespace TurkiyeSimulasyonu.Api.Dtos;

public record ItemDto
{
    /// <summary>Öğenin slug'ı.</summary>
    public required string Id { get; init; }
    /// <summary>Kategorinin slug'ı.</summary>
    public required string CategoryId { get; init; }
    /// <summary>Görünen ad.</summary>
    public required string Name { get; init; }
    /// <summary>Öğeyi simgeleyen emoji.</summary>
    public required string Emoji { get; init; }
    /// <summary>Kısa açıklama, en fazla 500 karakter.</summary>
    public required string Summary { get; init; }
    /// <summary>Yıldız başına oy sayısı: [1★, 2★, 3★, 4★, 5★].</summary>
    public required IReadOnlyList<int> Distribution { get; init; }
    /// <summary>Öğenin eklendiği an (UTC).</summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
