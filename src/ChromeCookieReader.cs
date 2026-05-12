using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace ClaudeUsageWidget;

/// <summary>
/// Reads and decrypts the claude.ai sessionKey cookie from the local Chrome
/// installation on Windows. Mirrors the macOS Swift implementation in the
/// sister repo, adapted for the Windows crypto scheme:
///
///   1. Master key lives in `%LOCALAPPDATA%\Google\Chrome\User Data\Local State`,
///      base64-encoded, DPAPI-protected.
///   2. Each cookie's `encrypted_value` starts with "v10" or "v20", followed by
///      a 12-byte nonce, ciphertext, and a 16-byte AES-256-GCM auth tag.
/// </summary>
public static class ChromeCookieReader
{
    public static string GetSessionKey()
    {
        var userData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Google", "Chrome", "User Data");
        var cookiesPath = Path.Combine(userData, "Default", "Network", "Cookies");
        var localStatePath = Path.Combine(userData, "Local State");

        if (!File.Exists(cookiesPath))
            throw new InvalidOperationException(L.Get("error.cookie_db"));
        if (!File.Exists(localStatePath))
            throw new InvalidOperationException(L.Get("error.cookie_db"));

        byte[] masterKey = LoadMasterKey(localStatePath);
        byte[] encrypted = LoadEncryptedCookieValue(cookiesPath);
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
        // First 5 bytes are the "DPAPI" prefix.
        if (encryptedKeyBytes.Length < 5 ||
            Encoding.ASCII.GetString(encryptedKeyBytes, 0, 5) != "DPAPI")
            throw new InvalidOperationException(L.Get("error.cookie_decrypt"));

        byte[] dpapiBlob = encryptedKeyBytes.Skip(5).ToArray();
        return ProtectedData.Unprotect(dpapiBlob, null, DataProtectionScope.CurrentUser);
    }

    private static byte[] LoadEncryptedCookieValue(string cookiesPath)
    {
        // Copy the live DB to a temp path because Chrome holds locks on it.
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

        // Layout: prefix(3) | nonce(12) | ciphertext(n) | tag(16)
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

        // Chrome v20 cookies prepend a 32-byte SHA-256 host hash. Strip it.
        int start = 0;
        if (prefix == "v20" && plaintext.Length > 32)
        {
            // Heuristic: if the first 32 bytes contain non-printable bytes, it's the hash.
            bool looksLikeHash = false;
            for (int i = 0; i < 32; i++)
            {
                if (plaintext[i] < 0x20 || plaintext[i] >= 0x7F) { looksLikeHash = true; break; }
            }
            if (looksLikeHash) start = 32;
        }
        // Trim trailing non-printable.
        int end = plaintext.Length;
        while (end > start && (plaintext[end - 1] < 0x20 || plaintext[end - 1] >= 0x7F)) end--;
        if (end <= start) throw new InvalidOperationException(L.Get("error.cookie_empty"));

        return Encoding.ASCII.GetString(plaintext, start, end - start);
    }
}
