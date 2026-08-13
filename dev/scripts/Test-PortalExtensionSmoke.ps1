<#
.SYNOPSIS
.LANG en
Runs the portal extension smoke suite.

.LANG zh-CN
运行门户扩展 smoke 套件。

.DESCRIPTION
<lang>
  <en>Orchestrate build, asset, provider proof, HIA boundary proof, root-site smoke, virtual-directory smoke, optional SQL compatibility, optional admin smoke, and optional theme/cache mutation proofs. Mutating checks require an explicit external Web configuration tree and are isolated to dedicated IIS Express ports.</en>
  <zh-CN>编排构建、前端资产、provider proof、HIA 边界 proof、根站点 smoke、虚拟目录 smoke、可选 SQL 兼容、可选管理员 smoke、可选主题/缓存变更 proof。会变更状态的检查必须提供显式外置 Web 配置树，并隔离在专用 IIS Express 端口上执行。</zh-CN>
</lang>

.PARAMETER Configuration
.LANG en
Build configuration used by the smoke suite when build steps are enabled.

.LANG zh-CN
启用构建步骤时 smoke 套件使用的构建配置。

.PARAMETER BaseUrl
.LANG en
Root portal URL used by HTTP smoke checks.

.LANG zh-CN
HTTP smoke 检查使用的根门户 URL。

.PARAMETER StartIISExpress
.LANG en
Allows child smoke checks to start and stop an IIS Express root site.

.LANG zh-CN
允许子 smoke 检查启动并关闭 IIS Express 根站点。

.PARAMETER SkipBuild
.LANG en
Skips Debug and Release solution build checks.

.LANG zh-CN
跳过 Debug 和 Release 解决方案构建检查。

.PARAMETER SkipAssets
.LANG en
Skips front-end asset build and npm audit checks.

.LANG zh-CN
跳过前端资产构建和 npm audit 检查。

.PARAMETER SkipVirtualDirectory
.LANG en
Skips the virtual-directory IIS Express smoke check.

.LANG zh-CN
跳过 IIS Express 虚拟目录 smoke 检查。

.PARAMETER VirtualDirectoryPort
.LANG en
IIS Express port for the isolated virtual-directory smoke check.

.LANG zh-CN
隔离虚拟目录 smoke 检查使用的 IIS Express 端口。

.PARAMETER VirtualPath
.LANG en
Virtual path mounted for the virtual-directory smoke check.

.LANG zh-CN
虚拟目录 smoke 检查挂载的虚拟路径。

.PARAMETER IncludeSqlCompatibility
.LANG en
Includes SQL compatibility checks that require an external connectionStrings.config.

.LANG zh-CN
纳入需要外置 connectionStrings.config 的 SQL 兼容检查。

.PARAMETER ConnectionStringsConfigPath
.LANG en
External connection string config file used by optional SQL, theme, and cache checks.

.LANG zh-CN
可选 SQL、主题和缓存检查使用的外置连接串配置文件。

.PARAMETER IncludeAdmin
.LANG en
Includes authenticated administrator smoke checks.

.LANG zh-CN
纳入已认证管理员 smoke 检查。

.PARAMETER AdminUser
.LANG en
Administrator account name used when IncludeAdmin is enabled.

.LANG zh-CN
启用 IncludeAdmin 时使用的管理员账号名。

.PARAMETER AdminPassword
.LANG en
Administrator password as a SecureString used only by the child smoke check.

.LANG zh-CN
以 SecureString 传入的管理员密码，只交给子 smoke 检查使用。

.PARAMETER IncludeThemeMutation
.LANG en
Includes isolated theme resolution mutation proof.

.LANG zh-CN
纳入隔离主题解析变更 proof。

.PARAMETER ThemeProofPort
.LANG en
IIS Express port used by the theme mutation proof.

.LANG zh-CN
主题变更 proof 使用的 IIS Express 端口。

.PARAMETER IncludeCacheMutation
.LANG en
Includes isolated module cache mutation proof.

.LANG zh-CN
纳入隔离模块缓存变更 proof。

.PARAMETER CacheProofPort
.LANG en
IIS Express port used by the module cache mutation proof.

.LANG zh-CN
模块缓存变更 proof 使用的 IIS Express 端口。
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [ValidatePattern('^https?://')]
    [string]$BaseUrl = 'http://localhost:40001/',

    [switch]$StartIISExpress,

    [switch]$SkipBuild,

    [switch]$SkipAssets,

    [switch]$SkipVirtualDirectory,

    [ValidateRange(1025, 65535)]
    [int]$VirtualDirectoryPort = 40003,

    [string]$VirtualPath = '/Portal',

    [switch]$IncludeSqlCompatibility,

    [string]$ConnectionStringsConfigPath,

    [switch]$IncludeAdmin,

    [string]$AdminUser,

    [SecureString]$AdminPassword,

    [switch]$IncludeThemeMutation,

    [ValidateRange(1025, 65535)]
    [int]$ThemeProofPort = 40005,

    [switch]$IncludeCacheMutation,

    [ValidateRange(1025, 65535)]
    [int]$CacheProofPort = 40004
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$pwsh = 'C:\Program Files\PowerShell\7\pwsh.exe'
$summary = New-Object 'System.Collections.Generic.List[string]'
$virtualDirectoryStarted = $false

# <lang>
#   <zh-CN>运行受控 PowerShell 子 smoke，传播非零退出并只在成功后记录完成项；不吞掉失败。</zh-CN>
#   <en>Run a controlled PowerShell child smoke, propagate non-zero exits, and record completion only after success.</en>
# </lang>
function Invoke-ChildPowerShell {
    param(
        [string]$Name,
        [string]$ScriptPath,
        [string[]]$Arguments = @()
    )

    Write-Host ('[RUN] ' + $Name)
    & $pwsh -NoLogo -NoProfile -ExecutionPolicy Bypass -File $ScriptPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw ($Name + ' failed with exit code ' + $LASTEXITCODE + '.')
    }

    $summary.Add($Name)
    Write-Host ('[PASS] ' + $Name)
}

# <lang>
#   <zh-CN>检查隔离 IIS Express 端口是否空闲，避免接管已有调试站点或错误归属进程。</zh-CN>
#   <en>Check that an isolated IIS Express port is free without taking ownership of an existing debug site or process.</en>
# </lang>
function Test-AvailablePort {
    param([int]$Port)

    $listening = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($listening) {
        throw "The requested isolated IIS Express port $Port is already in use."
    }
}

# <lang>
#   <zh-CN>仅在可选 SQL/主题/缓存检查启用时要求外置配置文件，并保持连接串不进入日志。</zh-CN>
#   <en>Require an external configuration file only when optional SQL/theme/cache checks are enabled, keeping connection strings out of logs.</en>
# </lang>
if ($IncludeSqlCompatibility -or $IncludeThemeMutation -or $IncludeCacheMutation) {
    if ([string]::IsNullOrWhiteSpace($ConnectionStringsConfigPath)) {
        throw 'ConnectionStringsConfigPath is required for SQL compatibility or cache-mutation checks.'
    }

    if (-not (Test-Path -LiteralPath $ConnectionStringsConfigPath -PathType Leaf)) {
        throw 'ConnectionStringsConfigPath does not exist.'
    }
}

# <lang>
#   <zh-CN>管理员 smoke 必须显式提供账号名；密码保持 SecureString 并只传给子检查。</zh-CN>
#   <en>Administrator smoke requires an explicit account name; the password remains SecureString and is passed only to the child check.</en>
# </lang>
if ($IncludeAdmin -and [string]::IsNullOrWhiteSpace($AdminUser)) {
    throw 'AdminUser is required when IncludeAdmin is specified.'
}

# <lang>
#   <zh-CN>主题/缓存变更 proof 只能使用显式外置 Web 配置树，防止把仓库配置或生产树误作隔离环境。</zh-CN>
#   <en>Theme/cache mutation proofs may use only an explicit external Web configuration tree so repository or production trees are not misused as isolation targets.</en>
# </lang>
if (($IncludeThemeMutation -or $IncludeCacheMutation) -and -not $ConnectionStringsConfigPath.Contains('\Web\HIA-ASPNETPortal\')) {
    throw 'Theme and cache mutation checks are restricted to the explicit external Web configuration tree.'
}

# <lang>
#   <zh-CN>按开关编排构建、资产、provider/HIA、站点和可选变更 proof；真实运行仅在调用方显式授权时发生。</zh-CN>
#   <en>Orchestrate build, asset, provider/HIA, site, and optional mutation proofs by switch; real execution occurs only with explicit caller authorization.</en>
# </lang>
try {
    if (-not $SkipBuild) {
        Invoke-ChildPowerShell -Name 'Debug solution build' -ScriptPath (Join-Path $PSScriptRoot 'Build-Solution.ps1') -Arguments @('-Configuration', 'Debug', '-Platform', 'Any CPU')
        Invoke-ChildPowerShell -Name 'Release solution build' -ScriptPath (Join-Path $PSScriptRoot 'Build-Solution.ps1') -Arguments @('-Configuration', 'Release', '-Platform', 'Any CPU')
    }

    if (-not $SkipAssets) {
        Write-Host '[RUN] Frontend assets build'
        Push-Location (Join-Path $repoRoot 'src\Portal')
        try {
            & npm run assets:build
            if ($LASTEXITCODE -ne 0) {
                throw 'Frontend assets build failed.'
            }

            # <lang>
            #   <zh-CN>本机可能将默认 registry 指向不实现 audit API 的镜像；审计固定走官方端点，避免把镜像能力误判成依赖风险。</zh-CN>
            #   <en>Some local registries do not implement the audit API. Use the official endpoint so mirror capability is not mistaken for dependency risk.</en>
            # </lang>
            & npm audit --audit-level=moderate --registry=https://registry.npmjs.org
            if ($LASTEXITCODE -ne 0) {
                throw 'npm audit reported a moderate-or-higher vulnerability.'
            }
        }
        finally {
            Pop-Location
        }

        $summary.Add('Frontend assets build and npm audit')
        Write-Host '[PASS] Frontend assets build and npm audit'
    }

    Invoke-ChildPowerShell -Name 'SQLite provider proof Debug' -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalDataProvider.ps1') -Arguments @('-Configuration', 'Debug')
    Invoke-ChildPowerShell -Name 'SQLite provider proof Release' -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalDataProvider.ps1') -Arguments @('-Configuration', 'Release')
    Invoke-ChildPowerShell -Name 'HIA boundary proof Debug' -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalHiaBoundary.ps1') -Arguments @('-Configuration', 'Debug')
    Invoke-ChildPowerShell -Name 'HIA boundary proof Release' -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalHiaBoundary.ps1') -Arguments @('-Configuration', 'Release')

    $rootSmokeArguments = @('-BaseUrl', $BaseUrl, '-SkipAuthenticated', '-CheckGenericErrorPage')
    if ($StartIISExpress) {
        # <lang>
        #   <zh-CN>仅在本脚本实际拉起根站点时关闭该实例；已有调试站点保持不受影响。</zh-CN>
        #   <en>Stop only an instance started by this script; leave an existing debugging site untouched.</en>
        # </lang>
        $rootSmokeArguments += '-StartIISExpress'
        $rootSmokeArguments += '-StopWhenComplete'
    }
    Invoke-ChildPowerShell -Name 'Root-site anonymous smoke' -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalSmoke.ps1') -Arguments $rootSmokeArguments

    if (-not $SkipVirtualDirectory) {
        Test-AvailablePort -Port $VirtualDirectoryPort
        Invoke-ChildPowerShell -Name 'Virtual-directory IIS Express start' -ScriptPath (Join-Path $PSScriptRoot 'Start-IISExpress.ps1') -Arguments @('-Port', $VirtualDirectoryPort, '-VirtualPath', $VirtualPath)
        $virtualDirectoryStarted = $true
        $virtualBaseUrl = 'http://localhost:' + $VirtualDirectoryPort + $VirtualPath.TrimEnd('/') + '/'
        Invoke-ChildPowerShell -Name 'Virtual-directory anonymous smoke' -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalSmoke.ps1') -Arguments @('-BaseUrl', $virtualBaseUrl, '-SkipAuthenticated', '-CheckGenericErrorPage')
    }

    if ($IncludeSqlCompatibility) {
        Invoke-ChildPowerShell -Name 'SQL Server P3 schema preflight' -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalSqlCompatibility.ps1') -Arguments @('-ConnectionStringsConfigPath', $ConnectionStringsConfigPath, '-RequireP2Migrations', '-RequireP3Migrations')
    }

    if ($IncludeAdmin) {
        Write-Host '[RUN] Authenticated administrator smoke'
        & (Join-Path $PSScriptRoot 'Test-PortalSmoke.ps1') -BaseUrl $BaseUrl -AdminUser $AdminUser -AdminPassword $AdminPassword -CheckGenericErrorPage
        $summary.Add('Authenticated administrator smoke')
        Write-Host '[PASS] Authenticated administrator smoke'
    }

    if ($IncludeThemeMutation) {
        Invoke-ChildPowerShell -Name 'Theme resolution isolation proof' -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalThemeResolution.ps1') -Arguments @('-ConnectionStringsConfigPath', $ConnectionStringsConfigPath, '-Port', $ThemeProofPort)
    }

    if ($IncludeCacheMutation) {
        Invoke-ChildPowerShell -Name 'Module cache isolation proof' -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalModuleCache.ps1') -Arguments @('-ConnectionStringsConfigPath', $ConnectionStringsConfigPath, '-Port', $CacheProofPort)
    }
}
finally {
# <lang>
#   <zh-CN>只清理本脚本成功启动的虚拟目录实例，保留已有调试站点和外部进程。</zh-CN>
#   <en>Clean up only the virtual-directory instance started by this script, leaving existing debug sites and external processes untouched.</en>
# </lang>
    if ($virtualDirectoryStarted) {
        Invoke-ChildPowerShell -Name 'Virtual-directory IIS Express stop' -ScriptPath (Join-Path $PSScriptRoot 'Stop-IISExpress.ps1') -Arguments @('-Port', $VirtualDirectoryPort)
    }
}

# <lang>
#   <zh-CN>输出低敏 smoke 完成摘要和可选范围，不输出管理员密码、连接串或 Cookie。</zh-CN>
#   <en>Output a low-sensitivity smoke completion summary and optional scopes without exposing administrator passwords, connection strings, or cookies.</en>
# </lang>
[pscustomobject]@{
    TotalChecks = $summary.Count
    CompletedChecks = $summary
    SqlCompatibilityIncluded = $IncludeSqlCompatibility
    AdministratorSmokeIncluded = $IncludeAdmin
    ThemeMutationIncluded = $IncludeThemeMutation
    CacheMutationIncluded = $IncludeCacheMutation
}
