using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClaudeUsageWidget;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // CLI / MCP modes short-circuit before any UI initialization.
        if (args.Contains("--mcp-server"))
        {
            MCPServer.RunAsync().GetAwaiter().GetResult();
            return 0;
        }
        if (args.Contains("--print-usage"))
        {
            return CLIRunner.PrintUsageAsync().GetAwaiter().GetResult();
        }

        // Single-instance guard for the tray app.
        using var mutex = new Mutex(true, "ClaudeUsageWidget-SingleInstance", out bool createdNew);
        if (!createdNew) return 0;

        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.Run(new TrayApp());
        return 0;
    }
}
