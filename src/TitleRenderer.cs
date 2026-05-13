using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ClaudeUsageWidget;

/// Windows tray icons are 16x16 raster images — there's no native way to
/// embed inline donut glyphs alongside text the way macOS NSAttributedString
/// does. So we render the entire title (icon + segments) into a single
/// 32x32 bitmap that Windows will display as the tray icon.
public static class TitleRenderer
{
    public sealed record Segment(string Kind, string? Text, double? FillPct, Color Color);
    //   Kind: "icon", "text", "donut"

    public static List<Segment> ComposeSegments(UsageSnapshot? snap, Preferences p, bool error = false)
    {
        var list = new List<Segment>();
        if (snap == null)
        {
            list.Add(new Segment("text", error ? L.Get("title.error") : L.Get("title.loading"), null, Color.White));
            return list;
        }

        // Prefix icon
        switch (p.IconType)
        {
            case IconType.Emoji or IconType.Custom:
                var v = (p.IconValue ?? "").Trim();
                if (!string.IsNullOrEmpty(v))
                    list.Add(new Segment("text", v, null, Color.White));
                break;
            case IconType.Donut:
                list.Add(new Segment("donut", null, snap.WeeklyUtilization / 100.0, ParseHex(p.WeeklyPctColor)));
                break;
            case IconType.None:
                break;
        }

        // Per-metric items
        var weeklyTimeFill = ElapsedFraction(snap.WeeklyResetsAt, 7 * 24 * 3600);
        var fiveTimeFill   = snap.FiveHourResetsAt is DateTime ft ? ElapsedFraction(ft, 5 * 3600) : 0;

        Add(p.WeeklyPctMode,    Pct(snap.WeeklyUtilization),                     snap.WeeklyUtilization / 100.0, ParseHex(p.WeeklyPctColor));
        Add(p.WeeklyTimeMode,   Formatting.FormatRemaining(snap.WeeklyResetsAt), weeklyTimeFill, ParseHex(p.WeeklyTimeColor));
        if (snap.FiveHourUtilization is double fu)
            Add(p.FiveHourPctMode, Pct(fu), fu / 100.0, ParseHex(p.FiveHourPctColor));
        if (snap.FiveHourResetsAt is DateTime fr)
            Add(p.FiveHourTimeMode, Formatting.FormatRemaining(fr), fiveTimeFill, ParseHex(p.FiveHourTimeColor));

        return list;

        void Add(MetricMode mode, string text, double fill, Color color)
        {
            if (mode == MetricMode.Hidden) return;
            list.Add(mode == MetricMode.Donut
                ? new Segment("donut", null, fill, color)
                : new Segment("text",  text, null, ColorForPercent(text)));
        }
    }

    private static double ElapsedFraction(DateTime resetsAt, double periodSec)
    {
        var remaining = (resetsAt - DateTime.UtcNow).TotalSeconds;
        var elapsed   = periodSec - remaining;
        return Math.Max(0, Math.Min(1, elapsed / periodSec));
    }

    /// Locale-aware percentage formatter (Turkish "%32" vs English "32%").
    private static string Pct(double v)
    {
        var raw = string.Format(L.Get("title.percent"), (int)Math.Round(v));
        return raw.Replace("🤖 ", "").Replace("🤖", "").Trim();
    }

    /// Text segments get colored by threshold of the value they encode
    /// (when parseable).
    private static Color ColorForPercent(string text)
    {
        var digits = new string(text.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out var p))
        {
            if (p >= 90) return Color.FromArgb(255, 235, 90, 90);
            if (p >= 75) return Color.FromArgb(255, 245, 165, 60);
        }
        return Color.White;
    }

    public static Color ParseHex(string hex)
    {
        var s = hex.Trim();
        if (s.StartsWith("#")) s = s.Substring(1);
        if (s.Length != 6) return Color.FromArgb(214, 140, 69);
        try
        {
            int r = Convert.ToInt32(s.Substring(0, 2), 16);
            int g = Convert.ToInt32(s.Substring(2, 2), 16);
            int b = Convert.ToInt32(s.Substring(4, 2), 16);
            return Color.FromArgb(r, g, b);
        }
        catch { return Color.FromArgb(214, 140, 69); }
    }
}

internal static class Formatting
{
    public static string FormatRemaining(DateTime until)
    {
        var total = Math.Max(0, (int)(until - DateTime.UtcNow).TotalSeconds);
        if (total <= 0) return L.Get("time.reset");
        int d = total / 86400;
        int h = (total % 86400) / 3600;
        int m = (total % 3600) / 60;
        if (d > 0) return string.Format(L.Get("time.days_hours"), d, h);
        if (h > 0) return string.Format(L.Get("time.hours_minutes"), h, m);
        return string.Format(L.Get("time.minutes"), m);
    }
}
