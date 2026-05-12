using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace ClaudeUsageWidget;

/// Minimal JSON-backed localization. Loads `Strings.<lang>.json` from embedded
/// resources, falling back to English. Use via `L.Get("key")` or `L.Fmt("key", args)`.
public static class L
{
    private static Dictionary<string, string> strings = Load(PickCulture());

    public static string Get(string key) =>
        strings.TryGetValue(key, out var v) ? v : key;

    public static string Fmt(string key, params object?[] args) =>
        string.Format(Get(key), args);

    private static string PickCulture()
    {
        string two = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        return two switch
        {
            "tr" or "de" or "es" or "fr" => two,
            _ => "en",
        };
    }

    private static Dictionary<string, string> Load(string lang)
    {
        var asm = Assembly.GetExecutingAssembly();
        // Try requested language, then English.
        foreach (var l in new[] { lang, "en" })
        {
            string resName = $"ClaudeUsageWidget.Resources.Strings.{l}.json";
            using var stream = asm.GetManifestResourceStream(resName);
            if (stream is null) continue;
            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict is not null) return dict;
        }
        return new Dictionary<string, string>();
    }
}
