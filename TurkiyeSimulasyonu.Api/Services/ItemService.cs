using Microsoft.EntityFrameworkCore;
using System.Globalization;
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
        var today = DateTimeOffset.UtcNow.UtcDateTime.Date;

        // Bu ISO haftasının Pazartesi'si. DayOfWeek: Pazar=0, Pazartesi=1, ... Cumartesi=6
        // olduğu için Pazartesi'ye kaç gün kaldığını bu formülle buluyoruz.
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var weekStart = new DateTimeOffset(today.AddDays(-daysSinceMonday), TimeSpan.Zero);

        // Havuz: "son 7 gün" (kayan pencere) değil, GERÇEK takvim haftası (Pazartesi'den bugüne).
        // Haftanın başında havuz küçük/boş olabilir — bu doğru, henüz o hafta az şey konuşulmuş demektir.
        // OrderBy şart: SQL Server sıralama vermeden satır sırasını garanti etmez, aşağıdaki
        // tohumlu karıştırmanın her istekte aynı sonucu vermesi için girdi sırası sabit olmalı.
        var activeSlugs = await _db.Comments
            .Where(c => c.CreatedAt >= weekStart)
            .Select(c => c.Item.Slug)
            .Distinct()
            .OrderBy(slug => slug)
            .ToArrayAsync(cancellationToken);

        // Tohum: yıl + ISO hafta numarası. Aynı hafta içindeki her istek aynı tohumu üretir
        // → sonuç o hafta boyunca sabit kalır (sayfa yenilendiğinde değişmez).
        // Hafta değişince tohum değişir → otomatik olarak yeni bir haftalık seçki.
        var seed = ISOWeek.GetYear(today) * 100 + ISOWeek.GetWeekOfYear(today);
        var weeklyRandom = new Random(seed);

        weeklyRandom.Shuffle(activeSlugs);
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

        var missing = FeaturedCount - result.Count;
        if (missing > 0)
        {
            var candidateIds = await _db.Items
                .Where(i => !pickedSlugs.Contains(i.Slug))
                .OrderBy(i => i.Id)
                .Select(i => i.Id)
                .ToArrayAsync(cancellationToken);

            weeklyRandom.Shuffle(candidateIds);
            var pickedIds = candidateIds.Take(missing).ToList();

            var fillers = await _db.Items
                .Where(i => pickedIds.Contains(i.Id))
                .Select(s_toDto)
                .ToListAsync(cancellationToken);

            result.AddRange(fillers);
        }

        var resultArray = result.ToArray();
        weeklyRandom.Shuffle(resultArray);
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