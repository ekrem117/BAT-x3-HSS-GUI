# BAT-X3 HSS — Operatör Arayüzü

BAT-X3 HSS, bir yarışma turret'ının (Havadan gelen tehditleri tespit edip izleyen bir silah sistemi) operatör kontrol sistemidir. Görüntü işleme ve tespit tarafı ayrı bir Python servisinde çalışır; bu repo, o servisle özel bir UDP protokolü üzerinden konuşan **operatör arayüzlerini** içerir.

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

Web arayüzünde bu üç kanal, WebApi içinde SignalR üzerinden tarayıcıya köprülenir: video ve tespit kareleri sürekli push edilir, komutlar (mod değiştirme, silah yetkilendirme, ateş, pan/tilt hareketi, konfigürasyon) tarayıcıdan SignalR hub metotları çağrılarak gönderilir.

## Özellikler (Web arayüzü)

- Canlı video akışı + tespit kutuları (sınıf, takım, güven yüzdesi; kilitli/tahmin edilen hedefler ayrı gösterilir)
- Çalışma modu değiştirme (Bekleme / Manuel / Otomatik / Döngü Demo / Acil Durdurma)
- Silah kontrolü: yetkilendirme/güvenliğe alma, ateş etme, atış modu/seri adedi/silah seçimi konfigürasyonu
- Pan/Tilt hareketi (ok tuşları ile, yalnızca Manuel modda)
- Tespit güven eşiği ayarı
- Tüm durumun (mod, bağlantı, telemetri, silah durumu) 500ms'de bir canlı güncellenmesi

## Teknoloji yığını

- .NET 8 (Domain/Application/Infrastructure/Client) ve .NET 10 (WebApi)
- WPF + CommunityToolkit.Mvvm (masaüstü)
- ASP.NET Core MVC + SignalR (web)
- Ham UDP soketleri üzerinde özel bir metin protokolü (Infrastructure katmanı)
- xUnit (testler)

## Nasıl çalıştırılır

Python görüntü/tespit/komut servisinin ayrı olarak çalışıyor olması gerekir. `src/BatX3_HSS_GUI.WebApi/appsettings.json` içindeki `Network` bölümü o servisin gerçek IP/port bilgileriyle eşleşmelidir:

```json
"Network": {
  "ServerIp": "127.0.0.1",
  "Command": { "RemotePort": 5005 },
  "Video": { "ListenPort": 5007 },
  "Detection": { "ListenPort": 5008 }
}
```

Web arayüzünü çalıştırmak için:

```bash
cd src/BatX3_HSS_GUI.WebApi
dotnet run
```

`http://localhost:5263/Video` adresinden operasyon paneline ulaşılır.

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

Web arayüzü şu an WPF'teki **Operasyon** ekranının tüm işlevlerini karşılıyor. Henüz taşınmamış WPF ekranları:

- Parametreler (genel parametre görüntüleme/düzenleme)
- Ağ Ayarları
- Tanılama (kanal sağlığı)
- Kare Senkronizasyonu
