using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurkiyeSimulasyonu.Api.Data;
using TurkiyeSimulasyonu.Api.Dtos;

namespace TurkiyeSimulasyonu.Api.Controllers;

[ApiController]
[Route("categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _db.Categories
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Id)
            .Select(c => new CategoryDto
            {
                Id = c.Slug,
                Name = c.Name,
                Emoji = c.Emoji,
                Order = c.DisplayOrder
            })
       .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _db.Categories
            .Where(c => c.Slug == id)
            .Select(c => new CategoryDto
            {
                Id = c.Slug,
                Name = c.Name,
                Emoji = c.Emoji,
                Order = c.DisplayOrder
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}