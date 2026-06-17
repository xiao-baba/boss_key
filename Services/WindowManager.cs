using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using BossKey.Models;

namespace BossKey.Services;

public sealed class WindowManager
{
    private readonly HashSet<IntPtr> _hiddenWindows = [];

    public int HiddenWindowCount => _hiddenWindows.Count;

    public bool IsHiddenWindow(IntPtr handle) => _hiddenWindows.Contains(handle);

    public IReadOnlyList<WindowInfo> GetOpenWindows()
    {
        var windows = new List<WindowInfo>();
        var ownProcessId = Environment.ProcessId;
        var shellWindow = NativeMethods.GetShellWindow();

        NativeMethods.EnumWindows((handle, _) =>
        {
            if (handle == shellWindow
                || !NativeMethods.IsWindowVisible(handle)
                || NativeMethods.GetWindow(handle, NativeMethods.GW_OWNER) != IntPtr.Zero
                || IsCloaked(handle))
            {
                return true;
            }

            var title = GetWindowTitle(handle);
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            NativeMethods.GetWindowThreadProcessId(handle, out var processId);
            if (processId == ownProcessId || processId == 0)
            {
                return true;
            }

            var processName = string.Empty;
            var processPath = string.Empty;
            var inaccessible = false;

            try
            {
                using var process = Process.GetProcessById((int)processId);
                processName = process.ProcessName;
                try
                {
                    processPath = process.MainModule?.FileName ?? string.Empty;
                }
                catch
                {
                    inaccessible = true;
                }
            }
            catch
            {
                inaccessible = true;
            }

            windows.Add(new WindowInfo
            {
                Handle = handle,
                Title = title,
                ProcessId = (int)processId,
                ProcessName = processName,
                ProcessPath = processPath,
                IsElevatedOrInaccessible = inaccessible
            });

            return true;
        }, IntPtr.Zero);

        return windows
            .OrderBy(window => window.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(window => window.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool HideWindow(IntPtr handle)
    {
        if (!NativeMethods.IsWindow(handle))
        {
            return false;
        }

        NativeMethods.ShowWindow(handle, NativeMethods.SW_HIDE);
        if (NativeMethods.IsWindowVisible(handle))
        {
            return false;
        }

        _hiddenWindows.Add(handle);
        return true;
    }

    public int RestoreHiddenWindows()
    {
        var restored = 0;
        foreach (var handle in _hiddenWindows.ToList())
        {
            if (!NativeMethods.IsWindow(handle))
            {
                _hiddenWindows.Remove(handle);
                continue;
            }

            NativeMethods.ShowWindow(handle, NativeMethods.SW_SHOWNORMAL);
            NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);
            restored++;
            _hiddenWindows.Remove(handle);
        }

        return restored;
    }

    private static string GetWindowTitle(IntPtr handle)
    {
        var length = NativeMethods.GetWindowTextLength(handle);
        if (length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        NativeMethods.GetWindowText(handle, builder, builder.Capacity);
        return builder.ToString();
    }

    private static bool IsCloaked(IntPtr handle)
    {
        try
        {
            var result = NativeMethods.DwmGetWindowAttribute(
                handle,
                NativeMethods.DWMWA_CLOAKED,
                out var cloaked,
                Marshal.SizeOf<int>());

            return result == 0 && cloaked != 0;
        }
        catch
        {
            return false;
        }
    }
}
