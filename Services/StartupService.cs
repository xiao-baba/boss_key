using Microsoft.Win32;

namespace BossKey.Services;

public sealed class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "BossKey";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(AppName) is string value
            && !string.IsNullOrWhiteSpace(value);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (!enabled)
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
            return;
        }

        var path = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("无法定位当前程序路径，不能设置开机自启。");
        }

        key.SetValue(AppName, $"\"{path}\"");
    }
}
