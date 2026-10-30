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

    /// <summary>Yeniden Doğma Anketi sonuçlarını döner.</summary>
    /// <remarks>Ülke koduna göre oy sayıları, çoktan aza; eşitse koda göre. Sayım her istekte veritabanında yapılır.</remarks>
    [HttpGet]
    public async Task<ActionResult<RebirthResultsDto>> GetResults(CancellationToken cancellationToken)
    {
        var results = await _surveyService.GetResultsAsync(cancellationToken);
        return Ok(results);
    }

    /// <summary>Çağıranın anket oyunu döner.</summary>
    /// <remarks>Kimlik `X-Voter-Id` başlığından gelir.</remarks>
    /// <response code="404">Bu kimlik henüz oy vermemiş.</response>
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

    /// <summary>Çağıranın anket oyunu belirler ya da değiştirir.</summary>
    /// <remarks>PUT: aynı isteği tekrarlamak sonucu değiştirmez. İlk oy da değişiklik de 200 döner. `country` ISO 3166-1 alfa-2 kodu olmalı (ör. `TR`); büyük/küçük harf fark etmez.</remarks>
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