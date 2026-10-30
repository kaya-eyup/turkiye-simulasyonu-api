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

    /// <summary>Öğeleri listeler (sayfasız).</summary>
    /// <param name="categoryId">Verilirse yalnızca bu kategorinin öğeleri döner. Olmayan kategori 200 + boş liste verir; boş değer süzgeç koymaz.</param>
    /// <remarks>`Id` sırasıyla döner. Arama için `/items/search` kullanılır.</remarks>
    [HttpGet]
    public async Task<ActionResult<List<ItemDto>>> GetAll(string? categoryId, CancellationToken cancellationToken)
    {
        var items = await _itemService.GetAllAsync(categoryId, cancellationToken);
        return Ok(items);
    }

    /// <summary>Haftanın öne çıkan 5 öğesini döner.</summary>
    /// <remarks>Bu takvim haftasında yorum almış öğelerden haftalık sabit bir seçki; eksik kalırsa rastgele öğelerle tamamlanır. Aynı hafta içinde her istek aynı seçkiyi döner.</remarks>
    [HttpGet("featured")]
    public async Task<ActionResult<List<ItemDto>>> GetFeatured(CancellationToken cancellationToken)
    {
        var featuredItems = await _itemService.GetFeaturedAsync(cancellationToken);
        return Ok(featuredItems);
    }

    /// <summary>Öğelerde arama yapar: Türkçe karakterden bağımsız, sayfalı.</summary>
    /// <param name="q">Aranan metin, en fazla 100 karakter. `ığdır` ile `igdir` aynı sonucu verir. Boş ya da eşleşmeyen arama 200 + `total: 0` döner.</param>
    /// <param name="page">Sayfa numarası, 1'den başlar; sayfa başına 12 öğe. Son sayfanın ötesi 200 + boş `items` döner.</param>
    [ProducesResponseType<SearchResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [HttpGet("search")]
    public async Task<ActionResult<SearchResultDto>> Search(
    [StringLength(100)] string? q,
    [Range(1, int.MaxValue)] int page = 1,
    CancellationToken cancellationToken = default)
    {
        var result = await _itemService.SearchAsync(q, page, cancellationToken);
        return Ok(result);
    }

    /// <summary>Tek bir öğeyi döner.</summary>
    /// <param name="id">Öğenin slug'ı. Büyük/küçük harf fark etmez: `KEBAP` istenirse cevapta kanonik `kebap` döner.</param>
    /// <response code="404">Bu slug'la öğe yok.</response>
    [ProducesResponseType<ItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
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