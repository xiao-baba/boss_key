using BossKey.Models;

namespace BossKey.Services;

public static class HotkeyHideTargetResolver
{
    public static IEnumerable<WindowInfo> Resolve(
        IEnumerable<WindowInfo> windows,
        IEnumerable<HideRule> rules,
        RuleMatcher ruleMatcher)
        => windows.Where(window => window.IsSelected);
}
