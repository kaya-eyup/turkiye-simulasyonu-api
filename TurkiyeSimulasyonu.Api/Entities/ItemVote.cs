namespace TurkiyeSimulasyonu.Api.Entities;

// Öğe oyu: tarayıcı başına, öğe başına tek satır; puan değişince satır güncellenir
public class ItemVote
{
    // Sıralı sayı anahtar: yeni satır hep sona eklenir (RebirthVote ile aynı karar)
    public int Id { get; set; }

    // Tarayıcının ürettiği rastgele kimlik; anketle aynı kimlik
    public Guid VoterId { get; set; }

    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;

    // 1-5. byte → SQL'de tinyint (1 bayt); aralığı veritabanındaki CHECK kısıtı da korur
    public byte Stars { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}