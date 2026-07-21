<#
.SYNOPSIS
    Builds a P12 business-flow evidence package from read-only gates.

.DESCRIPTION
    中文：本脚本编排 P12.2 业务身份、P12.3 轻量待办、P12.4 业务权限审计等只读门禁，
    并可选执行解决方案构建，形成 P12.5 周期验收证据包。
    English: This script orchestrates the P12.2 business-identity, P12.3 lightweight work-item,
    and P12.4 business permission/audit read-only gates, with an optional solution build, into a
    P12.5 acceptance evidence package.
#>
[CmdletBinding()]
param(
    [string]$OutputRoot,

    [switch]$SkipBuild,

    [switch]$AllowFailures
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = if (Test-Path -LiteralPath (Join-Path $repoRoot 'work-zone')) {
        Join-Path $repoRoot 'work-zone/dev/evidence/p12.5'
    }
    else {
        Join-Path $repoRoot 'temp/evidence/p12.5'
    }
}

$runId = (Get-Date).ToString('yyyyMMdd-HHmmss')
$runDirectory = Join-Path ([System.IO.Path]::GetFullPath($OutputRoot)) $runId
$steps = New-Object 'System.Collections.Generic.List[object]'

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory | Out-Null
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

function Invoke-P12EvidenceStep {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$ScriptPath,
        [string[]]$Arguments = @(),
        [Parameter(Mandatory = $true)][string]$LogPath
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
            # 证据日志入库前去除行尾空白，避免自动生成物触发 Git whitespace 检查。
            $capturedLines.Add(([string]$line).TrimEnd())
        }
    }
    catch {
        $exitCode = 1
        $capturedLines.Add($_.Exception.Message.TrimEnd())
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
        '```'
    )

    Write-Utf8NoBomFile -Path $LogPath -Content (($logLines -join [Environment]::NewLine) + [Environment]::NewLine)

    $step = [pscustomobject]@{
        Name = $Name
        Status = $status
        ExitCode = $exitCode
        LogPath = $LogPath
        StartedUtc = $startedAt.ToString('yyyy-MM-ddTHH:mm:ssZ')
        FinishedUtc = $finishedAt.ToString('yyyy-MM-ddTHH:mm:ssZ')
        Command = ($displayCommand -join ' ')
    }
    $steps.Add($step)
    Write-Host ('[{0}] {1} -> {2}' -f $status.ToUpperInvariant(), $Name, $LogPath)
}

New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null
Write-Host ('P12.5 evidence directory: {0}' -f $runDirectory)

$businessIdentityJson = Join-Path $runDirectory 'business-identity.json'
Invoke-P12EvidenceStep `
    -Name 'P12.2 business identity gate' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalBusinessIdentity.ps1') `
    -Arguments @('-OutputJson', $businessIdentityJson) `
    -LogPath (Join-Path $runDirectory 'business-identity.log.md')

$workItemJson = Join-Path $runDirectory 'work-item-smoke.json'
Invoke-P12EvidenceStep `
    -Name 'P12.3 work-item gate' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalWorkItemSmoke.ps1') `
    -Arguments @('-OutputJson', $workItemJson) `
    -LogPath (Join-Path $runDirectory 'work-item-smoke.log.md')

$businessPermissionJson = Join-Path $runDirectory 'business-permission-audit.json'
Invoke-P12EvidenceStep `
    -Name 'P12.4 business permission audit gate' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalBusinessPermissionAudit.ps1') `
    -Arguments @('-OutputJson', $businessPermissionJson) `
    -LogPath (Join-Path $runDirectory 'business-permission-audit.log.md')

if (-not $SkipBuild) {
    Invoke-P12EvidenceStep `
        -Name 'Solution build' `
        -ScriptPath (Join-Path $PSScriptRoot 'Build-Solution.ps1') `
        -Arguments @() `
        -LogPath (Join-Path $runDirectory 'solution-build.log.md')
}

$failedSteps = @($steps | Where-Object { $_.ExitCode -ne 0 })
$summary = [pscustomobject]@{
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    RunDirectory = $runDirectory
    SkipBuild = [bool]$SkipBuild
    Steps = $steps
    FailedStepCount = $failedSteps.Count
}

Write-Utf8NoBomFile -Path (Join-Path $runDirectory 'run-summary.json') -Content (($summary | ConvertTo-Json -Depth 6) + [Environment]::NewLine)

$readmeLines = @(
    '# Portal P12.5 Evidence Run',
    '',
    ('Generated UTC: `{0}`' -f $summary.GeneratedAtUtc),
    ('Output directory: `{0}`' -f $runDirectory),
    '',
    '## Scope',
    '',
    '1. This package records read-only P12 static gates and, unless skipped, a solution build.',
    '2. It does not sign in, write the database, read secret files, or store passwords, tokens, cookies, connection strings, certificate private keys, or production configuration.',
    '3. End-to-end browser and database scenarios still require a prepared development or test database and the manual walkthrough.',
    '',
    '## Steps',
    '',
    '| Step | Status | ExitCode | Log |',
    '| --- | --- | --- | --- |'
)

foreach ($step in $steps) {
    $logName = Split-Path -Leaf $step.LogPath
    $readmeLines += ('| {0} | {1} | {2} | [{3}]({3}) |' -f $step.Name, $step.Status, $step.ExitCode, $logName)
}

$readmeLines += @(
    '',
    ('Failed steps: `{0}`' -f $failedSteps.Count)
)
Write-Utf8NoBomFile -Path (Join-Path $runDirectory 'README.md') -Content (($readmeLines -join [Environment]::NewLine) + [Environment]::NewLine)

Write-Host ('SUMMARY: Steps={0}; Failed={1}' -f $steps.Count, $failedSteps.Count)
Write-Host ('README: {0}' -f (Join-Path $runDirectory 'README.md'))

if ($failedSteps.Count -gt 0 -and -not $AllowFailures) {
    exit 1
}

exit 0
