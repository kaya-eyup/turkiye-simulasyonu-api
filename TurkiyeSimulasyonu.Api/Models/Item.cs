namespace TurkiyeSimulasyonu.Api.Models;

public record Item(
    string Id,
    string CategoryId,
    string Name,
    string Emoji,
    string Summary,
    IReadOnlyList<int> Distribution,
    DateTime CreatedAt);