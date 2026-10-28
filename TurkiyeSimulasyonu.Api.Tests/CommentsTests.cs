using System.Net;
using System.Net.Http.Json;
using TurkiyeSimulasyonu.Api.Dtos;

namespace TurkiyeSimulasyonu.Api.Tests;

[Collection("Api")]
public class CommentsTests
{
    private readonly HttpClient _client;

    public CommentsTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private Task<HttpResponseMessage> Post(string itemId, string? author, string body) =>
        _client.PostAsJsonAsync("/comments", new { itemId, author, body });

    [Fact]
    public async Task Create_WithValidBody_Returns201()
    {
        var response = await Post("baklava", "Ahmet", "Harika bir lezzet.");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var comment = await response.Content.ReadFromJsonAsync<CommentDto>();
        Assert.NotNull(comment);
        Assert.Equal("baklava", comment.ItemId);
        Assert.Equal("Ahmet", comment.Author);
    }

    // Kenar değerler: tam 30 karakterlik author ve tam 500 karakterlik body kabul edilir
    [Fact]
    public async Task Create_WithMaxLengths_Returns201()
    {
        var response = await Post("baklava", new string('a', 30), new string('b', 500));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithTooLongBody_Returns400()
    {
        var response = await Post("baklava", "Ahmet", new string('b', 501));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithTooLongAuthor_Returns400()
    {
        var response = await Post("baklava", new string('a', 31), "Harika bir lezzet.");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Kırpılınca 2 karakter kalıyor: ham uzunluk (8) kuralı geçse de reddedilmeli
    [Fact]
    public async Task Create_WithBodyTooShortAfterTrim_Returns400()
    {
        var response = await Post("baklava", "Ahmet", "   ab   ");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithBlankAuthor_UsesAnonim()
    {
        var response = await Post("baklava", "   ", "Harika bir lezzet.");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var comment = await response.Content.ReadFromJsonAsync<CommentDto>();
        Assert.NotNull(comment);
        Assert.Equal("anonim", comment.Author);
    }

    [Fact]
    public async Task Create_WithUnknownItem_Returns400()
    {
        var response = await Post("olmayan-oge", "Ahmet", "Harika bir lezzet.");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetByItem_WithoutItemId_Returns400()
    {
        var response = await _client.GetAsync("/comments");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}