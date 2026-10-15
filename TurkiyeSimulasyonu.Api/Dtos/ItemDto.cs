namespace TurkiyeSimulasyonu.Api.Dtos;

public record ItemDto
{
    public required string Id { get; init; }
    public required string CategoryId { get; set; }
    public required string Name { get; set; }
    public required string Emoji { get; set; }
    public required string Summary { get; set; }
    public required IReadOnlyList<int> Distribution { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
