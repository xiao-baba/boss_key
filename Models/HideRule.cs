namespace BossKey.Models;

public sealed class HideRule : ObservableObject
{
    private bool _enabled = true;
    private RuleMatchType _matchType = RuleMatchType.Contains;
    private string _pattern = string.Empty;
    private bool _caseSensitive;
    private bool _matchTitle = true;
    private bool _matchProcessName = true;
    private bool _matchProcessPath = true;

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    public RuleMatchType MatchType
    {
        get => _matchType;
        set => SetProperty(ref _matchType, value);
    }

    public string Pattern
    {
        get => _pattern;
        set => SetProperty(ref _pattern, value);
    }

    public bool CaseSensitive
    {
        get => _caseSensitive;
        set => SetProperty(ref _caseSensitive, value);
    }

    public bool MatchTitle
    {
        get => _matchTitle;
        set => SetProperty(ref _matchTitle, value);
    }

    public bool MatchProcessName
    {
        get => _matchProcessName;
        set => SetProperty(ref _matchProcessName, value);
    }

    public bool MatchProcessPath
    {
        get => _matchProcessPath;
        set => SetProperty(ref _matchProcessPath, value);
    }
}
