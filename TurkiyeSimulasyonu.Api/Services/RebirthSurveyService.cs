using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TurkiyeSimulasyonu.Api.Data;
using TurkiyeSimulasyonu.Api.Dtos;
using TurkiyeSimulasyonu.Api.Entities;

namespace TurkiyeSimulasyonu.Api.Services;

public sealed class RebirthSurveyService : IRebirthSurveyService
{
    private readonly AppDbContext _db;

    public RebirthSurveyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RebirthResultsDto> GetResultsAsync(CancellationToken cancellationToken)
    {
        // SELECT CountryCode, COUNT(*) FROM RebirthVotes
        // GROUP BY CountryCode ORDER BY COUNT(*) DESC, CountryCode
        var results = await _db.RebirthVotes
            .GroupBy(v => v.CountryCode)
            .Select(g => new { Country = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Country)
            .Select(x => new CountryCountDto { Country = x.Country, Count = x.Count })
            .ToListAsync(cancellationToken);

        // Toplam ayrı bir COUNT(*) sorgusu değil, aynı sonuçtan:
        // iki sorgu arasında gelen bir oy, toplamla listeyi tutarsız yapamaz
        return new RebirthResultsDto
        {
            Total = results.Sum(r => r.Count),
            Results = results,
        };
    }

    public async Task<RebirthVoteDto?> GetVoteAsync(Guid voterId, CancellationToken cancellationToken)
    {
        return await _db.RebirthVotes
            .Where(v => v.VoterId == voterId)
            .Select(v => new RebirthVoteDto { Country = v.CountryCode })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RebirthVoteDto> UpsertVoteAsync(
        Guid voterId,
        string countryCode,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        // 1. Oy varsa tek UPDATE ile biter
        if (await TryUpdateAsync(voterId, countryCode, now, cancellationToken))
        {
            return new RebirthVoteDto { Country = countryCode };
        }

        // 2. Satır yok: ilk oy
        _db.RebirthVotes.Add(new RebirthVote
        {
            VoterId = voterId,
            CountryCode = countryCode,
            CreatedAt = now,
            UpdatedAt = now,
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // 3. Yarış: 1 ile 2 arasında aynı kimlikten başka bir istek satırı ekledi.
            // Başarısız INSERT'in nesnesi hâlâ "eklenecek" işaretli; temizlenmezse
            // bu DbContext'teki bir sonraki SaveChanges aynı INSERT'ü tekrar dener.
            _db.ChangeTracker.Clear();
            await TryUpdateAsync(voterId, countryCode, now, cancellationToken);
        }

        return new RebirthVoteDto { Country = countryCode };
    }

    private async Task<bool> TryUpdateAsync(
        Guid voterId,
        string countryCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // Okumadan, doğrudan: UPDATE RebirthVotes SET ... WHERE VoterId = @id
        var affected = await _db.RebirthVotes
            .Where(v => v.VoterId == voterId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(v => v.CountryCode, countryCode)
                .SetProperty(v => v.UpdatedAt, now),
                cancellationToken);

        return affected > 0;
    }

    // 2601: tekil index'e çift kayıt · 2627: tekil kısıt ya da birincil anahtar ihlali
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}