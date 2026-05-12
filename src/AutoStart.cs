using System;
using System.IO;
using Microsoft.Win32;

namespace ClaudeUsageWidget;

/// Registers/unregisters the app under HKCU\…\Run so it launches at login.
/// Per-user, no admin required, fully reversible.
public static class AutoStart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ClaudeUsageWidget";

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(ValueName) is string;
        }
    }

    public static void Enable()
    {
        string exe = Environment.ProcessPath
                     ?? throw new InvalidOperationException("Cannot resolve own executable path");
        // Wrap in quotes so paths with spaces work.
        string command = $"\"{exe}\"";
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
        key?.SetValue(ValueName, command);
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key?.GetValue(ValueName) is not null) key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    public static void Toggle()
    {
        if (IsEnabled) Disable();
        else Enable();
    }
}
