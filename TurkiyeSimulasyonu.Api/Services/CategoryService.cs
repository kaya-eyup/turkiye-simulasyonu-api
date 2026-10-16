using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TurkiyeSimulasyonu.Api.Data;
using TurkiyeSimulasyonu.Api.Dtos;
using TurkiyeSimulasyonu.Api.Entities;

namespace TurkiyeSimulasyonu.Api.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    // EF Core'un SQL'e çevirebilmesi için Expression kullanıyoruz
    private static readonly Expression<Func<Category, CategoryDto>> s_toDto = c => new CategoryDto
    {
        Id = c.Slug,
        Name = c.Name,
        Emoji = c.Emoji,
        Order = c.DisplayOrder,
    };
    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategoryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await _db.Categories
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Id)
            .Select(s_toDto)
            .ToListAsync(cancellationToken);

        return result;
    }

    public async Task<CategoryDto?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _db.Categories
            .Where(c => c.Slug == id)
            .Select(s_toDto)
            .FirstOrDefaultAsync(cancellationToken);

        return result;
    }
}