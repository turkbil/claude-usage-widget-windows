# Claude Usage Widget — Windows

A tiny Windows system tray widget that shows your Claude weekly usage % at a glance, right next to the clock.

> Looking for the macOS version? → [**claude-usage-widget**](https://github.com/turkbil/claude-usage-widget)

[**Türkçe README →**](README.tr.md)

```
…  [^]  [🟢32]  [🔊]  [📶]  [12:46]
        ↑ click for the rich dropdown
```

```
┌──────────────────────────────────────────┐
│  Nurullah                    [Max 20x]   │
│  ────────────────────────────────────    │
│  WEEKLY                  3d 18h left     │
│   All models   ████████░░░░░░░░  32%     │
│   Sonnet       █░░░░░░░░░░░░░░░░  2%     │
│  ────────────────────────────────────    │
│  5-HOUR WINDOW           2h 38m left     │
│   Usage        ██░░░░░░░░░░░░░░░  7%     │
│  ────────────────────────────────────    │
│           Updated: 12:46                 │
└──────────────────────────────────────────┘
```

## Features

- 🎯 Pulls the same `% used` number you see on [claude.ai/settings/usage](https://claude.ai/settings/usage)
- ⏱ Countdown to weekly reset (e.g. "3d 18h left")
- 🪟 Separate progress bar for the 5-hour rate window
- 🎨 Color-coded bars: green → yellow → orange → red
- 🌍 Auto-localized: English, Türkçe, Deutsch, Español, Français
- 🚀 Optional "Run at startup" (per-user, no admin)
- 🔒 No credentials stored — reads your existing Chrome session via Windows DPAPI

## Requirements

| Component | Why |
|---|---|
| **Windows 10/11 (x64)** | Native WinForms tray app |
| **Google Chrome** with an active claude.ai session | Reads `sessionKey` from Chrome's cookie store |
| A **Claude.ai account** (Free, Pro, Max — any tier) | For usage data to display |

> **No extension, no API key, no desktop Claude app.** Just Chrome + an active claude.ai login.

If you're not logged into claude.ai in Chrome, the widget shows `⚠ No claude.ai session — please log in via Chrome`.

## Install

### Easy way: download the prebuilt `.exe`

1. Grab the latest `ClaudeUsageWidget.exe` from the [Releases page](https://github.com/turkbil/claude-usage-widget-windows/releases) (or the most recent successful run on the [Actions tab](https://github.com/turkbil/claude-usage-widget-windows/actions))
2. Move it somewhere persistent (e.g. `C:\Tools\ClaudeUsageWidget\`)
3. Double-click. It appears in the system tray.
4. Right-click the tray icon → **Run at startup** to launch automatically on login

> **First-run note (Windows 11):** New tray icons get hidden behind the **^** overflow arrow by default. To pin it next to the clock: right-click taskbar → *Taskbar settings* → *Other system tray icons* → toggle ClaudeUsageWidget to **On**.

### Build it yourself

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

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

## How it works

```
┌──────────────────┐    DPAPI + AES-256-GCM     ┌─────────────────┐
│  Chrome cookies  │ ──────────────────────────▶│  sessionKey     │
│  (encrypted)     │   master key in Local State│  (decrypted)    │
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
                                    │  Tray icon UI   │
                                    │  refreshes 60s  │
                                    └─────────────────┘
```

The widget never sees your password. The same encrypted-cookie + DPAPI trick that Chrome itself uses — every Windows app does this for stored cookies on the user's own machine.

## Privacy

- **No telemetry.** Only two outbound HTTPS calls per refresh, both to `claude.ai`.
- **No third-party data.** Cookie is read locally, used only against claude.ai.
- **Cookie never written to disk.** Memory-only.
- **Settings stored under:** `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ClaudeUsageWidget` (auto-start flag only).

## Troubleshooting

| Symptom | Fix |
|---|---|
| Tray shows `?` with "No claude.ai session" | Open Chrome and log into claude.ai |
| `HTTP 401` | Your claude.ai session expired. Re-login via Chrome. |
| Can't see the icon | Windows 11 hides new tray icons behind the **^** overflow. Pin it via Taskbar settings → *Other system tray icons*. |
| Stale percentage | Right-click tray → **Refresh now** |
| Doesn't start at login | Right-click tray → toggle **Run at startup**, or check `HKCU\…\Run` |

## License

[MIT](LICENSE) © [Nurullah Okatan](https://www.nurullah.net)

Not affiliated with Anthropic. "Claude" is a trademark of Anthropic.
