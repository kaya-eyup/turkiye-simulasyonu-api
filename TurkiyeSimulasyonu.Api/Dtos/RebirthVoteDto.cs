namespace TurkiyeSimulasyonu.Api.Dtos;

public record RebirthVoteDto
{
    /// <summary>Oy verilen ülkenin ISO 3166-1 alfa-2 kodu, büyük harf.</summary>
    public required string Country { get; init; }
}