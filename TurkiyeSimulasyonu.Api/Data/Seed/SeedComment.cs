namespace TurkiyeSimulasyonu.Api.Data.Seed;

public record SeedComment(
    string Id,
    string ItemId,
    string Author,
    string Body,
    DateTime CreatedAt);