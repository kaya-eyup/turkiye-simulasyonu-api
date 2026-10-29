using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Threading.RateLimiting;
using TurkiyeSimulasyonu.Api.Data;
using TurkiyeSimulasyonu.Api.Data.Seed;
using TurkiyeSimulasyonu.Api.Infrastructure;
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
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
var commentsPermitLimit = builder.Configuration.GetValue("RateLimiting:CommentsWrite:PermitLimit", 5);

// Ankette listede gezinip fikir değiştirmek doğal: yorumdan geniş
var surveyPermitLimit = builder.Configuration.GetValue("RateLimiting:SurveyWrite:PermitLimit", 20);

// Öğe sayfalarını gezip puan vermek tek soruluk anketten çok daha sık bir iş
var itemVotesPermitLimit = builder.Configuration.GetValue("RateLimiting:ItemVotesWrite:PermitLimit", 60);

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(RateLimitPolicies.CommentsWrite,
        httpContext => PerIpPerMinute(httpContext, commentsPermitLimit));
    options.AddPolicy(RateLimitPolicies.SurveyWrite,
        httpContext => PerIpPerMinute(httpContext, surveyPermitLimit));
    options.AddPolicy(RateLimitPolicies.ItemVotesWrite,
        httpContext => PerIpPerMinute(httpContext, itemVotesPermitLimit));
    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;
        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        await httpContext.RequestServices
            .GetRequiredService<IProblemDetailsService>()
            .WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Çok fazla istek gönderdin, biraz bekleyip tekrar dene."
                }
            });
    };
});
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
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IRebirthSurveyService, RebirthSurveyService>();
builder.Services.AddScoped<IItemVoteService, ItemVoteService>();

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
app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Uç-bazlı limit için routing açıkça yazılmalı; limiter ondan sonra gelir
app.UseRouting();

app.UseCors(FrontendCorsPolicy);

// Cors'tan sonra: 429 cevabı da CORS başlığını taşır
app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.Run();

// IP başına dakikalık sabit pencere. Her politika kendi sayaçlarını tutar:
// 5 yorum yazmak anketteki hakkını yemez.
static RateLimitPartition<string> PerIpPerMinute(HttpContext httpContext, int permitLimit) =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
// WebApplicationFactory<Program> için: top-level Program varsayılan olarak internal, test projesi göremez
public partial class Program;