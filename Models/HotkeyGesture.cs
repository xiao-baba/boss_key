using System.Windows.Input;

namespace BossKey.Models;

public sealed class HotkeyGesture
{
    public HotkeyModifiers Modifiers { get; set; }

    public Key Key { get; set; }

    public bool IsValid => Key != Key.None
        && Modifiers != HotkeyModifiers.None
        && !IsModifierOnly(Key);

    public static HotkeyGesture DefaultHide()
        => new() { Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt, Key = Key.H };

    public static HotkeyGesture DefaultRestore()
        => new() { Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt, Key = Key.R };

    public static HotkeyGesture FromKeyEvent(System.Windows.Input.KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        key = key == Key.ImeProcessed ? e.ImeProcessedKey : key;

        var modifiers = HotkeyModifiers.None;
        var keyboardModifiers = Keyboard.Modifiers;
        if (keyboardModifiers.HasFlag(ModifierKeys.Control))
        {
            modifiers |= HotkeyModifiers.Control;
        }

        if (keyboardModifiers.HasFlag(ModifierKeys.Alt))
        {
            modifiers |= HotkeyModifiers.Alt;
        }

        if (keyboardModifiers.HasFlag(ModifierKeys.Shift))
        {
            modifiers |= HotkeyModifiers.Shift;
        }

        if (keyboardModifiers.HasFlag(ModifierKeys.Windows))
        {
            modifiers |= HotkeyModifiers.Win;
        }

        return new HotkeyGesture { Modifiers = modifiers, Key = key };
    }

    public override string ToString()
    {
        if (!IsValid)
        {
            return "未设置";
        }

        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Win))
        {
            parts.Add("Win");
        }

        parts.Add(Key.ToString());
        return string.Join(" + ", parts);
    }

    private static bool IsModifierOnly(Key key)
        => key is Key.LeftCtrl
            or Key.RightCtrl
            or Key.LeftAlt
            or Key.RightAlt
            or Key.LeftShift
            or Key.RightShift
            or Key.LWin
            or Key.RWin
            or Key.System;
}
