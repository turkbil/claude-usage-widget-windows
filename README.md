# Claude Usage Widget — Windows

[![Latest release](https://img.shields.io/github/v/release/turkbil/claude-usage-widget-windows?label=download&logo=github&color=d68c45)](https://github.com/turkbil/claude-usage-widget-windows/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-d68c45.svg)](LICENSE)
[![Windows 10/11](https://img.shields.io/badge/Windows-10%20%7C%2011-d68c45?logo=windows)](#requirements)

**Native Windows system-tray widget** — same data source, same dropdown, same Settings window, same features as the macOS sibling.

> 💻 **This is the Windows version.** On Mac? → [**claude-usage-widget**](https://github.com/turkbil/claude-usage-widget)
>
> 🌐 [**Türkçe README**](README.tr.md)  ·  Free, open source (MIT), `.exe` on the [Releases page](https://github.com/turkbil/claude-usage-widget-windows/releases/latest)

```
…  [^]  [🟢32]  [🔊]  [📶]  [12:46]            ← lives next to your clock

┌──────────────────────────────────────────┐
│  Nurullah                    [Max 20x]   │
│  ────────────────────────────────────    │
│  WEEKLY                  3d 18h left     │
│   All models   ████████░░░░░░░░░  32%    │
│   Sonnet       █░░░░░░░░░░░░░░░░   2%    │
│   ↗ Projected end-of-week: 64%           │
│   ╭───────●─ ─ ─ ─ ─ ─ ─◌╮               │
│  ────────────────────────────────────    │
│  5-HOUR WINDOW           2h 38m left     │
│   Usage        ██░░░░░░░░░░░░░░░░  7%    │
│  ────────────────────────────────────    │
│           Updated: 12:46                 │
│   nurullah.net ↗    @nurullah ↗          │
└──────────────────────────────────────────┘
```

---

## What makes this widget different

- 🍩 **Per-metric display** — weekly %, weekly remaining, 5-hour %, 5-hour remaining: each can be hidden, shown as text, or drawn as a tiny colored donut ring directly in the tray icon.
- 🔮 **Burn-rate forecast** — "↗ Projected end-of-week: 64 %", or when the pace exceeds 100 %: "⚠ At this pace, limit in ~1d 8h".
- 📈 **Sparkline trend** — past samples + projected future on a 7-day timeline. Useful from day one (no waiting for data).
- 🤝 **MCP server built-in** — Claude Code itself can read your weekly limit via the bundled `get_usage` tool.
- 🌐 **Local HTTP & CLI modes** — for Raycast/AutoHotKey/PowerShell automations.
- 🦊 **Multi-browser failover** — Chrome / Brave / Edge / Arc. Toggle the ones you use; first valid session wins.
- 🔔 **Edge-triggered notifications** — three configurable levels, each fires once per week (no spam).
- 🎚 **Real Settings window** — every preference visible at once.
- 🪙 **No third-party services** — only outbound traffic is claude.ai (data) and api.github.com (daily release check, can be disabled).

---

## Quick install

1. Download `ClaudeUsageWidget.exe` from the [**Releases page**](https://github.com/turkbil/claude-usage-widget-windows/releases/latest)
2. Move it somewhere persistent (e.g. `C:\Tools\ClaudeUsageWidget\`)
3. Double-click. It appears in the system tray.

> **Windows 11 first-run:** new tray icons get tucked under the **^** overflow arrow by default. To pin it next to the clock: right-click the taskbar → *Taskbar settings* → *Other system tray icons* → toggle **ClaudeUsageWidget** to **On**.

> **Windows SmartScreen warning:** since the binary isn't EV-signed (paid Authenticode certificates are expensive for an indie tool), Windows may show "Windows protected your PC". Click **More info → Run anyway** once. From then on it launches silently.

### Auto-start

Right-click the tray icon → **Run at startup**. Stores a single value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

### Uninstall

Right-click tray → **Quit**, then delete:
- the `.exe` itself
- `%LOCALAPPDATA%\ClaudeUsageWidget\` (settings + history)
- the auto-start registry key (auto-removed via *Run at startup* toggle)

---

## Requirements

| Component | Why |
|---|---|
| **Windows 10 / 11 (x64)** | Native WinForms tray app |
| **A Chromium-based browser** with an active claude.ai session | Reads `sessionKey` from the browser's cookie store. Chrome, Brave, Edge, Arc are all supported — toggle them in Settings → Browsers. |
| **Claude.ai account** (any plan) | To have usage data to display |

> **No extension, no API key, no desktop Claude app.** Just a Chromium browser + active claude.ai login.

---

## Settings overview

Right-click the tray icon → **Settings…**

| Section | What's there |
|---|---|
| **Title content** | For each of Weekly %, Weekly remaining, 5-hour %, 5-hour remaining: hide / show as text / show as a donut. When donut, pick from 8 swatch colors. |
| **Icon** | Emoji preset, custom emoji (click the box → Win+. opens emoji picker), donut summary, or no icon. |
| **Refresh interval** | 30 s · 1 min · 5 min · 10 min |
| **Notifications** | Toggle threshold alerts. Three configurable levels fire balloon tips when you cross warn / alert / critical. Each level fires once per week. |
| **Hotkey** | Global hotkey toggle. Default Ctrl+Alt+U opens the dropdown from anywhere. |
| **Browsers** | Chrome / Brave / Edge / Arc — toggle which to read from. First valid session wins. |
| **Network & integration** | Daily update check · Local HTTP endpoint :9123 · MCP install instructions |

---

## Integration

### MCP — Claude Code reads its own limit
Settings → "MCP install instructions…" gives you a JSON snippet for `~/.claude.json`:

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

Restart Claude Code → Claude can call `get_usage` to see your weekly limit.

### Local HTTP — for Raycast / scripts
Enable in Settings, then:
```powershell
PS> Invoke-RestMethod http://localhost:9123/usage
weekly_utilization_pct : 32
weekly_resets_at       : 2026-05-16T05:00:00Z
plan                   : Max 20x
…
```
Only listens on 127.0.0.1. Never exposed externally.

### CLI — one-shot JSON
```powershell
PS> ClaudeUsageWidget.exe --print-usage
{
  "weekly_utilization_pct": 32,
  "plan": "Max 20x",
  …
}
```

---

## Build from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
git clone https://github.com/turkbil/claude-usage-widget-windows.git
cd claude-usage-widget-windows
dotnet publish src\ClaudeUsageWidget.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true
.\src\bin\Release\net8.0-windows\win-x64\publish\ClaudeUsageWidget.exe
```

CI does the same on every tag push and publishes the resulting `.exe` to GitHub Releases.

---

## How it works

```
┌──────────────────┐  DPAPI + AES-256-GCM     ┌─────────────────┐
│  Browser cookies │ ────────────────────────▶│  sessionKey     │
│  (encrypted)     │  master key from         │  (decrypted)    │
└──────────────────┘  Local State + DPAPI     └────────┬────────┘
                                                       │
                                       Cookie: sessionKey=…
                                                       ▼
                          ┌─────────────────────────────────────┐
                          │ GET claude.ai/api/organizations/{id}/│
                          │     usage (seven_day.* + five_hour.*) │
                          │ GET claude.ai/api/account     (name) │
                          │ GET claude.ai/api/.../rate_limits    │
                          └──────────────┬──────────────────────┘
                                         ▼
                           ┌──────────────────────────────┐
                           │  Tray icon · refresh every   │
                           │  N seconds (configurable)    │
                           │  + history sample / 5 min    │
                           │  + threshold notifications   │
                           │  + sparkline trend           │
                           └──────────────────────────────┘
```

The widget never sees your password. It uses the same DPAPI + encrypted-cookie trick the browser itself uses for stored cookies on your own machine.

---

## Privacy

- **No telemetry.** Only outbound traffic is HTTPS to claude.ai (data) and once a day to api.github.com (release check, can be disabled).
- **Cookie never written to disk** — memory only.
- **Persisted files** in `%LOCALAPPDATA%\ClaudeUsageWidget\`:
  - `prefs.v1.json` — settings
  - `history.json` — 14-day sparkline samples
- **Settings** include only the toggle/value preferences — no PII.

---

## Languages

Auto-detects Windows preferred language and falls back to English.

| | |
|---|---|
| 🇬🇧 | English (default) |
| 🇹🇷 | Türkçe |
| 🇩🇪 | Deutsch |
| 🇪🇸 | Español |
| 🇫🇷 | Français |

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| Tray shows `?` with "No claude.ai session" | Open your browser, log into claude.ai. Make sure the browser is enabled in Settings → Browsers. |
| `HTTP 401` | Session expired. Re-login via your browser. |
| Can't see the icon | Windows 11 hides new tray icons behind the **^** overflow. Pin it via Taskbar settings → *Other system tray icons*. |
| SmartScreen warns | The binary is unsigned (EV cert is expensive). Click **More info → Run anyway** once. |
| Stale percentage | Right-click tray → **Refresh now** |

---

## Author

Built by **Nurullah Okatan** — [nurullah.net](https://www.nurullah.net) · [@nurullah](https://x.com/nurullah)

## License

[MIT](LICENSE) © Nurullah Okatan

Not affiliated with Anthropic. "Claude" is a trademark of Anthropic.
