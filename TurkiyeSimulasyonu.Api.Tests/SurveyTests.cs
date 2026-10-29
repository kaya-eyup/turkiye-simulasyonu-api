using System.Net;
using System.Net.Http.Json;
using TurkiyeSimulasyonu.Api.Dtos;

namespace TurkiyeSimulasyonu.Api.Tests;

[Collection("Api")]
public class SurveyTests
{
    private const string ResultsUrl = "/surveys/rebirth";
    private const string MyVoteUrl = "/surveys/rebirth/my-vote";
    private const string VoterIdHeader = "X-Voter-Id";

    private readonly HttpClient _client;

    public SurveyTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // Testler aynı veritabanını paylaşıyor: her test kendi yeni kimliğiyle oy verir
    private static string NewVoterId() => Guid.NewGuid().ToString();

    // Kimlik string: GUID olmayan değeri ("abc") ve başlıksız isteği de gönderebilmek için
    private Task<HttpResponseMessage> PutVote(string? voterId, string? country)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, MyVoteUrl)
        {
            Content = JsonContent.Create(new { country }),
        };

        if (voterId is not null)
        {
            request.Headers.Add(VoterIdHeader, voterId);
        }

        return _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> GetMyVote(string voterId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, MyVoteUrl);
        request.Headers.Add(VoterIdHeader, voterId);
        return _client.SendAsync(request);
    }

    private async Task<RebirthResultsDto> GetResults()
    {
        var results = await _client.GetFromJsonAsync<RebirthResultsDto>(ResultsUrl);
        Assert.NotNull(results);
        return results;
    }

    private static int CountOf(RebirthResultsDto results, string country) =>
        results.Results.FirstOrDefault(r => r.Country == country)?.Count ?? 0;

    [Fact]
    public async Task GetMyVote_BeforeVoting_Returns404()
    {
        var response = await GetMyVote(NewVoterId());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_FirstVote_Returns200AndCanBeReadBack()
    {
        var voterId = NewVoterId();

        var put = await PutVote(voterId, "DE");
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var get = await GetMyVote(voterId);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var vote = await get.Content.ReadFromJsonAsync<RebirthVoteDto>();
        Assert.Equal("DE", vote?.Country);
    }

    // Müfredattaki kabul ölçütü: aynı kimliğin ikinci oyu sayıyı artırmaz, oyu değiştirir
    [Fact]
    public async Task Put_SameVoterTwice_ChangesVoteWithoutAddingCount()
    {
        var voterId = NewVoterId();
        var before = await GetResults();

        (await PutVote(voterId, "DE")).EnsureSuccessStatusCode();
        (await PutVote(voterId, "JP")).EnsureSuccessStatusCode();

        var after = await GetResults();
        Assert.Equal(before.Total + 1, after.Total);
        Assert.Equal(CountOf(before, "DE"), CountOf(after, "DE"));
        Assert.Equal(CountOf(before, "JP") + 1, CountOf(after, "JP"));
    }

    // Turkish I: Türkçe ayarlı sunucuda bile "it" İtalya olarak kaydedilmeli
    [Fact]
    public async Task Put_LowercaseCodeWithSpaces_IsStoredUppercase()
    {
        var response = await PutVote(NewVoterId(), " it ");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var vote = await response.Content.ReadFromJsonAsync<RebirthVoteDto>();
        Assert.Equal("IT", vote?.Country);
    }

    [Theory]
    [InlineData("XX")]  // atanmamış
    [InlineData("TUR")] // alfa-3
    [InlineData("XK")]  // resmi listede yok
    [InlineData("")]
    [InlineData(null)]
    public async Task Put_InvalidCountry_Returns400AndWritesNothing(string? country)
    {
        var voterId = NewVoterId();

        var response = await PutVote(voterId, country);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // Reddedilen istek hiçbir şey yazmamış olmalı
        Assert.Equal(HttpStatusCode.NotFound, (await GetMyVote(voterId)).StatusCode);
    }

    [Fact]
    public async Task Put_WithoutVoterId_Returns400()
    {
        var response = await PutVote(voterId: null, "DE");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_NonGuidVoterId_Returns400()
    {
        var response = await PutVote("abc", "DE");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Results_AreOrderedByCountThenCode_AndTotalMatches()
    {
        // Sıralamanın görünmesi için farklı sayılar olsun
        (await PutVote(NewVoterId(), "NO")).EnsureSuccessStatusCode();
        (await PutVote(NewVoterId(), "NO")).EnsureSuccessStatusCode();
        (await PutVote(NewVoterId(), "FI")).EnsureSuccessStatusCode();

        var results = await GetResults();

        var expected = results.Results
            .OrderByDescending(r => r.Count)
            .ThenBy(r => r.Country, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(expected, results.Results);
        Assert.Equal(results.Results.Sum(r => r.Count), results.Total);
    }

    // Yarış: aynı kimlikten aynı anda 20 istek (çift tıklamanın abartılmış hâli)
    [Fact]
    public async Task Put_ConcurrentRequestsFromSameVoter_CountOnceAndAllSucceed()
    {
        var voterId = NewVoterId();
        var before = await GetResults();

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => PutVote(voterId, "SE")));

        // Önce veri: tekil index sayesinde tek satır olmalı
        var after = await GetResults();
        Assert.Equal(before.Total + 1, after.Total);

        // Sonra cevaplar: yarışı kaybeden istek de 500 değil 200 almalı
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }
}