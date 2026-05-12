using System;
using System.Threading;
using System.Windows.Forms;

namespace ClaudeUsageWidget;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Single-instance guard.
        using var mutex = new Mutex(true, "ClaudeUsageWidget-SingleInstance", out bool createdNew);
        if (!createdNew) return;

        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.Run(new TrayApp());
    }
}
