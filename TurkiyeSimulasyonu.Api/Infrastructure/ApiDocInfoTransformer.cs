using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace TurkiyeSimulasyonu.Api.Infrastructure;

// Belgenin "künyesi": başlık, açıklama, iletişim. Uçlara özel değil, bütün belge için tek yer.
public sealed class ApiDocInfoTransformer : IOpenApiDocumentTransformer
{
    private const string Description = """
        Türkiye'ye özgü yemekleri, şehirleri, adetleri ve absürtlükleri puanlama ve yorumlama platformunun backend'i.
        Web arayüzü: https://turkiyesimulasyonu.eyupkaya.com · Kaynak kod: https://github.com/kaya-eyup/turkiye-simulasyonu-api

        **Kimlik (giriş yok).** Oy uçları kullanıcıyı `X-Voter-Id` başlığıyla tanır: tarayıcının ürettiği rastgele bir GUID.
        Başlığı değiştiren biri yeni bir oy veren gibi görünür; bu bilinçli bir borç, giriş geldiğinde oy kullanıcıya bağlanacak.

        **Hata biçimi.** Bütün hatalar `application/problem+json` (ProblemDetails) olarak döner. Beklenmeyen hatalarda 500 gelir;
        ayrıntı istemciye gitmez, yalnızca sunucu logunda kalır.

        **İstek sınırı.** Yazma uçları IP başına dakikalık sınırlıdır. Aşılırsa 429 ve `Retry-After` başlığı döner.

        **Dikkat.** Bu sayfadaki "Try it out" canlı veritabanına yazar. Ücretsiz sunucu boştayken uyur:
        uzun bir aradan sonraki ilk istek yavaş olabilir.
        """;

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Türkiye Simülasyonu API",
            Version = "v1",
            Description = Description,
            Contact = new OpenApiContact
            {
                Name = "Eyüp Kaya",
                Url = new Uri("https://www.linkedin.com/in/eyupkaya-dev"),
                Email = "iletisim@eyupkaya.com",
            },
        };

        return Task.CompletedTask;
    }
}
