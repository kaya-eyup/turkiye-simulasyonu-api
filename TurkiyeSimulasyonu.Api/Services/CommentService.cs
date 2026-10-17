using Microsoft.EntityFrameworkCore;
using TurkiyeSimulasyonu.Api.Data;
using TurkiyeSimulasyonu.Api.Dtos;
using TurkiyeSimulasyonu.Api.Entities;

namespace TurkiyeSimulasyonu.Api.Services;

public sealed class CommentService : ICommentService
{
    private readonly AppDbContext _db;

    public CommentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CommentDto?> CreateAsync(CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var item = await _db.Items
        .Where(i => i.Slug == request.ItemId)
        .Select(i => new { i.Id, i.Slug })
        .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return null;
        }

        // 2. Yorum nesnesini oluşturma
        var comment = new Comment
        {
            ItemId = item.Id,
            Author = request.Author,
            Body = request.Body,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // 3. Veritabanına kaydetme
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(cancellationToken);

        // 4. DTO'yu kurma ve dönme (Slug bilgisini eşleyerek)
        return new CommentDto
        {
            Id = comment.Id.ToString(),
            ItemId = item.Slug,
            Author = comment.Author,
            Body = comment.Body,
            CreatedAt = comment.CreatedAt
        };
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