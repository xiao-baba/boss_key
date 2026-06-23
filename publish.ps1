param(
    [string]$PublishDir = 'E:\BossKeyApp',
    [string]$DotnetPath = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($DotnetPath)) {
    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnetCommand) {
        $DotnetPath = $dotnetCommand.Source
    }
}

if ([string]::IsNullOrWhiteSpace($DotnetPath) -or -not (Test-Path $DotnetPath)) {
    $fallbackDotnet = 'E:\DevTools\dotnet\dotnet.exe'
    if (Test-Path $fallbackDotnet) {
        $DotnetPath = $fallbackDotnet
    }
}

if (-not (Test-Path $DotnetPath)) {
    throw 'dotnet SDK not found. Install .NET 10 SDK or pass -DotnetPath.'
}

Get-Process BossKey -ErrorAction SilentlyContinue | Stop-Process -Force

New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

& $DotnetPath build
& $DotnetPath publish -c Release -r win-x64 --self-contained true -o $PublishDir

$publishedExe = Join-Path -Path $PublishDir -ChildPath 'BossKey.exe'
Write-Host ('Published: ' + $publishedExe)
