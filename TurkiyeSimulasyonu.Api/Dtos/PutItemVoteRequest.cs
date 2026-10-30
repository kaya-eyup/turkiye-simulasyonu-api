using System.ComponentModel.DataAnnotations;

namespace TurkiyeSimulasyonu.Api.Dtos;

public record PutItemVoteRequest
{
    // Gövdede hiç yoksa 0 olur; 0 da aralık dışı olduğu için yine 400
    /// <summary>Verilecek puan, 1-5 arası.</summary>
    [Range(1, 5)]
    public int Stars { get; init; }
}