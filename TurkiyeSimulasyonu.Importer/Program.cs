using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using TurkiyeSimulasyonu.Api.Data;
using TurkiyeSimulasyonu.Api.Entities;
using TurkiyeSimulasyonu.Api.Search;

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
//   - Yeni öğe 0 oyla girer: dosyadaki "distribution" yok sayılır (sahte oy
//     yok). "createdAt" verilmezse içe aktarma anı yazılır.
//
// Kullanım:
//   dotnet run --project TurkiyeSimulasyonu.Importer -- <dosya-veya-klasor> [...]
//   dotnet run --project TurkiyeSimulasyonu.Importer -- --connection "<cs>" --update out/
//   dotnet run --project TurkiyeSimulasyonu.Importer -- --dry-run out/
//
// Bakım komutu (dosya almaz, tek başına çalışır):
//   dotnet run --project TurkiyeSimulasyonu.Importer -- --backfill-search [--dry-run]
//   dotnet run --project TurkiyeSimulasyonu.Importer -- --connection "<cs>" --backfill-search
//   dotnet run --project TurkiyeSimulasyonu.Importer -- --recount-votes [--dry-run]
//
// Bağlantı sırası: --connection > ConnectionStrings__Default ortam değişkeni
// > appsettings.Development.json'daki yerel LocalDB adresi (varsayılan).
// ---------------------------------------------------------------------------

const string DefaultLocalConnection =
    "Server=(localdb)\\MSSQLLocalDB;Database=TurkiyeSimulasyonu;Trusted_Connection=True";

// ItemsController'daki sabit yollar (/items/featured, /items/search, /items/my-votes):
// bu slug'la açılan öğe kendi adresinden erişilemez. Veritabanı harf duyarsız: KEBAP = kebap.
var reservedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "featured", "search", "my-votes" };

var remaining = new List<string>(args);
string? connectionString = null;
bool updateExisting = false;
bool dryRun = false;
bool backfillSearch = false;
bool recountVotes = false;

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
    else if (remaining[i] == "--backfill-search")
    {
        backfillSearch = true;
        remaining.RemoveAt(i);
    }
    else if (remaining[i] == "--recount-votes")
    {
        recountVotes = true;
        remaining.RemoveAt(i);
    }
}

connectionString ??= Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? DefaultLocalConnection;

// ---------------------------------------------------------------------------
// --recount-votes: Sayaçları (Score1Count…Score5Count) ItemVotes'tan yeniden kurar.
//
// Gerçek oyların tek kaynağı (source of truth) ItemVotes; sayaçlar onun hızlı
// okunan özeti. Seed'in sahte payı sadece sayaçlarda duruyor. Sayaçları
// ItemVotes'tan yeniden saymak gerçek oyları bire bir korur, sahte payı siler.
//
// - Gerçek çalıştırma, UPDATE'ten önce mevcut sayaçların dökümünü backups/
//   altına yazar. Dosya yazılamazsa transaction geri alınır: yedeksiz değişiklik yok.
// - --dry-run aynı UPDATE'i çalıştırıp geri alır; söylediği sayı, gerçek
//   çalıştırmanın değiştireceği satır sayısının ta kendisi.
// - İş bitince tekrar --dry-run: "değişen öğe: 0" görmek kontrol sorgusudur.
// - Tekrar çalıştırmak zararsız (idempotent): Doğru sayacı olan satıra dokunulmaz.
// ---------------------------------------------------------------------------
if (recountVotes)
{
    if (remaining.Count > 0 || backfillSearch)
    {
        Console.Error.WriteLine("--recount-votes tek başına çalışır: dosya ya da başka bir bakım komutu almaz.");
        return 1;
    }

    var recountOptions = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
    using var recountDb = new AppDbContext(recountOptions);

    // SERIALIZABLE + ItemVotes'a tablo kilidi: Sayım sürerken gelen oy, transaction bitene
    // kadar bekler (birkaç milisaniye), sonra yazılır; arada kaybolmaz.
    // Kilit sırası oy yazma yoluyla aynı (önce ItemVotes, sonra Items): kilitlenme (deadlock) oluşmaz.
    using var recountTx = recountDb.Database.BeginTransaction(IsolationLevel.Serializable);
    recountDb.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM ItemVotes WITH (TABLOCKX, HOLDLOCK);");

    var realVotes = recountDb.ItemVotes.Count();
    var totalBefore = SumAllCounters(recountDb);

    if (!dryRun)
    {
        var rows = recountDb.Items
            .AsNoTracking()
            .OrderBy(i => i.Id)
            .Select(i => new { i.Slug, i.Score1Count, i.Score2Count, i.Score3Count, i.Score4Count, i.Score5Count })
            .ToList();

        // db.json / parti dosyasıyla aynı biçim: { slug, distribution: [1★..5★] }
        var snapshot = rows.Select(r => new
        {
            slug = r.Slug,
            distribution = new[] { r.Score1Count, r.Score2Count, r.Score3Count, r.Score4Count, r.Score5Count },
        });

        Directory.CreateDirectory("backups");
        var snapshotPath = Path.GetFullPath(Path.Combine("backups", $"scores-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json"));
        File.WriteAllText(snapshotPath, JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Yedek: {rows.Count} öğenin sayaçları yazıldı -> {snapshotPath}");
    }

    // Tek komut: Her öğenin sayaçlarını ItemVotes'taki puan dağılımıyla ezer.
    // Oy almamış öğe LEFT JOIN'de NULL döner -> COALESCE ile 0.
    // WHERE sadece yanlış sayacı olan satırları seçer: dönen sayı = değişen öğe sayısı.
    var changedItems = recountDb.Database.ExecuteSqlRaw("""
        UPDATE i SET
            Score1Count = COALESCE(v.C1, 0),
            Score2Count = COALESCE(v.C2, 0),
            Score3Count = COALESCE(v.C3, 0),
            Score4Count = COALESCE(v.C4, 0),
            Score5Count = COALESCE(v.C5, 0)
        FROM Items AS i
        LEFT JOIN (
            SELECT ItemId,
                SUM(CASE WHEN Stars = 1 THEN 1 ELSE 0 END) AS C1,
                SUM(CASE WHEN Stars = 2 THEN 1 ELSE 0 END) AS C2,
                SUM(CASE WHEN Stars = 3 THEN 1 ELSE 0 END) AS C3,
                SUM(CASE WHEN Stars = 4 THEN 1 ELSE 0 END) AS C4,
                SUM(CASE WHEN Stars = 5 THEN 1 ELSE 0 END) AS C5
            FROM ItemVotes
            GROUP BY ItemId
        ) AS v ON v.ItemId = i.Id
        WHERE i.Score1Count <> COALESCE(v.C1, 0)
           OR i.Score2Count <> COALESCE(v.C2, 0)
           OR i.Score3Count <> COALESCE(v.C3, 0)
           OR i.Score4Count <> COALESCE(v.C4, 0)
           OR i.Score5Count <> COALESCE(v.C5, 0);
        """);

    // Değişmez kural (invariant): Sayaçların toplamı = gerçek oy sayısı. Tutmuyorsa yazma.
    var totalAfter = SumAllCounters(recountDb);
    if (totalAfter != realVotes)
    {
        recountTx.Rollback();
        Console.Error.WriteLine($"Tutarsızlık: ItemVotes {realVotes}, sayaç toplamı {totalAfter}. Hiçbir şey yazılmadı.");
        return 1;
    }

    Console.WriteLine($"ItemVotes'taki gerçek oy: {realVotes}");
    Console.WriteLine($"Sayaçlardaki toplam oy: önce {totalBefore}, sonra {totalAfter}");
    Console.WriteLine($"Sayacı değişen öğe: {changedItems}");

    if (dryRun)
    {
        recountTx.Rollback();
        Console.WriteLine("\nDRY RUN — UPDATE çalıştı ve geri alındı, hiçbir şey yazılmadı.");
        return 0;
    }

    recountTx.Commit();
    Console.WriteLine("\nYazıldı.");
    return 0;
}

// ---------------------------------------------------------------------------
// --backfill-search: Var olan bütün öğelerin SearchText'ini yeniden hesaplatır.
//
// Yeni sütun eklendiğinde eski satırları doldurmak için bir kez çalıştırılır
// (sektörde buna backfill denir). Değeri AppDbContext.SaveChanges hesaplıyor;
// burada yaptığımız tek şey her satıra dokunup kaydettirmek.
//
// Tekrar çalıştırmak zararsız: Değer değişmiyorsa EF o satır için UPDATE
// göndermez, yani komut kaç kez çalışırsa çalışsın sonuç aynı (idempotent).
// ---------------------------------------------------------------------------
if (backfillSearch)
{
    if (remaining.Count > 0)
    {
        Console.Error.WriteLine("--backfill-search dosya almaz; dosya içe aktarmayı ayrı çalıştır.");
        return 1;
    }

    var backfillOptions = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
    using var backfillDb = new AppDbContext(backfillOptions);

    // 1200 satır belleğe sığar; EF'in değişiklik takibi için satırların elde olması gerekiyor.
    var allItems = backfillDb.Items.ToList();

    foreach (var item in allItems)
    {
        item.SearchText = SearchNormalizer.Normalize($"{item.Name} {item.Summary}");
    }

    // Değeri gerçekten değişen satırlar "Modified" işaretlenir; değişmeyenler "Unchanged" kalır.
    var changed = backfillDb.ChangeTracker.Entries<Item>()
        .Count(e => e.State == EntityState.Modified);

    Console.WriteLine($"{allItems.Count} öğe okundu, {changed} tanesinin SearchText'i değişiyor.");

    if (dryRun)
    {
        Console.WriteLine("\nDRY RUN — hiçbir şey yazılmadı.");
        return 0;
    }

    // Tek SaveChanges = tek transaction: ya hepsi yazılır ya hiçbiri.
    backfillDb.SaveChanges();
    Console.WriteLine("\nYazıldı.");
    return 0;
}

if (remaining.Count == 0)
{
    Console.Error.WriteLine("Kullanım: dotnet run -- [--connection \"<cs>\"] [--update] [--dry-run] <dosya-veya-klasor> [...]");
    Console.Error.WriteLine("          dotnet run -- [--connection \"<cs>\"] [--dry-run] --backfill-search");
    Console.Error.WriteLine("          dotnet run -- [--connection \"<cs>\"] [--dry-run] --recount-votes");
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

int categoriesAdded = 0, itemsAdded = 0, itemsUpdated = 0, itemsSkipped = 0, fakeDistributionsIgnored = 0;

// createdAt'i olmayan yeni öğelerin hepsi aynı anı alır: "bu partide eklendi"
var importedAt = DateTimeOffset.UtcNow;

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
        // /items/featured, /items/search, /items/my-votes ile aynı slug'lı öğe kendi adresinden açılamaz
        if (reservedSlugs.Contains(item.Id))
            throw new InvalidDataException($"'{item.Id}': ayrılmış kelime, öğe slug'ı olamaz.");
        if (item.CreatedAt is { Kind: not DateTimeKind.Utc })
            throw new InvalidDataException($"'{item.Id}': createdAt UTC olmalı ('Z' ile bitmeli).");
        if (!categoriesBySlug.TryGetValue(item.CategoryId, out var category))
            throw new InvalidDataException($"'{item.Id}': kategori '{item.CategoryId}' ne veritabanında ne bu çalıştırmada eklendi.");

        if (!existingItemSlugs.Contains(item.Id))
        {
            // Eski parti dosyalarında sahte dağılım var; yeniden yüklenirken sahte oy geri gelmesin
            if (item.Distribution is { } distribution && distribution.Any(n => n != 0))
                fakeDistributionsIgnored++;

            // SearchText burada set edilmiyor: AppDbContext.SaveChanges kaydederken kendisi dolduruyor.
            // Score*Count verilmiyor: int'in varsayılanı 0, yeni öğe hiç oy almamış olarak girer.
            db.Items.Add(new Item
            {
                Slug = item.Id,
                Name = item.Name,
                Emoji = item.Emoji,
                Summary = item.Summary,
                CreatedAt = item.CreatedAt is { } created ? new DateTimeOffset(created) : importedAt,
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
if (fakeDistributionsIgnored > 0)
    Console.WriteLine($"Uyarı: {fakeDistributionsIgnored} öğenin dosyadaki distribution'ı yok sayıldı (yeni öğe 0 oyla girer).");

if (dryRun)
{
    Console.WriteLine("\nDRY RUN — yukarıdaki sayılar gerçekleşecek olanı gösterir, hiçbir şey yazılmadı.");
    return 0;
}

// Tek SaveChanges = tek transaction: ya hepsi yazılır ya hiçbiri (DbSeeder ile aynı mantık).
db.SaveChanges();
Console.WriteLine("\nYazıldı.");
return 0;

// Bütün sayaçların toplamı = sayaçlara göre toplam oy
static int SumAllCounters(AppDbContext db) =>
    db.Items.Sum(i => i.Score1Count + i.Score2Count + i.Score3Count + i.Score4Count + i.Score5Count);

// ---------------------------------------------------------------------------
// Parti dosyası formatı:
//   { "category": {id,name,emoji,order}?, "items": [{id,categoryId,name,emoji,summary,distribution?,createdAt?}] }
// distribution yok sayılır (eski dosyalarla uyum için okunuyor); createdAt yoksa içe aktarma anı.
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
    IReadOnlyList<int>? Distribution,
    DateTime? CreatedAt);

record BatchFile(BatchCategory? Category, IReadOnlyList<BatchItem> Items);