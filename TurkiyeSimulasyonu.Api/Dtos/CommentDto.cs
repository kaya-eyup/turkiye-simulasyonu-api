namespace TurkiyeSimulasyonu.Api.Dtos;

public record CommentDto
{
    /// <summary>Yorumun numarası.</summary>
    public required string Id { get; init; }
    /// <summary>Yorumun yazıldığı öğenin slug'ı.</summary>
    public required string ItemId { get; init; }
    /// <summary>Yazan kişinin adı; boş bırakılmışsa `anonim`.</summary>
    public required string Author { get; init; }
    /// <summary>Yorum metni.</summary>
    public required string Body { get; init; }
    /// <summary>Yorumun yazıldığı an (UTC).</summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
