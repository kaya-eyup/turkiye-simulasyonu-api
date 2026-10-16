using Microsoft.EntityFrameworkCore;
using TurkiyeSimulasyonu.Api.Data;
using TurkiyeSimulasyonu.Api.Dtos;

namespace TurkiyeSimulasyonu.Api.Services;

public sealed class CommentService : ICommentService
{
    private readonly AppDbContext _db;

    public CommentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CommentDto>> GetByItemAsync(string itemId, CancellationToken cancellationToken)
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

        return result;
    }
}