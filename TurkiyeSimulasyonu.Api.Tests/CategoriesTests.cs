using System.Net;
using System.Net.Http.Json;
using TurkiyeSimulasyonu.Api.Dtos;

namespace TurkiyeSimulasyonu.Api.Tests;

[Collection("Api")]
public class CategoriesTests
{
    private readonly HttpClient _client;

    public CategoriesTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsCategoriesOrderedByOrder()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryDto>>("/categories");

        Assert.NotNull(categories);
        Assert.Equal(4, categories.Count);
        Assert.Equal([1, 2, 3, 4], categories.Select(c => c.Order));
    }

    [Fact]
    public async Task GetById_Missing_Returns404WithProblemDetails()
    {
        var response = await _client.GetAsync("/categories/olmayan-kategori");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}