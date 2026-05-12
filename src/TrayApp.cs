using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClaudeUsageWidget;

/// Top-level app context: tray icon, context menu, refresh timer, popup.
public sealed class TrayApp : ApplicationContext
{
    private const int PollIntervalMs = 60_000;

    private readonly NotifyIcon tray;
    private readonly System.Windows.Forms.Timer pollTimer;
    private readonly UsagePopup popup = new();
    private UsageSnapshot? lastSnapshot;
    private string? lastError;
    private readonly ToolStripMenuItem runAtStartupItem;

    public TrayApp()
    {
        var menu = new ContextMenuStrip();

        var refreshItem = new ToolStripMenuItem(L.Get("menu.refresh"));
        refreshItem.Click += async (_, _) => await RefreshAsync();
        menu.Items.Add(refreshItem);

        var openUsageItem = new ToolStripMenuItem(L.Get("menu.open_usage"));
        openUsageItem.Click += (_, _) => OpenUrl("https://claude.ai/settings/usage");
        menu.Items.Add(openUsageItem);

        runAtStartupItem = new ToolStripMenuItem(L.Get("menu.run_at_startup"))
        {
            Checked = AutoStart.IsEnabled,
            CheckOnClick = true,
        };
        runAtStartupItem.Click += (_, _) =>
        {
            AutoStart.Toggle();
            runAtStartupItem.Checked = AutoStart.IsEnabled;
        };
        menu.Items.Add(runAtStartupItem);

        menu.Items.Add(new ToolStripSeparator());

        var quitItem = new ToolStripMenuItem(L.Get("menu.quit"));
        quitItem.Click += (_, _) => ExitThread();
        menu.Items.Add(quitItem);

        tray = new NotifyIcon
        {
            Icon = IconRenderer.RenderPercent(0, isError: true),
            Visible = true,
            ContextMenuStrip = menu,
            Text = "Claude Usage Widget",
        };
        tray.MouseUp += Tray_MouseUp;

        pollTimer = new System.Windows.Forms.Timer { Interval = PollIntervalMs };
        pollTimer.Tick += async (_, _) => await RefreshAsync();
        pollTimer.Start();

        // Refresh title every 30s so the countdown advances without a full API call.
        var titleTimer = new System.Windows.Forms.Timer { Interval = 30_000 };
        titleTimer.Tick += (_, _) => UpdateTrayVisual();
        titleTimer.Start();

        _ = RefreshAsync();
    }

    private void Tray_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        if (popup.Visible) { popup.Hide(); return; }

        popup.Apply(lastSnapshot, lastError);
        var cursor = Cursor.Position;
        popup.ShowNear(cursor);
    }

    private async Task RefreshAsync()
    {
        try
        {
            var snap = await ClaudeApi.FetchSnapshotAsync();
            lastSnapshot = snap;
            lastError = null;
        }
        catch (Exception ex)
        {
            lastError = ex.Message;
        }
        UpdateTrayVisual();
        if (popup.Visible) popup.Apply(lastSnapshot, lastError);
    }

    private void UpdateTrayVisual()
    {
        var oldIcon = tray.Icon;
        if (lastSnapshot is not null && lastError is null)
        {
            int pct = (int)Math.Round(lastSnapshot.WeeklyUtilization);
            tray.Icon = IconRenderer.RenderPercent(pct);
            string remaining = UsagePopup.FormatRemaining(lastSnapshot.WeeklyResetsAt);
            tray.Text = TruncateForTooltip(L.Fmt("tooltip", pct, remaining));
        }
        else
        {
            tray.Icon = IconRenderer.RenderPercent(0, isError: true);
            tray.Text = TruncateForTooltip(lastError ?? "Claude Usage Widget");
        }
        oldIcon?.Dispose();
    }

    // NotifyIcon.Text is capped at 127 characters. Trim defensively.
    private static string TruncateForTooltip(string s) =>
        s.Length > 120 ? s[..120] + "…" : s;

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            pollTimer.Stop();
            pollTimer.Dispose();
            tray.Visible = false;
            tray.Dispose();
            popup.Dispose();
        }
        base.Dispose(disposing);
    }
}
