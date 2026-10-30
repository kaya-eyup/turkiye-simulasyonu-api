using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TurkiyeSimulasyonu.Api.Dtos;
using TurkiyeSimulasyonu.Api.Infrastructure;
using TurkiyeSimulasyonu.Api.Services;

namespace TurkiyeSimulasyonu.Api.Controllers;

// Adres öğelerle aynı (/items), sınıf ayrı: ItemsController sadece okur, bu sınıf oy yazar
[ApiController]
[Route("items")]
public class ItemVotesController : ControllerBase
{
    private readonly IItemVoteService _voteService;

    public ItemVotesController(IItemVoteService voteService)
    {
        _voteService = voteService;
    }

    // "my-votes" sabit bir parça: yönlendirme sabit parçayı {id}'den önce dener,
    // bu yüzden bu istek ItemsController'daki GET /items/{id}'ye düşmez
    [ProducesResponseType<List<ItemVoteDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [HttpGet("my-votes")]
    public async Task<ActionResult<List<ItemVoteDto>>> GetMyVotes(
        [FromHeader(Name = VoterIdHeader.Name)] Guid? voterId,
        CancellationToken cancellationToken)
    {
        if (voterId is not { } voter)
        {
            return MissingVoterId();
        }

        // Hiç oy yoksa boş liste: "oyların" koleksiyonu var, sadece boş (404 değil)
        var votes = await _voteService.GetVotesAsync(voter, cancellationToken);
        return Ok(votes);
    }

    [ProducesResponseType<ItemVoteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [HttpPut("{id}/my-vote")]
    [EnableRateLimiting(RateLimitPolicies.ItemVotesWrite)]
    public async Task<ActionResult<ItemVoteDto>> PutMyVote(
        string id,
        [FromHeader(Name = VoterIdHeader.Name)] Guid? voterId,
        PutItemVoteRequest request,
        CancellationToken cancellationToken)
    {
        if (voterId is not { } voter)
        {
            return MissingVoterId();
        }

        var vote = await _voteService.UpsertVoteAsync(voter, id, request.Stars, cancellationToken);

        if (vote is null)
        {
            return NotFound();
        }

        return Ok(vote);
    }

    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [HttpDelete("{id}/my-vote")]
    [EnableRateLimiting(RateLimitPolicies.ItemVotesWrite)]
    public async Task<IActionResult> DeleteMyVote(
        string id,
        [FromHeader(Name = VoterIdHeader.Name)] Guid? voterId,
        CancellationToken cancellationToken)
    {
        if (voterId is not { } voter)
        {
            return MissingVoterId();
        }

        var itemExists = await _voteService.DeleteVoteAsync(voter, id, cancellationToken);

        // 204: gövde yok, "artık oyun yok" durumu sağlandı (oy zaten yoksa da)
        return itemExists ? NoContent() : NotFound();
    }

    private ActionResult MissingVoterId()
    {
        ModelState.AddModelError(VoterIdHeader.Name, "Header is required.");
        return ValidationProblem(ModelState);
    }
}