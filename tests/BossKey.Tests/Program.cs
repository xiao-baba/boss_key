using BossKey.Models;
using BossKey.Services;

internal static class Program
{
    public static int Main()
    {
        var tests = new (string Name, Action Body)[]
        {
            ("hotkey with no selected windows does not use matching rules", HotkeyDoesNotUseRulesWhenNothingIsSelected),
            ("hotkey prefers selected windows over rules", HotkeyPrefersSelectedWindows),
            ("process name equals ignores optional exe suffix", ProcessNameEqualsIgnoresExeSuffix),
            ("process name equals trims user input", ProcessNameEqualsTrimsUserInput),
            ("restricted hide failure explains admin requirement", RestrictedHideFailureExplainsAdminRequirement)
        };

        var failures = new List<string>();
        foreach (var test in tests)
        {
            try
            {
                test.Body();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception ex)
            {
                failures.Add($"{test.Name}: {ex.Message}");
                Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
            }
        }

        if (failures.Count == 0)
        {
            return 0;
        }

        Console.WriteLine();
        Console.WriteLine($"{failures.Count} test(s) failed.");
        return 1;
    }

    private static void HotkeyDoesNotUseRulesWhenNothingIsSelected()
    {
        var matching = Window("GameAssist");
        var other = Window("Explorer");
        var rules = new[]
        {
            new HideRule
            {
                Pattern = "GameAssist",
                MatchType = RuleMatchType.Equals,
                MatchTitle = false,
                MatchProcessName = true,
                MatchProcessPath = false
            }
        };

        var targets = HotkeyHideTargetResolver.Resolve([matching, other], rules, new RuleMatcher()).ToList();

        AssertSequence([], targets);
    }

    private static void HotkeyPrefersSelectedWindows()
    {
        var selected = Window("Explorer", selected: true);
        var matchingRule = Window("GameAssist");
        var rules = new[]
        {
            new HideRule
            {
                Pattern = "GameAssist",
                MatchType = RuleMatchType.Equals,
                MatchTitle = false,
                MatchProcessName = true,
                MatchProcessPath = false
            }
        };

        var targets = HotkeyHideTargetResolver.Resolve([selected, matchingRule], rules, new RuleMatcher()).ToList();

        AssertSequence([selected], targets);
    }

    private static void ProcessNameEqualsIgnoresExeSuffix()
    {
        var matcher = new RuleMatcher();
        var rule = new HideRule
        {
            Pattern = "GameAssist.exe",
            MatchType = RuleMatchType.Equals,
            MatchTitle = false,
            MatchProcessName = true,
            MatchProcessPath = false
        };

        AssertTrue(matcher.IsMatch(Window("GameAssist"), rule), "expected GameAssist to match GameAssist.exe");
    }

    private static void ProcessNameEqualsTrimsUserInput()
    {
        var matcher = new RuleMatcher();
        var rule = new HideRule
        {
            Pattern = "  GameAssist  ",
            MatchType = RuleMatchType.Equals,
            MatchTitle = false,
            MatchProcessName = true,
            MatchProcessPath = false
        };

        AssertTrue(matcher.IsMatch(Window("GameAssist"), rule), "expected surrounding spaces to be ignored");
    }

    private static void RestrictedHideFailureExplainsAdminRequirement()
    {
        var message = HideOperationStatus.Create(hidden: 0, restrictedCount: 1, showAdminHint: true);

        AssertTrue(message.Contains("管理员", StringComparison.Ordinal), "expected admin hint for restricted hide failure");
    }

    private static WindowInfo Window(string processName, bool selected = false)
    {
        var window = new WindowInfo
        {
            Handle = (IntPtr)processName.GetHashCode(StringComparison.Ordinal),
            Title = $"{processName} Window",
            ProcessName = processName,
            ProcessPath = $@"C:\Program Files\{processName}\{processName}.exe",
            ProcessId = Math.Abs(processName.GetHashCode(StringComparison.Ordinal))
        };
        window.IsSelected = selected;
        return window;
    }

    private static void AssertSequence(IReadOnlyList<WindowInfo> expected, IReadOnlyList<WindowInfo> actual)
    {
        AssertTrue(expected.Count == actual.Count, $"expected {expected.Count} target(s), got {actual.Count}");
        for (var i = 0; i < expected.Count; i++)
        {
            AssertTrue(ReferenceEquals(expected[i], actual[i]), $"target {i} did not match expected window");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
