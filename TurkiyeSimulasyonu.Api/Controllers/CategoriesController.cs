using Microsoft.AspNetCore.Mvc;
using TurkiyeSimulasyonu.Api.Data.Seed;


namespace TurkiyeSimulasyonu.Api.Controllers;

[ApiController]
[Route("categories")]
public class CategoriesController : ControllerBase
{

    private readonly SeedData _data;

    public CategoriesController(SeedData data)
    {
        _data = data;
    }
    [HttpGet]
    public ActionResult<List<SeedCategory>> GetAll()
    {
        var result = _data.Categories
            .OrderBy(c => c.Order)
            .ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public ActionResult<SeedCategory> GetById(string id)
    {
        var category = _data.Categories.FirstOrDefault(c => c.Id == id);

        if (category is null)
        {
            return NotFound();
        }

        
        return Ok(category);
    }

}