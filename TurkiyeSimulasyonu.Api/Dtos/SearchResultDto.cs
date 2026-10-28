namespace TurkiyeSimulasyonu.Api.Dtos;

public record SearchResultDto
{
    public required IReadOnlyList<ItemDto> Items { get; init; }
    public required int Total { get; init; }
    public required int TotalPages { get; init; }
}