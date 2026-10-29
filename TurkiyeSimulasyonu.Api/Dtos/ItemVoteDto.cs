namespace TurkiyeSimulasyonu.Api.Dtos;

public record ItemVoteDto
{
    // Öğenin slug'ı: istemci öğeleri sayı anahtarla değil slug'la tanıyor
    public required string ItemId { get; init; }
    public required int Stars { get; init; }
}