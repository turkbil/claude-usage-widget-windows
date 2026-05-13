using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace ClaudeUsageWidget;

/// Three-level threshold notifications (warn / alert / critical). Uses the
/// existing tray NotifyIcon's BalloonTip — the most reliable cross-version
/// Windows notification surface that doesn't need package signing or COM
/// activator registration (which Windows Toast notifications do).
///
/// Edge-triggered: each level fires at most once until the percentage drops
/// below the warn threshold (e.g. after the weekly reset).
public static class NotificationManager
{
    public static void Evaluate(UsageSnapshot snap, NotifyIcon tray)
    {
        var prefs = PrefsStore.Current;
        if (!prefs.NotificationsEnabled) return;

        var pct = (int)Math.Round(snap.WeeklyUtilization);

        // Drop below warn → reset state, ready to fire next cycle.
        if (pct < prefs.WarnThreshold && !string.IsNullOrEmpty(prefs.LastNotifiedLevel))
        {
            PrefsStore.Update(p => p.LastNotifiedLevel = "");
            return;
        }

        var crossed = Level(pct, prefs);
        if (crossed == null || !IsHigher(crossed, prefs.LastNotifiedLevel)) return;

        Post(crossed, pct, tray);
        PrefsStore.Update(p => p.LastNotifiedLevel = crossed);
    }

    private static string? Level(int pct, Preferences p) =>
        pct >= p.CriticalThreshold ? "critical" :
        pct >= p.AlertThreshold    ? "alert"    :
        pct >= p.WarnThreshold     ? "warn"     : null;

    private static int Rank(string level) => level switch
    { "critical" => 3, "alert" => 2, "warn" => 1, _ => 0 };

    private static bool IsHigher(string a, string b) => Rank(a) > Rank(b);

    private static void Post(string level, int pct, NotifyIcon tray)
    {
        string body = level switch
        {
            "warn"     => string.Format(L.Get("notification.warn"), pct),
            "alert"    => string.Format(L.Get("notification.alert"), pct),
            "critical" => string.Format(L.Get("notification.critical"), pct),
            _          => $"{pct}%"
        };
        var icon = level == "critical" ? ToolTipIcon.Warning : ToolTipIcon.Info;
        tray.ShowBalloonTip(5000, "Claude Usage Widget", body, icon);
    }
}
