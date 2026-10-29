using Microsoft.EntityFrameworkCore;
using TurkiyeSimulasyonu.Api.Data;
using TurkiyeSimulasyonu.Api.Dtos;
using TurkiyeSimulasyonu.Api.Entities;
using TurkiyeSimulasyonu.Api.Infrastructure;

namespace TurkiyeSimulasyonu.Api.Services;

// İki tablo birlikte değişir: ItemVotes (kim kaç verdi) ve Items.ScoreNCount (dağılım).
// İkisi aynı transaction'da yazılır: ya ikisi birden ya hiçbiri.
public sealed class ItemVoteService : IItemVoteService
{
    // Aynı tarayıcıdan aynı öğeye aynı anda gelen istekler çakışırsa en fazla kaç kez baştan denenir.
    // Bir deneme ancak başka bir istek yazmayı başardığı için kaybeder; N eşzamanlı istekte
    // en şanssızı N-1 kez kaybeder. 5: beş yıldıza art arda hızla tıklamak bile sığar.
    private const int MaxAttempts = 5;

    private readonly AppDbContext _db;

    public ItemVoteService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ItemVoteDto>> GetVotesAsync(Guid voterId, CancellationToken cancellationToken)
    {
        // (VoterId, ItemId) index'inin ilk sütunu VoterId: bu sorgu da aynı index'i kullanır
        return await _db.ItemVotes
            .Where(v => v.VoterId == voterId)
            .OrderBy(v => v.Id)
            .Select(v => new ItemVoteDto { ItemId = v.Item.Slug, Stars = v.Stars })
            .ToListAsync(cancellationToken);
    }

    public async Task<ItemVoteDto?> UpsertVoteAsync(
        Guid voterId,
        string itemSlug,
        int stars,
        CancellationToken cancellationToken)
    {
        var item = await FindItemAsync(itemSlug, cancellationToken);
        if (item is null)
        {
            return null;
        }

        // [Range(1, 5)] buraya gelmeden doğruladı; dönüşüm güvenli
        var newStars = (byte)stars;

        await RetryOnConflictAsync(
            () => TryUpsertAsync(voterId, item.Id, newStars, cancellationToken));

        // Slug veritabanındaki hâliyle döner: adres "KEBAP" olsa da "kebap"
        return new ItemVoteDto { ItemId = item.Slug, Stars = stars };
    }

    public async Task<bool> DeleteVoteAsync(
        Guid voterId,
        string itemSlug,
        CancellationToken cancellationToken)
    {
        var item = await FindItemAsync(itemSlug, cancellationToken);
        if (item is null)
        {
            return false;
        }

        await RetryOnConflictAsync(
            () => TryDeleteAsync(voterId, item.Id, cancellationToken));

        return true;
    }

    // Tek deneme. true: iş bitti · false: okuduğumuz puan biz yazamadan değişti, baştan
    private async Task<bool> TryUpsertAsync(
        Guid voterId,
        int itemId,
        byte newStars,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var oldStars = await FindStarsAsync(voterId, itemId, cancellationToken);

        // Aynı puan tekrar geldi: değişecek bir şey yok (PUT'un tekrarı zararsız olmalı)
        if (oldStars == newStars)
        {
            return true;
        }

        var now = DateTimeOffset.UtcNow;

        if (oldStars is not { } current)
        {
            // İlk oy. Aynı anda aynı tarayıcıdan başka bir "ilk oy" da geldiyse
            // tekil index burada patlar → RetryOnConflictAsync yakalar, baştan dener
            _db.ItemVotes.Add(new ItemVote
            {
                VoterId = voterId,
                ItemId = itemId,
                Stars = newStars,
                CreatedAt = now,
                UpdatedAt = now,
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Karşılaştır-ve-değiştir: sadece okuduğumuz puan hâlâ yerindeyse yaz.
            // WHERE'de Stars = @current olmasa, arada 3→4 yapılmış bir oyu 4→5 yaparken
            // sayaçlardan 4'ü değil 3'ü düşerdik: dağılım sessizce bozulurdu.
            var affected = await _db.ItemVotes
                .Where(v => v.VoterId == voterId && v.ItemId == itemId && v.Stars == current)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(v => v.Stars, newStars)
                    .SetProperty(v => v.UpdatedAt, now),
                    cancellationToken);

            if (affected == 0)
            {
                return false;
            }
        }

        await ShiftCountersAsync(itemId, removed: oldStars, added: newStars, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<bool> TryDeleteAsync(
        Guid voterId,
        int itemId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        // Silinecek oy yok: DELETE'in tekrarı zararsız, sonuç aynı
        if (await FindStarsAsync(voterId, itemId, cancellationToken) is not { } current)
        {
            return true;
        }

        var affected = await _db.ItemVotes
            .Where(v => v.VoterId == voterId && v.ItemId == itemId && v.Stars == current)
            .ExecuteDeleteAsync(cancellationToken);

        if (affected == 0)
        {
            return false;
        }

        await ShiftCountersAsync(itemId, removed: current, added: null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    // Sayaçlar okunmadan, tek UPDATE ile kaydırılır:
    // UPDATE Items SET Score1Count = Score1Count + @d1, ... WHERE Id = @itemId
    // Satır kilidi, aynı öğeye aynı anda gelen oyları sıraya sokar; kimse diğerinin artışını ezmez.
    private Task<int> ShiftCountersAsync(
        int itemId,
        byte? removed,
        byte? added,
        CancellationToken cancellationToken)
    {
        // Eklenen puanın sütunu +1, kaldırılanınki -1, diğerleri 0
        int Delta(byte stars) => (added == stars ? 1 : 0) - (removed == stars ? 1 : 0);

        var d1 = Delta(1);
        var d2 = Delta(2);
        var d3 = Delta(3);
        var d4 = Delta(4);
        var d5 = Delta(5);

        return _db.Items
            .Where(i => i.Id == itemId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.Score1Count, i => i.Score1Count + d1)
                .SetProperty(i => i.Score2Count, i => i.Score2Count + d2)
                .SetProperty(i => i.Score3Count, i => i.Score3Count + d3)
                .SetProperty(i => i.Score4Count, i => i.Score4Count + d4)
                .SetProperty(i => i.Score5Count, i => i.Score5Count + d5),
                cancellationToken);
    }

    private async Task RetryOnConflictAsync(Func<Task<bool>> attempt)
    {
        for (var i = 0; i < MaxAttempts; i++)
        {
            try
            {
                if (await attempt())
                {
                    return;
                }
            }
            catch (DbUpdateException ex) when (SqlErrors.IsUniqueViolation(ex))
            {
                // Başarısız INSERT'in nesnesi hâlâ "eklenecek" işaretli;
                // temizlenmezse bir sonraki denemedeki SaveChanges onu tekrar yazmaya çalışır
                _db.ChangeTracker.Clear();
            }
        }

        // Beşten fazla eşzamanlı istek: pratikte olmaz; olursa 500 ve logda görünür
        throw new InvalidOperationException(
            $"Vote could not be written after {MaxAttempts} attempts due to concurrent requests.");
    }

    private Task<byte?> FindStarsAsync(Guid voterId, int itemId, CancellationToken cancellationToken) =>
        _db.ItemVotes
            .Where(v => v.VoterId == voterId && v.ItemId == itemId)
            .Select(v => (byte?)v.Stars)
            .FirstOrDefaultAsync(cancellationToken);

    private Task<ItemKey?> FindItemAsync(string slug, CancellationToken cancellationToken) =>
        _db.Items
            .Where(i => i.Slug == slug)
            .Select(i => new ItemKey(i.Id, i.Slug))
            .FirstOrDefaultAsync(cancellationToken);

    private sealed record ItemKey(int Id, string Slug);
}
