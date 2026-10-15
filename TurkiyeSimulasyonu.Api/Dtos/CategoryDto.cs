namespace TurkiyeSimulasyonu.Api.Dtos;

public record CategoryDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Emoji { get; set; }
    public required int Order { get; set; }
}
