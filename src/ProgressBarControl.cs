using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClaudeUsageWidget;

/// Pill-shaped, color-coded progress bar with a translucent track.
public sealed class ProgressBarControl : Control
{
    private double progress = 0;
    private Color fillColor = Color.FromArgb(80, 200, 110);

    public ProgressBarControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint |
                 ControlStyles.SupportsTransparentBackColor |
                 ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Height = 8;
    }

    public double Progress
    {
        get => progress;
        set { progress = value; Invalidate(); }
    }

    public Color FillColor
    {
        get => fillColor;
        set { fillColor = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = ClientRectangle;
        int radius = r.Height;

        using var track = new SolidBrush(Color.FromArgb(40, 128, 128, 128));
        FillRoundedRect(g, track, r, radius);

        double p = progress < 0 ? 0 : progress > 1 ? 1 : progress;
        if (p > 0)
        {
            int w = (int)System.Math.Max(r.Height, r.Width * p);
            var fillRect = new Rectangle(r.X, r.Y, w, r.Height);
            using var fill = new SolidBrush(fillColor);
            FillRoundedRect(g, fill, fillRect, radius);
        }
    }

    private static void FillRoundedRect(Graphics g, Brush brush, Rectangle r, int radius)
    {
        if (radius >= r.Height) radius = r.Height - 1;
        if (radius < 1) { g.FillRectangle(brush, r); return; }
        using var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, radius, radius, 180, 90);
        path.AddArc(r.Right - radius, r.Y, radius, radius, 270, 90);
        path.AddArc(r.Right - radius, r.Bottom - radius, radius, radius, 0, 90);
        path.AddArc(r.X, r.Bottom - radius, radius, radius, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
