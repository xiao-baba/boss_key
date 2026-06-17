$ErrorActionPreference = "Stop"

$dotnet = "E:\DevTools\dotnet\dotnet.exe"
$publishDir = "E:\BossKeyApp\publish"

if (-not (Test-Path $dotnet)) {
    throw "未找到 .NET SDK：$dotnet"
}

Get-Process BossKey -ErrorAction SilentlyContinue | Stop-Process -Force

& $dotnet build
& $dotnet publish -c Release -r win-x64 --self-contained true -o $publishDir

Write-Host "Published: $(Join-Path $publishDir 'BossKey.exe')"
