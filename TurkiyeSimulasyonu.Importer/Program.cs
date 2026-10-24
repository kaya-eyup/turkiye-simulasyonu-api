using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TurkiyeSimulasyonu.Api.Data;
using TurkiyeSimulasyonu.Api.Entities;

// ---------------------------------------------------------------------------
// Parti JSON'larını (build_batchNN.py çıktısı) var olan veritabanına yazar.
//
// DbSeeder (Api/Data/Seed) sadece BOŞ veritabanında çalışır — bu araç onun
// yerine geçmiyor, dolu veritabanına EKLEME yapıyor.
//
// Eşleştirme anahtarı Slug (DB'de unique). Kural:
//   - Slug veritabanında yoksa  -> eklenir.
//   - Slug veritabanında varsa  -> --update verilmediyse DOKUNULMAZ
//     (canlıda gerçek oy almış olabilir; Score*Count ve CreatedAt hiçbir
//     zaman güncellenmez, --update ile bile).
//
// Kullanım:
//   dotnet run --project TurkiyeSimulasyonu.Importer -- <dosya-veya-klasor> [...]
//   dotnet run --project TurkiyeSimulasyonu.Importer -- --connection "<cs>" --update out/
//   dotnet run --project TurkiyeSimulasyonu.Importer -- --dry-run out/
//
// Bağlantı sırası: --connection > ConnectionStrings__Default ortam değişkeni
// > appsettings.Development.json'daki yerel LocalDB adresi (varsayılan).
// ---------------------------------------------------------------------------

const string DefaultLocalConnection =
    "Server=(localdb)\\MSSQLLocalDB;Database=TurkiyeSimulasyonu;Trusted_Connection=True";

var remaining = new List<string>(args);
string? connectionString = null;
bool updateExisting = false;
bool dryRun = false;

for (int i = remaining.Count - 1; i >= 0; i--)
{
    if (remaining[i] == "--connection" && i + 1 < remaining.Count)
    {
        connectionString = remaining[i + 1];
        remaining.RemoveRange(i, 2);
    }
    else if (remaining[i] == "--update")
    {
        updateExisting = true;
        remaining.RemoveAt(i);
    }
    else if (remaining[i] == "--dry-run")
    {
        dryRun = true;
        remaining.RemoveAt(i);
    }
}

connectionString ??= Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? DefaultLocalConnection;

if (remaining.Count == 0)
{
    Console.Error.WriteLine("Kullanım: dotnet run -- [--connection \"<cs>\"] [--update] [--dry-run] <dosya-veya-klasor> [...]");
    return 1;
}

var files = new List<string>();
foreach (var path in remaining)
{
    if (Directory.Exists(path))
    {
        files.AddRange(Directory.GetFiles(path, "*.json").OrderBy(f => f, StringComparer.Ordinal));
    }
    else if (File.Exists(path))
    {
        files.Add(path);
    }
    else
    {
        Console.Error.WriteLine($"Bulunamadı: {path}");
        return 1;
    }
}

if (files.Count == 0)
{
    Console.Error.WriteLine("İçe aktarılacak .json dosyası bulunamadı.");
    return 1;
}

Console.WriteLine($"{files.Count} dosya okunacak ({(dryRun ? "DRY RUN — veritabanına yazılmayacak" : "gerçek içe aktarma")}).");
foreach (var f in files) Console.WriteLine($"  - {f}");

var dbOptions = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
using var db = new AppDbContext(dbOptions);

// Mevcut kategoriler, tekrar veritabanına gitmeden referans olarak elde tutulur.
var categoriesBySlug = db.Categories.ToDictionary(c => c.Slug, c => c);

// --update verilmediyse tek lazım olan bilgi "bu slug var mı" (HashSet yeter).
// --update verildiyse güncellenecek entity'nin kendisi de lazım.
Dictionary<string, Item>? existingItemsBySlug = updateExisting
    ? db.Items.ToDictionary(i => i.Slug, i => i)
    : null;
HashSet<string> existingItemSlugs = existingItemsBySlug?.Keys.ToHashSet()
    ?? db.Items.Select(i => i.Slug).ToHashSet();

int categoriesAdded = 0, itemsAdded = 0, itemsUpdated = 0, itemsSkipped = 0;

foreach (var file in files)
{
    Console.WriteLine($"\n--- {Path.GetFileName(file)} ---");
    var batch = ReadBatch(file);

    if (batch.Category is { } cat)
    {
        if (categoriesBySlug.TryGetValue(cat.Id, out var existingCat))
        {
            Console.WriteLine($"  [kategori] zaten var, dokunulmadı: {cat.Id} ({existingCat.Name})");
        }
        else
        {
            var newCat = new Category
            {
                Slug = cat.Id,
                Name = cat.Name,
                Emoji = cat.Emoji,
                DisplayOrder = cat.Order,
            };
            db.Categories.Add(newCat);
            categoriesBySlug[cat.Id] = newCat; // Id henüz yok ama navigation ile FK için yeterli
            categoriesAdded++;
            Console.WriteLine($"  [kategori] YENİ: {cat.Id} ({cat.Name})");
        }
    }

    foreach (var item in batch.Items)
    {
        if (item.Distribution.Count != 5 || item.Distribution.Any(n => n < 0))
            throw new InvalidDataException($"'{item.Id}': distribution tam 5 negatif-olmayan sayı olmalı.");
        if (item.CreatedAt.Kind != DateTimeKind.Utc)
            throw new InvalidDataException($"'{item.Id}': createdAt UTC olmalı ('Z' ile bitmeli).");
        if (!categoriesBySlug.TryGetValue(item.CategoryId, out var category))
            throw new InvalidDataException($"'{item.Id}': kategori '{item.CategoryId}' ne veritabanında ne bu çalıştırmada eklendi.");

        if (!existingItemSlugs.Contains(item.Id))
        {
            db.Items.Add(new Item
            {
                Slug = item.Id,
                Name = item.Name,
                Emoji = item.Emoji,
                Summary = item.Summary,
                Score1Count = item.Distribution[0],
                Score2Count = item.Distribution[1],
                Score3Count = item.Distribution[2],
                Score4Count = item.Distribution[3],
                Score5Count = item.Distribution[4],
                CreatedAt = new DateTimeOffset(item.CreatedAt),
                Category = category,
            });
            existingItemSlugs.Add(item.Id);
            itemsAdded++;
        }
        else if (updateExisting && existingItemsBySlug!.TryGetValue(item.Id, out var existingItem))
        {
            // Oy sayılarına (Score*Count) ve CreatedAt'e kasten dokunulmuyor: canlı oylar kaybolmasın.
            existingItem.Name = item.Name;
            existingItem.Emoji = item.Emoji;
            existingItem.Summary = item.Summary;
            itemsUpdated++;
        }
        else
        {
            itemsSkipped++;
        }
    }
}

Console.WriteLine();
Console.WriteLine($"Kategori eklendi: {categoriesAdded}");
Console.WriteLine($"Öğe eklendi: {itemsAdded} | güncellendi: {itemsUpdated} | atlandı (zaten var, --update verilmedi): {itemsSkipped}");

if (dryRun)
{
    Console.WriteLine("\nDRY RUN — yukarıdaki sayılar gerçekleşecek olanı gösterir, hiçbir şey yazılmadı.");
    return 0;
}

// Tek SaveChanges = tek transaction: ya hepsi yazılır ya hiçbiri (DbSeeder ile aynı mantık).
db.SaveChanges();
Console.WriteLine("\nYazıldı.");
return 0;

// ---------------------------------------------------------------------------
// Parti dosyası formatı: build_batchNN.py'nin çıktısı.
//   { "category": {id,name,emoji,order}?, "items": [{id,categoryId,name,emoji,summary,distribution,createdAt}] }
// db.json'daki SeedData'dan farkı: "categories" (çoğul, zorunlu) değil, tek
// ve opsiyonel "category"; bu yüzden SeedDataReader yeniden kullanılmıyor.
// ---------------------------------------------------------------------------
static BatchFile ReadBatch(string path)
{
    var json = File.ReadAllText(path);
    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    return JsonSerializer.Deserialize<BatchFile>(json, options)
        ?? throw new InvalidDataException($"'{path}' boş ya da okunamadı.");
}

record BatchCategory(string Id, string Name, string Emoji, int Order);

record BatchItem(
    string Id,
    string CategoryId,
    string Name,
    string Emoji,
    string Summary,
    IReadOnlyList<int> Distribution,
    DateTime CreatedAt);

record BatchFile(BatchCategory? Category, IReadOnlyList<BatchItem> Items);
