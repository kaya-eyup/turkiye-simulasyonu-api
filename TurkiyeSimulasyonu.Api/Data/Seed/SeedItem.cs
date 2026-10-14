namespace TurkiyeSimulasyonu.Api.Data.Seed;

public record SeedItem(
    string Id,
    string CategoryId,
    string Name,
    string Emoji,
    string Summary,
    IReadOnlyList<int> Distribution,
    DateTime CreatedAt);