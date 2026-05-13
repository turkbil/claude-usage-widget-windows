using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClaudeUsageWidget;

/// `--print-usage` mode: emits the latest usage as JSON to stdout and exits.
/// For shell scripts, statuslines, automation.
public static class CLIRunner
{
    public static async Task<int> PrintUsageAsync()
    {
        try
        {
            var snap = await ClaudeApi.FetchSnapshotAsync();
            Console.WriteLine(Encode(snap));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("error: " + ex.Message);
            return 1;
        }
    }

    private static string Encode(UsageSnapshot s)
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
}
