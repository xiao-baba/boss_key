using BossKey.Models;

namespace BossKey.Services;

public sealed class RuleMatcher
{
    public bool IsMatch(WindowInfo window, IEnumerable<HideRule> rules)
        => rules.Any(rule => IsMatch(window, rule));

    public bool IsMatch(WindowInfo window, HideRule rule)
    {
        if (!rule.Enabled || string.IsNullOrWhiteSpace(rule.Pattern))
        {
            return false;
        }

        if (rule.MatchTitle && Matches(window.Title, rule))
        {
            return true;
        }

        if (rule.MatchProcessName && Matches(window.ProcessName, rule))
        {
            return true;
        }

        return rule.MatchProcessPath && Matches(window.ProcessPath, rule);
    }

    private static bool Matches(string? value, HideRule rule)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var comparison = rule.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return rule.MatchType switch
        {
            RuleMatchType.Equals => string.Equals(value, rule.Pattern, comparison),
            RuleMatchType.StartsWith => value.StartsWith(rule.Pattern, comparison),
            RuleMatchType.EndsWith => value.EndsWith(rule.Pattern, comparison),
            _ => value.Contains(rule.Pattern, comparison)
        };
    }
}
