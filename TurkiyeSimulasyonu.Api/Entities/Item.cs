namespace TurkiyeSimulasyonu.Api.Entities;

public class Item
{
    public int Id { get; set; }
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public required string Emoji { get; set; }
    public required string Summary { get; set; }

    // Oy dağılımı: her puanın oy sayısı ayrı sütunda; sayaç tek komutla artırılabilsin (SET n = n + 1)
    public int Score1Count { get; set; }
    public int Score2Count { get; set; }
    public int Score3Count { get; set; }
    public int Score4Count { get; set; }
    public int Score5Count { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public List<Comment> Comments { get; set; } = [];
}

