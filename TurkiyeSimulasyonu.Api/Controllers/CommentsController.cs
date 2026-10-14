using Microsoft.AspNetCore.Mvc;
using TurkiyeSimulasyonu.Api.Data.Seed;

namespace TurkiyeSimulasyonu.Api.Controllers;

[ApiController]
[Route("comments")]
public class CommentsController : ControllerBase
{
    private readonly SeedData _data;

    public CommentsController(SeedData data)
    {
        _data = data;
    }

    [HttpGet]
    public ActionResult<List<SeedComment>> GetByItem(string itemId)
    {
        var result = _data.Comments
            .Where(c => c.ItemId == itemId)
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id, StringComparer.Ordinal)
            .ToList();

        return Ok(result);
    }
}
