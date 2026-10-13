namespace TurkiyeSimulasyonu.Api.Entities;

// Categories tablosunun bir satırı
public class Category
{
    // Değişmeyen sayı anahtar; numarayı veritabanı verir
    public int Id { get; set; }

    // Adreste görünen ad (yemek-kulturu). Düzeltilebilir; tablolar arası bağ buna değil Id'ye kurulur
    public required string Slug { get; set; }

    public required string Name { get; set; }
    public required string Emoji { get; set; }

    // "Order" SQL'de ayrılmış kelime (ORDER BY)
    public int DisplayOrder { get; set; }

    // Bu kategorinin öğeleri (navigation): C#'ta var, veritabanında sütunu yok
    public List<Item> Items { get; set; } = [];
}