namespace TurkiyeSimulasyonu.Api.Dtos;

public record ItemVoteDto
{
    /// <summary>Oy verilen öğenin slug'ı.</summary>
    // Öğenin slug'ı: istemci öğeleri sayı anahtarla değil slug'la tanıyor
    public required string ItemId { get; init; }
    /// <summary>Verilen puan, 1-5.</summary>
    public required int Stars { get; init; }
}