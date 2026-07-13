[CmdletBinding()]
param(
    [int]$Port = 40001,

    [string]$SitePath,

    [string]$VirtualPath = '/'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$VirtualPath = $VirtualPath.Trim()
if (-not $VirtualPath) {
    $VirtualPath = '/'
}
if (-not $VirtualPath.StartsWith('/')) {
    $VirtualPath = '/' + $VirtualPath
}
if ($VirtualPath.Length -gt 1) {
    $VirtualPath = $VirtualPath.TrimEnd('/')
}

$configPath = Join-Path $repoRoot "temp\iisexpress\applicationhost-$Port.config"
$escapedConfigPath = [regex]::Escape($configPath)

# SitePath 参数为既有命令兼容保留；停止范围严格由端口或同端口虚拟目录配置决定。
# The SitePath parameter remains for command compatibility; the stop scope is strictly identified by port or
# the same-port virtual-directory configuration.
$targets = Get-CimInstance Win32_Process -Filter "name = 'iisexpress.exe'" -ErrorAction SilentlyContinue |
    Where-Object {
        $_.CommandLine -match "/port:$Port(\s|$)" -or
        $_.CommandLine -match $escapedConfigPath
    }

if (-not $targets) {
    Write-Host "No matching IIS Express process found for port $Port or site path $SitePath."
    exit 0
}

foreach ($processInfo in $targets) {
    Write-Host "Stopping IIS Express PID $($processInfo.ProcessId)"
    Stop-Process -Id $processInfo.ProcessId -Force
}
