namespace TurkiyeSimulasyonu.Api.Dtos;

public record CountryCountDto
{
    public required string Country { get; init; }
    public required int Count { get; init; }
}