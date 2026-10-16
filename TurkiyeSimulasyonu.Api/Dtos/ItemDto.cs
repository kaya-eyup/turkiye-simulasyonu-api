namespace TurkiyeSimulasyonu.Api.Dtos;

public record ItemDto
{
    public required string Id { get; init; }
    public required string CategoryId { get; init; }
    public required string Name { get; init; }
    public required string Emoji { get; init; }
    public required string Summary { get; init; }
    public required IReadOnlyList<int> Distribution { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
