using TurkiyeSimulasyonu.Api.Dtos;

namespace TurkiyeSimulasyonu.Api.Services;

public interface ICommentService
{
    Task<List<CommentDto>> GetByItemAsync(string itemId, CancellationToken cancellationToken);
}
