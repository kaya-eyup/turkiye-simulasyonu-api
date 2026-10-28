using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TurkiyeSimulasyonu.Api.Data;
using TurkiyeSimulasyonu.Api.Dtos;
using TurkiyeSimulasyonu.Api.Entities;
using TurkiyeSimulasyonu.Api.Search;

namespace TurkiyeSimulasyonu.Api.Services;

// Sözü tutan sınıf: nasıl yapılacak
public sealed class ItemService : IItemService
{
    private readonly AppDbContext _db;
    private const int FeaturedCount = 5;
    private static readonly TimeSpan FeaturedWindow = TimeSpan.FromDays(7);
    public const int SearchPageSize = 12;

    // EF bu tarifi okuyup SQL'e çevirir; metot olsaydı içini göremezdi
    private static readonly Expression<Func<Item, ItemDto>> s_toDto = i => new ItemDto
    {
        Id = i.Slug,
        CategoryId = i.Category.Slug,
        Name = i.Name,
        Emoji = i.Emoji,
        Summary = i.Summary,
        Distribution = new[] { i.Score1Count, i.Score2Count, i.Score3Count, i.Score4Count, i.Score5Count },
        CreatedAt = i.CreatedAt,
    };
    public ItemService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ItemDto>> GetAllAsync(string? categoryId, CancellationToken cancellationToken)
    {
        IQueryable<Item> query = _db.Items;

        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            query = query.Where(i => i.Category.Slug == categoryId);
        }

        var result = await query
            .OrderBy(i => i.Id)
            .Select(s_toDto)
            .ToListAsync(cancellationToken);

        return result;
    }

    public async Task<ItemDto?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var item = await _db.Items
            .Where(i => i.Slug == id)
            .Select(s_toDto)
            .FirstOrDefaultAsync(cancellationToken);

        return item;
    }

    public async Task<List<ItemDto>> GetFeaturedAsync(CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow - FeaturedWindow;

        // Havuz: son 7 günde EN AZ bir yorum almış bütün öğeler.
        // Kasıtlı olarak sayıya göre sıralamıyoruz: aksi halde azınlık bir grubun
        // ("X okulu vs Y okulu" tipi yorum savaşları) sürekli üst sıraları işgal
        // etmesi, bölümü sonsuza kadar kilitler. Burada tek kriter "aktif mi".
        var activeSlugs = await _db.Comments
            .Where(c => c.CreatedAt >= since)
            .Select(c => c.Item.Slug)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        Random.Shared.Shuffle(activeSlugs);
        var pickedSlugs = activeSlugs.Take(FeaturedCount).ToList();

        var result = new List<ItemDto>(FeaturedCount);

        if (pickedSlugs.Count > 0)
        {
            var items = await _db.Items
                .Where(i => pickedSlugs.Contains(i.Slug))
                .Select(s_toDto)
                .ToListAsync(cancellationToken);

            result.AddRange(items);
        }

        // Havuz 5'i doldurmadıysa (az aktif öğe varsa) kalan yer tamamen rastgele dolar
        var missing = FeaturedCount - result.Count;
        if (missing > 0)
        {
            var candidateIds = await _db.Items
                .Where(i => !pickedSlugs.Contains(i.Slug))
                .Select(i => i.Id)
                .ToArrayAsync(cancellationToken);

            Random.Shared.Shuffle(candidateIds);
            var pickedIds = candidateIds.Take(missing).ToList();

            var fillers = await _db.Items
                .Where(i => pickedIds.Contains(i.Id))
                .Select(s_toDto)
                .ToListAsync(cancellationToken);

            result.AddRange(fillers);
        }

        // "Aktif" ve "dolgu" öğeler görünüşte birbirinden ayrılmasın diye
        // son bir karıştırma: ilk sıradaki hep "gerçek seçilmiş" olmasın.
        var resultArray = result.ToArray();
        Random.Shared.Shuffle(resultArray);
        return resultArray.ToList();
    }

    public async Task<SearchResultDto> SearchAsync(string? q, int page, CancellationToken cancellationToken)
    {
        var normalized = SearchNormalizer.Normalize(q);

        // Boş sorguda veritabanına hiç gitmiyoruz: "hepsini getir" demek olurdu
        if (normalized.Length == 0)
        {
            return new SearchResultDto { Items = [], Total = 0, TotalPages = 0 };
        }

        // Süzgeç tarifi bir kez kuruluyor, iki ayrı sorguda kullanılıyor: biri sayım, biri sayfa
        var query = _db.Items.Where(i => i.SearchText.Contains(normalized));

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(i => i.Id)
            .Skip((page - 1) * SearchPageSize)
            .Take(SearchPageSize)
            .Select(s_toDto)
            .ToListAsync(cancellationToken);

        return new SearchResultDto
        {
            Items = items,
            Total = total,
            TotalPages = (int)Math.Ceiling(total / (double)SearchPageSize),
        };
    }
}