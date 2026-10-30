namespace TurkiyeSimulasyonu.Api.Dtos;

public record CategoryDto
{
    /// <summary>Kategorinin slug'ı.</summary>
    public required string Id { get; init; }
    /// <summary>Görünen ad.</summary>
    public required string Name { get; init; }
    /// <summary>Kategoriyi simgeleyen emoji.</summary>
    public required string Emoji { get; init; }
    /// <summary>Gösterim sırası; küçük olan önce.</summary>
    public required int Order { get; init; }
}
