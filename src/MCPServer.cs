using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClaudeUsageWidget;

/// Minimal MCP (Model Context Protocol) server over stdio. When launched
/// with `--mcp-server`, the binary reads newline-delimited JSON-RPC from
/// stdin and writes responses to stdout instead of starting the tray UI.
///
/// Exposes one tool: `get_usage`, which returns the current weekly +
/// 5-hour utilization snapshot. Install by adding to ~/.claude.json:
///
/// {
///   "mcpServers": {
///     "claude-usage": {
///       "command": "C:\\Tools\\ClaudeUsageWidget\\ClaudeUsageWidget.exe",
///       "args": ["--mcp-server"]
///     }
///   }
/// }
public static class MCPServer
{
    public static async Task RunAsync()
    {
        using var stdin = Console.OpenStandardInput();
        using var reader = new StreamReader(stdin);

        while (true)
        {
            string? line = await reader.ReadLineAsync();
            if (line == null) return;
            if (line.Length == 0) continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                await HandleAsync(doc.RootElement);
            }
            catch { /* skip malformed lines */ }
        }
    }

    private static async Task HandleAsync(JsonElement msg)
    {
        if (!msg.TryGetProperty("method", out var methodEl)) return;
        var method = methodEl.GetString();
        var id = msg.TryGetProperty("id", out var idEl) ? (object?)idEl.Clone() : null;

        switch (method)
        {
            case "initialize":
                Send(new Dictionary<string, object>
                {
                    ["protocolVersion"] = "2024-11-05",
                    ["capabilities"]    = new Dictionary<string, object> { ["tools"] = new {} },
                    ["serverInfo"]      = new Dictionary<string, object>
                    {
                        ["name"]    = "claude-usage-widget",
                        ["version"] = VersionChecker.CurrentVersion,
                    },
                }, id);
                break;

            case "notifications/initialized":
                break;

            case "tools/list":
                Send(new Dictionary<string, object>
                {
                    ["tools"] = new[] { GetUsageToolSpec },
                }, id);
                break;

            case "tools/call":
                await HandleToolCallAsync(msg, id);
                break;

            case "ping":
                Send(new Dictionary<string, object>(), id);
                break;

            default:
                SendError(-32601, $"method not found: {method}", id);
                break;
        }
    }

    private static async Task HandleToolCallAsync(JsonElement msg, object? id)
    {
        var name = msg.TryGetProperty("params", out var p) &&
                   p.TryGetProperty("name", out var n) ? n.GetString() : "";
        if (name != "get_usage")
        {
            SendError(-32602, $"unknown tool: {name}", id);
            return;
        }

        try
        {
            var snap = await ClaudeApi.FetchSnapshotAsync();
            var payload = EncodeSnapshot(snap);
            Send(new Dictionary<string, object>
            {
                ["content"] = new[]
                {
                    new Dictionary<string, object> { ["type"] = "text", ["text"] = payload },
                },
            }, id);
        }
        catch (Exception ex)
        {
            Send(new Dictionary<string, object>
            {
                ["content"] = new[]
                {
                    new Dictionary<string, object> { ["type"] = "text", ["text"] = "error: " + ex.Message },
                },
                ["isError"] = true,
            }, id);
        }
    }

    private static readonly object GetUsageToolSpec = new Dictionary<string, object>
    {
        ["name"] = "get_usage",
        ["description"] =
            "Get the user's current Claude usage from claude.ai/settings/usage. Returns weekly utilization %, weekly reset date, 5-hour-window utilization %, plan label (e.g. 'Max 20x'), and account display name. Useful before starting a long task to gauge how much budget you have left.",
        ["inputSchema"] = new Dictionary<string, object>
        {
            ["type"]       = "object",
            ["properties"] = new {},
            ["required"]   = new string[] {},
        },
    };

    private static string EncodeSnapshot(UsageSnapshot s)
    {
        var dict = new Dictionary<string, object>
        {
            ["weekly_utilization_pct"] = (int)Math.Round(s.WeeklyUtilization),
            ["weekly_resets_at"]       = s.WeeklyResetsAt.ToString("O"),
        };
        if (s.FiveHourUtilization is double fu)  dict["five_hour_utilization_pct"] = (int)Math.Round(fu);
        if (s.FiveHourResetsAt is DateTime fr)   dict["five_hour_resets_at"]       = fr.ToString("O");
        if (s.SonnetUtilization is double su)    dict["sonnet_utilization_pct"]    = (int)Math.Round(su);
        if (!string.IsNullOrEmpty(s.DisplayName)) dict["display_name"]             = s.DisplayName;
        if (!string.IsNullOrEmpty(s.PlanLabel))   dict["plan"]                     = s.PlanLabel;
        return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
    }

    private static void Send(object result, object? id)
    {
        var msg = new Dictionary<string, object> { ["jsonrpc"] = "2.0", ["result"] = result };
        if (id != null) msg["id"] = id;
        Emit(msg);
    }

    private static void SendError(int code, string message, object? id)
    {
        var msg = new Dictionary<string, object>
        {
            ["jsonrpc"] = "2.0",
            ["error"]   = new Dictionary<string, object> { ["code"] = code, ["message"] = message },
        };
        if (id != null) msg["id"] = id;
        Emit(msg);
    }

    private static void Emit(object message)
    {
        var line = JsonSerializer.Serialize(message);
        Console.WriteLine(line);
        Console.Out.Flush();
    }
}
