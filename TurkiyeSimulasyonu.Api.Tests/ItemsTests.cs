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
}