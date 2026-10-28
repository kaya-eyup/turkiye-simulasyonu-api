using System.ComponentModel.DataAnnotations;

namespace TurkiyeSimulasyonu.Api.Dtos;

public record CreateCommentRequest
{
    private readonly string? _author;
    private readonly string _body = "";

    public required string ItemId { get; init; }

    [StringLength(30)]
    public string? Author
    {
        get => _author;
        init => _author = value?.Trim();
    }

    [StringLength(500, MinimumLength = 3)]
    public required string Body
    {
        get => _body;
        init => _body = value?.Trim() ?? "";
    }
}