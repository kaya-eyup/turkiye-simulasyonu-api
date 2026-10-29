using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TurkiyeSimulasyonu.Api.Infrastructure;

// SQL Server hata numaraları tek yerde: iki serviste kopyası olunca biri eksik güncellenir
public static class SqlErrors
{
    // 2601: tekil index'e çift kayıt · 2627: tekil kısıt ya da birincil anahtar ihlali
    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}