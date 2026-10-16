using TurkiyeSimulasyonu.Api.Dtos;

namespace TurkiyeSimulasyonu.Api.Services;

// Söz listesi: ne yapılacak (gövde yok)
public interface IItemService
{
    Task<List<ItemDto>> GetAllAsync(string? categoryId, CancellationToken cancellationToken);
    Task<ItemDto?> GetByIdAsync(string id, CancellationToken cancellationToken);
}
