# 老板键

一个 Windows 10/11 桌面老板键工具，使用 C# WPF 和 Win32 API 实现窗口枚举、多选隐藏、规则隐藏、全局快捷键恢复、托盘常驻和可选开机自启。

## 功能

- 列出当前可见顶层窗口，支持搜索和多选隐藏。
- 支持按窗口标题、进程名、程序路径配置隐藏规则。
- 支持从已选窗口生成规则，或手动选择 exe 路径。
- 支持全局隐藏/恢复快捷键，默认 `Ctrl + Alt + H` 和 `Ctrl + Alt + R`。
- 关闭主窗口后进入系统托盘，托盘菜单可显示、隐藏、恢复和退出。
- 设置保存在 `%AppData%\BossKey\settings.json`。
- UI 采用 mac 风格侧边栏、圆角面板、浅/深色主题和轻量动画。

## 开发和运行

当前项目目标框架是 `net10.0-windows`，需要安装 .NET 10 SDK 或更新版本的受支持 SDK。

```powershell
dotnet restore
dotnet run
```

发布 Windows x64 自包含版本：

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

## 权限说明

程序默认以普通权限运行。对于以管理员权限运行的窗口，系统可能限制普通进程操作；遇到权限受限窗口时，可在设置页选择“以管理员身份重启”。
