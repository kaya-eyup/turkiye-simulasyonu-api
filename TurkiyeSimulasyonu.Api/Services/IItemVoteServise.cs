using TurkiyeSimulasyonu.Api.Dtos;

namespace TurkiyeSimulasyonu.Api.Services;

public interface IItemVoteService
{
    Task<List<ItemVoteDto>> GetVotesAsync(Guid voterId, CancellationToken cancellationToken);

    // null: öğe yok
    Task<ItemVoteDto?> UpsertVoteAsync(
        Guid voterId, string itemSlug, int stars, CancellationToken cancellationToken);

    // false: öğe yok. Öğe var ama oy yoksa da true: "oyum olmasın" zaten sağlanmış
    Task<bool> DeleteVoteAsync(Guid voterId, string itemSlug, CancellationToken cancellationToken);
}