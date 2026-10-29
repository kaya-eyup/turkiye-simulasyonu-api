namespace TurkiyeSimulasyonu.Api.Dtos;

public record RebirthResultsDto
{
    public required int Total { get; init; }
    public required IReadOnlyList<CountryCountDto> Results { get; init; }
}