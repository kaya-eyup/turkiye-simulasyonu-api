namespace TurkiyeSimulasyonu.Api.Entities;

// Yeniden Doğma Anketi: tarayıcı başına tek satır; oy değişince satır güncellenir
public class RebirthVote
{
    // Sıralı sayı anahtar: yeni satır hep sona eklenir (GUID anahtar ortaya sıkıştırırdı)
    public int Id { get; set; }

    // Tarayıcının ürettiği rastgele kimlik; v1'de kullanıcı girişi yok
    public Guid VoterId { get; set; }

    // ISO 3166-1 alfa-2, büyük harf ("DE")
    public required string CountryCode { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}