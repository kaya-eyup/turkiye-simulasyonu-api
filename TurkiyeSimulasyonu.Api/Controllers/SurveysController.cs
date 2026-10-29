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
    // v1'de giriş yok: tarayıcı kendi ürettiği kimliği bu başlıkla gönderir.
    // Giriş gelince yerini Authorization başlığı alır; gövde ({ country }) değişmez.
    public const string VoterIdHeader = "X-Voter-Id";

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

    [HttpGet("my-vote")]
    public async Task<ActionResult<RebirthVoteDto>> GetMyVote(
        [FromHeader(Name = VoterIdHeader)] Guid? voterId,
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

    [HttpPut("my-vote")]
    [EnableRateLimiting(RateLimitPolicies.SurveyWrite)]
    public async Task<ActionResult<RebirthVoteDto>> PutMyVote(
        [FromHeader(Name = VoterIdHeader)] Guid? voterId,
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
        ModelState.AddModelError(VoterIdHeader, "Header is required.");
        return ValidationProblem(ModelState);
    }
}