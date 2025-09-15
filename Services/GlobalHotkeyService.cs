using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SnippetPad.Services;

public sealed class GlobalHotkeyService : IDisposable
{
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd,int id,uint fsModifiers,uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd,int id);

    private readonly Dictionary<int, Action> _map = new();
    private int _id = 300;

    public void Register(Form form, string? combo, Action action)
    {
        if (string.IsNullOrWhiteSpace(combo)) return;
        Parse(combo, out uint mod, out uint key);
        int id = ++_id;
        if (RegisterHotKey(form.Handle, id, mod, key))
            _map[id] = action;
    }

    public void ProcessMessage(Message m)
    {
        if (m.Msg == 0x0312) // WM_HOTKEY
        {
            int id = m.WParam.ToInt32();
            if (_map.TryGetValue(id, out var act))
                act();
        }
    }

    private void Parse(string combo, out uint modifiers, out uint key)
    {
        modifiers=0; key=0;
        foreach (var p in combo.Split('+', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = p.Trim().ToUpperInvariant();
            switch (t)
            {
                case "ALT": modifiers |= 0x1; break;
                case "CTRL": case "CONTROL": modifiers |= 0x2; break;
                case "SHIFT": modifiers |= 0x4; break;
                case "WIN": modifiers |= 0x8; break;
                default:
                    if (t.Length == 1) key = t[0];
                    else if (Enum.TryParse(typeof(Keys), t, out var k)) key = (uint)(Keys)k;
                    break;
            }
        }
    }

    public void Dispose()
    {
        // 简化：不枚举释放；进程退出系统自动释放。
    }
}