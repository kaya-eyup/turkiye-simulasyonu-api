using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TurkiyeSimulasyonu.Api.Data;

namespace TurkiyeSimulasyonu.Api.Tests;

// API'yi test içinde, bellekte ayağa kaldırır; veritabanı gerçek SQL Server
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    // CI'da ortam değişkeninden gelir; yerelde LocalDB'deki ayrı test veritabanı
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
        ?? "Server=(localdb)\\MSSQLLocalDB;Database=TurkiyeSimulasyonu_Tests;Trusted_Connection=True;TrustServerCertificate=True";

    public ApiFactory()
    {
        // Program.cs bu değerleri başlangıçta okur; ortam değişkeni appsettings'in üstüne yazar
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", ConnectionString);
        // Testlerdeki çok sayıda POST 429'a takılmasın
        Environment.SetEnvironmentVariable("RateLimiting__CommentsWrite__PermitLimit", "1000");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development değil: geliştirme ayarları (secrets, localhost CORS) testlere karışmasın
        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Her test çalışmasında veritabanı sıfırdan: migration'lar + db.json seed'i
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureDeleted();
        db.Database.Migrate();

        return host;
    }
}