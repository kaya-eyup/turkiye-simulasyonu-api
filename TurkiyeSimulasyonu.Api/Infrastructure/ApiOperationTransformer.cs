using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;

namespace TurkiyeSimulasyonu.Api.Infrastructure;

// Belge üretilirken her ucun üzerinden bir kez geçen kural. Aynı bilgiyi her uca elle tekrar yazmak yerine
// bilginin zaten durduğu yerden okur: 429 için [EnableRateLimiting] etiketi, başlık için parametrenin kendisi.
public sealed class ApiOperationTransformer : IOpenApiOperationTransformer
{
    private const string Json = "application/json";
    private const string ProblemJson = "application/problem+json";

    public async Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        // 1) Uç etiketli ise 429'u ekle. Etiketi olmayan uca (okuma uçları) eklenmez.
        var isRateLimited = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<EnableRateLimitingAttribute>()
            .Any();

        if (isRateLimited)
        {
            var problemSchema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), cancellationToken: cancellationToken);

            operation.Responses ??= [];
            operation.Responses["429"] = new OpenApiResponse
            {
                Description = "Bu uç IP başına dakikalık istek sınırına tabi ve sınır aşıldı. `Retry-After` başlığı kaç saniye sonra tekrar denenebileceğini söyler.",
                Headers = new Dictionary<string, IOpenApiHeader>
                {
                    ["Retry-After"] = new OpenApiHeader
                    {
                        Description = "Tekrar denemeden önce beklenecek saniye.",
                        Schema = new OpenApiSchema { Type = JsonSchemaType.Integer },
                    },
                },
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [ProblemJson] = new OpenApiMediaType { Schema = problemSchema },
                },
            };
        }

        // 2) Kimlik başlığı: tip `Guid?` olduğu için belge "isteğe bağlı" diyor, oysa başlıksız istek 400 alıyor.
        foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            if (parameter.In == ParameterLocation.Header && parameter.Name == VoterIdHeader.Name)
            {
                parameter.Required = true;
                parameter.Description = "Tarayıcının ürettiği anonim kimlik (GUID). Eksik ya da GUID olmayan değer 400 döner.";
            }
        }

        // 3) İçerik türü temizliği: ASP.NET her cevap için text/plain, application/json ve text/json yazıyor.
        // Gerçekte başarılı cevaplar application/json, hatalar application/problem+json döner.
        foreach (var (code, response) in operation.Responses ?? [])
        {
            if (response.Content is not { Count: > 0 } content)
            {
                continue;
            }

            var isError = code.Length > 0 && (code[0] == '4' || code[0] == '5');
            var source = content.TryGetValue(isError ? ProblemJson : Json, out var preferred)
                ? preferred
                : content.TryGetValue(Json, out var json) ? json : content.Values.First();

            content.Clear();
            content[isError ? ProblemJson : Json] = source;
        }
    }
}
