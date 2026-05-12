using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace ClaudeUsageWidget;

/// Renders a small percentage text into a tray-icon-sized Bitmap.
/// Result is converted to an Icon via DestroyIcon-safe handle.
public static class IconRenderer
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Icon RenderPercent(int percent, bool isError = false)
    {
        // Use 32x32 to look crisp on high-DPI displays; Windows downsamples for 16x16 trays.
        const int size = 32;
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(Color.Transparent);

            Color fg = isError
                ? Color.FromArgb(255, 235, 90, 90)
                : ColorForPercent(percent);

            string text = isError ? "?" : percent.ToString();

            // Pick font size that fits.
            float fontSize = text.Length switch
            {
                1 => 22f,
                2 => 18f,
                _ => 14f,
            };
            using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            var measured = g.MeasureString(text, font);
            float x = (size - measured.Width) / 2f;
            float y = (size - measured.Height) / 2f;

            using var brush = new SolidBrush(fg);
            g.DrawString(text, font, brush, x, y);
        }

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            // Clone before destroying handle so the Icon owns its own copy.
            using var fromHandle = Icon.FromHandle(hIcon);
            return (Icon)fromHandle.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    public static Color ColorForPercent(int p)
    {
        if (p >= 90) return Color.FromArgb(255, 235, 90, 90);    // red
        if (p >= 75) return Color.FromArgb(255, 245, 165, 60);   // orange
        if (p >= 50) return Color.FromArgb(255, 235, 210, 70);   // yellow
        return Color.FromArgb(255, 80, 200, 110);                // green
    }
}
