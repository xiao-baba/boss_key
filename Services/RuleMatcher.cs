using System.IO;
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

        if (rule.MatchProcessName && MatchesProcessName(window.ProcessName, rule))
        {
            return true;
        }

        return rule.MatchProcessPath && Matches(window.ProcessPath, rule);
    }

    private static bool Matches(string? value, HideRule rule)
        => Matches(value, rule.Pattern, rule.MatchType, rule.CaseSensitive);

    private static bool MatchesProcessName(string? value, HideRule rule)
    {
        if (Matches(value, rule))
        {
            return true;
        }

        return Matches(
            NormalizeProcessName(value),
            NormalizeProcessName(rule.Pattern),
            rule.MatchType,
            rule.CaseSensitive);
    }

    private static bool Matches(string? value, string? pattern, RuleMatchType matchType, bool caseSensitive)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        value = value.Trim();
        pattern = pattern?.Trim();
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return matchType switch
        {
            RuleMatchType.Equals => string.Equals(value, pattern, comparison),
            RuleMatchType.StartsWith => value.StartsWith(pattern, comparison),
            RuleMatchType.EndsWith => value.EndsWith(pattern, comparison),
            _ => value.Contains(pattern, comparison)
        };
    }

    private static string? NormalizeProcessName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var normalized = value.Trim().Trim('"');
        var fileName = Path.GetFileName(normalized);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            normalized = fileName;
        }

        return normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? normalized[..^4]
            : normalized;
    }
}
