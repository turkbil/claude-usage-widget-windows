# Claude Usage Widget — Windows

[![En son sürüm](https://img.shields.io/github/v/release/turkbil/claude-usage-widget-windows?label=indir&logo=github&color=d68c45)](https://github.com/turkbil/claude-usage-widget-windows/releases/latest)
[![Lisans: MIT](https://img.shields.io/badge/Lisans-MIT-d68c45.svg)](LICENSE)
[![Windows 10/11](https://img.shields.io/badge/Windows-10%20%7C%2011-d68c45?logo=windows)](#gereksinimler)

**Windows sistem tepsisi widget'ı** — macOS kardeşiyle aynı veri kaynağı, aynı dropdown, aynı Ayarlar penceresi, aynı özellikler.

> 💻 **Bu Windows sürümü.** Mac'te misin? → [**claude-usage-widget**](https://github.com/turkbil/claude-usage-widget)
>
> 🌐 [**English README**](README.md)  ·  Ücretsiz, açık kaynak (MIT), `.exe` [Releases sayfasında](https://github.com/turkbil/claude-usage-widget-windows/releases/latest)

```
…  [^]  [🟢32]  [🔊]  [📶]  [12:46]            ← saatin yanına oturur

┌──────────────────────────────────────────┐
│  Nurullah                    [Max 20x]   │
│  ────────────────────────────────────    │
│  BU HAFTA                3g 18s kaldı    │
│   Tüm modeller ████████░░░░░░░░░ %32     │
│   Sonnet       █░░░░░░░░░░░░░░░░  %2     │
│   ↗ Hafta sonu tahmini: %64              │
│   ╭───────●─ ─ ─ ─ ─ ─ ─◌╮               │
│  ────────────────────────────────────    │
│  5 SAATLİK PENCERE       2s 38dk kaldı   │
│   Kullanım     ██░░░░░░░░░░░░░░░░  %7    │
│  ────────────────────────────────────    │
│           Güncelleme: 12:46              │
│   nurullah.net ↗    @nurullah ↗          │
└──────────────────────────────────────────┘
```

---

## Bu widget'ı farklı kılan ne?

- 🍩 **Veri bazlı görüntüleme** — haftalık %, haftalık kalan süre, 5 saatlik %, 5 saatlik kalan süre: her biri gizlenebilir, yazı olarak gösterilebilir veya tepsi ikonuna küçük renkli donut halka olarak çizilebilir
- 🔮 **Burn-rate tahmini** — "↗ Hafta sonu tahmini: %64", veya hızın 100'ü aşıyorsa: "⚠ Bu hızla yaklaşık 1g 8s sonra limit"
- 📈 **Sparkline trend** — geçmiş örnekler + 7 günlük projeksiyon. İlk günden anlamlı
- 🤝 **Yerleşik MCP server** — Claude Code'un kendisi `get_usage` ile haftalık limitini görebilir
- 🌐 **Yerel HTTP + CLI** — Raycast / AutoHotKey / PowerShell otomasyonları için
- 🦊 **Çoklu tarayıcı failover** — Chrome / Brave / Edge / Arc. Kullandıklarını aç; geçerli oturum bulan ilk tarayıcı kazanır
- 🔔 **Edge-triggered bildirimler** — üç ayarlanabilir seviye, her seviye haftada bir kez tetiklenir
- 🎚 **Gerçek ayarlar penceresi** — tüm tercihler tek bakışta
- 🪙 **Üçüncü taraf servis yok** — sadece claude.ai (veri) ve günde bir kez api.github.com (sürüm kontrolü, kapatılabilir)

---

## Hızlı kurulum

1. [**Releases sayfasından**](https://github.com/turkbil/claude-usage-widget-windows/releases/latest) `ClaudeUsageWidget.exe`'yi indir
2. Kalıcı bir yere koy (örn. `C:\Tools\ClaudeUsageWidget\`)
3. Çift tıkla. Tepsiye gelir.

> **Windows 11 ilk açılış:** yeni tepsi ikonları varsayılan olarak **^** taşma menüsünde gizlenir. Saatin yanına sabitlemek için: görev çubuğuna sağ tıkla → *Görev çubuğu ayarları* → *Diğer sistem tepsisi simgeleri* → **ClaudeUsageWidget**'ı **Açık** yap.

> **Windows SmartScreen uyarısı:** binary EV-imzalı değil (paid Authenticode sertifikası küçük bir tool için pahalı), bu yüzden Windows "Windows PC'nizi korudu" diyebilir. **Daha fazla bilgi → Yine de çalıştır**'a tıkla. Bundan sonra sessizce açılır.

### Açılışta başlat

Tepsi ikonuna sağ tıkla → **Açılışta başlat**. `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` altında tek değer.

### Kaldırma

Tepsiye sağ tıkla → **Çıkış**, sonra sil:
- `.exe`'nin kendisi
- `%LOCALAPPDATA%\ClaudeUsageWidget\` (ayarlar + geçmiş)

---

## Gereksinimler

| Bileşen | Neden |
|---|---|
| **Windows 10 / 11 (x64)** | Native WinForms tepsi uygulaması |
| **Chromium tabanlı tarayıcı** + aktif claude.ai oturumu | `sessionKey`'i tarayıcı cookie deposundan okur. Chrome, Brave, Edge, Arc desteklenir — Ayarlar → Tarayıcılar'dan seç. |
| **Claude.ai hesabı** (herhangi bir plan) | Gösterilecek veri için |

> **Eklenti yok, API key yok, masaüstü Claude uygulaması yok.** Sadece Chromium tarayıcı + claude.ai'da açık oturum.

---

## Ayarlar özet

Tepsi ikonuna sağ tıkla → **Ayarlar…**

| Bölüm | İçerik |
|---|---|
| **Başlık içeriği** | Haftalık %, Haftalık kalan, 5 saatlik %, 5 saatlik kalan — her biri için: gizle / yazı / donut. Donut'ta 8 renkten seç. |
| **Simge** | Hazır emoji, özel emoji (kutuya tıkla → Win+. emoji seçici açılır), donut özet, ya da simge yok. |
| **Yenileme aralığı** | 30 sn · 1 dk · 5 dk · 10 dk |
| **Bildirimler** | Eşik bildirimlerini aç/kapat. Üç ayarlanabilir seviye uyarı/alarm/kritik eşiklerini geçince balon ipucu tetikler. Her seviye haftada bir. |
| **Klavye kısayolu** | Global kısayol aç/kapat. Varsayılan Ctrl+Alt+U her yerden dropdown açar. |
| **Tarayıcılar** | Chrome / Brave / Edge / Arc — hangisinden okuyacağını seç. İlk geçerli oturum kazanır. |
| **Ağ & entegrasyon** | Günlük güncelleme kontrolü · Yerel HTTP endpoint :9123 · MCP kurulum talimatları |

---

## Entegrasyon

### MCP — Claude Code kendi limitini görür
Ayarlar → "MCP kurulum talimatları…" `~/.claude.json` için JSON parçası verir:

```json
{
  "mcpServers": {
    "claude-usage": {
      "command": "C:\\Tools\\ClaudeUsageWidget\\ClaudeUsageWidget.exe",
      "args": ["--mcp-server"]
    }
  }
}
```

Claude Code'u yeniden başlat → Claude `get_usage`'ı çağırıp limiti görür.

### Yerel HTTP — Raycast / script'ler için
Ayarlardan aç, sonra:
```powershell
PS> Invoke-RestMethod http://localhost:9123/usage
weekly_utilization_pct : 32
weekly_resets_at       : 2026-05-16T05:00:00Z
plan                   : Max 20x
…
```
Sadece 127.0.0.1'de dinler. Dışarıya açılmaz.

### CLI — tek seferlik JSON
```powershell
PS> ClaudeUsageWidget.exe --print-usage
{
  "weekly_utilization_pct": 32,
  "plan": "Max 20x",
  …
}
```

---

## Kaynaktan derleme

[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) gerekiyor.

```powershell
git clone https://github.com/turkbil/claude-usage-widget-windows.git
cd claude-usage-widget-windows
dotnet publish src\ClaudeUsageWidget.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true
.\src\bin\Release\net8.0-windows\win-x64\publish\ClaudeUsageWidget.exe
```

CI her tag push'unda aynısını yapıp `.exe`'yi GitHub Releases'a yükler.

---

## Gizlilik

- **Telemetri yok.** Dışarı tek trafik claude.ai'ya HTTPS (veri) ve günde bir kez api.github.com (sürüm kontrolü, kapatılabilir).
- **Cookie diske yazılmaz** — sadece bellekte.
- **`%LOCALAPPDATA%\ClaudeUsageWidget\` içindeki dosyalar:**
  - `prefs.v1.json` — ayarlar
  - `history.json` — 14 günlük sparkline örnekleri
- **Ayarlar** sadece toggle/değer tercihleri — PII yok.

---

## Diller

Windows tercih edilen dilini otomatik algılar, İngilizce'ye geri düşer.

| | |
|---|---|
| 🇬🇧 | English (varsayılan) |
| 🇹🇷 | Türkçe |
| 🇩🇪 | Deutsch |
| 🇪🇸 | Español |
| 🇫🇷 | Français |

---

## Sorun giderme

| Belirti | Çözüm |
|---|---|
| Tepside `?` + "claude.ai oturumu yok" | Tarayıcını aç, claude.ai'a giriş yap. Tarayıcının Ayarlar → Tarayıcılar'da etkin olduğundan emin ol. |
| `HTTP 401` | Oturum süresi dolmuş. Tarayıcında tekrar giriş yap. |
| İkonu göremiyorum | Windows 11 yeni ikonları **^** taşma menüsünde gizler. Görev çubuğu ayarlarından sabitle. |
| SmartScreen uyarısı | Binary imzasız (EV sertifika pahalı). **Daha fazla bilgi → Yine de çalıştır**'a bir kere tıkla. |
| Eski yüzde | Tepsiye sağ tıkla → **Şimdi yenile** |

---

## Yazar

**Nurullah Okatan** — [nurullah.net](https://www.nurullah.net) · [@nurullah](https://x.com/nurullah)

## Lisans

[MIT](LICENSE) © Nurullah Okatan

Anthropic ile bağlı değildir. "Claude", Anthropic'in markasıdır.
