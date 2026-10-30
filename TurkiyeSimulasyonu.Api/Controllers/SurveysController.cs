using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TurkiyeSimulasyonu.Api.Dtos;
using TurkiyeSimulasyonu.Api.Infrastructure;
using TurkiyeSimulasyonu.Api.Services;

namespace TurkiyeSimulasyonu.Api.Controllers;

[ApiController]
[Route("surveys/rebirth")]
public class SurveysController : ControllerBase
{
    private readonly IRebirthSurveyService _surveyService;

    public SurveysController(IRebirthSurveyService surveyService)
    {
        _surveyService = surveyService;
    }

    [HttpGet]
    public async Task<ActionResult<RebirthResultsDto>> GetResults(CancellationToken cancellationToken)
    {
        var results = await _surveyService.GetResultsAsync(cancellationToken);
        return Ok(results);
    }

    [ProducesResponseType<RebirthVoteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [HttpGet("my-vote")]
    public async Task<ActionResult<RebirthVoteDto>> GetMyVote(
        [FromHeader(Name = VoterIdHeader.Name)] Guid? voterId,
        CancellationToken cancellationToken)
    {
        // GUID olmayan değer ("abc") buraya gelmeden 400 olur; başlığın hiç olmaması null gelir
        if (voterId is not { } id)
        {
            return MissingVoterId();
        }

        var vote = await _surveyService.GetVoteAsync(id, cancellationToken);

        // Henüz oy yok → kaynak yok → 404 (frontend bunu "oy vermedin" diye okur)
        if (vote is null)
        {
            return NotFound();
        }

        return Ok(vote);
    }

    [ProducesResponseType<RebirthVoteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [HttpPut("my-vote")]
    [EnableRateLimiting(RateLimitPolicies.SurveyWrite)]
    public async Task<ActionResult<RebirthVoteDto>> PutMyVote(
        [FromHeader(Name = VoterIdHeader.Name)] Guid? voterId,
        PutRebirthVoteRequest request,
        CancellationToken cancellationToken)
    {
        if (voterId is not { } id)
        {
            return MissingVoterId();
        }

        // İlk oy da değişiklik de 200: PUT "son hâl bu olsun" der, istemci ayrımı kullanmıyor
        var vote = await _surveyService.UpsertVoteAsync(id, request.Country, cancellationToken);
        return Ok(vote);
    }

    private ActionResult MissingVoterId()
    {
        ModelState.AddModelError(VoterIdHeader.Name, "Header is required.");
        return ValidationProblem(ModelState);
    }
}