using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClaudeUsageWidget;

/// Polls the GitHub Releases API once a day. Stores the latest tag in
/// Preferences so the tray menu can flag when a newer version is available.
public static class VersionChecker
{
    public const string ReleasesUrl  = "https://github.com/turkbil/claude-usage-widget-windows/releases";
    private const string ApiUrl       = "https://api.github.com/repos/turkbil/claude-usage-widget-windows/releases/latest";
    private const double OneDay       = 24 * 3600;

    private static readonly HttpClient http = new();

    public static async Task CheckIfDueAsync()
    {
        var prefs = PrefsStore.Current;
        if (!prefs.VersionCheckEnabled) return;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now - prefs.LastVersionCheckUnix < OneDay) return;

        PrefsStore.Update(p => p.LastVersionCheckUnix = now);
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
            req.Headers.UserAgent.ParseAdd("ClaudeUsageWidget");
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            using var resp = await http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return;
            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("tag_name", out var tag) &&
                tag.GetString() is { Length: > 0 } raw)
            {
                var cleaned = raw.TrimStart('v', ' ');
                PrefsStore.Update(p => p.LatestKnownVersion = cleaned);
            }
        }
        catch { /* network errors are fine — try again tomorrow */ }
    }

    public static string CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

    public static bool UpdateAvailable
    {
        get
        {
            var latest = PrefsStore.Current.LatestKnownVersion.TrimStart('v', ' ');
            if (string.IsNullOrEmpty(latest)) return false;
            return CompareSemver(latest, CurrentVersion) > 0;
        }
    }

    private static int CompareSemver(string a, string b)
    {
        var ax = ParseParts(a);
        var bx = ParseParts(b);
        int max = Math.Max(ax.Length, bx.Length);
        for (int i = 0; i < max; i++)
        {
            int av = i < ax.Length ? ax[i] : 0;
            int bv = i < bx.Length ? bx[i] : 0;
            if (av != bv) return av.CompareTo(bv);
        }
        return 0;
    }

    private static int[] ParseParts(string v)
    {
        var bits = v.Split('.');
        var ints = new int[bits.Length];
        for (int i = 0; i < bits.Length; i++)
            int.TryParse(bits[i], out ints[i]);
        return ints;
    }
}
