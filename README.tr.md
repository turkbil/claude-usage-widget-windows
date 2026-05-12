# Claude Kullanım Widget'ı — Windows

Windows sistem tepsisinde, saatin yanına yerleşen küçük bir widget. Claude haftalık kullanım yüzdeni tek bakışta gösterir.

> macOS sürümü mü arıyorsun? → [**claude-usage-widget**](https://github.com/turkbil/claude-usage-widget)

[**English README →**](README.md)

```
…  [^]  [🟢32]  [🔊]  [📶]  [12:46]
        ↑ tıklayınca detaylı dropdown açılır
```

```
┌──────────────────────────────────────────┐
│  Nurullah                    [Max 20x]   │
│  ────────────────────────────────────    │
│  BU HAFTA                3g 18s kaldı    │
│   Tüm modeller ████████░░░░░░░░░ %32     │
│   Sonnet       █░░░░░░░░░░░░░░░░  %2     │
│  ────────────────────────────────────    │
│  5 SAATLİK PENCERE       2s 38dk kaldı   │
│   Kullanım     ██░░░░░░░░░░░░░░░  %7     │
│  ────────────────────────────────────    │
│           Güncelleme: 12:46              │
└──────────────────────────────────────────┘
```

## Özellikler

- 🎯 [claude.ai/settings/usage](https://claude.ai/settings/usage)'da gördüğün `% used` rakamının aynısı
- ⏱ Haftalık reset'e geri sayım (örn. "3g 18s kaldı")
- 🪟 5 saatlik pencere için ayrı progress bar
- 🎨 Renkli barlar: yeşil → sarı → turuncu → kırmızı
- 🌍 Otomatik dil: English, Türkçe, Deutsch, Español, Français
- 🚀 İsteğe bağlı "Açılışta başlat" (kullanıcı seviyesinde, admin gerekmez)
- 🔒 Şifre saklamaz — Chrome oturumunu Windows DPAPI ile okur

## Gereksinimler

| Bileşen | Neden |
|---|---|
| **Windows 10/11 (x64)** | Native WinForms tray uygulaması |
| **Google Chrome** + aktif claude.ai oturumu | `sessionKey` Chrome'un cookie deposundan okunur |
| **Claude.ai hesabı** (Free, Pro, Max) | Gösterilecek veri için |

> **Eklenti gerekmez, API key gerekmez, masaüstü Claude uygulaması gerekmez.** Sadece Chrome + claude.ai'da açık oturum.

Chrome'da claude.ai'a giriş yapmadıysan widget `⚠ claude.ai oturumu yok — Chrome'da giriş yap` gösterir.

## Kurulum

### Kolay yol: hazır `.exe` indir

1. En son `ClaudeUsageWidget.exe`'yi [Releases sayfasından](https://github.com/turkbil/claude-usage-widget-windows/releases) (veya en son başarılı [Actions koşumu](https://github.com/turkbil/claude-usage-widget-windows/actions)) indir
2. Kalıcı bir yere koy (örn. `C:\Tools\ClaudeUsageWidget\`)
3. Çift tıkla. Sistem tepsisinde belirir.
4. Tepsi ikonuna sağ tıkla → **Açılışta başlat** seçeneğini işaretle

> **Windows 11 ilk-açılış notu:** Yeni tepsi ikonları varsayılan olarak **^** taşma menüsünde gizlenir. Saatin yanına sabitlemek için: taskbar'a sağ tıkla → *Görev çubuğu ayarları* → *Diğer sistem tepsisi simgeleri* → ClaudeUsageWidget'ı **Açık** yap.

### Kendin derle

[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) gerekiyor.

```powershell
git clone https://github.com/turkbil/claude-usage-widget-windows.git
cd claude-usage-widget-windows
dotnet publish src\ClaudeUsageWidget.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true
.\src\bin\Release\net8.0-windows\win-x64\publish\ClaudeUsageWidget.exe
```

## Nasıl çalışıyor?

```
┌──────────────────┐    DPAPI + AES-256-GCM     ┌─────────────────┐
│  Chrome cookie   │ ──────────────────────────▶│  sessionKey     │
│  (şifreli)       │ master key Local State'ten │  (çözülmüş)     │
└──────────────────┘                            └────────┬────────┘
                                                         │
                                       Cookie: sessionKey=...
                                                         ▼
                            ┌────────────────────────────────────────┐
                            │ GET claude.ai/api/organizations/{id}/   │
                            │     usage                              │
                            │      → seven_day.utilization (32.0)    │
                            │      → seven_day.resets_at             │
                            │      → five_hour.utilization (7.0)     │
                            │      → five_hour.resets_at             │
                            └────────────────┬───────────────────────┘
                                             ▼
                                    ┌─────────────────┐
                                    │  Tepsi ikonu UI │
                                    │  60s yenilir    │
                                    └─────────────────┘
```

Widget şifreni asla görmez. Chrome'un kendisinin kullandığı şifreli-cookie + DPAPI mantığını kullanır — Windows'taki her uygulama saklanmış cookie'ler için aynı şeyi yapar.

## Gizlilik

- **Telemetri yok.** Her yenilemede tam olarak iki HTTPS çağrısı, ikisi de `claude.ai`'ya.
- **Üçüncü taraf yok.** Cookie yerel olarak okunur, sadece claude.ai'a karşı kullanılır.
- **Cookie diske yazılmaz.** Sadece bellekte.
- **Saklanan tek ayar:** `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ClaudeUsageWidget` (sadece açılışta başlatma bayrağı).

## Sorun giderme

| Belirti | Çözüm |
|---|---|
| Tepside `?` + "claude.ai oturumu yok" | Chrome'u aç, claude.ai'a giriş yap |
| `HTTP 401` | Oturumun süresi dolmuş. Chrome'da tekrar giriş yap. |
| İkonu göremiyorum | Windows 11 yeni ikonları **^** taşma menüsünde gizler. Görev çubuğu ayarlarından "Diğer sistem tepsisi simgeleri" altından sabitle. |
| Eski yüzde | Tepsiye sağ tıkla → **Şimdi yenile** |
| Açılışta çalışmıyor | Tepsiye sağ tıkla → **Açılışta başlat**'ı işaretle veya `HKCU\…\Run`'ı kontrol et |

## Lisans

[MIT](LICENSE) © [Nurullah Okatan](https://www.nurullah.net)

Anthropic ile bağlı değildir. "Claude", Anthropic'in markasıdır.
