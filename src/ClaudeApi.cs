using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClaudeUsageWidget;

public record UsageSnapshot(
    double WeeklyUtilization,
    DateTime WeeklyResetsAt,
    double? FiveHourUtilization,
    DateTime? FiveHourResetsAt,
    double? SonnetUtilization,
    string? DisplayName,
    string? PlanLabel,
    DateTime FetchedAt
);

public static class ClaudeApi
{
    private static readonly HttpClient http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://claude.ai"),
            Timeout = TimeSpan.FromSeconds(20),
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 ClaudeUsageWidget");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public static async Task<UsageSnapshot> FetchSnapshotAsync()
    {
        string cookie = BrowserCookieReader.GetSessionKey();
        string orgId = await FindOrgIdAsync(cookie);

        // Account & plan are optional — usage works without them.
        string? displayName = null;
        string? planLabel = null;
        try { displayName = await FetchDisplayNameAsync(cookie); } catch { }
        try { planLabel = await FetchPlanLabelAsync(cookie, orgId); } catch { }

        using var usageDoc = await GetJsonAsync($"/api/organizations/{orgId}/usage", cookie);
        var root = usageDoc.RootElement;
        if (!root.TryGetProperty("seven_day", out var sevenDay))
            throw new InvalidOperationException(L.Get("error.api_parse"));

        double weeklyUtil = sevenDay.GetProperty("utilization").GetDouble();
        DateTime weeklyResets = sevenDay.GetProperty("resets_at").GetDateTime().ToUniversalTime();

        double? fiveUtil = null;
        DateTime? fiveResets = null;
        if (root.TryGetProperty("five_hour", out var fiveHour))
        {
            if (fiveHour.TryGetProperty("utilization", out var fu))
                fiveUtil = fu.GetDouble();
            if (fiveHour.TryGetProperty("resets_at", out var fr))
                fiveResets = fr.GetDateTime().ToUniversalTime();
        }

        double? sonnetUtil = null;
        foreach (var key in new[] { "seven_day_sonnet", "seven_day_sonnet_only", "seven_day_opus" })
        {
            if (root.TryGetProperty(key, out var block) &&
                block.TryGetProperty("utilization", out var u))
            {
                sonnetUtil = u.GetDouble();
                break;
            }
        }

        return new UsageSnapshot(
            weeklyUtil, weeklyResets,
            fiveUtil, fiveResets,
            sonnetUtil,
            displayName, planLabel,
            DateTime.UtcNow
        );
    }

    private static async Task<string> FetchDisplayNameAsync(string cookie)
    {
        using var doc = await GetJsonAsync("/api/account", cookie);
        var r = doc.RootElement;
        if (r.TryGetProperty("display_name", out var dn) && dn.GetString() is { Length: > 0 } s) return s;
        if (r.TryGetProperty("full_name", out var fn) && fn.GetString() is { Length: > 0 } s2) return s2;
        if (r.TryGetProperty("email_address", out var em) && em.GetString() is { Length: > 0 } s3) return s3;
        throw new InvalidOperationException(L.Get("error.api_parse"));
    }

    private static async Task<string> FetchPlanLabelAsync(string cookie, string orgId)
    {
        using var doc = await GetJsonAsync($"/api/organizations/{orgId}/rate_limits", cookie);
        if (!doc.RootElement.TryGetProperty("rate_limit_tier", out var tier))
            throw new InvalidOperationException(L.Get("error.api_parse"));
        return PrettyPlanName(tier.GetString() ?? "");
    }

    private static async Task<string> FindOrgIdAsync(string cookie)
    {
        using var doc = await GetJsonAsync("/api/organizations", cookie);
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            if (el.TryGetProperty("uuid", out var uuid) && uuid.GetString() is { Length: > 0 } id)
                return id;
        }
        throw new InvalidOperationException(L.Get("error.org_not_found"));
    }

    private static async Task<JsonDocument> GetJsonAsync(string path, string cookie)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        req.Headers.Add("Cookie", $"sessionKey={cookie}");
        using var resp = await http.SendAsync(req);
        string body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            string snippet = body.Length > 120 ? body[..120] : body;
            throw new InvalidOperationException(string.Format(L.Get("error.http"), (int)resp.StatusCode, snippet));
        }
        return JsonDocument.Parse(body);
    }

    /// "default_claude_max_20x" → "Max 20x"
    private static string PrettyPlanName(string tier)
    {
        string s = tier.ToLowerInvariant();
        s = s.Replace("default_claude_", "").Replace("default_", "");
        var parts = new List<string>();
        foreach (var p in s.Split('_'))
        {
            if (p is "max" or "pro" or "team" or "free" or "enterprise")
                parts.Add(char.ToUpperInvariant(p[0]) + p[1..]);
            else
                parts.Add(p);
        }
        return string.Join(" ", parts);
    }
}
