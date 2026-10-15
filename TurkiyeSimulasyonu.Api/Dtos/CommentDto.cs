namespace TurkiyeSimulasyonu.Api.Dtos;

public record CommentDto
{
    public required string Id { get; init; }
    public required string ItemId { get; init; }
    public required string Author { get; init; }
    public required string Body { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
