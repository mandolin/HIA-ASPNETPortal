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

function Test-AvailablePort {
    param([int]$Port)

    $listening = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($listening) {
        throw "The requested isolated IIS Express port $Port is already in use."
    }
}

if ($IncludeSqlCompatibility -or $IncludeThemeMutation -or $IncludeCacheMutation) {
    if ([string]::IsNullOrWhiteSpace($ConnectionStringsConfigPath)) {
        throw 'ConnectionStringsConfigPath is required for SQL compatibility or cache-mutation checks.'
    }

    if (-not (Test-Path -LiteralPath $ConnectionStringsConfigPath -PathType Leaf)) {
        throw 'ConnectionStringsConfigPath does not exist.'
    }
}

if ($IncludeAdmin -and [string]::IsNullOrWhiteSpace($AdminUser)) {
    throw 'AdminUser is required when IncludeAdmin is specified.'
}

if (($IncludeThemeMutation -or $IncludeCacheMutation) -and -not $ConnectionStringsConfigPath.Contains('\Web\HIA-ASPNETPortal\')) {
    throw 'Theme and cache mutation checks are restricted to the explicit external Web configuration tree.'
}

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

            # 本机可能将默认 registry 指向不实现 audit API 的镜像；审计固定走官方端点，避免把镜像能力误判成依赖风险。
            # Some local registries do not implement the audit API. Use the official endpoint so mirror capability is not mistaken for dependency risk.
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
        # 仅在本脚本实际拉起根站点时关闭该实例；已有调试站点保持不受影响。
        # Stop only an instance started by this script; leave an existing debugging site untouched.
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
    if ($virtualDirectoryStarted) {
        Invoke-ChildPowerShell -Name 'Virtual-directory IIS Express stop' -ScriptPath (Join-Path $PSScriptRoot 'Stop-IISExpress.ps1') -Arguments @('-Port', $VirtualDirectoryPort)
    }
}

[pscustomobject]@{
    TotalChecks = $summary.Count
    CompletedChecks = $summary
    SqlCompatibilityIncluded = $IncludeSqlCompatibility
    AdministratorSmokeIncluded = $IncludeAdmin
    ThemeMutationIncluded = $IncludeThemeMutation
    CacheMutationIncluded = $IncludeCacheMutation
}
