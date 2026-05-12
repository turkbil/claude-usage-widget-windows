using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClaudeUsageWidget;

/// Borderless, always-on-top popup window shown when the tray icon is clicked.
/// Layout mirrors the macOS dropdown: account header, weekly section,
/// 5-hour section, footer. Hides on focus loss.
public sealed class UsagePopup : Form
{
    private const int PopupWidth = 320;
    private const int HPad = 18;

    private readonly Label nameLabel;
    private readonly PillLabel planPill;
    private readonly SectionHeader weeklyHeader;
    private readonly UsageRow allModelsRow;
    private readonly UsageRow sonnetRow;
    private readonly SectionHeader fiveHourHeader;
    private readonly UsageRow fiveHourRow;
    private readonly Label footerLabel;

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

        nameLabel = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI Semibold", 11),
            ForeColor = Color.White,
            Left = HPad,
            Top = 14,
            Width = PopupWidth - HPad * 2 - 96,
            Height = 18,
        };
        Controls.Add(nameLabel);

        planPill = new PillLabel { Top = 12 };
        Controls.Add(planPill);

        weeklyHeader = new SectionHeader { Top = 44, Width = PopupWidth };
        Controls.Add(weeklyHeader);

        allModelsRow = new UsageRow { Top = 78, Width = PopupWidth };
        Controls.Add(allModelsRow);

        sonnetRow = new UsageRow { Top = 124, Width = PopupWidth };
        Controls.Add(sonnetRow);

        fiveHourHeader = new SectionHeader { Top = 180, Width = PopupWidth };
        Controls.Add(fiveHourHeader);

        fiveHourRow = new UsageRow { Top = 214, Width = PopupWidth };
        Controls.Add(fiveHourRow);

        footerLabel = new Label
        {
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(150, 150, 155),
            Left = HPad,
            Top = 274,
            Width = PopupWidth - HPad * 2,
            Height = 18,
        };
        Controls.Add(footerLabel);

        Height = 304;
    }

    private Region MakeRoundedRegion()
    {
        const int r = 12;
        using var path = new GraphicsPath();
        var rect = new Rectangle(0, 0, PopupWidth, 304);
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
            nameLabel.Text = snap.DisplayName ?? "";
            planPill.SetText(snap.PlanLabel ?? "");
            planPill.Visible = !string.IsNullOrEmpty(snap.PlanLabel);
            if (planPill.Visible)
                planPill.Left = PopupWidth - HPad - planPill.Width;

            weeklyHeader.SetTitle(L.Get("section.weekly"),
                L.Fmt("remaining.suffix", FormatRemaining(snap.WeeklyResetsAt)));

            allModelsRow.SetData(L.Get("label.all_models"), snap.WeeklyUtilization);
            allModelsRow.Visible = true;

            if (snap.SonnetUtilization.HasValue)
            {
                sonnetRow.SetData(L.Get("label.sonnet"), snap.SonnetUtilization.Value);
                sonnetRow.Visible = true;
            }
            else sonnetRow.Visible = false;

            if (snap.FiveHourUtilization.HasValue && snap.FiveHourResetsAt.HasValue)
            {
                fiveHourHeader.SetTitle(L.Get("section.five_hour"),
                    L.Fmt("remaining.suffix", FormatRemaining(snap.FiveHourResetsAt.Value)));
                fiveHourRow.SetData(L.Get("label.usage"), snap.FiveHourUtilization.Value);
                fiveHourHeader.Visible = fiveHourRow.Visible = true;
            }
            else fiveHourHeader.Visible = fiveHourRow.Visible = false;
        }

        if (!string.IsNullOrEmpty(error))
        {
            footerLabel.Text = "⚠ " + error;
            footerLabel.ForeColor = Color.FromArgb(235, 90, 90);
        }
        else if (snap is not null)
        {
            footerLabel.Text = L.Fmt("footer.updated", snap.FetchedAt.ToLocalTime().ToString("t"));
            footerLabel.ForeColor = Color.FromArgb(150, 150, 155);
        }
    }

    public static string FormatRemaining(DateTime until)
    {
        var total = until - DateTime.UtcNow;
        if (total.TotalSeconds <= 0) return L.Get("time.reset");
        int d = (int)total.TotalDays;
        int h = total.Hours;
        int m = total.Minutes;
        if (d > 0) return L.Fmt("time.days_hours", d, h);
        if (h > 0) return L.Fmt("time.hours_minutes", h, m);
        return L.Fmt("time.minutes", m);
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

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        Hide();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) Hide();
        base.OnKeyDown(e);
    }
}

/// Small rounded-rect "Max 20x" badge.
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
    private readonly Label titleLabel = new()
    {
        Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
        ForeColor = Color.FromArgb(160, 160, 165),
        AutoSize = false,
        Left = 18,
        Top = 8,
        Height = 14,
        Width = 180,
    };
    private readonly Label trailingLabel = new()
    {
        Font = new Font("Cascadia Mono", 9),
        ForeColor = Color.FromArgb(140, 140, 145),
        TextAlign = ContentAlignment.MiddleRight,
        AutoSize = false,
        Top = 8,
        Height = 14,
        Width = 140,
    };

    public SectionHeader()
    {
        Height = 26;
        Controls.Add(titleLabel);
        Controls.Add(trailingLabel);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        trailingLabel.Left = Width - 18 - trailingLabel.Width;
    }

    public void SetTitle(string title, string trailing)
    {
        titleLabel.Text = title;
        trailingLabel.Text = trailing;
    }
}

internal sealed class UsageRow : Control
{
    private readonly Label nameLabel = new()
    {
        Font = new Font("Segoe UI", 10),
        ForeColor = Color.White,
        AutoSize = false,
        Left = 18,
        Top = 8,
        Height = 18,
        Width = 160,
    };
    private readonly Label valueLabel = new()
    {
        Font = new Font("Cascadia Mono", 9, FontStyle.Bold),
        ForeColor = Color.FromArgb(180, 180, 185),
        TextAlign = ContentAlignment.MiddleRight,
        AutoSize = false,
        Top = 8,
        Height = 18,
        Width = 64,
    };
    private readonly ProgressBarControl bar = new()
    {
        Left = 18,
        Top = 30,
        Height = 8,
    };

    public UsageRow()
    {
        Height = 44;
        Controls.Add(nameLabel);
        Controls.Add(valueLabel);
        Controls.Add(bar);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        valueLabel.Left = Width - 18 - valueLabel.Width;
        bar.Width = Width - 36;
    }

    public void SetData(string label, double percent)
    {
        int rounded = (int)Math.Round(percent);
        nameLabel.Text = label;
        valueLabel.Text = L.Fmt("value.percent", rounded);
        var color = IconRenderer.ColorForPercent(rounded);
        bar.FillColor = color;
        bar.Progress = percent / 100.0;
        valueLabel.ForeColor = color;
    }
}
