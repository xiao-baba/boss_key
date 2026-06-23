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

## 日常使用

1. 启动 `BossKey.exe` 后，程序会列出当前可见窗口。
2. 在“窗口”页可以勾选窗口，然后点击“隐藏选中”。
3. 在“规则”页可以新增规则，按窗口标题、程序名或程序路径自动匹配。
4. 点击“按规则隐藏”会隐藏所有匹配当前规则的可见窗口。
5. 使用隐藏快捷键时，只会隐藏当前勾选窗口；没有勾选窗口时不会按规则隐藏。
6. 点击“恢复隐藏”或使用恢复快捷键，会恢复本程序隐藏过的窗口。
7. 关闭主窗口不会退出程序，程序会进入系统托盘；需要完全退出时使用托盘菜单或设置页的退出按钮。

## 规则配置说明

规则页字段说明：

- `启用`：关闭后该规则不会参与匹配。
- `关键词/路径`：要匹配的窗口标题、程序名或 exe 路径。
- `匹配`：支持 `Contains`、`Equals`、`StartsWith`、`EndsWith`。
- `标题`：匹配窗口标题。
- `程序名`：匹配进程名，例如 `GameAssist`。程序名匹配会兼容 `GameAssist.exe` 这种写法。
- `路径`：匹配完整程序路径，例如 `C:\Program Files\App\App.exe`。
- `区分大小写`：默认不区分大小写，只有特殊需求时再开启。

推荐做法：

- 想隐藏某个软件的所有窗口，优先使用“程序名 + Equals”，例如 `GameAssist`。
- 想只隐藏某个安装路径下的程序，使用“路径 + Equals”。
- 想按网页标题、文档标题等内容匹配，使用“标题 + Contains”。
- 从窗口页勾选目标窗口后点击“生成规则”，可以自动生成更准确的路径或程序名规则。

## 开发和运行

当前项目目标框架是 `net10.0-windows`，需要安装 .NET 10 SDK 或更新版本的受支持 SDK。

```powershell
dotnet restore
dotnet run
```

## 发布到 `E:\BossKeyApp`

仓库内提供了 `publish.ps1`，默认会发布 Windows x64 自包含版本到 `E:\BossKeyApp`。

在项目根目录执行：

```powershell
.\publish.ps1
```

发布完成后，主程序路径是：

```text
E:\BossKeyApp\BossKey.exe
```

如果 PowerShell 提示脚本执行策略限制，可以使用：

```powershell
powershell -ExecutionPolicy Bypass -File .\publish.ps1
```

如果没有把 `dotnet` 加入环境变量，也可以手动指定 .NET SDK 路径：

```powershell
.\publish.ps1 -DotnetPath "E:\DevTools\dotnet\dotnet.exe"
```

如果想发布到其他目录：

```powershell
.\publish.ps1 -PublishDir "D:\Apps\BossKey"
```

也可以不用脚本，直接执行 `dotnet publish`：

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o "E:\BossKeyApp"
```

发布脚本会先尝试关闭正在运行的 `BossKey` 进程，避免 exe 被占用导致覆盖失败。

## 发布后运行和升级

首次发布后，双击 `E:\BossKeyApp\BossKey.exe` 即可运行。

后续升级时：

1. 关闭正在运行的 BossKey，或直接使用 `.\publish.ps1` 让脚本自动关闭。
2. 重新执行发布命令。
3. 再次启动 `E:\BossKeyApp\BossKey.exe`。

用户设置保存在 `%AppData%\BossKey\settings.json`，重新发布不会覆盖现有规则和快捷键配置。

## 权限说明

程序默认以普通权限运行。对于以管理员权限运行的窗口，系统可能限制普通进程操作；遇到权限受限窗口时，可在设置页选择“以管理员身份重启”。

窗口列表中的“受限”勾选表示当前程序无法完整读取该窗口所属进程信息。此类窗口可能仍能隐藏；如果状态栏提示需要管理员权限，请以管理员身份重启 BossKey 后再隐藏。
