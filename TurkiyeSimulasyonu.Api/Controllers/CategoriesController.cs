using Microsoft.AspNetCore.Mvc;
using TurkiyeSimulasyonu.Api.Dtos;
using TurkiyeSimulasyonu.Api.Services;

namespace TurkiyeSimulasyonu.Api.Controllers;

[ApiController]
[Route("categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>Bütün kategorileri listeler.</summary>
    /// <remarks>`order` alanına göre sıralı döner; eşitse iç numaraya göre.</remarks>
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>Tek bir kategoriyi döner.</summary>
    /// <param name="id">Kategorinin slug'ı, ör. `yemek-kulturu`.</param>
    /// <response code="404">Bu slug'la kategori yok.</response>
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetByIdAsync(id, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}