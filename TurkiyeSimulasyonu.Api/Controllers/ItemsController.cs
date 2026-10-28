using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TurkiyeSimulasyonu.Api.Dtos;
using TurkiyeSimulasyonu.Api.Services;

namespace TurkiyeSimulasyonu.Api.Controllers;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private readonly IItemService _itemService;

    public ItemsController(IItemService itemService)
    {
        _itemService = itemService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ItemDto>>> GetAll(string? categoryId, CancellationToken cancellationToken)
    {
        var items = await _itemService.GetAllAsync(categoryId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("featured")]
    public async Task<ActionResult<List<ItemDto>>> GetFeatured(CancellationToken cancellationToken)
    {
        var featuredItems = await _itemService.GetFeaturedAsync(cancellationToken);
        return Ok(featuredItems);
    }

    [HttpGet("search")]
    public async Task<ActionResult<SearchResultDto>> Search(
    [StringLength(100)] string? q,
    [Range(1, int.MaxValue)] int page = 1,
    CancellationToken cancellationToken = default)
    {
        var result = await _itemService.SearchAsync(q, page, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ItemDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var item = await _itemService.GetByIdAsync(id, cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }
}