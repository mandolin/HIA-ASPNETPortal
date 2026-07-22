<#
.SYNOPSIS
    Checks the read-only operations readiness surface for the Portal project.

.DESCRIPTION
    中文：本脚本检查运维页面、诊断日志、运营审计、公开运行手册、发布门禁和目标环境补证边界。
    它只读取仓库文件，不连接 IIS、数据库或外置配置，也不修改日志、任务计划、备份或系统设置。
    English: This script checks operations pages, diagnostic logging, operation audits, public runbook material,
    release gates, and target-environment evidence boundaries. It only reads repository files and never connects to
    IIS, databases, or external configuration, nor modifies logs, scheduled tasks, backups, or system settings.
#>
[CmdletBinding()]
param(
    [string]$PortalPath,

    [string]$DocsPath,

    [string]$ScriptsPath,

    [ValidateSet('Dev', 'Test', 'Prod', 'Scan', 'LegacyIe')]
    [string]$Profile = 'Dev',

    [string]$OutputJson,

    [switch]$FailOnWarning
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($PortalPath)) {
    $PortalPath = Join-Path $repoRoot 'src/Portal'
}
if ([string]::IsNullOrWhiteSpace($DocsPath)) {
    $DocsPath = Join-Path $repoRoot 'docs'
}
if ([string]::IsNullOrWhiteSpace($ScriptsPath)) {
    $ScriptsPath = Join-Path $repoRoot 'dev/scripts'
}

$resolvedPortalPath = (Resolve-Path -LiteralPath $PortalPath).Path
$resolvedDocsPath = (Resolve-Path -LiteralPath $DocsPath).Path
$resolvedScriptsPath = (Resolve-Path -LiteralPath $ScriptsPath).Path
$checks = New-Object 'System.Collections.Generic.List[object]'

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Add-OperationsCheck {
    param(
        [ValidateSet('Pass', 'Warning', 'Fail', 'Info', 'Pending')]
        [string]$Severity,

        [string]$Code,

        [string]$Message,

        [string]$Evidence = ''
    )

    $checks.Add([pscustomobject][ordered]@{
            Severity = $Severity
            Code = $Code
            Message = $Message
            Evidence = $Evidence
        })

    Write-Host ('[{0}] {1}: {2}' -f $Severity.ToUpperInvariant(), $Code, $Message)
    if (-not [string]::IsNullOrWhiteSpace($Evidence)) {
        Write-Host ('       {0}' -f $Evidence)
    }
}

function Get-Utf8Text {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)

    return [System.IO.File]::ReadAllText($LiteralPath, [System.Text.UTF8Encoding]::new($false))
}

function ConvertTo-DisplayPath {
    param([string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $repoPrefix = $repoRoot.TrimEnd('\') + '\'
    if ($fullPath.StartsWith($repoPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ($fullPath.Substring($repoPrefix.Length) -replace '\\', '/')
    }

    return $fullPath
}

function Test-FileContains {
    param(
        [string]$LiteralPath,
        [string]$Pattern
    )

    return (Test-Path -LiteralPath $LiteralPath -PathType Leaf) -and
        [regex]::IsMatch((Get-Utf8Text -LiteralPath $LiteralPath), $Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
}

function Resolve-PortalFile {
    param([string]$RelativePath)

    return Join-Path $resolvedPortalPath ($RelativePath -replace '/', '\')
}

function Resolve-DocsFile {
    param([string]$RelativePath)

    return Join-Path $resolvedDocsPath ($RelativePath -replace '/', '\')
}

function Resolve-ScriptFile {
    param([string]$RelativePath)

    return Join-Path $resolvedScriptsPath ($RelativePath -replace '/', '\')
}

Write-Host ('PROFILE: {0}' -f $Profile)
Write-Host 'MODE: read-only repository operations readiness check.'

$adminPages = @(
    'Admin/SystemHealth.aspx',
    'Admin/SystemHealth.aspx.cs',
    'Admin/DiagnosticsLogs.aspx',
    'Admin/DiagnosticsLogs.aspx.cs',
    'Admin/OperationAudits.aspx',
    'Admin/OperationAudits.aspx.cs'
)
$missingAdminPages = @($adminPages | Where-Object { -not (Test-Path -LiteralPath (Resolve-PortalFile -RelativePath $_) -PathType Leaf) })
if ($missingAdminPages.Count -eq 0) {
    Add-OperationsCheck -Severity Pass -Code 'OPS-ADMIN-PAGES' -Message 'System health, diagnostics logs and operation audits Admin pages exist.'
}
else {
    Add-OperationsCheck -Severity Fail -Code 'OPS-ADMIN-PAGES' -Message 'Required operations Admin pages are missing.' -Evidence ($missingAdminPages -join '; ')
}

$projectPath = Resolve-PortalFile -RelativePath 'Portal.csproj'
if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    Add-OperationsCheck -Severity Fail -Code 'OPS-CSPROJ' -Message 'Portal.csproj was not found.'
}
else {
    $projectText = Get-Utf8Text -LiteralPath $projectPath
    $projectMissing = New-Object 'System.Collections.Generic.List[string]'
    foreach ($page in @('Admin\SystemHealth.aspx', 'Admin\DiagnosticsLogs.aspx', 'Admin\OperationAudits.aspx')) {
        if ($projectText -notmatch ('Content Include="' + [regex]::Escape($page) + '"')) {
            $projectMissing.Add($page)
        }
    }
    foreach ($codeBehind in @('Admin\SystemHealth.aspx.cs', 'Admin\DiagnosticsLogs.aspx.cs', 'Admin\OperationAudits.aspx.cs')) {
        if ($projectText -notmatch ('Compile Include="' + [regex]::Escape($codeBehind) + '"')) {
            $projectMissing.Add($codeBehind)
        }
    }

    if ($projectMissing.Count -eq 0) {
        Add-OperationsCheck -Severity Pass -Code 'OPS-CSPROJ' -Message 'Operations Admin pages are declared in Portal.csproj.'
    }
    else {
        Add-OperationsCheck -Severity Fail -Code 'OPS-CSPROJ' -Message 'Some operations files are not declared in Portal.csproj.' -Evidence ($projectMissing -join '; ')
    }
}

$diagnosticsPath = Resolve-PortalFile -RelativePath 'Components/PortalDiagnostics.cs'
$registryPath = Resolve-PortalFile -RelativePath 'Components/PortalSettingsRegistry.cs'
$diagnosticsReady =
    (Test-FileContains -LiteralPath $diagnosticsPath -Pattern 'JsonConvert\.SerializeObject') -and
    (Test-FileContains -LiteralPath $diagnosticsPath -Pattern 'portal-\*\.jsonl') -and
    (Test-FileContains -LiteralPath $diagnosticsPath -Pattern 'CleanupExpiredLogs') -and
    (Test-FileContains -LiteralPath $diagnosticsPath -Pattern 'ResolveLogDirectory') -and
    (Test-FileContains -LiteralPath $diagnosticsPath -Pattern 'App_Data.+Logs') -and
    (Test-FileContains -LiteralPath $registryPath -Pattern 'DiagnosticsRetentionDays') -and
    (Test-FileContains -LiteralPath $registryPath -Pattern 'DiagnosticsMaxFileBytes')
if ($diagnosticsReady) {
    Add-OperationsCheck -Severity Pass -Code 'OPS-DIAGNOSTICS' -Message 'Structured diagnostics logging, log directory resolution and retention settings are present.'
}
else {
    Add-OperationsCheck -Severity Fail -Code 'OPS-DIAGNOSTICS' -Message 'Structured diagnostics logging or retention metadata is incomplete.'
}

$queryServicePath = Resolve-PortalFile -RelativePath 'Components/PortalDiagnosticQueryService.cs'
$logQueryReady =
    (Test-FileContains -LiteralPath $queryServicePath -Pattern 'Only scans NDJSON files') -and
    (Test-FileContains -LiteralPath (Resolve-PortalFile -RelativePath 'Admin/DiagnosticsLogs.aspx.cs') -Pattern 'PortalDiagnosticQueryService') -and
    (Test-FileContains -LiteralPath $diagnosticsPath -Pattern 'DiagnosticsAllowAdminDetailView')
if ($logQueryReady) {
    Add-OperationsCheck -Severity Pass -Code 'OPS-LOG-QUERY' -Message 'Diagnostics log query is path-restricted and Admin-gated.'
}
else {
    Add-OperationsCheck -Severity Fail -Code 'OPS-LOG-QUERY' -Message 'Diagnostics log query boundary needs review.'
}

$operationAuditPath = Resolve-PortalFile -RelativePath 'Components/PortalOperationAudit.cs'
$operationAuditsPage = Resolve-PortalFile -RelativePath 'Admin/OperationAudits.aspx.cs'
$auditReady =
    (Test-FileContains -LiteralPath $operationAuditPath -Pattern 'PortalCfg_OperationAudits') -and
    (Test-FileContains -LiteralPath $operationAuditsPage -Pattern 'PortalOperationAudit\.Query') -and
    (-not (Test-FileContains -LiteralPath $operationAuditsPage -Pattern 'Content-Disposition|TransmitFile|WriteFile|attachment|Export|导出|下载'))
if ($auditReady) {
    Add-OperationsCheck -Severity Pass -Code 'OPS-AUDIT-QUERY' -Message 'Operation audit query exists and no export/download path is present in the Admin page.'
}
else {
    Add-OperationsCheck -Severity Fail -Code 'OPS-AUDIT-QUERY' -Message 'Operation audit query or export boundary needs review.'
}

$scriptFiles = @(
    'Test-PortalLogMaintenance.ps1',
    'Test-PortalOperationsReadiness.ps1',
    'New-PortalOperationsEvidencePackage.ps1',
    'Test-PortalPublishReadiness.ps1',
    'Test-PortalComplianceBaseline.ps1',
    'Test-PortalPublicDocumentation.ps1'
)
$missingScripts = @($scriptFiles | Where-Object { -not (Test-Path -LiteralPath (Resolve-ScriptFile -RelativePath $_) -PathType Leaf) })
if ($missingScripts.Count -eq 0) {
    Add-OperationsCheck -Severity Pass -Code 'OPS-SCRIPTS' -Message 'Operations evidence, log dry-run, publish, compliance and documentation scripts are present.'
}
else {
    Add-OperationsCheck -Severity Fail -Code 'OPS-SCRIPTS' -Message 'Required operations scripts are missing.' -Evidence ($missingScripts -join '; ')
}

$runbookPath = Resolve-DocsFile -RelativePath 'operations-runbook.md'
$docsReadmePath = Resolve-DocsFile -RelativePath 'README.md'
$runbookReady =
    (Test-FileContains -LiteralPath $runbookPath -Pattern 'SystemHealth') -and
    (Test-FileContains -LiteralPath $runbookPath -Pattern 'DiagnosticsLogs') -and
    (Test-FileContains -LiteralPath $runbookPath -Pattern 'OperationAudits') -and
    (Test-FileContains -LiteralPath $runbookPath -Pattern 'Test-PortalLogMaintenance\.ps1') -and
    (Test-FileContains -LiteralPath $runbookPath -Pattern 'Windows Task Scheduler|计划任务') -and
    (Test-FileContains -LiteralPath $runbookPath -Pattern '不自动执行.*备份|does not execute database backups') -and
    (Test-FileContains -LiteralPath $docsReadmePath -Pattern 'operations-runbook\.md')
if ($runbookReady) {
    Add-OperationsCheck -Severity Pass -Code 'OPS-RUNBOOK' -Message 'Public operations runbook is present and indexed.'
}
else {
    Add-OperationsCheck -Severity Fail -Code 'OPS-RUNBOOK' -Message 'Public operations runbook or docs index is incomplete.'
}

$deploymentChecklistPath = Resolve-DocsFile -RelativePath 'deployment-checklist.md'
if ((Test-FileContains -LiteralPath $deploymentChecklistPath -Pattern 'App_Data/Logs') -and
    (Test-FileContains -LiteralPath $deploymentChecklistPath -Pattern '数据库已完成备份|backup')) {
    Add-OperationsCheck -Severity Pass -Code 'OPS-DEPLOYMENT-CHECKLIST' -Message 'Deployment checklist covers log directory ACL and database backup reminders.'
}
else {
    Add-OperationsCheck -Severity Warning -Code 'OPS-DEPLOYMENT-CHECKLIST' -Message 'Deployment checklist should mention log directory ACL and database backup reminders.'
}

Add-OperationsCheck -Severity Pending -Code 'OPS-TARGET-IIS' -Message 'Real IIS site, virtual directory, application pool identity, TLS and ACL evidence must be collected on the target environment.'
Add-OperationsCheck -Severity Pending -Code 'OPS-TARGET-SQL-BACKUP' -Message 'SQL Server backup job, restore drill and recovery objective evidence are target-environment responsibilities.'
Add-OperationsCheck -Severity Pending -Code 'OPS-ALERT-INTEGRATION' -Message 'Email, IM and webhook alerts remain extension points; no external alert channel is configured in this stage.'

$summary = [pscustomobject][ordered]@{
    Profile = $Profile
    PortalPath = $resolvedPortalPath
    DocsPath = $resolvedDocsPath
    ScriptsPath = $resolvedScriptsPath
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Checks = $checks
    TotalChecks = $checks.Count
    FailedChecks = @($checks | Where-Object { $_.Severity -eq 'Fail' }).Count
    WarningChecks = @($checks | Where-Object { $_.Severity -eq 'Warning' }).Count
    PendingChecks = @($checks | Where-Object { $_.Severity -eq 'Pending' }).Count
}

$summary

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    Write-Utf8NoBomFile -Path $OutputJson -Content (($summary | ConvertTo-Json -Depth 8) + [Environment]::NewLine)
    Write-Host ('JSON: {0}' -f $OutputJson)
}

if ($summary.FailedChecks -gt 0 -or ($FailOnWarning -and $summary.WarningChecks -gt 0)) {
    exit 1
}

exit 0
