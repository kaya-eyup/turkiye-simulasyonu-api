using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TurkiyeSimulasyonu.Api.Tests;

// Belge de sözleşmenin parçası: biri bir ucun etiketini silerse ya da belge üretimi bozulursa CI kırmızıya döner
[Collection("Api")]
public class OpenApiTests
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public OpenApiTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<JsonNode> GetDocument()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var document = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.NotNull(document);
        return document;
    }

    private static JsonNode Operation(JsonNode document, string path, string method)
    {
        var operation = document["paths"]?[path]?[method];
        Assert.True(operation is not null, $"Belgede {method.ToUpper()} {path} yok.");
        return operation;
    }

    [Fact]
    public async Task Document_IsServed_AndListsTheRoutes()
    {
        var document = await GetDocument();

        Assert.Equal("Türkiye Simülasyonu API", (string?)document["info"]?["title"]);

        foreach (var (path, method) in new[]
        {
            ("/categories", "get"),
            ("/items/search", "get"),
            ("/items/featured", "get"),
            ("/comments", "post"),
            ("/items/{id}/my-vote", "put"),
            ("/surveys/rebirth/my-vote", "put"),
        })
        {
            Operation(document, path, method);
        }
    }

    [Fact]
    public async Task RateLimitedEndpoints_Declare429_AndOthersDoNot()
    {
        var document = await GetDocument();

        var with429 = new List<string>();
        foreach (var (path, pathItem) in document["paths"]!.AsObject())
        {
            foreach (var (method, operation) in pathItem!.AsObject())
            {
                if (operation?["responses"]?["429"] is not null)
                {
                    with429.Add($"{method.ToUpper()} {path}");
                }
            }
        }

        // Yeni bir yazma ucuna [EnableRateLimiting] konunca bu liste de güncellenmeli: bilerek elle yazılı
        Assert.Equal(
            new[]
            {
                "DELETE /items/{id}/my-vote",
                "POST /comments",
                "PUT /items/{id}/my-vote",
                "PUT /surveys/rebirth/my-vote",
            },
            with429.Order().ToArray());
    }

    [Fact]
    public async Task VoterIdHeader_IsDocumentedAsRequired()
    {
        var document = await GetDocument();

        var parameters = Operation(document, "/items/my-votes", "get")["parameters"]!.AsArray();
        var header = parameters.Single(p => (string?)p?["name"] == "X-Voter-Id");

        Assert.Equal("header", (string?)header?["in"]);
        Assert.True((bool?)header?["required"]);
    }

    [Fact]
    public async Task PostComments_Declares201_NotJust200()
    {
        var document = await GetDocument();

        var responses = Operation(document, "/comments", "post")["responses"]!.AsObject();

        Assert.True(responses.ContainsKey("201"));
        Assert.True(responses.ContainsKey("400"));
    }

    [Fact]
    public async Task ErrorResponses_UseProblemJson_AndSuccessUsesJson()
    {
        var document = await GetDocument();

        var responses = Operation(document, "/items/{id}", "get")["responses"]!.AsObject();

        Assert.NotNull(responses["200"]?["content"]?["application/json"]);
        Assert.NotNull(responses["404"]?["content"]?["application/problem+json"]);
        Assert.Null(responses["404"]?["content"]?["text/plain"]);
    }

    [Fact]
    public async Task SwaggerUi_IsServed()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("swagger-ui", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Root_RedirectsToSwagger()
    {
        // Yönlendirmeyi takip etmeyen istemci: 302'nin kendisini görmek için
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/swagger", response.Headers.Location?.OriginalString);
    }
}
