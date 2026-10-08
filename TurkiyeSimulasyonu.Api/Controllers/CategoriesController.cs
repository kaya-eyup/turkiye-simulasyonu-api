using Microsoft.AspNetCore.Mvc;
using TurkiyeSimulasyonu.Api.Models;
using TurkiyeSimulasyonu.Api.Data;


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
    public ActionResult<List<Category>> GetAll()
    {
        var result = _data.Categories
            .OrderBy(c => c.Order)
            .ToList();

        return Ok(result);
    }
}