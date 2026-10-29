using TurkiyeSimulasyonu.Api.Dtos;

namespace TurkiyeSimulasyonu.Api.Services;

public interface IRebirthSurveyService
{
    Task<RebirthResultsDto> GetResultsAsync(CancellationToken cancellationToken);
    Task<RebirthVoteDto?> GetVoteAsync(Guid voterId, CancellationToken cancellationToken);
    Task<RebirthVoteDto> UpsertVoteAsync(Guid voterId, string countryCode, CancellationToken cancellationToken);
}
