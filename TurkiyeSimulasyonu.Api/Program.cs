using TurkiyeSimulasyonu.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var seedPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "db.json");
var seedData = SeedDataReader.Read(seedPath);
builder.Services.AddSingleton(seedData);

// Connection string eksikse uygulamanın açılışta hemen çökmesi (Fail Fast) ve adresin her istekte değil yalnızca açılışta bir kez okunması için dışarıda tanımlandı.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// CORS: tarayıcıda hangi adreslerdeki sayfaların bu API'nin cevabını okuyabileceği.
// Liste ortama göre appsettings'ten gelir; liste yoksa kimseye izin verilmez.
const string FrontendCorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
