using Microsoft.EntityFrameworkCore;
using TurkiyeSimulasyonu.Api.Data;
using TurkiyeSimulasyonu.Api.Dtos;

namespace TurkiyeSimulasyonu.Api.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<CategoryDto?> GetByIdAsync(string id, CancellationToken cancellationToken);
}

public sealed class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategoryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await _db.Categories
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Id)
            .Select(c => new CategoryDto
            {
                Id = c.Slug,
                Name = c.Name,
                Emoji = c.Emoji,
                Order = c.DisplayOrder,
            })
            .ToListAsync(cancellationToken);

        return result;
    }

    public async Task<CategoryDto?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _db.Categories
            .Where(c => c.Slug == id)
            .Select(c => new CategoryDto
            {
                Id = c.Slug,
                Name = c.Name,
                Emoji = c.Emoji,
                Order = c.DisplayOrder,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return result;
    }
}