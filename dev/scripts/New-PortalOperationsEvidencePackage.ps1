<#
.SYNOPSIS
    Builds a read-only operations evidence package for Portal maintenance review.

.DESCRIPTION
    中文：本脚本编排 P13.2 运维只读门禁，包括运维 readiness、日志维护 dry-run、发布资源、公开文档、
    基础合规和默认凭据风险检查。它不登录、不写数据库、不读取外置敏感配置、不创建计划任务，也不删除日志。
    English: This script orchestrates the P13.2 read-only operations gates: operations readiness, log-maintenance
    dry run, publish resources, public documentation, baseline compliance, and default-credential risk checks. It
    does not sign in, write databases, read external secrets, create scheduled tasks, or delete logs.
#>
[CmdletBinding()]
param(
    [ValidateSet('Dev', 'Test', 'Prod', 'Scan', 'LegacyIe')]
    [string]$Profile = 'Dev',

    [ValidatePattern('^https?://')]
    [string]$BaseUrl,

    [string]$LogDirectory,

    [string]$OutputRoot,

    [switch]$AllowFailures
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = if (Test-Path -LiteralPath (Join-Path $repoRoot 'work-zone')) {
        Join-Path $repoRoot 'work-zone/dev/evidence/p13.2'
    }
    else {
        Join-Path $repoRoot 'temp/evidence/p13.2'
    }
}

$resolvedOutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
$runId = (Get-Date).ToString('yyyyMMdd-HHmmss')
$runDirectory = Join-Path $resolvedOutputRoot ('{0}-{1}' -f $runId, $Profile)
$steps = New-Object 'System.Collections.Generic.List[object]'

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

function Format-EvidenceArgument {
    param([string]$Value)

    if ($Value -match '\s|["'']') {
        return '"' + ($Value -replace '"', '\"') + '"'
    }

    return $Value
}

function Get-PwshPath {
    $preferred = 'C:\Program Files\PowerShell\7\pwsh.exe'
    if (Test-Path -LiteralPath $preferred -PathType Leaf) {
        return $preferred
    }

    $command = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    throw 'PowerShell 7 (pwsh) was not found.'
}

function Invoke-EvidenceStep {
    param(
        [string]$Name,
        [string]$ScriptPath,
        [string[]]$Arguments,
        [string]$LogPath
    )

    $pwshPath = Get-PwshPath
    $argumentList = @('-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $ScriptPath) + $Arguments
    $displayCommand = @($pwshPath) + $argumentList | ForEach-Object { Format-EvidenceArgument -Value $_ }

    $startedAt = (Get-Date).ToUniversalTime()
    $capturedLines = New-Object 'System.Collections.Generic.List[string]'
    $exitCode = 0

    try {
        $output = & $pwshPath @argumentList 2>&1
        $exitCode = if ($null -eq $LASTEXITCODE) { 0 } else { $LASTEXITCODE }
        foreach ($line in $output) {
            $capturedLines.Add([string]$line)
        }
    }
    catch {
        $exitCode = 1
        $capturedLines.Add($_.Exception.Message)
    }

    $finishedAt = (Get-Date).ToUniversalTime()
    $status = if ($exitCode -eq 0) { 'Passed' } else { 'Failed' }
    $logLines = @(
        ('# {0}' -f $Name),
        '',
        ('Started UTC: {0}' -f $startedAt.ToString('yyyy-MM-ddTHH:mm:ssZ')),
        ('Finished UTC: {0}' -f $finishedAt.ToString('yyyy-MM-ddTHH:mm:ssZ')),
        ('ExitCode: {0}' -f $exitCode),
        ('Command: {0}' -f ($displayCommand -join ' ')),
        '',
        '```text'
    ) + $capturedLines + @(
        '```',
        ''
    )

    Write-Utf8NoBomFile -Path $LogPath -Content (($logLines -join [Environment]::NewLine) + [Environment]::NewLine)

    $result = [pscustomobject][ordered]@{
        Name = $Name
        Status = $status
        ExitCode = $exitCode
        LogPath = $LogPath
        StartedUtc = $startedAt.ToString('yyyy-MM-ddTHH:mm:ssZ')
        FinishedUtc = $finishedAt.ToString('yyyy-MM-ddTHH:mm:ssZ')
        Command = ($displayCommand -join ' ')
    }

    $steps.Add($result)
    Write-Host ('[{0}] {1} -> {2}' -f $status.ToUpperInvariant(), $Name, $LogPath)
}

New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null
Write-Host ('Operations evidence directory: {0}' -f $runDirectory)

$operationsJson = Join-Path $runDirectory 'operations-readiness.json'
Invoke-EvidenceStep `
    -Name 'Operations readiness' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalOperationsReadiness.ps1') `
    -Arguments @('-Profile', $Profile, '-OutputJson', $operationsJson) `
    -LogPath (Join-Path $runDirectory 'operations-readiness.log.md')

$logMaintenanceJson = Join-Path $runDirectory 'log-maintenance-dry-run.json'
$logMaintenanceArgs = @('-OutputJson', $logMaintenanceJson)
if (-not [string]::IsNullOrWhiteSpace($LogDirectory)) {
    $logMaintenanceArgs += @('-LogDirectory', $LogDirectory)
}
Invoke-EvidenceStep `
    -Name 'Diagnostics log maintenance dry run' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalLogMaintenance.ps1') `
    -Arguments $logMaintenanceArgs `
    -LogPath (Join-Path $runDirectory 'log-maintenance-dry-run.log.md')

Invoke-EvidenceStep `
    -Name 'Publish readiness' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalPublishReadiness.ps1') `
    -Arguments @() `
    -LogPath (Join-Path $runDirectory 'publish-readiness.log.md')

Invoke-EvidenceStep `
    -Name 'Public documentation gate' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalPublicDocumentation.ps1') `
    -Arguments @() `
    -LogPath (Join-Path $runDirectory 'public-documentation.log.md')

$complianceJson = Join-Path $runDirectory 'compliance-baseline.json'
$complianceArgs = @('-Profile', $Profile, '-OutputJson', $complianceJson)
if (-not [string]::IsNullOrWhiteSpace($BaseUrl)) {
    $complianceArgs += @('-BaseUrl', $BaseUrl)
}
Invoke-EvidenceStep `
    -Name 'Compliance baseline' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalComplianceBaseline.ps1') `
    -Arguments $complianceArgs `
    -LogPath (Join-Path $runDirectory 'compliance-baseline.log.md')

$defaultCredentialJson = Join-Path $runDirectory 'default-credential-risk.json'
$defaultCredentialProfile = if ($Profile -eq 'LegacyIe') { 'Dev' } else { $Profile }
Invoke-EvidenceStep `
    -Name 'Default credential risk' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalDefaultCredentialRisk.ps1') `
    -Arguments @('-Profile', $defaultCredentialProfile, '-OutputJson', $defaultCredentialJson) `
    -LogPath (Join-Path $runDirectory 'default-credential-risk.log.md')

$failedSteps = @($steps | Where-Object { $_.ExitCode -ne 0 })
$summary = [pscustomobject][ordered]@{
    Profile = $Profile
    BaseUrl = $BaseUrl
    LogDirectory = $LogDirectory
    RunDirectory = $runDirectory
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Steps = $steps
    FailedStepCount = $failedSteps.Count
}

Write-Utf8NoBomFile -Path (Join-Path $runDirectory 'run-summary.json') -Content (($summary | ConvertTo-Json -Depth 8) + [Environment]::NewLine)

$markdownLines = @(
    '# Portal Operations Evidence Run',
    '',
    ('Profile: `{0}`' -f $Profile),
    ('Generated UTC: `{0}`' -f $summary.GeneratedAtUtc),
    ('Output directory: `{0}`' -f $runDirectory),
    '',
    '## Scope',
    '',
    '1. This package records read-only operations, release, documentation, compliance, and credential-risk gate output.',
    '2. It does not sign in, write databases, read secret files, create scheduled tasks, or delete logs.',
    '3. Real IIS, TLS, app-pool ACL, SQL Server backup, restore drill, disk monitoring, and enterprise scan evidence must still be collected in the target environment.',
    '',
    '## Steps',
    '',
    '| Step | Status | ExitCode | Log |',
    '| --- | --- | --- | --- |'
)

foreach ($step in $steps) {
    $logName = Split-Path -Leaf $step.LogPath
    $markdownLines += ('| {0} | {1} | {2} | [{3}]({3}) |' -f $step.Name, $step.Status, $step.ExitCode, $logName)
}

$markdownLines += @(
    '',
    ('Failed steps: `{0}`' -f $failedSteps.Count),
    ''
)

Write-Utf8NoBomFile -Path (Join-Path $runDirectory 'README.md') -Content (($markdownLines -join [Environment]::NewLine) + [Environment]::NewLine)

Write-Host ('SUMMARY: Steps={0}; Failed={1}' -f $steps.Count, $failedSteps.Count)
Write-Host ('README: {0}' -f (Join-Path $runDirectory 'README.md'))

if ($failedSteps.Count -gt 0 -and -not $AllowFailures) {
    exit 1
}

exit 0
