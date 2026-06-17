using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using BossKey.Models;

namespace BossKey.Services;

public sealed class HotkeyService : IDisposable
{
    private const int HideHotkeyId = 0xB05501;
    private const int RestoreHotkeyId = 0xB05502;

    private HwndSource? _source;
    private IntPtr _handle;
    private bool _hideRegistered;
    private bool _restoreRegistered;

    public event EventHandler<HotkeyAction>? HotkeyPressed;

    public bool Register(Window window, HotkeyGesture hideHotkey, HotkeyGesture restoreHotkey, out string? error)
    {
        error = null;
        EnsureHook(window);
        UnregisterAll();

        if (!hideHotkey.IsValid)
        {
            error = "隐藏快捷键无效";
            return false;
        }

        if (!restoreHotkey.IsValid)
        {
            error = "恢复快捷键无效";
            return false;
        }

        if (!RegisterOne(HideHotkeyId, hideHotkey, out error))
        {
            return false;
        }

        _hideRegistered = true;
        if (!RegisterOne(RestoreHotkeyId, restoreHotkey, out error))
        {
            UnregisterAll();
            return false;
        }

        _restoreRegistered = true;
        return true;
    }

    public void Dispose()
    {
        UnregisterAll();
        if (_source is not null)
        {
            _source.RemoveHook(WndProc);
            _source = null;
        }
    }

    private void EnsureHook(Window window)
    {
        if (_source is not null)
        {
            return;
        }

        var helper = new WindowInteropHelper(window);
        _handle = helper.EnsureHandle();
        _source = HwndSource.FromHwnd(_handle);
        _source?.AddHook(WndProc);
    }

    private bool RegisterOne(int id, HotkeyGesture gesture, out string? error)
    {
        error = null;
        var modifiers = ToNativeModifiers(gesture.Modifiers) | NativeMethods.MOD_NOREPEAT;
        var key = (uint)KeyInterop.VirtualKeyFromKey(gesture.Key);

        if (NativeMethods.RegisterHotKey(_handle, id, modifiers, key))
        {
            return true;
        }

        var win32Error = Marshal.GetLastWin32Error();
        error = $"快捷键 {gesture} 注册失败：{new Win32Exception(win32Error).Message}";
        return false;
    }

    private void UnregisterAll()
    {
        if (_handle == IntPtr.Zero)
        {
            return;
        }

        if (_hideRegistered)
        {
            NativeMethods.UnregisterHotKey(_handle, HideHotkeyId);
            _hideRegistered = false;
        }

        if (_restoreRegistered)
        {
            NativeMethods.UnregisterHotKey(_handle, RestoreHotkeyId);
            _restoreRegistered = false;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != NativeMethods.WM_HOTKEY)
        {
            return IntPtr.Zero;
        }

        var action = wParam.ToInt32() switch
        {
            HideHotkeyId => HotkeyAction.Hide,
            RestoreHotkeyId => HotkeyAction.Restore,
            _ => (HotkeyAction?)null
        };

        if (action is not null)
        {
            handled = true;
            HotkeyPressed?.Invoke(this, action.Value);
        }

        return IntPtr.Zero;
    }

    private static uint ToNativeModifiers(HotkeyModifiers modifiers)
    {
        var native = 0u;
        if (modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            native |= 0x0001;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Control))
        {
            native |= 0x0002;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            native |= 0x0004;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Win))
        {
            native |= 0x0008;
        }

        return native;
    }
}
