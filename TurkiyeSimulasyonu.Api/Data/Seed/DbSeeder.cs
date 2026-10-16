using Microsoft.EntityFrameworkCore;
using TurkiyeSimulasyonu.Api.Entities;

namespace TurkiyeSimulasyonu.Api.Data.Seed;

// db.json'daki başlangıç verisini boş veritabanına yazar.
// EF, dotnet ef database update'in sonunda çağırır (Program.cs → UseSeeding); uygulama çalışırken çağrılmaz.
public static class DbSeeder
{
    public static void Seed(DbContext context, string seedPath)
    {
        // Her database update'te çalışır; veritabanı doluysa dokunma, ikinci çalıştırma çift kayıt üretmesin.
        // Hepsi tek SaveChanges ile yazıldığı için kategori varsa öğeler ve yorumlar da var demektir.
        if (context.Set<Category>().Any())
        {
            return;
        }
        var data = SeedDataReader.Read(seedPath);
        // Anahtar db.json'daki slug. Öğeler kategorilerini, yorumlar öğelerini bu sözlüklerden bulur.
        var categoriesBySlug = data.Categories.ToDictionary(
            c => c.Id,
            c => new Category
            {
                Slug = c.Id,
                Name = c.Name,
                Emoji = c.Emoji,
                DisplayOrder = c.Order,
            });

        var itemsBySlug = data.Items.ToDictionary(
            i => i.Id,
            i => new Item
            {
                Slug = i.Id,
                Name = i.Name,
                Emoji = i.Emoji,
                Summary = i.Summary,
                // 0. eleman 1 yıldızın oy sayısı
                Score1Count = i.Distribution[0],
                Score2Count = i.Distribution[1],
                Score3Count = i.Distribution[2],
                Score4Count = i.Distribution[3],
                Score5Count = i.Distribution[4],
                // Kind = Utc (SeedDataReader garanti ediyor) → +00:00
                CreatedAt = new DateTimeOffset(i.CreatedAt),
                // Numara (CategoryId) henüz yok; nesneyi veriyoruz, EF kaydettikten sonra numarayı kendisi yazar.
                Category = categoriesBySlug[i.CategoryId],
            });

        // Eski "c-001" kimlikleri taşınmıyor; numarayı veritabanı verir.
        var comments = data.Comments
            .Select(c => new Comment
            {
                Author = c.Author,
                Body = c.Body,
                CreatedAt = new DateTimeOffset(c.CreatedAt),
                Item = itemsBySlug[c.ItemId],
            })
            .ToList();

        context.Set<Category>().AddRange(categoriesBySlug.Values);
        context.Set<Item>().AddRange(itemsBySlug.Values);
        context.Set<Comment>().AddRange(comments);

        // Tek SaveChanges = tek transaction: ya hepsi yazılır ya hiçbiri.
        // Ekleme sırasını EF foreign key'lerden çıkarır: önce kategoriler, sonra öğeler, sonra yorumlar.
        context.SaveChanges();
    }
}