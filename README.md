# turkiye-simulasyonu-api

[Türkiye Simülasyonu](https://github.com/kaya-eyup/turkiye-simulasyonu-web)'nun backend'i. ASP.NET Core Web API (.NET 10, controller tabanlı).

Kabul listesi web deposundaki **"Backend sözleşmesi"** tablosu: API o tablodaki uçları karşılar.

## Durum

Taslak. Sözleşmenin okuma uçları hazır ve React uygulaması geliştirmede bu API'den okuyor. Veri henüz veritabanında değil: `Data/db.json` uygulama açılırken bir kez belleğe okunur. Yazma ucu yok.

## Çalıştırma

1. Visual Studio'da `https` profiliyle F5 (ya da `dotnet run --launch-profile https`).
2. API `https://localhost:7129` adresinde açılır. `http://localhost:5267`'ye gelen istekler https'e yönlendirilir.
3. İstekler `TurkiyeSimulasyonu.Api/TurkiyeSimulasyonu.Api.http` dosyasında: her uç için bir doğru istek ve bilerek bozulmuş istekler, başlıklarında beklenen sonuçla.

`dotnet run` profil belirtilmeden çalıştırılırsa `launchSettings.json`'daki ilk profil (`http`) seçilir ve 7129 açılmaz.

## Uçlar

| Uç | Başarılı | Diğer durumlar |
| --- | --- | --- |
| `GET /categories` | 200, `order`'a göre sıralı | |
| `GET /categories/{id}` | 200 | olmayan id → 404 (ProblemDetails) |
| `GET /items` | 200, bütün öğeler (arama sayfası için) | |
| `GET /items?categoryId=` | 200, kategorinin öğeleri | olmayan kategori → 200 + `[]` · boş değer → bütün öğeler |
| `GET /items/{id}` | 200 | olmayan id → 404 · büyük harfli id (`KEBAP`) → 404 |
| `GET /comments?itemId=` | 200, yeniden eskiye | `itemId` yok ya da boş → 400 (`errors.itemId`) · olmayan öğe → 200 + `[]` |

- Tanınmayan query parametreleri yok sayılır (ör. json-server'dan kalma `_sort`).
- Desteklenmeyen metot → 405.

## CORS

İzin verilen origin listesi `Cors:AllowedOrigins` ayarında; geliştirmede `appsettings.Development.json` içinde `http://localhost:5173`. Liste tanımlı değilse hiçbir origin'e izin verilmez.

- İzinsiz bir origin'den gelen istek de 200 alır, ama cevapta `Access-Control-Allow-Origin` başlığı olmaz. Cevabı JavaScript'e vermeyen tarayıcıdır; CORS kullanıcıyı korur, API'yi değil.
- Veri değiştiren isteklerden önce tarayıcının gönderdiği `OPTIONS` (preflight) isteğine CORS ara katmanı controller'a gitmeden 204 ile cevap verir.
- `UseCors`, `UseAuthorization`'dan önce: hata cevapları da izin başlığını taşır ve tarayıcı gerçek durum kodunu JavaScript'e iletir.

## Kararlar

- **Sıralama sunucuda.** Liste uçları sıralı döner; istemciye güvenilmez. Her sıralamanın son anahtarı benzersiz bir alan (tie-breaker): yorumlar `CreatedAt` azalan, eşitse `Id` azalan (ordinal).
- **Her kaynak kendi controller'ında** (`categories`, `items`, `comments`).
- **Bulunamayan kayıt ile eşleşmeyen süzgeç farklı.** `/items/{id}` için olmayan id 404; `?categoryId=` ile eşleşen kayıt olmaması 200 + `[]`.
- **`itemId` zorunlu.** Süzgeçsiz bir yorum listesi her yeni yorumla sınırsız büyür; hiçbir sayfa da onu istemiyor.
- **Uyarılar hata gibi ele alınır** (`TreatWarningsAsErrors`) ve null güvenliği açık (`Nullable`).
- **Bozuk seed uygulamayı açtırmaz.** JSON okuyucu null olmaması gereken alanda null'u ve eksik zorunlu alanı reddeder; `distribution` tam 5 negatif olmayan sayı olmalı.

## Bilinçli borçlar

- Veri bellekte ve salt okunur; veritabanı (EF Core + SQL Server) sırada.
- Öğe ve kategori `id`'si slug: değişmeyen bir sayı anahtar + ayrı `slug` sütununa geçilecek.
- `id` büyük/küçük harf duyarlı; veritabanına geçişte SQL Server'ın karşılaştırma kuralıyla yeniden değerlendirilecek.
- `/items` ve kategori öğeleri dosya sırasıyla dönüyor; açık sıralama yok.
- Tarihlerin UTC etiketi `db.json`'dan geliyor (`…Z`). Veritabanından okunan tarihlerde bu etiket kaybolabilir; web'deki zod şeması `Z`'siz tarihi reddeder.
- `POST /comments` yok (405).
- 405 cevabının gövdesi boş; hata cevapları henüz tek bir şekilde değil.
- Sorgu mantığı controller'larda; servis katmanı yok.
- Test yok.
