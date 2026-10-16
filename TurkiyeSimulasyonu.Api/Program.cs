using Microsoft.EntityFrameworkCore;
using TurkiyeSimulasyonu.Api.Data;
using TurkiyeSimulasyonu.Api.Data.Seed;
using TurkiyeSimulasyonu.Api.Json;
using TurkiyeSimulasyonu.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeOffsetConverter());
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var seedPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "Seed", "db.json");


// Connection string eksikse uygulamanın açılışta açılmaması (Fail Fast)
// ve adresin her istekte değil yalnızca açılışta bir kez okunması için dışarıda tanımlandı.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

builder.Services.AddDbContext<AppDbContext>(options => options
    .UseSqlServer(connectionString)
    // dotnet ef database update'in sonunda EF çağırır; uygulama çalışırken (F5) çağrılmaz
    .UseSeeding((context, _) => DbSeeder.Seed(context, seedPath)));
builder.Services.AddScoped<IItemService, ItemService>();
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
