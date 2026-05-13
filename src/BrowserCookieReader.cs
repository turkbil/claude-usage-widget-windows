using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace ClaudeUsageWidget;

public sealed record BrowserSource(string Id, string DisplayName, string UserDataDir)
{
    public string CookiesPath    => Path.Combine(UserDataDir, "Default", "Network", "Cookies");
    public string LocalStatePath => Path.Combine(UserDataDir, "Local State");

    public static readonly BrowserSource Chrome = new(
        "chrome", "Google Chrome",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "Google", "Chrome", "User Data"));

    public static readonly BrowserSource Brave = new(
        "brave", "Brave Browser",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "BraveSoftware", "Brave-Browser", "User Data"));

    public static readonly BrowserSource Edge = new(
        "edge", "Microsoft Edge",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "Microsoft", "Edge", "User Data"));

    public static readonly BrowserSource Arc = new(
        "arc", "Arc",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "Arc", "User Data"));

    public static IEnumerable<BrowserSource> EnabledFor(Preferences p)
    {
        if (p.BrowserChromeEnabled) yield return Chrome;
        if (p.BrowserBraveEnabled)  yield return Brave;
        if (p.BrowserEdgeEnabled)   yield return Edge;
        if (p.BrowserArcEnabled)    yield return Arc;
    }
}

/// Reads and decrypts the claude.ai sessionKey cookie. Tries each enabled
/// Chromium-family browser in order; first valid session wins.
public static class BrowserCookieReader
{
    public static string GetSessionKey()
    {
        var prefs = PrefsStore.Current;
        var sources = BrowserSource.EnabledFor(prefs).ToList();
        if (sources.Count == 0) sources.Add(BrowserSource.Chrome);   // safety net

        Exception? lastError = null;
        foreach (var src in sources)
        {
            try { return GetSessionKey(src); }
            catch (Exception ex) { lastError = ex; }
        }
        throw lastError ?? new InvalidOperationException(L.Get("error.no_session"));
    }

    public static string GetSessionKey(BrowserSource src)
    {
        if (!File.Exists(src.CookiesPath))    throw new InvalidOperationException(L.Get("error.cookie_db"));
        if (!File.Exists(src.LocalStatePath)) throw new InvalidOperationException(L.Get("error.cookie_db"));

        byte[] masterKey = LoadMasterKey(src.LocalStatePath);
        byte[] encrypted = LoadEncryptedCookieValue(src.CookiesPath);
        return DecryptValue(encrypted, masterKey);
    }

    private static byte[] LoadMasterKey(string localStatePath)
    {
        string json = File.ReadAllText(localStatePath);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("os_crypt", out var osCrypt) ||
            !osCrypt.TryGetProperty("encrypted_key", out var encryptedKey))
            throw new InvalidOperationException(L.Get("error.cookie_db"));

        byte[] encryptedKeyBytes = Convert.FromBase64String(encryptedKey.GetString()!);
        if (encryptedKeyBytes.Length < 5 ||
            Encoding.ASCII.GetString(encryptedKeyBytes, 0, 5) != "DPAPI")
            throw new InvalidOperationException(L.Get("error.cookie_decrypt"));

        byte[] dpapiBlob = encryptedKeyBytes.Skip(5).ToArray();
        return ProtectedData.Unprotect(dpapiBlob, null, DataProtectionScope.CurrentUser);
    }

    private static byte[] LoadEncryptedCookieValue(string cookiesPath)
    {
        string tmp = Path.Combine(Path.GetTempPath(),
            $"claude_widget_cookies_{Guid.NewGuid():N}.db");
        File.Copy(cookiesPath, tmp, overwrite: true);
        try
        {
            using var conn = new SqliteConnection($"Data Source={tmp};Mode=ReadOnly");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT encrypted_value FROM cookies
                WHERE host_key LIKE '%claude.ai%' AND name='sessionKey'
                ORDER BY length(encrypted_value) DESC LIMIT 1";
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                throw new InvalidOperationException(L.Get("error.no_session"));

            using var stream = reader.GetStream(0);
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
        finally
        {
            try { File.Delete(tmp); } catch { /* ignore */ }
        }
    }

    private static string DecryptValue(byte[] encrypted, byte[] masterKey)
    {
        if (encrypted.Length < 3 + 12 + 16)
            throw new InvalidOperationException(L.Get("error.cookie_decrypt"));

        string prefix = Encoding.ASCII.GetString(encrypted, 0, 3);
        if (prefix != "v10" && prefix != "v20")
            throw new InvalidOperationException(L.Get("error.unknown_cookie_version") + ": " + prefix);

        byte[] nonce = new byte[12];
        Array.Copy(encrypted, 3, nonce, 0, 12);
        int ctLen = encrypted.Length - 3 - 12 - 16;
        byte[] ciphertext = new byte[ctLen];
        Array.Copy(encrypted, 3 + 12, ciphertext, 0, ctLen);
        byte[] tag = new byte[16];
        Array.Copy(encrypted, encrypted.Length - 16, tag, 0, 16);

        byte[] plaintext = new byte[ctLen];
        using var aes = new AesGcm(masterKey, 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        int start = 0;
        if (prefix == "v20" && plaintext.Length > 32)
        {
            bool looksLikeHash = false;
            for (int i = 0; i < 32; i++)
                if (plaintext[i] < 0x20 || plaintext[i] >= 0x7F) { looksLikeHash = true; break; }
            if (looksLikeHash) start = 32;
        }
        int end = plaintext.Length;
        while (end > start && (plaintext[end - 1] < 0x20 || plaintext[end - 1] >= 0x7F)) end--;
        if (end <= start) throw new InvalidOperationException(L.Get("error.cookie_empty"));

        return Encoding.ASCII.GetString(plaintext, start, end - start);
    }
}
