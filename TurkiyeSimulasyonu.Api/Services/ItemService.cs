using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TurkiyeSimulasyonu.Api.Data;
using TurkiyeSimulasyonu.Api.Dtos;
using TurkiyeSimulasyonu.Api.Entities;

namespace TurkiyeSimulasyonu.Api.Services;

// Sözü tutan sınıf: nasıl yapılacak
public sealed class ItemService : IItemService
{
    private readonly AppDbContext _db;
    private const int FeaturedCount = 4;
    private static readonly TimeSpan FeaturedWindow = TimeSpan.FromDays(7);

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

        var topSlugs = await _db.Comments
            .Where(c => c.CreatedAt >= since)
            .GroupBy(c => c.Item.Slug)
            .Select(g => new
            {
                Slug = g.Key,
                CommentCount = g.Count()
            })
            .OrderByDescending(x => x.CommentCount)
            .ThenBy(x => x.Slug)
            .Take(FeaturedCount)
            .Select(x => x.Slug)
            .ToListAsync(cancellationToken);

        if (topSlugs.Count == 0)
        {
            return [];
        }

        var items = await _db.Items
            .Where(i => topSlugs.Contains(i.Slug))
            .Select(s_toDto)
            .ToListAsync(cancellationToken);

        var itemDict = items.ToDictionary(i => i.Id);

        return topSlugs
            .Where(slug => itemDict.ContainsKey(slug))
            .Select(slug => itemDict[slug])
            .ToList();
    }
}