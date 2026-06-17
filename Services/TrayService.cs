using System.IO;
using Forms = System.Windows.Forms;

namespace BossKey.Services;

public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly System.Drawing.Icon? _icon;

    public TrayService()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("显示主界面", null, (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("隐藏匹配窗口", null, (_, _) => HideRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("恢复隐藏窗口", null, (_, _) => RestoreRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _icon = LoadTrayIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "老板键",
            Icon = _icon ?? System.Drawing.SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = menu
        };
        _notifyIcon.DoubleClick += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? ShowRequested;

    public event EventHandler? HideRequested;

    public event EventHandler? RestoreRequested;

    public event EventHandler? ExitRequested;

    public void ShowBalloon(string title, string message)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(1800);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon?.Dispose();
    }

    private static System.Drawing.Icon? LoadTrayIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BossKey.ico");
        if (!File.Exists(iconPath))
        {
            return null;
        }

        try
        {
            return new System.Drawing.Icon(iconPath);
        }
        catch
        {
            return null;
        }
    }
}
