using Microsoft.AspNetCore.Mvc;
using TurkiyeSimulasyonu.Api.Data.Seed;


namespace TurkiyeSimulasyonu.Api.Controllers;

[ApiController]                                     
[Route("items")]
public class ItemsController : ControllerBase
{
    private readonly SeedData _data;
    public ItemsController(SeedData data)
    {
        _data = data;
    }
    [HttpGet("{id}")]
    public ActionResult<SeedItem> GetById(string id)
    {
        var item = _data.Items.FirstOrDefault(i => i.Id == id);

        if (item is null)
        {
            return NotFound();
        }
        return Ok(item);
    }
    [HttpGet]
    public ActionResult<List<SeedItem>> GetAll(string? categoryId)
    {
        
        IEnumerable<SeedItem> query = _data.Items;

        
        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            query = query.Where(i => i.CategoryId == categoryId);
        }

        return Ok(query.ToList());
    }
}
