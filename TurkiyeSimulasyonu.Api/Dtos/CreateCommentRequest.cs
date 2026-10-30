using System.ComponentModel.DataAnnotations;

namespace TurkiyeSimulasyonu.Api.Dtos;

public record CreateCommentRequest
{
    private readonly string? _author;
    private readonly string _body = "";

    /// <summary>Yorumun yazılacağı öğenin slug'ı.</summary>
    public required string ItemId { get; init; }

    /// <summary>Yazanın adı, en fazla 30 karakter. Boş ya da yoksa `anonim` olur.</summary>
    [StringLength(30)]
    public string? Author
    {
        get => _author;
        init => _author = value?.Trim();
    }

    /// <summary>Yorum metni; kırpıldıktan sonra 3-500 karakter.</summary>
    [StringLength(500, MinimumLength = 3)]
    public required string Body
    {
        get => _body;
        init => _body = value?.Trim() ?? "";
    }
}