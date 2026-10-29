# turkiye-simulasyonu-api

[![CI](https://github.com/kaya-eyup/turkiye-simulasyonu-api/actions/workflows/ci.yml/badge.svg)](https://github.com/kaya-eyup/turkiye-simulasyonu-api/actions/workflows/ci.yml)

[Türkiye Simülasyonu](https://github.com/kaya-eyup/turkiye-simulasyonu-web)'nun backend'i. ASP.NET Core Web API (.NET 10, controller tabanlı).

Kabul listesi web deposundaki **"Backend sözleşmesi"** tablosu. API bu tablonun bir kısmını karşılıyor: hangi uçların hazır, hangilerinin henüz yok olduğu tablonun **Durum** sütununda ve aşağıdaki [Bilinçli borçlar](#bilinçli-borçlar) bölümünde.

## Durum

**Canlı:** https://turkiye-simulasyonu-api.runasp.net (ör. [`/categories`](https://turkiye-simulasyonu-api.runasp.net/categories)). Frontend ([https://turkiyesimulasyonu.eyupkaya.com](https://turkiyesimulasyonu.eyupkaya.com), Cloudflare; yedek: [Vercel](https://turkiye-simulasyonu-web.vercel.app)) bu API'ye bağlı.

Veri SQL Server'da, EF Core ile okunuyor ve yazılıyor: geliştirmede LocalDB, canlıda MonsterASP. React'teki yorum formu `POST /comments`'e yazıyor. Uçlar DTO döndürüyor. Sorgular servis sınıflarında, controller’lar sadece HTTP’yi karşılıyor.

Yeniden Doğma Anketi'nin oyları sunucuda, `RebirthVotes` tablosunda; frontend tarayıcı başına ürettiği anonim kimliği `X-Voter-Id` başlığıyla gönderiyor.

Öğe oyları da sunucuda: `ItemVotes` tablosu + `Items`'taki `ScoreNCount` sayaçları, aynı anonim kimlikle.

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

### Testler

`dotnet test` (çözüm kökünde). Entegrasyon testleri API'yi bellekte ayağa kaldırır ve gerçek bir SQL Server'a bağlanır: yerelde LocalDB'deki `TurkiyeSimulasyonu_Tests` (her çalışmada silinip migration + seed ile yeniden kurulur), CI'da GitHub Actions'ın yanında açtığı SQL Server konteyneri (`TEST_DB_CONNECTION` ortam değişkeni). SQLite ya da bellek içi veritabanı kullanılmıyor: `KEBAP → 200` davranışı SQL Server'ın harf duyarsız karşılaştırmasından geliyor.

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
- **Her Web Deploy'dan önce** `dotnet ef migrations list --connection $cs`: Listede `(Pending)` kalmışsa önce `database update`. Kod, canlıda olmayan bir sütunu sorgularsa o uç 500 döner (28 Ekim'de `SearchText` migration'ı canlıya uygulanmadan yayın yapıldı; 29 Ekim'de fark edilene kadar canlıda arama 500 verdi).
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

Yeni öğe **0 oyla** girer: dosyadaki `distribution` yok sayılır (eski parti dosyaları yeniden yüklenirken sahte oy geri gelmesin diye uyarıyla). `createdAt` verilmezse içe aktarma anı yazılır. `featured`, `search`, `my-votes` slug olarak reddedilir.

```powershell
# local (varsayılan bağlantı: LocalDB)
dotnet run --project TurkiyeSimulasyonu.Importer -- --dry-run <klasör>
dotnet run --project TurkiyeSimulasyonu.Importer -- <klasör>

# production
dotnet run --project TurkiyeSimulasyonu.Importer -- --connection "<dış connection string>" --dry-run <klasör>
dotnet run --project TurkiyeSimulasyonu.Importer -- --connection "<dış connection string>" <klasör>
```

Parti dosyası formatı: `{ "category": {id,name,emoji,order}?, "items": [{id,categoryId,name,emoji,summary}] }`.
`"category"` sadece yeni bir kategori açılıyorsa var; mevcut kategoriye öğe
eklerken yok. Bozuk bir dosya `SaveChanges()` öncesi `json.loads`/deserialize
aşamasında patlar — tek transaction olduğu için ya hepsi yazılır ya hiçbiri.

Bakım komutu — `--backfill-search`: Var olan bütün öğelerin `SearchText`'ini yeniden hesaplatır. Yeni sütun eklendiğinde eski satırları doldurmak için bir kez çalıştırılır. Dosya almaz. Tekrar çalıştırmak zararsız: Değer değişmeyen satır için EF `UPDATE` göndermez.

dotnet run --project TurkiyeSimulasyonu.Importer -- --backfill-search --dry-run
dotnet run --project TurkiyeSimulasyonu.Importer -- --backfill-search
dotnet run --project TurkiyeSimulasyonu.Importer -- --connection "<dış connection string>" --backfill-search

Bakım komutu — `--recount-votes`: `Score1Count`…`Score5Count` sayaçlarını `ItemVotes`'tan yeniden kurar. Gerçek oylar bire bir korunur; tabloda karşılığı olmayan (seed'den kalma sahte) pay silinir. Dosya almaz.

- Gerçek çalıştırma, değiştirmeden önce bütün sayaçların dökümünü `backups/scores-<tarih>.json`'a yazar (depoya girmez). Dosya yazılamazsa hiçbir şey değişmez.
- Sayım SERIALIZABLE transaction'da ve `ItemVotes`'a tablo kilidiyle yapılır: o anda gelen oy birkaç milisaniye bekler, kaybolmaz. Kilit sırası oy yazma yoluyla aynı (önce `ItemVotes`, sonra `Items`).
- Değişmez kural kontrolü: Sayaç toplamı `ItemVotes`'taki satır sayısına eşit değilse transaction geri alınır.
- `--dry-run` aynı `UPDATE`'i çalıştırıp geri alır. Gerçek çalıştırmadan sonraki `--dry-run`'da `Sayacı değişen öğe: 0` görmek, sayaçla oy tablosunun tutarlı olduğunun kontrolüdür.

dotnet run --project TurkiyeSimulasyonu.Importer -- --recount-votes --dry-run
dotnet run --project TurkiyeSimulasyonu.Importer -- --connection "<dış connection string>" --recount-votes --dry-run
dotnet run --project TurkiyeSimulasyonu.Importer -- --connection "<dış connection string>" --recount-votes

## Uçlar

| Uç | Başarılı | Diğer durumlar |
| --- | --- | --- |
| `GET /categories` | 200, `order`'a göre sıralı (eşitse `Id`) | |
| `GET /categories/{id}` | 200 | olmayan id → 404 (ProblemDetails) |
| `GET /items` | 200, bütün öğeler (`Id` sırasıyla, arama sayfası için) | |
| `GET /items?categoryId=` | 200, kategorinin öğeleri (`Id` sırasıyla) | olmayan kategori → 200 + `[]` · boş değer → bütün öğeler |
| `GET /items/featured`    | 200, her zaman 5 öğe, aktifliğe göre rastgele öğelerle dolar | boşluk kalma durumunda tamamen rastgele öğeler seçilir |
| `GET /items/search?q=&page=` | 200, `{ items, total, totalPages }`; sayfa başına 12 öğe, `Id` sırasıyla | boş ya da eşleşmeyen `q` → 200 + `total: 0` · son sayfanın ötesi → 200 + boş `items`, `total` aynı · `q` 100 karakterden uzun ya da `page < 1` → 400 |
| `GET /items/{id}` | 200 | olmayan id → 404 · büyük harfli id (`KEBAP`) → 200, cevapta kanonik `"id": "kebap"` |
| `GET /comments?itemId=` | 200, yeniden eskiye | `itemId` yok ya da boş → 400 (`errors.itemId`) · olmayan öğe → 200 + `[]` |
| `POST /comments` | 201 + oluşturulan kayıt | olmayan öğe → 400 (`errors.itemId`) · eksik alan ya da bozuk JSON → 400 · `Content-Type` yok → 415 · uzun ya da çok kısa metin → 400 |
| `GET /surveys/rebirth` | 200, `{ total, results: [{ country, count }] }`; çoktan aza, eşitse koda göre | hiç oy yok → 200 + `total: 0`, `results: []` |
| `GET /surveys/rebirth/my-vote` | 200 + `{ country }`; kimlik `X-Voter-Id` başlığında | henüz oy yok → 404 · başlık yok ya da GUID değil → 400 |
| `PUT /surveys/rebirth/my-vote` | 200 + `{ country }`; ilk oy da değişiklik de 200 | atanmamış ya da alfa-3 kod → 400 (`errors.Country`) · başlık yok ya da GUID değil → 400 · IP başına dakikada 20'yi aşınca → 429 |
| `GET /items/my-votes` | 200 + `[{ itemId, stars }]`; kimlik `X-Voter-Id` başlığında | hiç oy yok → 200 + `[]` · başlık yok ya da GUID değil → 400 |
| `PUT /items/{id}/my-vote` | 200 + `{ itemId, stars }` (kanonik slug); ilk oy da değişiklik de 200 | `stars` 1-5 dışında ya da yok → 400 (`errors.Stars`) · olmayan öğe → 404 · başlık yok → 400 · IP başına dakikada 60'ı aşınca → 429 |
| `DELETE /items/{id}/my-vote` | 204 (oy zaten yoksa da) | olmayan öğe → 404 · başlık yok → 400 · 429 aynı sınır |

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
  - `SearchText`: metin, zorunlu, en fazla 700 karakter; `Name + " " + Summary`'nin sadeleştirilmiş hâli. `AppDbContext.SaveChanges` dolduruyor, elle yazılmıyor. Index yok (aşağıda).
- **Comments**
  - `Id`: int, birincil anahtar (identity).
  - `ItemId`: int, zorunlu; foreign key → `Items.Id`.
  - `Author`: metin, zorunlu, en fazla 30 karakter.
  - `Body`: metin, zorunlu, en fazla 500 karakter.
  - `CreatedAt`: `DateTimeOffset`, zorunlu.
- **RebirthVotes**
  - `Id`: int, birincil anahtar (identity).
  - `VoterId`: `uniqueidentifier`, zorunlu, tekil; tarayıcının ürettiği anonim kimlik.
  - `CountryCode`: `char(2)`, zorunlu, ISO 3166-1 alfa-2, büyük harf; index'li.
  - `CreatedAt`, `UpdatedAt`: `DateTimeOffset`, zorunlu.
- **ItemVotes**
  - `Id`: int, birincil anahtar (identity).
  - `VoterId`: `uniqueidentifier`, zorunlu; anketle aynı anonim kimlik.
  - `ItemId`: int, zorunlu; foreign key → `Items.Id`.
  - `Stars`: `tinyint`, zorunlu; `CHECK (Stars BETWEEN 1 AND 5)`.
  - `CreatedAt`, `UpdatedAt`: `DateTimeOffset`, zorunlu.
  - `(VoterId, ItemId)` tekil index: tarayıcı başına öğe başına tek oy; VoterId önde olduğu için "bir tarayıcının bütün oyları" da aynı index'le bulunur.

Foreign key sütunlarında (`Items.CategoryId`, `Comments.ItemId`) EF'in varsayılan index'i var: bir öğenin yorumları bütün tablo okunmadan bulunur.

### İlişkiler ve silme

- **Kategori → öğe (1-N):** Öğesi olan kategori silinemez (`Restrict`).
- **Öğe → yorum (1-N):** Öğe silinirse yorumları da silinir (`Cascade`).
- **Öğe → oy (1-N):** Öğe silinirse oyları da silinir (Cascade).

## Yeniden Doğma Anketi

- **Girişsiz v1: tarayıcı başına bir oy.** Frontend ilk ziyarette `crypto.randomUUID()` ile bir kimlik üretip saklıyor ve her istekte `X-Voter-Id` başlığıyla gönderiyor. Çerez değil başlık, çünkü frontend ve API farklı alan adlarında; Safari gibi tarayıcılar başka alan adındaki sunucunun çerezini engelliyor. Giriş geldiğinde bu başlığın yerini `Authorization` alacak; gövde (`{ country }`) değişmeyecek.
- **Kimlik gövdede değil başlıkta**, çünkü "kim olduğun" kaynağın içeriği değil, isteğin bağlamı.
- **Upsert, yarışa dayanıklı.** Önce `UPDATE … WHERE VoterId = @id` (okumadan, `ExecuteUpdateAsync`). 0 satır değiştiyse `INSERT`. `INSERT` tekillik hatası verirse (SQL Server 2601/2627) aynı kimlikten başka bir istek araya girmiş demektir: `UPDATE` tekrar çalıştırılır. Doğruluğu kod değil `VoterId`'deki tekil index garanti ediyor; kod yalnızca kısıtın cevabına göre davranıyor. Entegrasyon testi aynı kimlikten aynı anda 20 istek atıp tek oy sayıldığını ve hiçbir isteğin 500 almadığını kontrol ediyor.
- **PUT, POST değil.** İstek "benim oyum bu olsun" diyor; aynı isteği tekrar göndermek sonucu değiştirmiyor. Çift tıklama ve ağ kopunca yeniden deneme güvenli. İlk oy da değişiklik de 200.
- **Sayaç yok, sonuç her istekte `GROUP BY` ile sayılıyor.** Oy değiştirmek tek satırın tek sütununu değiştirmek demek; "eskiyi azalt, yeniyi artır" ikilisine ve onları birlikte tutacak transaction'a gerek kalmıyor, sayaçla gerçek oylar ayrışamıyor. En fazla 249 grup var. Toplam ayrı bir `COUNT(*)` değil, aynı sonucun toplamı: İki sorgu arasında gelen oy, toplamla listeyi tutarsız yapamıyor.
- **Ülke listesi kodda** (`Surveys/Countries.cs`, ISO 3166-1'in resmi 249 kodu), .NET'in `RegionInfo`'sundan değil. `RegionInfo` makinenin dil verisine (ICU) bakıyor; Windows sunucu ile Linux CI farklı liste verebilir. Kosova (`XK`) gibi yaygın ama resmi listede olmayan kodlar kabul edilmiyor.
- **Kod `ToUpperInvariant` ile büyütülüyor.** `ToUpper()` Türkçe ayarlı makinede `"it"`'i `"İT"` yapar ve İtalya'ya oy veren 400 alırdı (Turkish I problem, aramadaki `SearchNormalizer` kararının aynısı).
- **Birincil anahtar sıralı `int`, `VoterId` ayrı tekil index.** SQL Server satırları birincil anahtar sırasına diziyor (clustered index). Rastgele GUID anahtar her yeni satırı ortaya sıkıştırırdı; sıralı sayıda yeni satır hep sona ekleniyor.
- **`CountryCode` `char(2)`, ASCII.** Hep tam 2 harf; `nvarchar(2)`'den küçük.

## Öğe oyları

- **Anketin tersine sayaç var.** Dağılım her liste sayfasında okunuyor (1224 öğeye kadar); çok okunan sayı her okumada `GROUP BY` ile sayılmıyor, yazarken bir kez hesaplanıyor.
- **Gerçek oyların kaynağı `ItemVotes`, sayaçlar onun özeti.** Seed'in sahte başlangıç dağılımı `--recount-votes` ile silindi; yeni öğeler 0 oyla giriyor. Sayaçlar her an `ItemVotes`'tan yeniden kurulabilir.
- **Oy satırı ve sayaç aynı transaction'da.** Ya ikisi birden yazılır ya hiçbiri.
- **Oy değişikliği karşılaştır-ve-değiştir ile:** `UPDATE … SET Stars = @new WHERE … AND Stars = @old`. Arada başka bir istek oyu değiştirdiyse 0 satır etkilenir ve işlem baştan okunarak tekrar denenir (en fazla 5 kez). Koşul olmasa iki eşzamanlı değişiklik yanlış sütunu azaltır, dağılım sessizce bozulurdu. Entegrasyon testi aynı kimlikten aynı anda 5 farklı puan gönderip tek oy sayıldığını kontrol ediyor.
- **Sayaçlar okunmadan kaydırılıyor:** `SET Score4Count = Score4Count + 1` tek komut; satır kilidi aynı öğeye gelen oyları sıraya sokuyor. Bütün yollar kilitleri aynı sırayla alıyor (önce `ItemVotes`, sonra `Items`); kilitlenme oluşmuyor.
- **`DELETE` tekrarlanabilir:** oy yoksa da 204. **`my-votes` koleksiyon:** boşsa 404 değil `[]`.
- **Tarayıcıdaki eski oylar taşınmadı.** Birkaç tarayıcıdaki birkaç oy için tek seferlik taşıma kodu, depoda kalıcı yük olurdu.

### Kararlar

- **Değişmeyen sayı anahtar + ayrı tekil slug.** Tablolar arası bağlar sayıya kurulu; slug düzeltildiğinde hiçbir bağ kopmaz.
- **Dağılım 5 ayrı sütun.** Oy geldiğinde sayaç veritabanında tek komutla artırılabilsin diye (`SET Score4Count = Score4Count + 1`). Dağılım tek hücrede JSON dizi olarak tutulsaydı bu mümkün olmazdı: değeri okuyup değiştirip geri yazmak gerekirdi ve aynı anda gelen iki oydan biri kaybolabilirdi.
- **`Order` değil `DisplayOrder`.** `ORDER` SQL'in ayrılmış kelimesi (`ORDER BY`); elle yazılan sorgularda karışıklık çıkarmasın.
- **Tarihler `DateTimeOffset`.** Değer, UTC'ye göre farkıyla birlikte saklanır; an tek anlamlı tanımlanır.
- **Uzunluklar.** `Author` 30 ve `Body` 500 sözleşmeden. Diğerleri `db.json`'daki en uzun değerlerin rahatça üstünde seçildi; `Emoji` 32, çünkü ekranda tek görünen bir emoji içeride 10'dan fazla karakter tutabilir.
- **DTO:** `id = slug`, iç numara dışarı çıkmıyor. Başka bir kaynağa işaret eden alan, o kaynağın slug’ını taşıyor (`categoryId`, `itemId`).
- **Arama ayrı sütunda, dil ayarından bağımsız.** Aranan metin ve kayıttaki metin aynı kurala göre sadeleştirilir (`"Çiğ Köfte"` → `"cig kofte"`) ve arama bu sütunda yapılır. `SearchNormalizer` Türkçe harfleri açık bir tabloyla eşler; `ToLower()` kullanılmaz, çünkü o makinenin dil ayarına bakar: Türkçe ayarlı bir sunucuda `"I"` küçülünce `"ı"`, İngilizce ayarlıda `"i"` olur ve aynı kod iki makinede farklı sonuç verir ("Turkish I problem"). Sütunu `SaveChanges` doldurduğu için DbSeeder, Importer ve API aynı kuralı kullanır, kimse doldurmayı unutamaz.
- **Arama `/items`'ta değil `/items/search`'te.** `/items?categoryId=` kategori sayfası için sayfalanmamış tam listeyi dönüyor. Aynı uçtan bazen dizi bazen `{ items, total, totalPages }` dönmesi, istemcide union, belge sayfasında ise anlatılamayan bir uç demekti. Arama kendi ucunda; `featured` ile aynı desen.
- **Sayfalama veritabanında.** Toplam `COUNT(*)` ile, sayfa `OFFSET … FETCH NEXT` ile alınır (EF'te `Skip`/`Take`). İki sorgu atılır: sayfa sayısını bilmek için toplam gerekir, toplamı öğrenmek için bütün satırları çekmek gerekmez. Sayfa boyutu 12.
- **`featured` gerçek takvim haftasına göre sabit, "son 7 gün" kayan pencereye göre değil.** Havuz, bu ISO haftasının Pazartesi'sinden bugüne yorum almış öğelerden oluşuyor. Seçim, haftanın yıl+numarasından türetilen sabit bir tohumla karıştırılıyor; bu yüzden aynı hafta içindeki her istek aynı 5 öğeyi döndürüyor (sayfa yenilemede değişmiyor), hafta değişince otomatik olarak yeni bir seçki çıkıyor. Haftanın başında havuz küçük/boş olabilir — bu beklenen bir durum, henüz o hafta az şey konuşulmuş demektir.
- **Rastgele seçim bellekte.** SQL Server'da rastgele sıralamanın yolu `ORDER BY NEWID()`; `EF.Functions.Random()` ise `RAND()`'a çevrilir ve `RAND()` sorgu başına tek değer ürettiği için hiç karıştırmaz. Aday id’ler çekilip haftanın yıl+numarasından tohumlanan new Random(seed).Shuffle (Fisher-Yates) ile karıştırılıyor; aynı tohum aynı sırayı verdiği için seçki hafta boyunca sabit.

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
- Doğrulama: DataAnnotations CreateCommentRequest üzerinde; metin kırpıldıktan sonra kontrol edilir; boş author “anonim” olur.
- Hatalar: AddProblemDetails + GlobalExceptionHandler + UseStatusCodePages; ayrıntı sadece logda.
- Hız sınırı: IP başına dakikalık sabit pencere, aşılınca 429 + Retry-After. `POST /comments` 5, `PUT /surveys/rebirth/my-vote` 20 (listede gezinip fikir değiştirmek doğal), `PUT`/`DELETE /items/{id}/my-vote` 60 (öğeler arasında gezip puan vermek sık bir iş). Her politika kendi sayacını tutuyor.

## Bilinçli borçlar

- Anket kimliği anonim: Tarayıcı verisini silen ya da başlığa yeni bir GUID yazan biri tekrar oy verebilir. İstek sınırı bunu yavaşlatıyor ama durdurmuyor. Giriş geldiğinde oy kullanıcıya bağlanacak.
- Anket sonuçları her istekte yeniden sayılıyor. Binlerce oyda milisaniyeler; milyonlarca oyda önbellek ya da ayrı bir özet tablo gerekir.
- `/items` ve yorum listesi sayfasız.
- Seed tablo seviyesinde kontrol ediyor ve sadece senkron.
- `featured`, `search` ve `my-votes` ayrılmış kelimeler: Bu slug'la bir öğe açılırsa kendi adresinden erişilemez; slug kontrolü yok.
- Canlı veritabanına dışarıdan bağlanırken sunucu sertifikası doğrulanmıyor (`TrustServerCertificate=True`): Bağlantı şifreli, ama karşı tarafın kimliği kanıtlanmıyor. Ücretsiz katmanın sertifikası herkesin güvendiği bir otoriteden değil.
- Yayın ve tablo değişiklikleri canlıya elle uygulanıyor; push'ta otomatik yayın yok.
- Ücretsiz katmanda veritabanının otomatik yedeği yok.
- `SearchText`'te index yok. Arama `LIKE '%…%'` ile yapılıyor ve bu kalıp baştan eşleşmediği için B-tree index kullanılamaz; işe yaramayacak bir index sadece her yazmayı yavaşlatırdı. 1224 kayıtta tam tablo taraması kabul edilebilir. Gerçek çözüm full-text search, Blok H'de.
- Arama sonuçları alaka düzeyine göre değil `Id` sırasına göre dönüyor.
- `featured` dolgusu bütün aday id'leri belleğe çekip orada karıştırıyor. 1224 kayıtta bedeli yok; tablo büyürse karıştırma SQL'e taşınmalı. `ORDER BY NEWID()` her istekte farklı sonuç verip haftalık sabitliği bozacağı için tohumdan türetilen bir sıralama gerekir (ör. `ORDER BY CHECKSUM(Id, @seed)`).
- `GetFeaturedAsync`, sistem saatini (`DateTimeOffset.UtcNow`) doğrudan okuyor; bu, hafta geçişi davranışını otomatik testle doğrulamayı zorlaştırıyor. Gerçek bir üründe `TimeProvider` DI ile enjekte edilip sahte bir saatle test edilirdi.
- Sayaç ile `ItemVotes` arasındaki tutarlılık kontrolü elle (`--recount-votes --dry-run`); düzenli çalışan bir iş ya da alarm yok.
- Yerel seed (`db.json`) hâlâ sahte dağılım taşıyor; sadece geliştirme ve testler için. Yerelde de silmek için `--recount-votes`.