using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClaudeUsageWidget;

/// Top-level tray app: NotifyIcon, refresh timer, popup, hotkey, HTTP server,
/// version checker, settings window dispatch.
public sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly Timer _pollTimer;
    private readonly UsagePopup _popup = new();
    private readonly LocalHTTPServer _http = new();
    private readonly HotKeyManager _hotkey = new();
    private UsageSnapshot? _lastSnapshot;
    private string? _lastError;

    private ToolStripMenuItem _versionItem = null!;

    public TrayApp()
    {
        var menu = BuildMenu();

        _tray = new NotifyIcon
        {
            Icon = IconRenderer.Render(null, PrefsStore.Current, error: true),
            Visible = true,
            ContextMenuStrip = menu,
            Text = "Claude Usage Widget",
        };
        _tray.MouseUp += Tray_MouseUp;

        _pollTimer = new Timer { Interval = PrefsStore.Current.PollIntervalSec * 1000 };
        _pollTimer.Tick += async (_, _) => await RefreshAsync();
        _pollTimer.Start();

        // Tick the icon every 30s so the countdown advances between refreshes.
        var titleTimer = new Timer { Interval = 30_000 };
        titleTimer.Tick += (_, _) => UpdateTrayVisual();
        titleTimer.Start();

        PrefsStore.Changed += OnPrefsChanged;
        _hotkey.Apply(() => Tray_MouseUp(this, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)));
        _http.Apply(PrefsStore.Current.LocalApiEnabled);

        _ = RefreshAsync();
        _ = VersionChecker.CheckIfDueAsync();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        var refreshItem = new ToolStripMenuItem(L.Get("menu.refresh"));
        refreshItem.Click += async (_, _) => await RefreshAsync();
        menu.Items.Add(refreshItem);

        var openUsageItem = new ToolStripMenuItem(L.Get("menu.open_usage"));
        openUsageItem.Click += (_, _) => OpenUrl("https://claude.ai/settings/usage");
        menu.Items.Add(openUsageItem);

        var settingsItem = new ToolStripMenuItem(L.Get("menu.settings"));
        settingsItem.Click += (_, _) => SettingsForm.ShowOnce();
        menu.Items.Add(settingsItem);

        var autoStartItem = new ToolStripMenuItem(L.Get("menu.run_at_startup"))
        {
            Checked = AutoStart.IsEnabled,
            CheckOnClick = true,
        };
        autoStartItem.Click += (_, _) =>
        {
            AutoStart.Toggle();
            autoStartItem.Checked = AutoStart.IsEnabled;
        };
        menu.Items.Add(autoStartItem);

        menu.Items.Add(new ToolStripSeparator());

        _versionItem = new ToolStripMenuItem(L.Get("menu.version_new")) { Visible = false };
        _versionItem.Click += (_, _) => OpenUrl(VersionChecker.ReleasesUrl);
        menu.Items.Add(_versionItem);

        var quitItem = new ToolStripMenuItem(L.Get("menu.quit"));
        quitItem.Click += (_, _) => ExitThread();
        menu.Items.Add(quitItem);

        menu.Items.Add(new ToolStripSeparator());

        var siteItem = new ToolStripMenuItem("nurullah.net ↗")
            { ForeColor = Color.Gray, Font = new Font("Segoe UI", 8) };
        siteItem.Click += (_, _) => OpenUrl("https://www.nurullah.net");
        menu.Items.Add(siteItem);

        var xItem = new ToolStripMenuItem("@nurullah ↗")
            { ForeColor = Color.Gray, Font = new Font("Segoe UI", 8) };
        xItem.Click += (_, _) => OpenUrl("https://x.com/nurullah");
        menu.Items.Add(xItem);

        return menu;
    }

    private void Tray_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        if (_popup.Visible) { _popup.Hide(); return; }
        _popup.Apply(_lastSnapshot, _lastError);
        _popup.ShowNear(Cursor.Position);
    }

    private void OnPrefsChanged()
    {
        // PrefsStore.Update can fire from any thread. Marshal back to the UI
        // thread via the popup form (which always has a window handle).
        if (_popup.IsHandleCreated && _popup.InvokeRequired)
        {
            try { _popup.BeginInvoke(new Action(OnPrefsChanged)); } catch { }
            return;
        }
        _pollTimer.Interval = Math.Max(15, PrefsStore.Current.PollIntervalSec) * 1000;
        _hotkey.Apply(() => Tray_MouseUp(this, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)));
        _http.Apply(PrefsStore.Current.LocalApiEnabled);
        UpdateTrayVisual();
        if (_popup.Visible) _popup.Apply(_lastSnapshot, _lastError);
        RefreshVersionBadge();
    }

    private async Task RefreshAsync()
    {
        try
        {
            var snap = await ClaudeApi.FetchSnapshotAsync();
            _lastSnapshot = snap;
            _lastError = null;
            UsageHistory.Record(snap.WeeklyUtilization);
            _http.SetSnapshot(snap);
        }
        catch (Exception ex) { _lastError = ex.Message; }

        UpdateTrayVisual();
        if (_popup.Visible) _popup.Apply(_lastSnapshot, _lastError);
        if (_lastSnapshot is not null)
            NotificationManager.Evaluate(_lastSnapshot, _tray);
        await VersionChecker.CheckIfDueAsync();
        RefreshVersionBadge();
    }

    private void UpdateTrayVisual()
    {
        var oldIcon = _tray.Icon;
        _tray.Icon = IconRenderer.Render(_lastSnapshot, PrefsStore.Current,
                                         error: _lastSnapshot == null && _lastError != null);
        _tray.Text = TruncateForTooltip(BuildTooltip());
        oldIcon?.Dispose();
    }

    private string BuildTooltip()
    {
        if (_lastSnapshot is null) return _lastError ?? "Claude Usage Widget";
        int pct = (int)Math.Round(_lastSnapshot.WeeklyUtilization);
        var remaining = Formatting.FormatRemaining(_lastSnapshot.WeeklyResetsAt);
        return string.Format(L.Get("tooltip"), pct, remaining);
    }

    private void RefreshVersionBadge()
    {
        if (VersionChecker.UpdateAvailable)
        {
            _versionItem.Text = string.Format(L.Get("menu.version_new"),
                PrefsStore.Current.LatestKnownVersion);
            _versionItem.Visible = true;
        }
        else _versionItem.Visible = false;
    }

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
            PrefsStore.Changed -= OnPrefsChanged;
            _hotkey.Dispose();
            _http.Dispose();
            _pollTimer.Stop();
            _pollTimer.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
            _popup.Dispose();
        }
        base.Dispose(disposing);
    }
}
