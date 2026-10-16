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
}