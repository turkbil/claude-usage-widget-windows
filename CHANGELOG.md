# Changelog

All notable changes to **Claude Usage Widget (Windows)** are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versions follow [SemVer](https://semver.org/spec/v2.0.0.html).

## [1.1.0] — 2026-05-13

Full feature parity with the macOS sibling at v1.4.x.

### Added
- **Per-metric title display** — Weekly %, Weekly remaining, 5-hour %,
  5-hour remaining: each can be hidden, shown as text, or drawn as a
  tiny colored donut ring directly in the tray icon.
- **Burn-rate forecast** in the dropdown — "↗ Projected end-of-week: 64 %"
  or, when pace exceeds 100 %, "⚠ At this pace, limit in ~1d 8h".
- **Sparkline trend** spanning weekStart (0 %) → samples → now → weekEnd
  (projected, dashed). Useful from day one without waiting for data.
- **Settings window** — vertical-scroll WinForms form with all preferences
  visible at once: title content, icon picker, refresh interval,
  notifications, hotkey, browsers, network & integration.
- **Multi-browser cookie source** — Chrome, Brave, Edge, Arc. Tries each
  enabled browser in order; first valid session wins.
- **Threshold notifications** — three configurable levels fire balloon
  tips when crossed. Edge-triggered (one fire per week per level).
- **Global hotkey** — Win32 RegisterHotKey wrapper. Default Ctrl+Alt+U
  opens the dropdown from anywhere.
- **MCP server mode** (`--mcp-server`) — stdio JSON-RPC. Adds the binary
  to `~/.claude.json` mcpServers so Claude Code can call `get_usage`.
- **Local HTTP endpoint** (`127.0.0.1:9123`) — `GET /usage` returns JSON.
  Toggle in Settings.
- **CLI mode** (`--print-usage`) — emits current usage as JSON for shell
  scripts and statuslines.
- **Daily version check** against GitHub Releases.
- **Credit footer** in the popup with links to nurullah.net and @nurullah.
- **Localization fallback** — keys missing from a language now fall back
  to English instead of showing the raw key.

### Changed
- `ChromeCookieReader` replaced by `BrowserCookieReader` (multi-browser
  iterator + cleaner separation of concerns).
- `TrayApp.OnPrefsChanged` marshals to the UI thread via the popup form
  (NotifyIcon has no InvokeRequired).

### Dropped (intentional)
- Sparkle auto-update — same reasoning as macOS sibling.
- Sentry crash reporting — Windows native crash dumps in
  `%LOCALAPPDATA%\CrashDumps\` are the fallback.
- Authenticode EV signing — too expensive for an indie tool. Users may
  see a one-time SmartScreen warning; "More info → Run anyway" once.

## [1.0.0] — 2026-05-12

### Added
- Initial release. Native Windows system-tray widget showing Claude
  weekly utilization % and time until reset, mirroring the macOS app.
- Auto-localized: English, Türkçe, Deutsch, Español, Français.
- Reads claude.ai session cookie from Chrome via Windows DPAPI.

[1.1.0]: https://github.com/turkbil/claude-usage-widget-windows/releases/tag/v1.1.0
[1.0.0]: https://github.com/turkbil/claude-usage-widget-windows/releases/tag/v1.0.0
