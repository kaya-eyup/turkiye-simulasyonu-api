using System.Net;
using System.Net.Http.Json;
using TurkiyeSimulasyonu.Api.Dtos;

namespace TurkiyeSimulasyonu.Api.Tests;

[Collection("Api")]
public class ItemVoteTests
{
    private const string ItemSlug = "kebap";
    private const string MyVotesUrl = "/items/my-votes";
    private const string MyVoteUrl = $"/items/{ItemSlug}/my-vote";
    private const string VoterIdHeader = "X-Voter-Id";

    private readonly HttpClient _client;

    public ItemVoteTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // Testler aynı veritabanını paylaşıyor: her test kendi yeni kimliğiyle oy verir
    // ve dağılımı "önce/sonra" farkıyla ölçer, mutlak sayıya bakmaz
    private static string NewVoterId() => Guid.NewGuid().ToString();

    private Task<HttpResponseMessage> Send(
        HttpMethod method, string url, string? voterId, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (voterId is not null)
        {
            request.Headers.Add(VoterIdHeader, voterId);
        }

        return _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> PutVote(string voterId, int stars, string url = MyVoteUrl) =>
        Send(HttpMethod.Put, url, voterId, new { stars });

    private async Task<int[]> GetDistribution()
    {
        var item = await _client.GetFromJsonAsync<ItemDto>($"/items/{ItemSlug}");
        Assert.NotNull(item);
        return item.Distribution.ToArray();
    }

    private async Task<List<ItemVoteDto>> GetMyVotes(string voterId)
    {
        var response = await Send(HttpMethod.Get, MyVotesUrl, voterId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var votes = await response.Content.ReadFromJsonAsync<List<ItemVoteDto>>();
        Assert.NotNull(votes);
        return votes;
    }

    // Beklenen dağılım: önceki hâl + değişim (dizinin 0. elemanı 1 yıldız)
    private static int[] Shifted(int[] before, int? removed, int? added)
    {
        var expected = before.ToArray();
        if (removed is { } r) expected[r - 1]--;
        if (added is { } a) expected[a - 1]++;
        return expected;
    }

    [Fact]
    public async Task GetMyVotes_WithoutHeader_Returns400()
    {
        var response = await Send(HttpMethod.Get, MyVotesUrl, voterId: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMyVotes_BeforeVoting_ReturnsEmptyList()
    {
        Assert.Empty(await GetMyVotes(NewVoterId()));
    }

    [Fact]
    public async Task Put_FirstVote_AddsOneToThatColumn()
    {
        var voterId = NewVoterId();
        var before = await GetDistribution();

        var response = await PutVote(voterId, 4);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Shifted(before, removed: null, added: 4), await GetDistribution());

        var vote = Assert.Single(await GetMyVotes(voterId));
        Assert.Equal(ItemSlug, vote.ItemId);
        Assert.Equal(4, vote.Stars);
    }

    // Oy değişince toplam artmaz: sayı bir sütundan diğerine taşınır
    [Fact]
    public async Task Put_ChangedVote_MovesCountBetweenColumns()
    {
        var voterId = NewVoterId();
        var before = await GetDistribution();

        await PutVote(voterId, 2);
        await PutVote(voterId, 5);

        Assert.Equal(Shifted(before, removed: null, added: 5), await GetDistribution());
    }

    [Fact]
    public async Task Put_SameVoteTwice_CountsOnce()
    {
        var voterId = NewVoterId();
        var before = await GetDistribution();

        await PutVote(voterId, 3);
        var second = await PutVote(voterId, 3);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(Shifted(before, removed: null, added: 3), await GetDistribution());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Put_StarsOutOfRange_Returns400(int stars)
    {
        var response = await PutVote(NewVoterId(), stars);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_UnknownItem_Returns404()
    {
        var response = await PutVote(NewVoterId(), 3, "/items/olmayan-oge/my-vote");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Adres büyük harfle gelse de (SQL Server harf duyarsız) cevap kayıtlı slug'la döner
    [Fact]
    public async Task Put_UppercaseSlug_ReturnsCanonicalSlug()
    {
        var response = await PutVote(NewVoterId(), 3, "/items/KEBAP/my-vote");

        var vote = await response.Content.ReadFromJsonAsync<ItemVoteDto>();
        Assert.Equal(ItemSlug, vote?.ItemId);
    }

    [Fact]
    public async Task Delete_ExistingVote_RemovesItsCount()
    {
        var voterId = NewVoterId();
        var before = await GetDistribution();
        await PutVote(voterId, 1);

        var response = await Send(HttpMethod.Delete, MyVoteUrl, voterId);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(before, await GetDistribution());
        Assert.Empty(await GetMyVotes(voterId));
    }

    [Fact]
    public async Task Delete_WithoutVote_Returns204AndChangesNothing()
    {
        var before = await GetDistribution();

        var response = await Send(HttpMethod.Delete, MyVoteUrl, NewVoterId());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(before, await GetDistribution());
    }

    [Fact]
    public async Task Delete_UnknownItem_Returns404()
    {
        var response = await Send(HttpMethod.Delete, "/items/olmayan-oge/my-vote", NewVoterId());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Hızlı tıklama: aynı tarayıcıdan aynı anda beş farklı puan. Hangisi kazanırsa kazansın
    // tek satır kalmalı, dağılım tam olarak o puan kadar değişmeli ve hiçbir istek 500 almamalı.
    [Fact]
    public async Task Put_ConcurrentVotesFromSameVoter_CountOnce()
    {
        var voterId = NewVoterId();
        var before = await GetDistribution();

        var responses = await Task.WhenAll(
            Enumerable.Range(1, 5).Select(stars => PutVote(voterId, stars)));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        var vote = Assert.Single(await GetMyVotes(voterId));
        Assert.Equal(Shifted(before, removed: null, added: vote.Stars), await GetDistribution());
    }
}