namespace BossKey.Models;

public sealed class AppSettings
{
    public HotkeyGesture HideHotkey { get; set; } = HotkeyGesture.DefaultHide();

    public HotkeyGesture RestoreHotkey { get; set; } = HotkeyGesture.DefaultRestore();

    public bool StartWithWindows { get; set; }

    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;

    public bool ReduceMotion { get; set; }

    public bool ShowAdminHint { get; set; } = true;

    public List<HideRule> HideRules { get; set; } = [];
}
