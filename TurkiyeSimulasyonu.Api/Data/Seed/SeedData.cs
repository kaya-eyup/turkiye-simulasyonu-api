namespace TurkiyeSimulasyonu.Api.Data.Seed;

public record SeedData(
    IReadOnlyList<SeedCategory> Categories,
    IReadOnlyList<SeedItem> Items,
    IReadOnlyList<SeedComment> Comments);