using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ClaudeUsageWidget;

/// Single settings window. Vertical-scroll only, fixed width. All preferences
/// visible at once — no nested menus. Mirrors the macOS SettingsWindow layout.
public sealed class SettingsForm : Form
{
    private static SettingsForm? _instance;

    public static void ShowOnce()
    {
        if (_instance == null || _instance.IsDisposed)
            _instance = new SettingsForm();
        _instance.Show();
        _instance.BringToFront();
        _instance.Activate();
    }

    private readonly FlowLayoutPanel _stack;
    private readonly List<TitleRow> _titleRows = new();
    private readonly List<Button> _emojiButtons = new();
    private TextBox? _customEmoji;
    private RadioButton? _radCustom, _radDonut, _radNone;
    private readonly List<RadioButton> _refreshRadios = new();
    private CheckBox? _notifEnable;
    private TrackBar? _warnSlider, _alertSlider, _criticalSlider;
    private Label? _warnLabel, _alertLabel, _criticalLabel;
    private CheckBox? _hotkeyEnable;
    private Label? _hotkeyCurrent;
    private readonly List<CheckBox> _browserChecks = new();
    private CheckBox? _versionCheck;
    private CheckBox? _localApi;

    public SettingsForm()
    {
        Text = L.Get("settings.title");
        Width = 620;
        Height = 720;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        _stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(28, 22, 28, 24),
        };
        Controls.Add(_stack);

        AddTitleContentSection();
        AddIconSection();
        AddRefreshSection();
        AddNotificationsSection();
        AddHotkeySection();
        AddBrowsersSection();
        AddNetworkSection();

        PrefsStore.Changed += RefreshAll;
        RefreshAll();

        FormClosing += (_, _) => PrefsStore.Changed -= RefreshAll;
    }

    private void AddHeader(string text)
    {
        var lbl = new Label
        {
            Text = text.ToUpperInvariant(),
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            ForeColor = Color.Gray,
            AutoSize = false,
            Width = Width - 80,
            Height = 18,
            Margin = new Padding(0, 18, 0, 4),
        };
        _stack.Controls.Add(lbl);
        _stack.Controls.Add(new Panel { Width = Width - 80, Height = 1, BackColor = Color.FromArgb(200, 200, 200), Margin = new Padding(0, 0, 0, 12) });
    }

    // §01 Title content
    private void AddTitleContentSection()
    {
        AddHeader(L.Get("menu.title_content"));
        var metrics = new (string key, string label)[]
        {
            ("WeeklyPctMode",    L.Get("metric.weekly_pct")),
            ("WeeklyTimeMode",   L.Get("metric.weekly_time")),
            ("FiveHourPctMode",  L.Get("metric.five_pct")),
            ("FiveHourTimeMode", L.Get("metric.five_time")),
        };
        foreach (var (key, label) in metrics)
        {
            var row = new TitleRow(key, label, contentWidth: Width - 80);
            _titleRows.Add(row);
            _stack.Controls.Add(row);
        }
    }

    // §02 Icon
    private void AddIconSection()
    {
        AddHeader(L.Get("menu.icon"));

        var grid = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
        foreach (var e in new[] { "🤖", "🧠", "⚡", "✨", "◉", "●", "▲", "◐" })
        {
            var b = new Button
            {
                Text = e,
                Font = new Font("Segoe UI Emoji", 14),
                Size = new Size(44, 38),
                FlatStyle = FlatStyle.Flat,
                Tag = e,
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
            b.Click += (_, _) =>
                PrefsStore.Update(p => { p.IconType = IconType.Emoji; p.IconValue = (string)b.Tag!; });
            _emojiButtons.Add(b);
            grid.Controls.Add(b);
        }
        _stack.Controls.Add(grid);

        var customRow = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
        _radCustom = new RadioButton { Text = L.Get("icon.custom"), AutoSize = true, Margin = new Padding(0, 6, 8, 0) };
        _radCustom.Click += (_, _) =>
        {
            if (_radCustom.Checked)
                PrefsStore.Update(p => { p.IconType = IconType.Custom; if (!string.IsNullOrEmpty(_customEmoji?.Text)) p.IconValue = _customEmoji!.Text; });
        };
        _customEmoji = new TextBox { Width = 70, Font = new Font("Segoe UI Emoji", 12), PlaceholderText = "🪐" };
        _customEmoji.Click += (_, _) => OpenEmojiPicker();
        _customEmoji.TextChanged += (_, _) =>
        {
            var v = _customEmoji.Text.Trim();
            if (!string.IsNullOrEmpty(v))
                PrefsStore.Update(p => { p.IconType = IconType.Custom; p.IconValue = v; });
        };
        customRow.Controls.Add(_radCustom);
        customRow.Controls.Add(_customEmoji);
        _stack.Controls.Add(customRow);

        _radDonut = new RadioButton { Text = L.Get("icon.donut"), AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
        _radDonut.Click += (_, _) => { if (_radDonut.Checked) PrefsStore.Update(p => { p.IconType = IconType.Donut; p.IconValue = ""; }); };
        _stack.Controls.Add(_radDonut);

        _radNone = new RadioButton { Text = L.Get("icon.none"), AutoSize = true };
        _radNone.Click += (_, _) => { if (_radNone.Checked) PrefsStore.Update(p => { p.IconType = IconType.None; p.IconValue = ""; }); };
        _stack.Controls.Add(_radNone);
    }

    private static void OpenEmojiPicker()
    {
        // Windows 10+ shortcut: Win + .
        SendKeys.Send("^{ESC}");        // close any open menus first
        SendKeys.Send("({WIN})({.})");  // not perfect but works for many setups
        // Best-effort; user can also press Win+. manually.
    }

    // §06 Refresh
    private void AddRefreshSection()
    {
        AddHeader(L.Get("menu.refresh_interval"));
        var row = new FlowLayoutPanel { AutoSize = true };
        foreach (var (sec, label) in new (int, string)[]
                 {
                     (30,  L.Get("refresh.30s")),
                     (60,  L.Get("refresh.1m")),
                     (300, L.Get("refresh.5m")),
                     (600, L.Get("refresh.10m")),
                 })
        {
            var r = new RadioButton { Text = label, AutoSize = true, Margin = new Padding(0, 0, 16, 0), Tag = sec };
            r.Click += (_, _) => { if (r.Checked) PrefsStore.Update(p => p.PollIntervalSec = sec); };
            _refreshRadios.Add(r);
            row.Controls.Add(r);
        }
        _stack.Controls.Add(row);
    }

    // §05 Notifications
    private void AddNotificationsSection()
    {
        AddHeader(L.Get("menu.notifications"));
        _notifEnable = new CheckBox { Text = L.Get("notifications.enable"), AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
        _notifEnable.CheckedChanged += (_, _) => PrefsStore.Update(p => p.NotificationsEnabled = _notifEnable.Checked);
        _stack.Controls.Add(_notifEnable);

        (Panel row, TrackBar slider, Label valueLabel) MakeSlider(string name, Color dotColor, int min, int max, Action<int> save)
        {
            var panel = new Panel { Width = Width - 80, Height = 32 };

            var dot = new Label { Text = "●", ForeColor = dotColor, AutoSize = true, Top = 8, Left = 0 };
            var label = new Label { Text = name, AutoSize = false, Top = 8, Left = 18, Width = 80, Height = 16 };
            var slider = new TrackBar { Minimum = min, Maximum = max, TickStyle = TickStyle.None, Top = 0, Left = 104, Width = 320 };
            var valueLabel = new Label { Text = $"%50", AutoSize = false, Top = 8, Left = 432, Width = 40, Height = 16, Font = new Font("Cascadia Mono", 10) };

            slider.ValueChanged += (_, _) =>
            {
                valueLabel.Text = $"%{slider.Value}";
                save(slider.Value);
            };

            panel.Controls.Add(dot);
            panel.Controls.Add(label);
            panel.Controls.Add(slider);
            panel.Controls.Add(valueLabel);
            return (panel, slider, valueLabel);
        }

        var warn     = MakeSlider(L.Get("notifications.warn"),     Color.Gold,    20, 80, v => PrefsStore.Update(p => p.WarnThreshold     = v));
        var alert    = MakeSlider(L.Get("notifications.alert"),    Color.Orange,  50, 95, v => PrefsStore.Update(p => p.AlertThreshold    = v));
        var critical = MakeSlider(L.Get("notifications.critical"), Color.IndianRed, 70, 99, v => PrefsStore.Update(p => p.CriticalThreshold = v));
        _warnSlider = warn.slider;     _warnLabel     = warn.valueLabel;
        _alertSlider = alert.slider;   _alertLabel    = alert.valueLabel;
        _criticalSlider = critical.slider; _criticalLabel = critical.valueLabel;

        _stack.Controls.Add(warn.row);
        _stack.Controls.Add(alert.row);
        _stack.Controls.Add(critical.row);
    }

    // §07 Hotkey
    private void AddHotkeySection()
    {
        AddHeader(L.Get("menu.hotkey"));
        _hotkeyEnable = new CheckBox { Text = L.Get("hotkey.enable"), AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
        _hotkeyEnable.CheckedChanged += (_, _) => PrefsStore.Update(p => p.HotkeyEnabled = _hotkeyEnable.Checked);
        _stack.Controls.Add(_hotkeyEnable);

        _hotkeyCurrent = new Label { AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 9) };
        _stack.Controls.Add(_hotkeyCurrent);
    }

    // §08 Browsers
    private void AddBrowsersSection()
    {
        AddHeader(L.Get("menu.browsers"));
        var grid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true };
        foreach (var (id, label) in new (string, string)[]
                 {
                     ("chrome", "🟢 " + L.Get("browser.chrome")),
                     ("brave",  "🦁 " + L.Get("browser.brave")),
                     ("edge",   "🟦 " + L.Get("browser.edge")),
                     ("arc",    "🏹 " + L.Get("browser.arc")),
                 })
        {
            var cb = new CheckBox { Text = label, AutoSize = true, Tag = id, Margin = new Padding(0, 0, 24, 8) };
            cb.CheckedChanged += (_, _) =>
            {
                var on = cb.Checked;
                PrefsStore.Update(p =>
                {
                    switch ((string)cb.Tag!)
                    {
                        case "chrome": p.BrowserChromeEnabled = on; break;
                        case "brave":  p.BrowserBraveEnabled  = on; break;
                        case "edge":   p.BrowserEdgeEnabled   = on; break;
                        case "arc":    p.BrowserArcEnabled    = on; break;
                    }
                });
            };
            _browserChecks.Add(cb);
            grid.Controls.Add(cb);
        }
        _stack.Controls.Add(grid);
    }

    // §09 Network
    private void AddNetworkSection()
    {
        AddHeader(L.Get("menu.network"));

        _versionCheck = new CheckBox { Text = L.Get("menu.version_check"), AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
        _versionCheck.CheckedChanged += (_, _) => PrefsStore.Update(p => p.VersionCheckEnabled = _versionCheck.Checked);
        _stack.Controls.Add(_versionCheck);

        _localApi = new CheckBox { Text = L.Get("menu.local_http"), AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
        _localApi.CheckedChanged += (_, _) => PrefsStore.Update(p => p.LocalApiEnabled = _localApi.Checked);
        _stack.Controls.Add(_localApi);

        var mcpBtn = new Button { Text = L.Get("menu.mcp_install"), AutoSize = true };
        mcpBtn.Click += (_, _) => ShowMCPInstall();
        _stack.Controls.Add(mcpBtn);
    }

    private void ShowMCPInstall()
    {
        var exe = Process.GetCurrentProcess().MainModule?.FileName ?? "ClaudeUsageWidget.exe";
        var json = $@"{{
  ""mcpServers"": {{
    ""claude-usage"": {{
      ""command"": ""{exe.Replace("\\", "\\\\")}"",
      ""args"": [""--mcp-server""]
    }}
  }}
}}";
        var dialog = new Form
        {
            Text = L.Get("mcp.install_title"),
            Width = 600,
            Height = 380,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
        };
        var info = new Label
        {
            Text = L.Get("mcp.install_intro"),
            AutoSize = false, Top = 12, Left = 16, Width = 560, Height = 60,
        };
        var code = new TextBox
        {
            Multiline = true, ReadOnly = true, Text = json,
            Font = new Font("Cascadia Mono", 10),
            ScrollBars = ScrollBars.Vertical,
            Top = 80, Left = 16, Width = 560, Height = 200,
        };
        var copyJson = new Button { Text = L.Get("mcp.copy_json"), Top = 290, Left = 16, AutoSize = true };
        copyJson.Click += (_, _) => Clipboard.SetText(json);
        var copyPath = new Button { Text = L.Get("button.copy_path"), Top = 290, Left = 160, AutoSize = true };
        copyPath.Click += (_, _) => Clipboard.SetText(exe);
        var ok = new Button { Text = L.Get("button.ok"), Top = 290, Left = 500, AutoSize = true };
        ok.Click += (_, _) => dialog.Close();
        dialog.Controls.Add(info);
        dialog.Controls.Add(code);
        dialog.Controls.Add(copyJson);
        dialog.Controls.Add(copyPath);
        dialog.Controls.Add(ok);
        dialog.ShowDialog(this);
    }

    private void RefreshAll()
    {
        if (InvokeRequired) { BeginInvoke(new Action(RefreshAll)); return; }

        var p = PrefsStore.Current;
        foreach (var row in _titleRows) row.Refresh();

        foreach (var b in _emojiButtons)
        {
            var e = (string)b.Tag!;
            b.FlatAppearance.BorderColor = (p.IconType == IconType.Emoji && p.IconValue == e)
                ? Color.FromArgb(214, 140, 69) : Color.FromArgb(180, 180, 180);
            b.FlatAppearance.BorderSize = (p.IconType == IconType.Emoji && p.IconValue == e) ? 2 : 1;
        }
        if (_radCustom != null) _radCustom.Checked = p.IconType == IconType.Custom;
        if (_radDonut  != null) _radDonut.Checked  = p.IconType == IconType.Donut;
        if (_radNone   != null) _radNone.Checked   = p.IconType == IconType.None;
        if (_customEmoji != null && p.IconType == IconType.Custom) _customEmoji.Text = p.IconValue;

        foreach (var r in _refreshRadios) r.Checked = (int)r.Tag! == p.PollIntervalSec;

        if (_notifEnable != null) _notifEnable.Checked = p.NotificationsEnabled;
        if (_warnSlider != null)     { _warnSlider.Value     = p.WarnThreshold;     _warnLabel!.Text     = $"%{p.WarnThreshold}"; }
        if (_alertSlider != null)    { _alertSlider.Value    = p.AlertThreshold;    _alertLabel!.Text    = $"%{p.AlertThreshold}"; }
        if (_criticalSlider != null) { _criticalSlider.Value = p.CriticalThreshold; _criticalLabel!.Text = $"%{p.CriticalThreshold}"; }

        if (_hotkeyEnable != null) _hotkeyEnable.Checked = p.HotkeyEnabled;
        if (_hotkeyCurrent != null)
            _hotkeyCurrent.Text = string.Format(L.Get("hotkey.current"),
                HotKeyManager.Label(p.HotkeyVirtKey, p.HotkeyModifiers));

        foreach (var cb in _browserChecks)
        {
            cb.Checked = (string)cb.Tag! switch
            {
                "chrome" => p.BrowserChromeEnabled,
                "brave"  => p.BrowserBraveEnabled,
                "edge"   => p.BrowserEdgeEnabled,
                "arc"    => p.BrowserArcEnabled,
                _ => false,
            };
        }

        if (_versionCheck != null) _versionCheck.Checked = p.VersionCheckEnabled;
        if (_localApi != null)     _localApi.Checked     = p.LocalApiEnabled;
    }
}

// MARK: TitleRow — one row per metric

internal sealed class TitleRow : Panel
{
    public string MetricKey { get; }
    private readonly ComboBox _modeCombo;
    private readonly Panel _colorRow;
    private readonly List<Button> _colorButtons = new();

    public TitleRow(string metricKey, string label, int contentWidth)
    {
        MetricKey = metricKey;
        Width = contentWidth;
        Height = 64;
        Margin = new Padding(0, 4, 0, 8);

        var lbl = new Label { Text = label, AutoSize = false, Top = 6, Left = 0, Width = 200, Height = 22 };
        _modeCombo = new ComboBox
        {
            Top = 4, Left = 210, Width = 140,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _modeCombo.Items.AddRange(new object[] { L.Get("mode.hidden"), L.Get("mode.text"), L.Get("mode.donut") });
        _modeCombo.SelectedIndexChanged += (_, _) =>
        {
            var mode = (MetricMode)_modeCombo.SelectedIndex;
            PrefsStore.Update(p => SetMode(p, mode));
        };

        _colorRow = new Panel { Top = 34, Left = 210, Width = 280, Height = 24 };
        var palette = new[] { "#d68c45", "#5dc97f", "#d4c25a", "#d6645a", "#a87fd6", "#7fb8b8", "#f4eee3", "#8a8378" };
        int x = 0;
        foreach (var hex in palette)
        {
            var b = new Button
            {
                Size = new Size(22, 22),
                FlatStyle = FlatStyle.Flat,
                BackColor = TitleRenderer.ParseHex(hex),
                Left = x, Top = 0, Tag = hex,
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(40, 40, 40);
            b.FlatAppearance.BorderSize = 1;
            b.Click += (_, _) =>
            {
                var color = (string)b.Tag!;
                PrefsStore.Update(p => SetColor(p, color));
            };
            _colorButtons.Add(b);
            _colorRow.Controls.Add(b);
            x += 30;
        }

        Controls.Add(lbl);
        Controls.Add(_modeCombo);
        Controls.Add(_colorRow);
        Refresh();
    }

    public new void Refresh()
    {
        var p = PrefsStore.Current;
        var mode = GetMode(p);
        _modeCombo.SelectedIndex = (int)mode;
        _colorRow.Visible = (mode == MetricMode.Donut);

        var currentColor = GetColor(p);
        foreach (var b in _colorButtons)
        {
            var hex = (string)b.Tag!;
            b.FlatAppearance.BorderColor = (hex == currentColor) ? Color.White : Color.FromArgb(40, 40, 40);
            b.FlatAppearance.BorderSize = (hex == currentColor) ? 2 : 1;
        }
    }

    private MetricMode GetMode(Preferences p) => MetricKey switch
    {
        "WeeklyPctMode"    => p.WeeklyPctMode,
        "WeeklyTimeMode"   => p.WeeklyTimeMode,
        "FiveHourPctMode"  => p.FiveHourPctMode,
        "FiveHourTimeMode" => p.FiveHourTimeMode,
        _ => MetricMode.Hidden,
    };
    private void SetMode(Preferences p, MetricMode m)
    {
        switch (MetricKey)
        {
            case "WeeklyPctMode":    p.WeeklyPctMode    = m; break;
            case "WeeklyTimeMode":   p.WeeklyTimeMode   = m; break;
            case "FiveHourPctMode":  p.FiveHourPctMode  = m; break;
            case "FiveHourTimeMode": p.FiveHourTimeMode = m; break;
        }
    }
    private string GetColor(Preferences p) => MetricKey switch
    {
        "WeeklyPctMode"    => p.WeeklyPctColor,
        "WeeklyTimeMode"   => p.WeeklyTimeColor,
        "FiveHourPctMode"  => p.FiveHourPctColor,
        "FiveHourTimeMode" => p.FiveHourTimeColor,
        _ => "#d68c45",
    };
    private void SetColor(Preferences p, string hex)
    {
        switch (MetricKey)
        {
            case "WeeklyPctMode":    p.WeeklyPctColor    = hex; break;
            case "WeeklyTimeMode":   p.WeeklyTimeColor   = hex; break;
            case "FiveHourPctMode":  p.FiveHourPctColor  = hex; break;
            case "FiveHourTimeMode": p.FiveHourTimeColor = hex; break;
        }
    }
}
