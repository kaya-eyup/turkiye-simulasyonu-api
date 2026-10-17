using Microsoft.AspNetCore.Mvc;
using TurkiyeSimulasyonu.Api.Dtos;
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

    [HttpGet]
    public async Task<ActionResult<List<CommentDto>>> GetByItem(string itemId, CancellationToken cancellationToken)
    {
        var result = await _commentService.GetByItemAsync(itemId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> Create(
    CreateCommentRequest request,
    CancellationToken cancellationToken)
    {
        var comment = await _commentService.CreateAsync(request, cancellationToken);

        if (comment is null)
        {
            ModelState.AddModelError("itemId", "Belirtilen slug değerine sahip öğe bulunamadı.");
            return ValidationProblem(ModelState);
        }

        return StatusCode(StatusCodes.Status201Created, comment);
    }
}