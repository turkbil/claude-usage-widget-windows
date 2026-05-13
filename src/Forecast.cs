using System;

namespace ClaudeUsageWidget;

/// Burn-rate forecast for the weekly window. Given the current utilization
/// and how much of the 7-day window has elapsed, predict either:
///   - the projected utilization at week end (if pace stays the same), or
///   - the moment at which the limit will be hit (if pace already exceeds it).
public static class Forecast
{
    private const double WeeklyPeriodSec = 7 * 24 * 3600;

    public abstract record Result;
    public sealed record UnderBudget(int EndPct)         : Result;
    public sealed record WillHitLimit(TimeSpan TimeLeft) : Result;
    public sealed record OverLimit()                     : Result;

    /// Returns null when the forecast is too noisy to be useful
    /// (first hour of the week, last 5% of the week, or near-zero usage).
    public static Result? Compute(UsageSnapshot snap)
    {
        var remaining = (snap.WeeklyResetsAt - DateTime.UtcNow).TotalSeconds;
        var elapsedSec = WeeklyPeriodSec - remaining;
        var elapsedFrac = elapsedSec / WeeklyPeriodSec;

        if (elapsedFrac <= 0.05 || elapsedFrac >= 0.97) return null;
        if (snap.WeeklyUtilization <= 0.5)              return null;

        var pct = snap.WeeklyUtilization;
        if (pct >= 100) return new OverLimit();

        var projected = pct / elapsedFrac;
        if (projected < 100)
            return new UnderBudget((int)Math.Round(projected));

        var rate = pct / elapsedSec;                 // % per second
        var secondsToLimit = (100 - pct) / rate;
        return new WillHitLimit(TimeSpan.FromSeconds(secondsToLimit));
    }

    /// Localized human-friendly line, or null if no useful forecast.
    public static string? Line(UsageSnapshot snap)
    {
        var r = Compute(snap);
        return r switch
        {
            UnderBudget u  => string.Format(L.Get("forecast.under"), u.EndPct),
            WillHitLimit w => string.Format(L.Get("forecast.over"), FormatDuration(w.TimeLeft)),
            OverLimit      => L.Get("forecast.exceeded"),
            _              => null
        };
    }

    private static string FormatDuration(TimeSpan t)
    {
        int total = Math.Max(0, (int)t.TotalSeconds);
        int d = total / 86400;
        int h = (total % 86400) / 3600;
        int m = (total % 3600) / 60;
        if (d > 0) return string.Format(L.Get("time.days_hours"), d, h);
        if (h > 0) return string.Format(L.Get("time.hours_minutes"), h, m);
        return string.Format(L.Get("time.minutes"), m);
    }
}
