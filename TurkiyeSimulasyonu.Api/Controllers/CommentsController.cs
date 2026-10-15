using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurkiyeSimulasyonu.Api.Data;
using TurkiyeSimulasyonu.Api.Dtos;

namespace TurkiyeSimulasyonu.Api.Controllers;

[ApiController]
[Route("comments")]
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CommentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<CommentDto>>> GetByItem(string itemId, CancellationToken cancellationToken)
    {
        var result = await _db.Comments
            .Where(c => c.Item.Slug == itemId)
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Select(c => new CommentDto
            {
                Id = c.Id.ToString(),
                ItemId = c.Item.Slug,
                Author = c.Author,
                Body = c.Body,
                CreatedAt = c.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }
}