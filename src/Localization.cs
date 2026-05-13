using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace ClaudeUsageWidget;

/// Minimal JSON-backed localization. Loads `Strings.<lang>.json` from embedded
/// resources. Falls back to English when a key isn't present in the user's
/// language, so partial translations don't break the UI.
///
/// Usage:  L.Get("key")  or  string.Format(L.Get("key"), args...)
public static class L
{
    private static readonly Dictionary<string, string> Primary  = Load(PickCulture());
    private static readonly Dictionary<string, string> Fallback = Load("en");

    public static string Get(string key)
    {
        if (Primary.TryGetValue(key, out var v))  return v;
        if (Fallback.TryGetValue(key, out v))     return v;
        return key;
    }

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
        string resName = $"ClaudeUsageWidget.Resources.Strings.{lang}.json";
        using var stream = asm.GetManifestResourceStream(resName);
        if (stream is null) return new Dictionary<string, string>();
        using var reader = new StreamReader(stream);
        string json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
               ?? new Dictionary<string, string>();
    }
}
