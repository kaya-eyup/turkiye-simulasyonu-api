namespace TurkiyeSimulasyonu.Api.Dtos;

public record CategoryDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Emoji { get; init; }
    public required int Order { get; init; }
}
