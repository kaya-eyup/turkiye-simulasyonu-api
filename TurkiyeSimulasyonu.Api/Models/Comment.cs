namespace TurkiyeSimulasyonu.Api.Models;

public record Comment(
    string Id,
    string ItemId,
    string Author,
    string Body,
    DateTime CreatedAt);