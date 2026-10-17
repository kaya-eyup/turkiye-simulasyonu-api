namespace TurkiyeSimulasyonu.Api.Dtos;

public record CreateCommentRequest
{
    public required string ItemId { get; init; }
    public required string Author { get; init; }
    public required string Body { get; init; }
}
