using System.Net;
using System.Net.Http.Json;
using TurkiyeSimulasyonu.Api.Dtos;

namespace TurkiyeSimulasyonu.Api.Tests;

[Collection("Api")]
public class ItemsTests
{
    private readonly HttpClient _client;

    public ItemsTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetById_Missing_Returns404()
    {
        var response = await _client.GetAsync("/items/olmayan-oge");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Gerçek SQL Server'ın harf duyarsız karşılaştırması: KEBAP, kebap ile aynı kaydı bulur
    [Fact]
    public async Task GetById_UppercaseId_ReturnsCanonicalSlug()
    {
        var item = await _client.GetFromJsonAsync<ItemDto>("/items/KEBAP");

        Assert.NotNull(item);
        Assert.Equal("kebap", item.Id);
    }

    [Fact]
    public async Task Search_FindsItemIgnoringTurkishCharacters()
    {
        var result = await _client.GetFromJsonAsync<SearchResultDto>("/items/search?q=cig");

        Assert.NotNull(result);
        Assert.Contains(result.Items, i => i.Id == "cig-kofte");
    }

    // Aynı öğe, Türkçe büyük harfle aranınca da bulunmalı
    [Fact]
    public async Task Search_UppercaseTurkishQuery_FindsSameItem()
    {
        var result = await _client.GetFromJsonAsync<SearchResultDto>(
            $"/items/search?q={Uri.EscapeDataString("ÇİĞ")}");

        Assert.NotNull(result);
        Assert.Contains(result.Items, i => i.Id == "cig-kofte");
    }

    [Fact]
    public async Task Search_EmptyQuery_ReturnsEmptyResult()
    {
        var result = await _client.GetFromJsonAsync<SearchResultDto>("/items/search?q=");

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task Search_NoMatch_ReturnsEmptyResult()
    {
        var result = await _client.GetFromJsonAsync<SearchResultDto>("/items/search?q=zzzzzzzz");

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    // Son sayfanın ötesi: öğe gelmez ama toplam değişmez
    [Fact]
    public async Task Search_PageBeyondRange_ReturnsEmptyItemsButKeepsTotal()
    {
        var first = await _client.GetFromJsonAsync<SearchResultDto>("/items/search?q=a");
        var far = await _client.GetFromJsonAsync<SearchResultDto>("/items/search?q=a&page=999");

        Assert.NotNull(first);
        Assert.NotNull(far);
        Assert.Empty(far.Items);
        Assert.Equal(first.Total, far.Total);
    }

    [Fact]
    public async Task Search_FirstPage_RespectsPageSize()
    {
        var result = await _client.GetFromJsonAsync<SearchResultDto>("/items/search?q=a");

        Assert.NotNull(result);
        Assert.True(result.Items.Count <= 12);
        Assert.Equal((int)Math.Ceiling(result.Total / 12.0), result.TotalPages);
    }

    [Fact]
    public async Task Search_TooLongQuery_Returns400()
    {
        var response = await _client.GetAsync($"/items/search?q={new string('a', 101)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_InvalidPage_Returns400()
    {
        var response = await _client.GetAsync("/items/search?q=kebap&page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}