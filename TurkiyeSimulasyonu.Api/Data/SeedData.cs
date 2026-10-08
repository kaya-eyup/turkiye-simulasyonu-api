using TurkiyeSimulasyonu.Api.Models;

namespace TurkiyeSimulasyonu.Api.Data;

public record SeedData(
    IReadOnlyList<Category> Categories,
    IReadOnlyList<Item> Items,
    IReadOnlyList<Comment> Comments);