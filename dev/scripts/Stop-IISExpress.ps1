<#
.SYNOPSIS
.LANG en
Stops the Portal IIS Express process for a known port or generated config.

.LANG zh-CN
按指定端口或生成配置停止 Portal IIS Express 进程。

.DESCRIPTION
<lang>
  <en>Finds and stops IIS Express processes that match the requested port or the generated Portal applicationhost.config path. The stop scope is deliberately limited to avoid killing unrelated Visual Studio or IIS Express sessions. It does not remove site files, databases, logs, or external configuration.</en>
  <zh-CN>查找并停止匹配指定端口或 Portal 生成的 applicationhost.config 路径的 IIS Express 进程。停止范围被刻意限制，避免误杀无关的 Visual Studio 或 IIS Express 会话。本脚本不删除站点文件、数据库、日志或外置配置。</zh-CN>
</lang>

.PARAMETER Port
.LANG en
Local IIS Express port used as the primary process boundary.

.LANG zh-CN
作为主要进程边界的本地 IIS Express 端口。

.PARAMETER SitePath
.LANG en
Compatibility parameter kept for older task invocations; process matching does
not rely on the physical site path.

.LANG zh-CN
为旧任务调用保留的兼容参数；进程匹配不依赖物理站点路径。

.PARAMETER VirtualPath
.LANG en
Compatibility parameter for matching generated same-port virtual-directory
configuration.

.LANG zh-CN
用于匹配同端口虚拟目录生成配置的兼容参数。
#>
[CmdletBinding()]
param(
    [int]$Port = 40001,

    [string]$SitePath,

    [string]$VirtualPath = '/'
)

$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>以下状态只用于本次受限停止请求；SitePath 保留为兼容参数，不参与进程归属判定。</zh-CN>
#   <en>The state below serves only this bounded stop request; SitePath remains a compatibility parameter and does not determine process ownership.</en>
# </lang>
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

# <lang>
#   <zh-CN>SitePath 参数为既有命令兼容保留；停止范围严格由端口或同端口虚拟目录配置决定。</zh-CN>
#   <en>The SitePath parameter remains for command compatibility; the stop scope is strictly identified by port or the same-port virtual-directory configuration.</en>
# </lang>
$targets = Get-CimInstance Win32_Process -Filter "name = 'iisexpress.exe'" -ErrorAction SilentlyContinue |
    Where-Object {
        $_.CommandLine -match "/port:$Port(\s|$)" -or
        $_.CommandLine -match $escapedConfigPath
    }

if (-not $targets) {
    Write-Host "No matching IIS Express process found for port $Port or site path $SitePath."
    exit 0
}

# <lang>
#   <zh-CN>只强制停止已按端口或生成配置匹配的 IIS Express 进程，不清理站点目录、日志或配置文件。</zh-CN>
#   <en>Force-stops only IIS Express processes matched by port or generated config; it does not clean site directories, logs, or config files.</en>
# </lang>
foreach ($processInfo in $targets) {
    Write-Host "Stopping IIS Express PID $($processInfo.ProcessId)"
    Stop-Process -Id $processInfo.ProcessId -Force
}
