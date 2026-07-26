<#
.SYNOPSIS
Stops the Portal IIS Express process for a known port or generated config.

.LANG en
Finds and stops IIS Express processes that match the requested port or the
generated Portal applicationhost.config path. The stop scope is deliberately
limited to avoid killing unrelated Visual Studio or IIS Express sessions. It
does not remove site files, databases, logs, or external configuration.

.LANG zh-CN
查找并停止匹配指定端口或 Portal 生成的 applicationhost.config 路径的 IIS Express
进程。停止范围被刻意限制，避免误杀无关的 Visual Studio 或 IIS Express 会话。
本脚本不删除站点文件、数据库、日志或外置配置。

.PARAMETER Port
Local IIS Express port used as the primary process boundary.

.PARAMETER SitePath
Compatibility parameter kept for older task invocations; process matching does
not rely on the physical site path.

.PARAMETER VirtualPath
Compatibility parameter for matching generated same-port virtual-directory
configuration.
#>
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
