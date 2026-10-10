using Microsoft.AspNetCore.Mvc;
using TurkiyeSimulasyonu.Api.Models;
using TurkiyeSimulasyonu.Api.Data;


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
    public ActionResult<Item> GetById(string id)
    {
        var item = _data.Items.FirstOrDefault(i => i.Id == id);

        if (item is null)
        {
            return NotFound();
        }
        return Ok(item);
    }
    [HttpGet]
    public ActionResult<List<Item>> GetAll(string? categoryId)
    {
        
        IEnumerable<Item> query = _data.Items;

        
        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            query = query.Where(i => i.CategoryId == categoryId);
        }

        return Ok(query.ToList());
    }
}
