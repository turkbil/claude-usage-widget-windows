using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ClaudeUsageWidget;

/// Persistent circular buffer of weekly-utilization samples. Every refresh
/// adds one sample. The sparkline view reads from this to draw the trend.
public static class UsageHistory
{
    public sealed record Sample(double T, double V);

    private static readonly object _lock = new();
    private static List<Sample> _samples = new();
    private static readonly string _path;
    private const int MaxSamples = 4032;                // 14 days at 5-min granularity
    private const double MinSpacingSec = 5 * 60;

    static UsageHistory()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClaudeUsageWidget");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "history.json");
        Load();
    }

    public static IReadOnlyList<Sample> All
    {
        get { lock (_lock) { return _samples.ToList(); } }
    }

    /// Samples within the last `seconds` window.
    public static IReadOnlyList<Sample> Recent(double seconds)
    {
        var cutoff = UnixNow() - seconds;
        lock (_lock)
        {
            return _samples.Where(s => s.T >= cutoff).ToList();
        }
    }

    /// Append a sample if at least MinSpacingSec has passed since the last one.
    public static void Record(double weeklyPct)
    {
        var now = UnixNow();
        lock (_lock)
        {
            if (_samples.Count > 0 && now - _samples[^1].T < MinSpacingSec) return;
            _samples.Add(new Sample(now, weeklyPct));
            if (_samples.Count > MaxSamples)
                _samples.RemoveRange(0, _samples.Count - MaxSamples);
            Save();
        }
    }

    private static double UnixNow() =>
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

    private static void Load()
    {
        if (!File.Exists(_path)) return;
        try
        {
            var json = File.ReadAllText(_path);
            var arr = JsonSerializer.Deserialize<List<Sample>>(json);
            if (arr != null) _samples = arr;
        }
        catch { /* keep empty */ }
    }

    private static void Save()
    {
        try { File.WriteAllText(_path, JsonSerializer.Serialize(_samples)); }
        catch { /* ignore */ }
    }
}
