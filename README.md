# turkiye-simulasyonu-api

[Türkiye Simülasyonu](https://github.com/kaya-eyup/turkiye-simulasyonu-web)'nun backend'i. ASP.NET Core Web API (.NET 10, controller tabanlı).

Kabul listesi web deposundaki **"Backend sözleşmesi"** tablosu. API bu tablonun bir kısmını karşılıyor: hangi uçların hazır, hangilerinin henüz yok olduğu tablonun **Durum** sütununda ve aşağıdaki [Bilinçli borçlar](#bilinçli-borçlar) bölümünde.

## Durum

**Canlı:** https://turkiye-simulasyonu-api.runasp.net (ör. [`/categories`](https://turkiye-simulasyonu-api.runasp.net/categories)). Frontend ([https://turkiyesimulasyonu.eyupkaya.com](https://turkiyesimulasyonu.eyupkaya.com), Cloudflare; yedek: [Vercel](https://turkiye-simulasyonu-web.vercel.app)) bu API'ye bağlı.

Veri SQL Server'da, EF Core ile okunuyor ve yazılıyor: geliştirmede LocalDB, canlıda MonsterASP. React'teki yorum formu `POST /comments`'e yazıyor. Uçlar DTO döndürüyor. Sorgular servis sınıflarında, controller’lar sadece HTTP’yi karşılıyor.

## Çalıştırma

### Önkoşullar
- LocalDB (Visual Studio ile birlikte geliyor)
- EF Core komut aracı: proje klasöründe `dotnet tool restore`. Sürüm `dotnet-tools.json`'da sabit (10.0.13); her bilgisayarda aynı araç çalışır.

### İlk Kurulum ve Çalıştırma
1. Proje klasöründe (TurkiyeSimulasyonu.Api/) `dotnet ef database update` çalıştır. Bu komut tabloları kurar ve seed’i çalıştırır. (Veritabanını sıfırlamak için: `dotnet ef database drop --force` + `dotnet ef database update`.)
2. Visual Studio'da `https` profiliyle F5 (ya da `dotnet run --launch-profile https`).
3. API `https://localhost:7129` adresinde açılır. `http://localhost:5267`'ye gelen istekler https'e yönlendirilir.
4. İstekler `TurkiyeSimulasyonu.Api/TurkiyeSimulasyonu.Api.http` dosyasında: her uç için bir doğru istek ve bilerek bozulmuş istekler, başlıklarında beklenen sonuçla. Adres `http-client.env.json`'dan gelir: editörün sağ üstündeki ortam seçiciden `local` ya da `production` seçilir. Canlıda veri yazan başarılı POST'lar gönderilmez.

`dotnet run` profil belirtilmeden çalıştırılırsa `launchSettings.json`'daki ilk profil (`http`) seçilir ve 7129 açılmaz.

## Mimari

Controller → Service (`IXxxService` / `XxxService`) → AppDbContext → SQL Server

- **Controller HTTP’yi karşılıyor:** Parametreyi alıyor, servisten gelen sonucu HTTP cevabına çeviriyor: veri → 200 ya da 201; null → 404 (okuma uçları) ya da 400 (yorum ekleme).
- **Servis sorguyu ve DTO’yu kuruyor:** HTTP bilmiyor.
- **Scoped Servisler:** Servisler scoped, çünkü `AppDbContext` scoped. Ayrı bir repository katmanı yok: `DbSet` zaten repository’nin, `DbContext` de unit of work’ün işini yapıyor.

## Canlı ortam

```
React (Cloudflare · yedek: Vercel) ──HTTPS──▶ API (MonsterASP: Windows + IIS, .NET 10) ──iç ağ──▶ SQL Server 2025 (MonsterASP)
```

- **Yayın:** Visual Studio'dan Web Deploy ile, framework-dependent: Sunucuda .NET 10 kurulu, paket sadece uygulamanın dosyalarını taşıyor. Sunucuda pakette olmayan dosyalar silinir. Yayından önce çalışma ağacı temiz olmalı; sunucudaki kod her zaman bir commit'e karşılık gelir.
- **Ayarlar:** Sunucu `Production` ortamında çalışıyor (`ASPNETCORE_ENVIRONMENT` tanımlı değil, varsayılan bu).
  - Gizli olmayan ayar depoda: `appsettings.Production.json` (CORS izni).
  - Bağlantı dizesi depoda değil: sunucuda `ConnectionStrings__Default` ortam değişkeni. Ortam değişkenleri ayar dosyalarından sonra okunduğu için onları ezer.
  - Dize yoksa uygulama açılışta durur (fail fast, `Program.cs`) ve IIS `500.30 - ASP.NET Core app failed to start` döner. Nedeni panelin olay günlüğünde yazar: `InvalidOperationException: Connection string 'Default' not found.`
- **Veritabanına iki yol:**
  - Uygulama → iç adres, `Encrypt=False`: Trafik veri merkezinden çıkmıyor.
  - Geliştirici → dış adres (`…public.databaseasp.net`), `Encrypt=True;TrustServerCertificate=True`. Uzaktan erişim panelden açılıyor.
- **Tablo değişiklikleri canlıya elle**, proje klasöründe:
  ```powershell
  $cs = Read-Host "Remote connection string"   # dış adresin dizesi; Read-Host girdisi komut geçmişine yazılmaz
  dotnet ef migrations list --connection $cs   # bekleyenler (Pending) işaretli
  dotnet ef database update --connection $cs
  ```
- **Cold start:** Ücretsiz sunucu boştaki uygulamayı durduruyor; sonraki ilk istek uygulamayı yeniden başlattığı için yavaş.
- **HTTPS:** Let's Encrypt sertifikası; `http` istekleri `https`'e yönlendiriliyor.

## Toplu veri ekleme (Importer)

`TurkiyeSimulasyonu.Importer`, var olan (boş olmayan) bir veritabanına parti
hâlinde JSON ekleyen ayrı bir konsol aracı. `DbSeeder`'ın yerine geçmiyor —
o sadece boş veritabanında çalışıyor; bu araç dolu veritabanına **ekleme**
yapıyor.

Eşleştirme anahtarı `Slug`: slug veritabanında yoksa eklenir, varsa
`--update` verilmediği sürece dokunulmaz (`Score*Count` ve `CreatedAt`
`--update` ile bile hiçbir zaman güncellenmez — canlıda gerçek oy almış
olabilir).

```powershell
# local (varsayılan bağlantı: LocalDB)
dotnet run --project TurkiyeSimulasyonu.Importer -- --dry-run <klasör>
dotnet run --project TurkiyeSimulasyonu.Importer -- <klasör>

# production
dotnet run --project TurkiyeSimulasyonu.Importer -- --connection "<dış connection string>" --dry-run <klasör>
dotnet run --project TurkiyeSimulasyonu.Importer -- --connection "<dış connection string>" <klasör>
```

Parti dosyası formatı: `{ "category": {id,name,emoji,order}?, "items": [...] }`.
`"category"` sadece yeni bir kategori açılıyorsa var; mevcut kategoriye öğe
eklerken yok. Bozuk bir dosya `SaveChanges()` öncesi `json.loads`/deserialize
aşamasında patlar — tek transaction olduğu için ya hepsi yazılır ya hiçbiri.

## Uçlar

| Uç | Başarılı | Diğer durumlar |
| --- | --- | --- |
| `GET /categories` | 200, `order`'a göre sıralı (eşitse `Id`) | |
| `GET /categories/{id}` | 200 | olmayan id → 404 (ProblemDetails) |
| `GET /items` | 200, bütün öğeler (`Id` sırasıyla, arama sayfası için) | |
| `GET /items?categoryId=` | 200, kategorinin öğeleri (`Id` sırasıyla) | olmayan kategori → 200 + `[]` · boş değer → bütün öğeler |
| `GET /items/featured` | 200, son 7 günde en çok yorum alan en fazla 4 öğe (çoktan aza, eşitse slug) | son 7 günde yorum yoksa 200 + `[]` |
| `GET /items/{id}` | 200 | olmayan id → 404 · büyük harfli id (`KEBAP`) → 200, cevapta kanonik `"id": "kebap"` |
| `GET /comments?itemId=` | 200, yeniden eskiye | `itemId` yok ya da boş → 400 (`errors.itemId`) · olmayan öğe → 200 + `[]` |
| `POST /comments` | 201 + oluşturulan kayıt | olmayan öğe → 400 (`errors.itemId`) · eksik alan ya da bozuk JSON → 400 · `Content-Type` yok → 415 · uzun metin → 500 (borç) |

- Tanınmayan query parametreleri yok sayılır (ör. json-server'dan kalma `_sort`).
- Desteklenmeyen metot → 405.

## Veri modeli

Tablo yapısı `InitialCreate` migration’ında tanımlı. Seed `UseSeeding` + `DbSeeder` ile çalışıyor; sadece `database update` sırasında ve veritabanı boşsa. `db.json` uygulama çalışırken hiç okunmuyor.

### Tablolar

- **Categories**
  - `Id`: int, birincil anahtar; numarayı veritabanı verir (identity).
  - `Slug`: metin, zorunlu, en fazla 100 karakter, tekil.
  - `Name`: metin, zorunlu, en fazla 100 karakter.
  - `Emoji`: metin, zorunlu, en fazla 32 karakter.
  - `DisplayOrder`: int, zorunlu.
- **Items**
  - `Id`: int, birincil anahtar (identity).
  - `CategoryId`: int, zorunlu; foreign key → `Categories.Id`.
  - `Slug`: metin, zorunlu, en fazla 100 karakter, tekil.
  - `Name`: metin, zorunlu, en fazla 100 karakter.
  - `Emoji`: metin, zorunlu, en fazla 32 karakter.
  - `Summary`: metin, zorunlu, en fazla 500 karakter.
  - `Score1Count` … `Score5Count`: int, zorunlu; her puanın aldığı oy sayısı.
  - `CreatedAt`: `DateTimeOffset`, zorunlu.
- **Comments**
  - `Id`: int, birincil anahtar (identity).
  - `ItemId`: int, zorunlu; foreign key → `Items.Id`.
  - `Author`: metin, zorunlu, en fazla 30 karakter.
  - `Body`: metin, zorunlu, en fazla 500 karakter.
  - `CreatedAt`: `DateTimeOffset`, zorunlu.

Foreign key sütunlarında (`Items.CategoryId`, `Comments.ItemId`) EF'in varsayılan index'i var: bir öğenin yorumları bütün tablo okunmadan bulunur.

### İlişkiler ve silme

- **Kategori → öğe (1-N):** Öğesi olan kategori silinemez (`Restrict`).
- **Öğe → yorum (1-N):** Öğe silinirse yorumları da silinir (`Cascade`).

### Kararlar

- **Değişmeyen sayı anahtar + ayrı tekil slug.** Tablolar arası bağlar sayıya kurulu; slug düzeltildiğinde hiçbir bağ kopmaz.
- **Dağılım 5 ayrı sütun.** Oy geldiğinde sayaç veritabanında tek komutla artırılabilsin diye (`SET Score4Count = Score4Count + 1`). Dağılım tek hücrede JSON dizi olarak tutulsaydı bu mümkün olmazdı: değeri okuyup değiştirip geri yazmak gerekirdi ve aynı anda gelen iki oydan biri kaybolabilirdi.
- **`Order` değil `DisplayOrder`.** `ORDER` SQL'in ayrılmış kelimesi (`ORDER BY`); elle yazılan sorgularda karışıklık çıkarmasın.
- **Tarihler `DateTimeOffset`.** Değer, UTC'ye göre farkıyla birlikte saklanır; an tek anlamlı tanımlanır.
- **Uzunluklar.** `Author` 30 ve `Body` 500 sözleşmeden. Diğerleri `db.json`'daki en uzun değerlerin rahatça üstünde seçildi; `Emoji` 32, çünkü ekranda tek görünen bir emoji içeride 10'dan fazla karakter tutabilir.
- **DTO:** `id = slug`, iç numara dışarı çıkmıyor. Başka bir kaynağa işaret eden alan, o kaynağın slug’ını taşıyor (`categoryId`, `itemId`).

## CORS

İzin verilen origin listesi `Cors:AllowedOrigins` ayarında: geliştirmede `appsettings.Development.json` içinde `http://localhost:5173`, canlıda `appsettings.Production.json` içinde üç adres: `https://turkiyesimulasyonu.eyupkaya.com` (ana adres), `https://turkiye-simulasyonu-web.keyup017-973.workers.dev` (Cloudflare'ın kendi adresi) ve `https://turkiye-simulasyonu-web.vercel.app` (yedek). Önizleme (Preview) adresleri izinli değil. Origin, adresle birebir aynı olmalı: `https://` ile başlar, sonunda `/` olmaz. Liste tanımlı değilse hiçbir origin'e izin verilmez.

- İzinsiz bir origin'den gelen istek de 200 alır, ama cevapta `Access-Control-Allow-Origin` başlığı olmaz. Cevabı JavaScript'e vermeyen tarayıcıdır; CORS kullanıcıyı korur, API'yi değil.
- Veri değiştiren isteklerden önce tarayıcının gönderdiği `OPTIONS` (preflight) isteğine CORS ara katmanı controller'a gitmeden 204 ile cevap verir.
- `UseCors`, `UseAuthorization`'dan önce: hata cevapları da izin başlığını taşır ve tarayıcı gerçek durum kodunu JavaScript'e iletir.

## Kararlar

- **Sıralama sunucuda.** Select’ten önce ve entity’nin sütunlarıyla. Liste uçları sıralı döner; istemciye güvenilmez. Her sıralamanın son anahtarı benzersiz bir alan (tie-breaker): yorumlar CreatedAt azalan, eşitse Id azalan.
- **Her kaynak kendi controller'ında** (`categories`, `items`, `comments`).
- **Bulunamayan kayıt ile eşleşmeyen süzgeç farklı.** `/items/{id}` için olmayan id 404; `?categoryId=` ile eşleşen kayıt olmaması 200 + `[]`.
- **`itemId` zorunlu.** Süzgeçsiz bir yorum listesi her yeni yorumla sınırsız büyür; hiçbir sayfa da onu istemiyor.
- **Uyarılar hata gibi ele alınır** (`TreatWarningsAsErrors`) ve null güvenliği açık (`Nullable`).
- **Bozuk seed dosyası database update’i durdurur.** JSON okuyucu null olmaması gereken alanda null’u ve eksik zorunlu alanı reddeder; distribution tam 5 negatif olmayan sayı olmalı. Dosya yazmadan önce okunduğu için yarım veri yazılmaz.
- **Projection:** Her uç tek sorgu atıyor ve sadece DTO’nun sütunlarını çekiyor.
- **Async + CancellationToken:** İstemci vazgeçince sorgu kesiliyor. `TaskCanceledException` yakalanmıyor.
- **Tarihler dışarıda UTC + Z.** Tek bir converter bütün tarihleri UTC + Z ile yazıyor (zod’un `z.iso.datetime()` şeması bunu istiyor).
- **Collation (KEBAP kararı):** LocalDB’nin karşılaştırma kuralı (…_CI_AS) büyük/küçük harfi ayırmıyor. Tekil index de aynı kuralla çalıştığı için KEBAP ile kebap aynı kaydı gösteriyor. Büyük harfli istek 200 alıyor ve cevapta kanonik slug dönüyor.
- **Giriş paketi sadece `itemId`, `author`, `body` taşıyor.** `id` ve `createdAt` sunucuda üretiliyor; gövdede gelirlerse yok sayılıyor (toplu atama).
- **`Location` başlığı yok**, çünkü tek bir yorumu gösteren bir uç yok.
- **Olmayan öğe 404 değil 400**, çünkü sorun adreste değil gövdede.
- **Cevaptaki `itemId` kanonik slug**.
- **Tanınmayan alanlar yok sayılır (varsayılan).** Kalıpta `createdAt` alanı zaten bulunmadığından ve zorunlu alan doğrulaması yazım hatalarını 400 ile yakaladığından, eski frontend sekmelerinin göndereceği fazladan alanları reddetmek güvenlik kazancı sağlamadan kullanıcı deneyimini bozar.

## Bilinçli borçlar

- Web deposundaki sözleşmeden henüz karşılanmayanlar: `GET /items?q=&page=` (arama ve sayfalama; bugün arama istemcide), `GET /surveys/rebirth`, `PUT /surveys/rebirth/my-vote` ve sunucuda oy (`votes` tablosu, kullanıcı girişi). Oylar şimdilik sadece tarayıcıda.
- Uzunluk doğrulaması yok; 30/500 sınırını aşan metin 500 döner. Boş author “anonim”e çevrilmiyor.
- 405 cevabının gövdesi boş, hata cevapları tek şekilde değil.
- Test yok.
- Kullanıcı hataları 500'e dönüşüyor.
- `/items` ve yorum listesi sayfasız.
- Seed tablo seviyesinde kontrol ediyor ve sadece senkron.
- `featured` ayrılmış bir kelime: Bu slug'la bir öğe açılırsa kendi adresinden erişilemez; slug kontrolü yok.
- Canlı veritabanına dışarıdan bağlanırken sunucu sertifikası doğrulanmıyor (`TrustServerCertificate=True`): Bağlantı şifreli, ama karşı tarafın kimliği kanıtlanmıyor. Ücretsiz katmanın sertifikası herkesin güvendiği bir otoriteden değil.
- Yayın ve tablo değişiklikleri canlıya elle uygulanıyor; push'ta otomatik yayın yok.
- Ücretsiz katmanda veritabanının otomatik yedeği yok.