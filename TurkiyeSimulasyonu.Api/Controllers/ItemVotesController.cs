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
    /// <summary>Çağıranın bütün öğe oylarını döner.</summary>
    /// <remarks>Hiç oy yoksa 200 + boş liste.</remarks>
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

    /// <summary>Bir öğeye puan verir ya da puanı değiştirir.</summary>
    /// <param name="id">Öğenin slug'ı.</param>
    /// <remarks>`stars` 1-5 olmalı. Oy satırı ve öğenin dağılım sayaçları aynı transaction'da yazılır. İlk oy da değişiklik de 200 döner.</remarks>
    /// <response code="404">Bu slug'la öğe yok.</response>
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

    /// <summary>Çağıranın bir öğedeki oyunu siler.</summary>
    /// <param name="id">Öğenin slug'ı.</param>
    /// <remarks>Oy zaten yoksa da 204 döner.</remarks>
    /// <response code="404">Bu slug'la öğe yok.</response>
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