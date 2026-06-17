using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BossKey.Models;
using BossKey.Services;
using Microsoft.Win32;
using WpfMessageBox = System.Windows.MessageBox;

namespace BossKey;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private readonly StartupService _startupService = new();
    private readonly WindowManager _windowManager = new();
    private readonly RuleMatcher _ruleMatcher = new();
    private readonly HotkeyService _hotkeyService = new();
    private readonly ObservableCollection<WindowInfo> _windows = [];
    private readonly ObservableCollection<HideRule> _rules = [];
    private readonly HashSet<IntPtr> _selectedWindowHandles = [];

    private AppSettings _settings = new();
    private TrayService? _trayService;
    private bool _allowClose;
    private bool _isLoading;
    private HotkeyAction? _recordingHotkey;

    public ObservableCollection<WindowInfo> Windows => _windows;

    public ObservableCollection<HideRule> Rules => _rules;

    public RuleMatchType[] MatchTypes { get; } = Enum.GetValues<RuleMatchType>();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        _hotkeyService.HotkeyPressed += HotkeyService_HotkeyPressed;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        RegisterHotkeys(showSuccess: false);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _isLoading = true;
        _settings = _settingsService.Load();

        _rules.Clear();
        foreach (var rule in _settings.HideRules)
        {
            AttachRule(rule);
            _rules.Add(rule);
        }

        if (_rules.Count == 0)
        {
            AddRule(new HideRule { Pattern = "chrome", MatchTitle = false, MatchProcessName = true, MatchProcessPath = true });
        }

        _settings.StartWithWindows = _startupService.IsEnabled();
        StartWithWindowsCheckBox.IsChecked = _settings.StartWithWindows;
        ReduceMotionCheckBox.IsChecked = _settings.ReduceMotion;
        AdminHintCheckBox.IsChecked = _settings.ShowAdminHint;
        SelectThemeComboItem();
        UpdateHotkeyButtons();
        ApplyTheme();
        _isLoading = false;

        _trayService = new TrayService();
        _trayService.ShowRequested += (_, _) => ShowMainWindow();
        _trayService.HideRequested += (_, _) => HideWindowsByRules();
        _trayService.RestoreRequested += (_, _) => RestoreHiddenWindows();
        _trayService.ExitRequested += (_, _) => ExitApplication();

        RefreshWindows();
        RegisterHotkeys(showSuccess: false);
        ShowPage(WindowsPage, WindowsNavButton, "窗口", "勾选窗口后隐藏，或使用规则一键隐藏匹配窗口。");
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
        _trayService?.ShowBalloon("老板键仍在运行", "可以通过托盘菜单或恢复快捷键找回隐藏窗口。");
        SetStatus("已隐藏到托盘");
    }

    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_recordingHotkey is null)
        {
            return;
        }

        e.Handled = true;
        if (e.Key == Key.Escape)
        {
            _recordingHotkey = null;
            UpdateHotkeyButtons();
            SetStatus("已取消快捷键录制");
            return;
        }

        var gesture = HotkeyGesture.FromKeyEvent(e);
        if (!gesture.IsValid)
        {
            SetStatus("请按下包含 Ctrl、Alt、Shift 或 Win 的组合键");
            return;
        }

        if (_recordingHotkey == HotkeyAction.Hide)
        {
            _settings.HideHotkey = gesture;
        }
        else
        {
            _settings.RestoreHotkey = gesture;
        }

        _recordingHotkey = null;
        SaveSettings();
        UpdateHotkeyButtons();
        RegisterHotkeys(showSuccess: true);
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => RefreshWindows();

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
        {
            RefreshWindows();
        }
    }

    private void HideSelectedButton_Click(object sender, RoutedEventArgs e) => HideSelectedWindows();

    private void HideByRulesButton_Click(object sender, RoutedEventArgs e) => HideWindowsByRules();

    private void RestoreAllButton_Click(object sender, RoutedEventArgs e) => RestoreHiddenWindows();

    private void SelectAllWindowsButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var window in _windows)
        {
            window.IsSelected = true;
        }

        UpdateSelectedCount();
        SetStatus($"已选择 {_windows.Count} 个窗口");
    }

    private void ClearSelectedWindowsButton_Click(object sender, RoutedEventArgs e)
    {
        _selectedWindowHandles.Clear();
        foreach (var window in _windows)
        {
            window.IsSelected = false;
        }

        WindowsGrid.SelectedItems.Clear();
        UpdateSelectedCount();
        SetStatus("已清空选择");
    }

    private void AddRuleButton_Click(object sender, RoutedEventArgs e)
    {
        AddRule(new HideRule { Pattern = "请输入关键词" });
        SaveRules();
        SetStatus("已新增规则");
    }

    private void AddPathRuleButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "程序 (*.exe)|*.exe|所有文件 (*.*)|*.*",
            Title = "选择要匹配的程序"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        AddRuleForPath(dialog.FileName);
        SaveRules();
        ShowPage(RulesPage, RulesNavButton, "规则", "按进程名、路径或窗口标题配置隐藏规则。");
        SetStatus("已添加路径规则");
    }

    private void CreateRulesFromSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = _windows.Where(window => window.IsSelected).ToList();
        if (selected.Count == 0)
        {
            SetStatus("请先在窗口页勾选至少一个窗口");
            return;
        }

        foreach (var window in selected)
        {
            if (!string.IsNullOrWhiteSpace(window.ProcessPath))
            {
                AddRuleForPath(window.ProcessPath);
            }
            else
            {
                AddRuleForProcessName(window.ProcessName);
            }
        }

        SaveRules();
        ShowPage(RulesPage, RulesNavButton, "规则", "按进程名、路径或窗口标题配置隐藏规则。");
        SetStatus($"已从 {selected.Count} 个窗口生成规则");
    }

    private void DeleteSelectedRulesButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = RulesGrid.SelectedItems.Cast<HideRule>().ToList();
        if (selected.Count == 0)
        {
            SetStatus("请选择要删除的规则");
            return;
        }

        foreach (var rule in selected)
        {
            DetachRule(rule);
            _rules.Remove(rule);
        }

        SaveRules();
        SetStatus($"已删除 {selected.Count} 条规则");
    }

    private void SaveRulesButton_Click(object sender, RoutedEventArgs e)
    {
        SaveRules();
        SetStatus("规则已保存");
    }

    private void RulesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(SaveRules));
    }

    private void HideHotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        _recordingHotkey = HotkeyAction.Hide;
        HideHotkeyButton.Content = "按下组合键...";
        SetStatus("正在录制隐藏快捷键");
        Focus();
    }

    private void RestoreHotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        _recordingHotkey = HotkeyAction.Restore;
        RestoreHotkeyButton.Content = "按下组合键...";
        SetStatus("正在录制恢复快捷键");
        Focus();
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || ThemeComboBox.SelectedItem is not ComboBoxItem item || item.Tag is not string tag)
        {
            return;
        }

        if (Enum.TryParse(tag, out AppThemeMode theme))
        {
            _settings.ThemeMode = theme;
            ApplyTheme();
            SaveSettings();
        }
    }

    private void ReduceMotionCheckBox_Click(object sender, RoutedEventArgs e)
    {
        _settings.ReduceMotion = ReduceMotionCheckBox.IsChecked == true;
        SaveSettings();
        SetStatus(_settings.ReduceMotion ? "已减少动画" : "已启用轻量动画");
    }

    private void StartWithWindowsCheckBox_Click(object sender, RoutedEventArgs e)
    {
        var enabled = StartWithWindowsCheckBox.IsChecked == true;
        try
        {
            _startupService.SetEnabled(enabled);
            _settings.StartWithWindows = enabled;
            SaveSettings();
            SetStatus(enabled ? "已开启开机自启" : "已关闭开机自启");
        }
        catch (Exception ex)
        {
            StartWithWindowsCheckBox.IsChecked = _startupService.IsEnabled();
            SetStatus("开机自启设置失败");
            WpfMessageBox.Show(this, ex.Message, "开机自启设置失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void AdminHintCheckBox_Click(object sender, RoutedEventArgs e)
    {
        _settings.ShowAdminHint = AdminHintCheckBox.IsChecked == true;
        SaveSettings();
        SetStatus(_settings.ShowAdminHint ? "已开启权限提示" : "已关闭权限提示");
    }

    private void RestartAsAdminButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException("无法定位当前程序路径。");
            }

            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                Verb = "runas"
            });
            ExitApplication();
        }
        catch (Win32Exception)
        {
            SetStatus("已取消管理员重启");
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show(this, ex.Message, "管理员重启失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenConfigFolderButton_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_settingsService.ConfigDirectory);
        Process.Start(new ProcessStartInfo(_settingsService.ConfigDirectory) { UseShellExecute = true });
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e) => ExitApplication();

    private void WindowsNavButton_Click(object sender, RoutedEventArgs e)
        => ShowPage(WindowsPage, WindowsNavButton, "窗口", "勾选窗口后隐藏，或使用规则一键隐藏匹配窗口。");

    private void RulesNavButton_Click(object sender, RoutedEventArgs e)
        => ShowPage(RulesPage, RulesNavButton, "规则", "按进程名、路径或窗口标题配置隐藏规则。");

    private void HotkeysNavButton_Click(object sender, RoutedEventArgs e)
        => ShowPage(HotkeysPage, HotkeysNavButton, "快捷键", "设置一键隐藏和恢复的全局快捷键。");

    private void SettingsNavButton_Click(object sender, RoutedEventArgs e)
        => ShowPage(SettingsPage, SettingsNavButton, "设置", "管理外观、启动和权限提示。");

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            MaximizeButton_Click(sender, e);
            return;
        }

        DragMove();
    }

    private void HotkeyService_HotkeyPressed(object? sender, HotkeyAction action)
    {
        Dispatcher.Invoke(() =>
        {
            if (action == HotkeyAction.Hide)
            {
                HideSelectedFromHotkey();
            }
            else
            {
                RestoreHiddenWindows();
            }
        });
    }

    private void RefreshWindows()
    {
        foreach (var oldWindow in _windows)
        {
            oldWindow.PropertyChanged -= WindowInfo_PropertyChanged;
        }

        _windows.Clear();
        var query = SearchBox.Text?.Trim();
        var openWindows = _windowManager.GetOpenWindows();
        var openHandles = openWindows.Select(window => window.Handle).ToHashSet();
        _selectedWindowHandles.RemoveWhere(handle => !openHandles.Contains(handle) && !_windowManager.IsHiddenWindow(handle));

        foreach (var window in openWindows)
        {
            if (!MatchesSearch(window, query))
            {
                continue;
            }

            window.IsSelected = _selectedWindowHandles.Contains(window.Handle);
            window.PropertyChanged += WindowInfo_PropertyChanged;
            _windows.Add(window);
        }

        WindowsGrid.SelectedItems.Clear();
        UpdateSelectedCount();
        AnimateElement(WindowsGrid);
        SetStatus($"已刷新 {_windows.Count} 个窗口");
    }

    private static bool MatchesSearch(WindowInfo window, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return Contains(window.Title, query)
            || Contains(window.ProcessName, query)
            || Contains(window.ProcessPath, query);
    }

    private static bool Contains(string? value, string query)
        => value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;

    private void WindowInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WindowInfo.IsSelected))
        {
            if (sender is WindowInfo window)
            {
                if (window.IsSelected)
                {
                    _selectedWindowHandles.Add(window.Handle);
                }
                else
                {
                    _selectedWindowHandles.Remove(window.Handle);
                }
            }

            UpdateSelectedCount();
        }
    }

    private void UpdateSelectedCount()
    {
        var count = _windows.Count(window => window.IsSelected);
        SelectedCountText.Text = $"已选择 {count} 个窗口";
    }

    private void HideSelectedFromHotkey()
    {
        var selected = _windows.Where(window => window.IsSelected).ToList();
        if (selected.Count == 0)
        {
            SetStatus("没有勾选窗口；如需按规则隐藏，请点击“按规则隐藏”");
            return;
        }

        HideWindows(selected);
    }

    private void HideSelectedWindows()
    {
        var selected = _windows.Where(window => window.IsSelected).ToList();
        if (selected.Count == 0)
        {
            SetStatus("请先勾选要隐藏的窗口");
            return;
        }

        HideWindows(selected);
    }

    private void HideWindowsByRules()
    {
        SaveRules();
        var targets = _windowManager.GetOpenWindows()
            .Where(window => _ruleMatcher.IsMatch(window, _rules))
            .ToList();

        if (targets.Count == 0)
        {
            SetStatus("没有匹配规则的可见窗口");
            return;
        }

        HideWindows(targets);
    }

    private int HideWindows(IReadOnlyCollection<WindowInfo> targets)
    {
        var hidden = 0;
        var restrictedCount = 0;
        foreach (var target in targets)
        {
            if (target.IsElevatedOrInaccessible)
            {
                restrictedCount++;
            }

            if (_windowManager.HideWindow(target.Handle))
            {
                hidden++;
            }
        }

        RefreshWindows();
        UpdateHiddenCount();

        if (hidden == 0)
        {
            SetStatus("没有窗口被隐藏");
            return hidden;
        }

        var message = $"已隐藏 {hidden} 个窗口";
        if (restrictedCount > 0 && _settings.ShowAdminHint)
        {
            message += $"，其中 {restrictedCount} 个可能需要管理员权限";
        }

        SetStatus(message);
        _trayService?.ShowBalloon("老板键", message);
        return hidden;
    }

    private void RestoreHiddenWindows()
    {
        var restored = _windowManager.RestoreHiddenWindows();
        RefreshWindows();
        UpdateHiddenCount();
        SetStatus(restored > 0 ? $"已恢复 {restored} 个窗口" : "没有需要恢复的窗口");
    }

    private void UpdateHiddenCount()
    {
        StatusBadgeText.Text = $"{_windowManager.HiddenWindowCount} 个隐藏窗口";
        PulseElement(StatusBadge);
    }

    private void AddRuleForPath(string path)
    {
        if (_rules.Any(rule => rule.MatchProcessPath
            && string.Equals(rule.Pattern, path, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        AddRule(new HideRule
        {
            Pattern = path,
            MatchType = RuleMatchType.Equals,
            MatchTitle = false,
            MatchProcessName = false,
            MatchProcessPath = true
        });
    }

    private void AddRuleForProcessName(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)
            || _rules.Any(rule => rule.MatchProcessName
                && string.Equals(rule.Pattern, processName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        AddRule(new HideRule
        {
            Pattern = processName,
            MatchType = RuleMatchType.Equals,
            MatchTitle = false,
            MatchProcessName = true,
            MatchProcessPath = false
        });
    }

    private void AddRule(HideRule rule)
    {
        AttachRule(rule);
        _rules.Add(rule);
    }

    private void AttachRule(HideRule rule)
    {
        rule.PropertyChanged -= HideRule_PropertyChanged;
        rule.PropertyChanged += HideRule_PropertyChanged;
    }

    private void DetachRule(HideRule rule)
    {
        rule.PropertyChanged -= HideRule_PropertyChanged;
    }

    private void HideRule_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        SaveRules();
    }

    private void SaveRules()
    {
        _settings.HideRules = _rules.ToList();
        SaveSettings();
    }

    private void SaveSettings()
    {
        _settingsService.Save(_settings);
    }

    private void RegisterHotkeys(bool showSuccess)
    {
        if (!IsInitialized)
        {
            return;
        }

        if (_hotkeyService.Register(this, _settings.HideHotkey, _settings.RestoreHotkey, out var error))
        {
            if (showSuccess)
            {
                SetStatus("快捷键已更新");
            }
        }
        else
        {
            SetStatus(error ?? "快捷键注册失败");
        }
    }

    private void UpdateHotkeyButtons()
    {
        HideHotkeyButton.Content = _settings.HideHotkey.ToString();
        RestoreHotkeyButton.Content = _settings.RestoreHotkey.ToString();
    }

    private void ShowMainWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        RefreshWindows();
    }

    private void ExitApplication()
    {
        SaveRules();
        _allowClose = true;
        _hotkeyService.Dispose();
        _trayService?.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    private void SetStatus(string message)
    {
        SidebarStatusText.Text = message;
        PulseElement(SidebarStatusCard);
    }

    private void ShowPage(Grid page, System.Windows.Controls.Button selectedButton, string title, string subtitle)
    {
        foreach (var candidate in new[] { WindowsPage, RulesPage, HotkeysPage, SettingsPage })
        {
            candidate.Visibility = candidate == page ? Visibility.Visible : Visibility.Collapsed;
        }

        foreach (var button in new[] { WindowsNavButton, RulesNavButton, HotkeysNavButton, SettingsNavButton })
        {
            button.Tag = button == selectedButton ? "Selected" : null;
        }

        PageTitleText.Text = title;
        PageSubtitleText.Text = subtitle;
        AnimateElement(page, 5);
    }

    private void AnimateElement(UIElement element, double offsetY = 4)
    {
        if (_settings.ReduceMotion || !SystemParameters.ClientAreaAnimation)
        {
            element.BeginAnimation(OpacityProperty, null);
            element.Opacity = 1;
            element.RenderTransform = new TranslateTransform(0, 0);
            return;
        }

        element.Opacity = 0;
        element.RenderTransform = new TranslateTransform(0, offsetY);

        var opacity = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(145))
        {
            EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
        };
        var offset = new DoubleAnimation(offsetY, 0, TimeSpan.FromMilliseconds(165))
        {
            EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
        };

        element.BeginAnimation(OpacityProperty, opacity);
        ((TranslateTransform)element.RenderTransform).BeginAnimation(TranslateTransform.YProperty, offset);
    }

    private void PulseElement(UIElement element)
    {
        if (_settings.ReduceMotion || !SystemParameters.ClientAreaAnimation)
        {
            element.BeginAnimation(OpacityProperty, null);
            element.Opacity = 1;
            return;
        }

        var pulse = new DoubleAnimation(0.72, 1, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        element.BeginAnimation(OpacityProperty, pulse);
    }

    private void SelectThemeComboItem()
    {
        foreach (ComboBoxItem item in ThemeComboBox.Items)
        {
            if (string.Equals(item.Tag?.ToString(), _settings.ThemeMode.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                ThemeComboBox.SelectedItem = item;
                return;
            }
        }

        ThemeComboBox.SelectedIndex = 0;
    }

    private void ApplyTheme()
    {
        var dark = _settings.ThemeMode switch
        {
            AppThemeMode.Dark => true,
            AppThemeMode.Light => false,
            _ => IsSystemDarkMode()
        };

        SetBrush("WindowBackgroundBrush", dark ? "#111419" : "#F6F8FB");
        SetBrush("ChromeBrush", dark ? "#191D24" : "#EEF3F8");
        SetBrush("PanelBrush", dark ? "#222832" : "#FAFCFF");
        SetBrush("PanelMutedBrush", dark ? "#2A303B" : "#F2F5F9");
        SetBrush("PanelHoverBrush", dark ? "#2C3441" : "#F7FAFE");
        SetBrush("PanelPressedBrush", dark ? "#343C49" : "#E9F0F8");
        SetBrush("SidebarBrush", dark ? "#1E242D" : "#F1F5FA");
        SetBrush("BorderBrush", dark ? "#3B4452" : "#D9E1EC");
        SetBrush("SoftBorderBrush", dark ? "#303847" : "#E7ECF3");
        SetBrush("TextBrush", dark ? "#EEF3F8" : "#202631");
        SetBrush("MutedTextBrush", dark ? "#A7B2C1" : "#667285");
        SetBrush("AccentBrush", dark ? "#65A9FF" : "#2576E8");
        SetBrush("AccentSoftBrush", dark ? "#233D61" : "#E5F0FF");
        SetBrush("RowSelectedBrush", dark ? "#263B56" : "#EDF5FF");
    }

    private static void SetBrush(string key, string color)
    {
        var nextBrush = new SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
        nextBrush.Freeze();
        System.Windows.Application.Current.Resources[key] = nextBrush;
    }

    private static bool IsSystemDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }
}
