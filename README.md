# BAT-X3 HSS — Operatör Arayüzü

BAT-X3 HSS, TEKNOFEST 2026 Çelik Kubbe Hava Savunma Sistemleri yarışması için geliştirilen bir taretin (havadan gelen tehditleri tespit edip izleyen bir silah sistemi) operatör kontrol sistemidir. Görüntü işleme ve tespit tarafı ayrı bir Python servisinde çalışır; bu repo, o servisle özel bir UDP protokolü üzerinden konuşan **operatör arayüzlerini** içerir.

Python servisi: [ekrem117/bat-x3-hss](https://github.com/ekrem117/bat-x3-hss)

İki farklı istemci aynı alt katmanları (Domain/Application/Infrastructure) paylaşır:

- **WPF masaüstü uygulaması** (`BatX3_HSS_GUI.Client`) — projenin orijinal, tam özellikli operatör arayüzü.
- **ASP.NET Core web uygulaması** (`BatX3_HSS_GUI.WebApi`) — WPF arayüzünü tarayıcıya taşıyan, SignalR ile gerçek zamanlı çalışan yeni geliştirme.

## Mimari

```
src/
  BatX3_HSS_GUI.Domain          Protokol/veri modelleri (framework'ten bağımsız)
  BatX3_HSS_GUI.Application     İş kuralları: mod geçişleri, silah/hareket komutları, parametre servisi
  BatX3_HSS_GUI.Infrastructure  UDP protokolü: komut (GET/SET), video ve tespit akışları
  BatX3_HSS_GUI.Client          WPF masaüstü arayüzü (CommunityToolkit.Mvvm)
  BatX3_HSS_GUI.WebApi          ASP.NET Core MVC + SignalR web arayüzü
tests/
  BatX3_HSS_GUI.Tests           Domain/Application katmanları için birim testleri
```

Domain/Application/Infrastructure katmanları her iki istemci tarafından da olduğu gibi kullanılır — turret ile konuşan UDP protokolü, mod/silah/hareket iş kuralları yalnızca bir kez yazılır.

### İletişim protokolü

Python tarafındaki servis ile üç ayrı UDP kanalı üzerinden konuşulur:

| Kanal | Yön | Amaç |
|---|---|---|
| Komut (GET/SET) | Çift yönlü, istek/cevap | Sistem modu, silah durumu, hareket komutları, parametreler |
| Video | Tek yönlü, sürekli akış | JPEG video kareleri |
| Tespit | Tek yönlü, sürekli akış | Tespit edilen hedeflerin koordinat/sınıf/güven bilgisi |

Protokolün tam tanımı Python deposundaki [`docs/protocol_spec.md`](https://github.com/ekrem117/bat-x3-hss/blob/main/docs/protocol_spec.md) dosyasındadır.

Web arayüzünde bu üç kanal, WebApi içinde SignalR üzerinden tarayıcıya köprülenir: video ve tespit kareleri sürekli push edilir, komutlar (mod değiştirme, silah yetkilendirme, ateş, pan/tilt hareketi, konfigürasyon) tarayıcıdan SignalR hub metotları çağrılarak gönderilir.

## Özellikler (Web arayüzü)

- Canlı video akışı + tespit kutuları (sınıf, takım, güven yüzdesi; kilitli/tahmin edilen hedefler ayrı gösterilir)
- Çalışma modu değiştirme (Bekleme / Manuel / Otomatik / Döngü Demo / Acil Durdurma)
- Silah kontrolü: yetkilendirme/güvenliğe alma, ateş etme, atış modu/seri adedi/silah seçimi konfigürasyonu
- Pan/Tilt hareketi (ok tuşları ile, yalnızca Manuel modda)
- Tespit güven eşiği ayarı
- Ağ ayarları ve kanal sağlığı (tanılama) sayfaları
- Tüm durumun (mod, bağlantı, telemetri, silah durumu) 500ms'de bir canlı güncellenmesi

## Teknoloji yığını

- .NET 8 (Domain/Application/Infrastructure/Client) ve .NET 10 (WebApi)
- WPF + CommunityToolkit.Mvvm (masaüstü)
- ASP.NET Core MVC + SignalR (web)
- Ham UDP soketleri üzerinde özel bir metin protokolü (Infrastructure katmanı)
- xUnit (testler)

## Nasıl çalıştırılır

**Gereksinimler:** .NET 8 SDK ve .NET 10 SDK. WPF uygulaması yalnızca Windows'ta çalışır.

Python görüntü/tespit/komut servisinin ayrı olarak çalışıyor olması gerekir. Her istemcinin `appsettings.json` dosyasındaki `Network` bölümü o servisin gerçek IP/port bilgileriyle eşleşmelidir. Python servisinin varsayılan portları:

```json
"Network": {
  "ServerIp": "127.0.0.1",
  "Command": { "RemotePort": 5005 },
  "Video": { "ListenPort": 5007 },
  "Detection": { "ListenPort": 5008 }
}
```

> **Not:** Web arayüzü (`BatX3_HSS_GUI.WebApi/appsettings.json`) bu portlarla gelir. WPF uygulamasının `BatX3_HSS_GUI.Client/appsettings.json` dosyası ise 2023/2024/2025 portlarıyla gelir; Python servisinin varsayılanlarıyla kullanmak için bu değerleri 5005/5007/5008 olarak güncelleyin veya uygulama içindeki Ağ Ayarları ekranından değiştirin.

Web arayüzünü çalıştırmak için:

```bash
cd src/BatX3_HSS_GUI.WebApi
dotnet run
```

Konsoldaki `Now listening on:` satırında görünen adresin sonuna `/Video` ekleyerek operasyon paneline ulaşılır. Web arayüzü SignalR istemci kütüphanesini CDN'den yüklediği için tarayıcının internet erişimi olmalıdır.

WPF masaüstü uygulaması için (yalnızca Windows):

```bash
cd src/BatX3_HSS_GUI.Client
dotnet run
```

## Testler

```bash
dotnet test
```

Domain/Application katmanlarındaki iş kurallarını (mod geçiş mantığı, gamepad/analog hareket kontrolü, parametre servisi, ayar doğrulama vb.) kapsayan birim testleri içerir.

## Yol haritası

Web arayüzü şu an WPF'teki **Operasyon**, **Ağ Ayarları** ve **Tanılama** ekranlarını karşılıyor. Henüz taşınmamış WPF ekranları:

- Parametreler (genel parametre görüntüleme/düzenleme)
- Kare Senkronizasyonu
- Gamepad ve klavye kısayolu ayarları

## Geliştirici

İbrahim Halil Işık — BAT-X3 Robotik

## Üçüncü taraf bileşenler

`src/BatX3_HSS_GUI.WebApi/wwwroot/lib/` altındaki Bootstrap, jQuery ve jQuery Validation kütüphaneleri kendi lisans dosyalarıyla birlikte dağıtılır.
