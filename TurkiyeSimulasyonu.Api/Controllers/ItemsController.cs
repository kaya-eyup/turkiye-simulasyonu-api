using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurkiyeSimulasyonu.Api.Data;
using TurkiyeSimulasyonu.Api.Dtos;
using TurkiyeSimulasyonu.Api.Entities;


namespace TurkiyeSimulasyonu.Api.Controllers;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ItemsController(AppDbContext data)
    {
        _db = data;
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<ItemDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var item = await _db.Items
            .Where(i => i.Slug == id)
            .Select(i => new ItemDto
            {
                Id = i.Slug,
                CategoryId = i.Category.Slug,
                Name = i.Name,
                Emoji = i.Emoji,
                Summary = i.Summary,
                Distribution = new[] { i.Score1Count, i.Score2Count, i.Score3Count, i.Score4Count, i.Score5Count },
                CreatedAt = i.CreatedAt,
            }).FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return NotFound();
        }
        return Ok(item);
    }
    [HttpGet]
    public async Task<ActionResult<List<ItemDto>>> GetAll(string? categoryId, CancellationToken cancellationToken)
    {
        IQueryable<Item> query = _db.Items;

        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            query = query.Where(i => i.Category.Slug == categoryId);
        }

        var result = await query
            .OrderBy(i => i.Id)
            .Select(i => new ItemDto
            {
                Id = i.Slug,
                CategoryId = i.Category.Slug,
                Name = i.Name,
                Emoji = i.Emoji,
                Summary = i.Summary,
                Distribution = new[] { i.Score1Count, i.Score2Count, i.Score3Count, i.Score4Count, i.Score5Count },
                CreatedAt = i.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }
}
