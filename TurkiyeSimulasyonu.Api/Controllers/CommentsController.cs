using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TurkiyeSimulasyonu.Api.Dtos;
using TurkiyeSimulasyonu.Api.Infrastructure;
using TurkiyeSimulasyonu.Api.Services;

namespace TurkiyeSimulasyonu.Api.Controllers;

[ApiController]
[Route("comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    /// <summary>Bir öğenin yorumlarını döner, yeniden eskiye.</summary>
    /// <param name="itemId">Zorunlu: öğenin slug'ı. Olmayan öğe 200 + boş liste verir.</param>
    [ProducesResponseType<List<CommentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [HttpGet]
    public async Task<ActionResult<List<CommentDto>>> GetByItem(string itemId, CancellationToken cancellationToken)
    {
        var result = await _commentService.GetByItemAsync(itemId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Bir öğeye yorum ekler.</summary>
    /// <remarks>`author` en fazla 30 karakter, boşsa `anonim` olur. `body` kırpıldıktan sonra 3-500 karakter olmalı. `id` ve `createdAt` sunucuda üretilir; gövdede gelirlerse yok sayılır. Olmayan öğe 400 döner (`errors.itemId`).</remarks>
    [ProducesResponseType<CommentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.CommentsWrite)]
    public async Task<ActionResult<CommentDto>> Create(
        CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var comment = await _commentService.CreateAsync(request, cancellationToken);

        if (comment is null)
        {
            ModelState.AddModelError("itemId", "Item not found.");
            return ValidationProblem(ModelState);
        }

        return StatusCode(StatusCodes.Status201Created, comment);
    }
}