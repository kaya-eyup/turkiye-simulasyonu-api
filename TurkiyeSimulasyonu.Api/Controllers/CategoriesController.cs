using Microsoft.AspNetCore.Mvc;
using TurkiyeSimulasyonu.Api.Models;

namespace TurkiyeSimulasyonu.Api.Controllers;

[ApiController]
[Route("categories")]
public class CategoriesController : ControllerBase
{
    private static readonly Category[] Categories =
    [
   new("adet-gelenekler", "Adet/Gelenekler", "🧿", 3),
   new("yemek-kulturu", "Yemek Kültürü", "🍽️", 1),
   new("absurtluk", "Absürtlük", "🤪", 4),
   new("sehir-ilce", "Şehir/İlçe", "🏙️", 2)
    ];

    [HttpGet]
    public ActionResult<List<Category>> GetAll()
    {
        var result = Categories
            .OrderBy(c => c.Order)
            .ToList();

        return Ok(result);
    }
}