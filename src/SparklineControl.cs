using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ClaudeUsageWidget;

/// Sparkline that combines real past samples + projected future on a
/// full-week timeline. Even with zero samples the line spans weekStart →
/// now → weekEnd (projected, dashed).
public sealed class SparklineControl : Control
{
    private UsageSnapshot? _snapshot;
    private IReadOnlyList<UsageHistory.Sample> _samples = Array.Empty<UsageHistory.Sample>();

    public UsageSnapshot? Snapshot
    {
        get => _snapshot;
        set { _snapshot = value; Invalidate(); }
    }

    public IReadOnlyList<UsageHistory.Sample> Samples
    {
        get => _samples;
        set { _samples = value ?? Array.Empty<UsageHistory.Sample>(); Invalidate(); }
    }

    public SparklineControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint |
                 ControlStyles.SupportsTransparentBackColor |
                 ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Height = 44;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new Rectangle(18, 6, Width - 36, Height - 12);
        if (r.Width <= 10 || r.Height <= 10) return;

        if (_snapshot == null)
        {
            DrawEmptyHint(g, r);
            return;
        }

        double weekEndT   = ((DateTimeOffset)_snapshot.WeeklyResetsAt).ToUnixTimeMilliseconds() / 1000.0;
        double weekStartT = weekEndT - 7 * 24 * 3600;
        double nowT       = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        double tRange = Math.Max(1, weekEndT - weekStartT);

        PointF Pt(double t, double v)
        {
            var xFrac = Math.Max(0, Math.Min(1, (t - weekStartT) / tRange));
            var yFrac = Math.Max(0, Math.Min(1, v / 100));
            return new PointF(
                r.Left + (float)(r.Width * xFrac),
                r.Top + (float)(r.Height * (1 - yFrac)));
        }

        // Top baseline rule (the 100% mark).
        using (var penTop = new Pen(Color.FromArgb(50, 160, 160, 160), 0.5f))
            g.DrawLine(penTop, r.Left, r.Top, r.Right, r.Top);

        // "Now" guide
        var nowX = Pt(nowT, 0).X;
        using (var penNow = new Pen(Color.FromArgb(50, 160, 160, 160), 0.5f))
            g.DrawLine(penNow, nowX, r.Top, nowX, r.Bottom);

        // Past path
        var past = new List<PointF> { Pt(weekStartT, 0) };
        foreach (var s in _samples.Where(s => s.T > weekStartT && s.T <= nowT))
            past.Add(Pt(s.T, s.V));
        past.Add(Pt(nowT, _snapshot.WeeklyUtilization));

        var ember = Color.FromArgb(214, 140, 69);

        // Gradient fill under past line
        if (past.Count >= 2)
        {
            var fill = new List<PointF>(past);
            fill.Add(new PointF(past[^1].X, r.Bottom));
            fill.Add(new PointF(past[0].X,  r.Bottom));
            var fillRect = new RectangleF(r.Left, r.Top, r.Width, r.Height);
            using var brush = new LinearGradientBrush(
                fillRect, Color.FromArgb(80, ember), Color.FromArgb(0, ember),
                LinearGradientMode.Vertical);
            g.FillPolygon(brush, fill.ToArray());
        }

        // Past solid line
        using (var pen = new Pen(ember, 1.5f)
               { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
        {
            if (past.Count >= 2) g.DrawLines(pen, past.ToArray());
        }

        // Future dashed line to projected end-of-week
        var projected = Projected(_snapshot);
        var futureEnd = Pt(weekEndT, projected);
        using (var pen = new Pen(Color.FromArgb(140, ember), 1.2f)
               { DashPattern = new float[] { 3, 3 }, StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawLine(pen, past[^1], futureEnd);
        }

        // Now dot
        using (var b = new SolidBrush(ember))
            g.FillEllipse(b, past[^1].X - 2.8f, past[^1].Y - 2.8f, 5.6f, 5.6f);

        // Projected end ghost dot
        using (var b = new SolidBrush(Color.FromArgb(140, ember)))
            g.FillEllipse(b, futureEnd.X - 2.0f, futureEnd.Y - 2.0f, 4.0f, 4.0f);
    }

    private static double Projected(UsageSnapshot snap)
    {
        double weekEndT   = ((DateTimeOffset)snap.WeeklyResetsAt).ToUnixTimeMilliseconds() / 1000.0;
        double weekStartT = weekEndT - 7 * 24 * 3600;
        double nowT       = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        var elapsed = Math.Max(60, nowT - weekStartT);
        var total   = Math.Max(60, weekEndT - weekStartT);
        var projected = snap.WeeklyUtilization * (total / elapsed);
        return Math.Min(100, Math.Max(snap.WeeklyUtilization, projected));
    }

    private void DrawEmptyHint(Graphics g, Rectangle r)
    {
        using var font = new Font("Segoe UI", 9);
        using var brush = new SolidBrush(Color.FromArgb(140, 140, 145));
        var text = L.Get("sparkline.empty");
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, brush,
            r.Left + (r.Width - size.Width) / 2, r.Top + (r.Height - size.Height) / 2);
    }
}
