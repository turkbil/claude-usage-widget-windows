using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace ClaudeUsageWidget;

/// Renders the tray icon. Windows tray slots are 16×16 (32×32 on high-DPI)
/// raster images — we draw all enabled title segments (icon glyph, text,
/// donut rings) into a single bitmap, then convert it to an HICON.
public static class IconRenderer
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    /// Render the configured composition from Preferences + snapshot.
    public static Icon Render(UsageSnapshot? snap, Preferences prefs, bool error = false)
    {
        var segments = TitleRenderer.ComposeSegments(snap, prefs, error);
        // Trim leading "icon" segment if it duplicates upcoming text (rare)
        return RenderSegments(segments);
    }

    private static Icon RenderSegments(List<TitleRenderer.Segment> segments)
    {
        // Wide bitmap; Windows squashes the tray icon down to 16×16, but
        // we render at higher resolution for crisp downsampling.
        const int height = 32;

        // Pre-measure to size the bitmap width
        using var probeBmp = new Bitmap(1, 1);
        using var probeG = Graphics.FromImage(probeBmp);
        int totalW = 0;
        var widths = new List<int>();
        var font = ResolveTextFont(out var fontSize);
        foreach (var seg in segments)
        {
            int w;
            if (seg.Kind == "donut") w = height;             // square
            else
            {
                var m = probeG.MeasureString(seg.Text ?? "", font);
                w = (int)Math.Ceiling(m.Width);
            }
            widths.Add(w);
            totalW += w + 4;
        }
        if (totalW <= 0) totalW = height;
        totalW = Math.Max(totalW, height);   // never narrower than a single icon

        var bmp = new Bitmap(totalW, height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            int x = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];
                int w = widths[i];
                if (seg.Kind == "donut")
                {
                    DrawDonut(g, new RectangleF(x, 2, height - 4, height - 4),
                              seg.FillPct ?? 0, seg.Color);
                }
                else
                {
                    using var brush = new SolidBrush(seg.Color);
                    var m = g.MeasureString(seg.Text ?? "", font);
                    g.DrawString(seg.Text ?? "", font, brush,
                                 x, (height - m.Height) / 2);
                }
                x += w + 4;
            }
        }

        try
        {
            IntPtr hIcon = bmp.GetHicon();
            try
            {
                using var fromHandle = Icon.FromHandle(hIcon);
                return (Icon)fromHandle.Clone();
            }
            finally { DestroyIcon(hIcon); }
        }
        finally { bmp.Dispose(); }
    }

    private static Font ResolveTextFont(out float fontSize)
    {
        fontSize = 20;
        return new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
    }

    private static void DrawDonut(Graphics g, RectangleF rect, double fill, Color color)
    {
        float thickness = rect.Width * 0.22f;
        var inset = thickness / 2f;
        var r = new RectangleF(rect.X + inset, rect.Y + inset,
                               rect.Width - thickness, rect.Height - thickness);

        // Track
        using (var trackPen = new Pen(Color.FromArgb(70, Color.White), thickness))
            g.DrawEllipse(trackPen, r);

        if (fill <= 0) return;
        var sweep = (float)(Math.Max(0, Math.Min(1, fill)) * 360);
        using var arc = new Pen(color, thickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawArc(arc, r, -90, sweep);
    }

    public static Color ColorForPercent(int p)
    {
        if (p >= 90) return Color.FromArgb(255, 235, 90, 90);   // red
        if (p >= 75) return Color.FromArgb(255, 245, 165, 60);  // orange
        if (p >= 50) return Color.FromArgb(255, 235, 210, 70);  // yellow
        return Color.FromArgb(255, 80, 200, 110);                // green
    }
}
