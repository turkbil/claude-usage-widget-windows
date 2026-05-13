using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeUsageWidget;

public enum MetricMode { Hidden, Text, Donut }
public enum IconType { Emoji, Custom, Donut, None }

/// Mirrors the macOS Preferences struct. JSON-backed, lives in
/// %LOCALAPPDATA%\ClaudeUsageWidget\prefs.v1.json. Mutations go through
/// PrefsStore.Update so observers (timer, hotkey, HTTP server, etc.) see
/// changes as soon as they're persisted.
public sealed class Preferences
{
    // §01 Title content — per-metric display modes
    public MetricMode WeeklyPctMode    { get; set; } = MetricMode.Text;
    public MetricMode WeeklyTimeMode   { get; set; } = MetricMode.Text;
    public MetricMode FiveHourPctMode  { get; set; } = MetricMode.Hidden;
    public MetricMode FiveHourTimeMode { get; set; } = MetricMode.Hidden;

    // §01 Donut colors (hex)
    public string WeeklyPctColor    { get; set; } = "#d68c45"; // ember
    public string WeeklyTimeColor   { get; set; } = "#d68c45";
    public string FiveHourPctColor  { get; set; } = "#5dc97f"; // grass
    public string FiveHourTimeColor { get; set; } = "#5dc97f";

    // §02 Icon
    public IconType IconType  { get; set; } = IconType.Emoji;
    public string   IconValue { get; set; } = "🤖";

    // §06 Refresh
    public int PollIntervalSec { get; set; } = 60;

    // §05 Notifications
    public bool   NotificationsEnabled  { get; set; } = false;
    public int    WarnThreshold         { get; set; } = 50;
    public int    AlertThreshold        { get; set; } = 75;
    public int    CriticalThreshold     { get; set; } = 90;
    public string LastNotifiedLevel     { get; set; } = "";  // "", "warn", "alert", "critical"

    // §07 Global hotkey (Win32 modifiers: MOD_ALT=1, MOD_CONTROL=2, MOD_SHIFT=4, MOD_WIN=8)
    public bool   HotkeyEnabled   { get; set; } = false;
    public uint   HotkeyVirtKey   { get; set; } = 0x55;   // 'U'
    public uint   HotkeyModifiers { get; set; } = 1 | 2;  // ALT+CTRL

    // §08 Browsers (Chromium family)
    public bool BrowserChromeEnabled { get; set; } = true;
    public bool BrowserBraveEnabled  { get; set; } = false;
    public bool BrowserEdgeEnabled   { get; set; } = false;
    public bool BrowserArcEnabled    { get; set; } = false;

    // §09 Version check
    public bool   VersionCheckEnabled  { get; set; } = true;
    public string LatestKnownVersion   { get; set; } = "";
    public long   LastVersionCheckUnix { get; set; } = 0;

    // §09 Local HTTP endpoint
    public bool LocalApiEnabled { get; set; } = false;
}

/// Thread-safe, single-instance preferences store.
public static class PrefsStore
{
    private static readonly object _lock = new();
    private static Preferences _prefs = new();
    private static readonly string _path;
    public static event Action? Changed;

    static PrefsStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClaudeUsageWidget");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "prefs.v1.json");
        Load();
    }

    public static Preferences Current
    {
        get { lock (_lock) { return _prefs; } }
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static void Load()
    {
        if (!File.Exists(_path)) return;
        try
        {
            var json = File.ReadAllText(_path);
            var p = JsonSerializer.Deserialize<Preferences>(json, JsonOpts);
            if (p != null) _prefs = p;
        }
        catch { /* keep defaults on read error */ }
    }

    private static void Save()
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(_prefs, JsonOpts));
        }
        catch { /* ignore — disk full etc. */ }
    }

    /// Mutate prefs, persist, and notify observers.
    public static void Update(Action<Preferences> mutator)
    {
        lock (_lock)
        {
            mutator(_prefs);
            Save();
        }
        Changed?.Invoke();
    }
}
