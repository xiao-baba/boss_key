namespace BossKey.Models;

public sealed class WindowInfo : ObservableObject
{
    private bool _isSelected;

    public IntPtr Handle { get; init; }

    public string HandleHex => $"0x{Handle.ToInt64():X}";

    public string Title { get; init; } = string.Empty;

    public string ProcessName { get; init; } = string.Empty;

    public string ProcessPath { get; init; } = string.Empty;

    public int ProcessId { get; init; }

    public bool IsElevatedOrInaccessible { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
