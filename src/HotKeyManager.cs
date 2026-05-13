using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClaudeUsageWidget;

/// Win32 RegisterHotKey wrapper. Listens for a global keyboard shortcut and
/// invokes a callback on the UI thread.
///
/// Modifier values (Win32):
///   MOD_ALT = 0x1, MOD_CONTROL = 0x2, MOD_SHIFT = 0x4, MOD_WIN = 0x8
public sealed class HotKeyManager : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;
    private const int HotKeyId  = 0xCFA1;   // 'CUW1'

    private readonly HotKeyWindow _window;
    private bool _registered;
    private Action? _onTrigger;

    public HotKeyManager()
    {
        _window = new HotKeyWindow(OnHotKey);
    }

    public void Apply(Action onTrigger)
    {
        var prefs = PrefsStore.Current;
        Unregister();
        if (!prefs.HotkeyEnabled) return;
        _onTrigger = onTrigger;
        _registered = RegisterHotKey(_window.Handle, HotKeyId, prefs.HotkeyModifiers, prefs.HotkeyVirtKey);
    }

    public void Unregister()
    {
        if (_registered)
        {
            UnregisterHotKey(_window.Handle, HotKeyId);
            _registered = false;
        }
    }

    private void OnHotKey()
    {
        try { _onTrigger?.Invoke(); } catch { /* don't crash on bad callback */ }
    }

    public static string Label(uint vk, uint mods)
    {
        var parts = new System.Collections.Generic.List<string>();
        if ((mods & 0x2) != 0) parts.Add("Ctrl");
        if ((mods & 0x1) != 0) parts.Add("Alt");
        if ((mods & 0x4) != 0) parts.Add("Shift");
        if ((mods & 0x8) != 0) parts.Add("Win");
        parts.Add(KeyLabel(vk));
        return string.Join(" + ", parts);
    }

    private static string KeyLabel(uint vk)
    {
        if (vk >= 0x30 && vk <= 0x39) return ((char)vk).ToString();           // '0'..'9'
        if (vk >= 0x41 && vk <= 0x5A) return ((char)vk).ToString();           // 'A'..'Z'
        return vk switch
        {
            0x20 => "Space",
            0x0D => "Enter",
            0x1B => "Esc",
            _ => $"VK{vk:X2}",
        };
    }

    public void Dispose()
    {
        Unregister();
        _window.DestroyHandle();
    }

    /// Hidden message-only window that receives WM_HOTKEY.
    private sealed class HotKeyWindow : NativeWindow
    {
        private readonly Action _onHotKey;
        public HotKeyWindow(Action onHotKey)
        {
            _onHotKey = onHotKey;
            CreateHandle(new CreateParams { Caption = "ClaudeUsageWidget_HotKey" });
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HotKeyId)
            {
                _onHotKey();
            }
            base.WndProc(ref m);
        }
    }
}
