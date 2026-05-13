using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClaudeUsageWidget;

/// Borderless, always-on-top popup shown when the tray icon is clicked.
/// Account header → weekly rows → forecast line → sparkline → 5h rows →
/// footer → credit links.
public sealed class UsagePopup : Form
{
    private const int PopupWidth = 320;
    private const int HPad = 18;

    private readonly Label _nameLabel;
    private readonly PillLabel _planPill;
    private readonly SectionHeader _weeklyHeader;
    private readonly UsageRow _allModelsRow;
    private readonly UsageRow _sonnetRow;
    private readonly Label _forecastLabel;
    private readonly SparklineControl _sparkline;
    private readonly SectionHeader _fiveHourHeader;
    private readonly UsageRow _fiveHourRow;
    private readonly Label _footerLabel;
    private readonly LinkLabel _siteLink;
    private readonly LinkLabel _xLink;

    public UsagePopup()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        Width = PopupWidth;
        DoubleBuffered = true;
        BackColor = Color.FromArgb(28, 28, 30);
        ForeColor = Color.White;
        Padding = new Padding(0, 8, 0, 8);
        KeyPreview = true;
        Region = MakeRoundedRegion();

        _nameLabel = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI Semibold", 11),
            ForeColor = Color.White,
            Left = HPad, Top = 14, Width = PopupWidth - HPad * 2 - 96, Height = 18,
        };
        Controls.Add(_nameLabel);

        _planPill = new PillLabel { Top = 12 };
        Controls.Add(_planPill);

        _weeklyHeader = new SectionHeader { Top = 44, Width = PopupWidth };
        Controls.Add(_weeklyHeader);

        _allModelsRow = new UsageRow { Top = 78, Width = PopupWidth };
        Controls.Add(_allModelsRow);

        _sonnetRow = new UsageRow { Top = 124, Width = PopupWidth };
        Controls.Add(_sonnetRow);

        _forecastLabel = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(180, 180, 185),
            TextAlign = ContentAlignment.MiddleLeft,
            Left = HPad, Top = 170, Width = PopupWidth - HPad * 2, Height = 18,
            Visible = false,
        };
        Controls.Add(_forecastLabel);

        _sparkline = new SparklineControl
        {
            Left = 0, Top = 194, Width = PopupWidth, Height = 44,
        };
        Controls.Add(_sparkline);

        _fiveHourHeader = new SectionHeader { Top = 250, Width = PopupWidth };
        Controls.Add(_fiveHourHeader);

        _fiveHourRow = new UsageRow { Top = 284, Width = PopupWidth };
        Controls.Add(_fiveHourRow);

        _footerLabel = new Label
        {
            AutoSize = false, TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(150, 150, 155),
            Left = HPad, Top = 344, Width = PopupWidth - HPad * 2, Height = 18,
        };
        Controls.Add(_footerLabel);

        _siteLink = new LinkLabel
        {
            Text = "nurullah.net ↗", AutoSize = true,
            Font = new Font("Segoe UI", 8),
            LinkColor = Color.FromArgb(170, 170, 175),
            ActiveLinkColor = Color.FromArgb(214, 140, 69),
            VisitedLinkColor = Color.FromArgb(170, 170, 175),
            Top = 370, Left = HPad,
        };
        _siteLink.Click += (_, _) => OpenUrl("https://www.nurullah.net");
        Controls.Add(_siteLink);

        _xLink = new LinkLabel
        {
            Text = "@nurullah ↗", AutoSize = true,
            Font = new Font("Segoe UI", 8),
            LinkColor = Color.FromArgb(170, 170, 175),
            ActiveLinkColor = Color.FromArgb(214, 140, 69),
            VisitedLinkColor = Color.FromArgb(170, 170, 175),
            Top = 370, Left = PopupWidth - HPad - 80,
        };
        _xLink.Click += (_, _) => OpenUrl("https://x.com/nurullah");
        Controls.Add(_xLink);

        Height = 400;
        Region = MakeRoundedRegion();
    }

    private Region MakeRoundedRegion()
    {
        const int r = 12;
        using var path = new GraphicsPath();
        var rect = new Rectangle(0, 0, PopupWidth, Height);
        path.AddArc(rect.X, rect.Y, r, r, 180, 90);
        path.AddArc(rect.Right - r - 1, rect.Y, r, r, 270, 90);
        path.AddArc(rect.Right - r - 1, rect.Bottom - r - 1, r, r, 0, 90);
        path.AddArc(rect.X, rect.Bottom - r - 1, r, r, 90, 90);
        path.CloseFigure();
        return new Region(path);
    }

    public void Apply(UsageSnapshot? snap, string? error)
    {
        if (snap is not null)
        {
            _nameLabel.Text = snap.DisplayName ?? "";
            _planPill.SetText(snap.PlanLabel ?? "");
            _planPill.Visible = !string.IsNullOrEmpty(snap.PlanLabel);
            if (_planPill.Visible)
                _planPill.Left = PopupWidth - HPad - _planPill.Width;

            _weeklyHeader.SetTitle(L.Get("section.weekly"),
                string.Format(L.Get("remaining.suffix"), Formatting.FormatRemaining(snap.WeeklyResetsAt)));

            _allModelsRow.SetData(L.Get("label.all_models"), snap.WeeklyUtilization);
            _allModelsRow.Visible = true;

            if (snap.SonnetUtilization is double sonnet)
            {
                _sonnetRow.SetData(L.Get("label.sonnet"), sonnet);
                _sonnetRow.Visible = true;
            }
            else _sonnetRow.Visible = false;

            var forecastLine = Forecast.Line(snap);
            _forecastLabel.Text = forecastLine ?? "";
            _forecastLabel.Visible = forecastLine != null;
            _forecastLabel.ForeColor = (Forecast.Compute(snap) is Forecast.WillHitLimit)
                ? Color.FromArgb(255, 235, 165, 60)
                : Color.FromArgb(180, 180, 185);

            _sparkline.Snapshot = snap;
            _sparkline.Samples = UsageHistory.Recent(7 * 24 * 3600);

            if (snap.FiveHourUtilization is double fu && snap.FiveHourResetsAt is DateTime fr)
            {
                _fiveHourHeader.SetTitle(L.Get("section.five_hour"),
                    string.Format(L.Get("remaining.suffix"), Formatting.FormatRemaining(fr)));
                _fiveHourRow.SetData(L.Get("label.usage"), fu);
                _fiveHourHeader.Visible = _fiveHourRow.Visible = true;
            }
            else _fiveHourHeader.Visible = _fiveHourRow.Visible = false;
        }

        if (!string.IsNullOrEmpty(error))
        {
            _footerLabel.Text = "⚠ " + error;
            _footerLabel.ForeColor = Color.FromArgb(235, 90, 90);
        }
        else if (snap is not null)
        {
            _footerLabel.Text = string.Format(L.Get("footer.updated"),
                snap.FetchedAt.ToLocalTime().ToString("t"));
            _footerLabel.ForeColor = Color.FromArgb(150, 150, 155);
        }
    }

    public void ShowNear(Point anchorPoint)
    {
        var screen = Screen.FromPoint(anchorPoint).WorkingArea;
        int x = anchorPoint.X - Width / 2;
        int y = anchorPoint.Y - Height - 12;
        x = Math.Min(Math.Max(x, screen.Left + 8), screen.Right - Width - 8);
        y = Math.Max(y, screen.Top + 8);
        Location = new Point(x, y);
        Show();
        Activate();
    }

    protected override void OnDeactivate(EventArgs e) { base.OnDeactivate(e); Hide(); }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) Hide();
        base.OnKeyDown(e);
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }
}

internal sealed class PillLabel : Control
{
    public PillLabel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw, true);
        Font = new Font("Cascadia Mono", 8.5f, FontStyle.Bold);
        ForeColor = Color.White;
        Height = 20;
    }

    public void SetText(string text)
    {
        Text = text;
        if (string.IsNullOrEmpty(text)) { Width = 0; return; }
        using var g = CreateGraphics();
        var size = g.MeasureString(text, Font);
        Width = (int)Math.Ceiling(size.Width) + 16;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = new GraphicsPath();
        int radius = 6;
        path.AddArc(r.X, r.Y, radius, radius, 180, 90);
        path.AddArc(r.Right - radius, r.Y, radius, radius, 270, 90);
        path.AddArc(r.Right - radius, r.Bottom - radius, radius, radius, 0, 90);
        path.AddArc(r.X, r.Bottom - radius, radius, radius, 90, 90);
        path.CloseFigure();
        using var bg = new SolidBrush(Color.FromArgb(60, 200, 200, 200));
        g.FillPath(bg, path);

        var textSize = g.MeasureString(Text, Font);
        var tx = (Width - textSize.Width) / 2f;
        var ty = (Height - textSize.Height) / 2f;
        using var brush = new SolidBrush(ForeColor);
        g.DrawString(Text, Font, brush, tx, ty);
    }
}

internal sealed class SectionHeader : Control
{
    private readonly Label _title = new()
    {
        Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
        ForeColor = Color.FromArgb(160, 160, 165),
        AutoSize = false, Left = 18, Top = 8, Height = 14, Width = 180,
    };
    private readonly Label _trailing = new()
    {
        Font = new Font("Cascadia Mono", 9),
        ForeColor = Color.FromArgb(140, 140, 145),
        TextAlign = ContentAlignment.MiddleRight,
        AutoSize = false, Top = 8, Height = 14, Width = 140,
    };

    public SectionHeader()
    {
        Height = 26;
        Controls.Add(_title);
        Controls.Add(_trailing);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        _trailing.Left = Width - 18 - _trailing.Width;
    }

    public void SetTitle(string title, string trailing)
    {
        _title.Text = title;
        _trailing.Text = trailing;
    }
}

internal sealed class UsageRow : Control
{
    private readonly Label _name = new()
    {
        Font = new Font("Segoe UI", 10), ForeColor = Color.White,
        AutoSize = false, Left = 18, Top = 8, Height = 18, Width = 160,
    };
    private readonly Label _value = new()
    {
        Font = new Font("Cascadia Mono", 9, FontStyle.Bold),
        ForeColor = Color.FromArgb(180, 180, 185),
        TextAlign = ContentAlignment.MiddleRight,
        AutoSize = false, Top = 8, Height = 18, Width = 64,
    };
    private readonly ProgressBarControl _bar = new() { Left = 18, Top = 30, Height = 8 };

    public UsageRow()
    {
        Height = 44;
        Controls.Add(_name); Controls.Add(_value); Controls.Add(_bar);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        _value.Left = Width - 18 - _value.Width;
        _bar.Width = Width - 36;
    }

    public void SetData(string label, double percent)
    {
        int rounded = (int)Math.Round(percent);
        _name.Text = label;
        _value.Text = string.Format(L.Get("value.percent"), rounded);
        var color = IconRenderer.ColorForPercent(rounded);
        _bar.FillColor = color;
        _bar.Progress = percent / 100.0;
        _value.ForeColor = color;
    }
}
