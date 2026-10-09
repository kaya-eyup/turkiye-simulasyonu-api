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
        // 1. Başlangıç: Sorguyu bir IEnumerable (plan) olarak başlat.
        // AsEnumerable() diyerek bunun henüz bitmiş bir liste olmadığını, 
        // üzerine filtreler eklenebilecek bir veri kümesi olduğunu belirtiyoruz.
        IEnumerable<Item> query = _data.Items.AsEnumerable();

        // 2. Filtre Zinciri
        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            query = query.Where(i => i.CategoryId == categoryId);
        }

        // 3. Çalıştırma: ToList() çağrıldığı an yukarıdaki tüm filtreler hesaplanır.
        return Ok(query.ToList());
    }
}
