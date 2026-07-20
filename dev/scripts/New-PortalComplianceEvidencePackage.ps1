[CmdletBinding()]
param(
    [ValidateSet('Dev', 'Test', 'Prod', 'Scan', 'LegacyIe')]
    [string]$Profile = 'Dev',

    [ValidatePattern('^https?://')]
    [string]$BaseUrl,

    [string]$OutputRoot,

    [switch]$AllowFailures
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$workZoneEvidenceRoot = Join-Path $repoRoot 'work-zone/dev/evidence/p10'

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    if (Test-Path -LiteralPath (Join-Path $repoRoot 'work-zone')) {
        $OutputRoot = $workZoneEvidenceRoot
    }
    else {
        $OutputRoot = Join-Path $repoRoot 'temp/compliance/evidence/p10'
    }
}

$resolvedOutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
$runId = (Get-Date).ToString('yyyyMMdd-HHmmss')
$runDirectory = Join-Path $resolvedOutputRoot ('{0}-{1}' -f $runId, $Profile)
$steps = New-Object 'System.Collections.Generic.List[object]'

function Write-Utf8NoBomFile {
    param(
        [string]$Path,
        [string]$Content
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory | Out-Null
    }

    $encoding = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
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

    $result = [pscustomobject]@{
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

if (-not (Test-Path -LiteralPath $runDirectory)) {
    New-Item -ItemType Directory -Path $runDirectory | Out-Null
}

# 中文：本脚本只编排已有只读门禁，形成可留存证据包；不写数据库、不尝试登录、不读取 secret 文件。
# English: This script only orchestrates existing read-only gates into an evidence package; it never writes databases,
# attempts sign-in, or reads secret files.
Write-Host ('Evidence directory: {0}' -f $runDirectory)

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

Invoke-EvidenceStep `
    -Name 'Public documentation gate' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalPublicDocumentation.ps1') `
    -Arguments @() `
    -LogPath (Join-Path $runDirectory 'public-documentation.log.md')

$failedSteps = @($steps | Where-Object { $_.ExitCode -ne 0 })
$summary = [pscustomobject]@{
    Profile = $Profile
    BaseUrl = $BaseUrl
    RunDirectory = $runDirectory
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Steps = $steps
    FailedStepCount = $failedSteps.Count
}

Write-Utf8NoBomFile -Path (Join-Path $runDirectory 'run-summary.json') -Content (($summary | ConvertTo-Json -Depth 6) + [Environment]::NewLine)

$markdownLines = @(
    '# Portal Compliance Evidence Run',
    '',
    ('Profile: `{0}`' -f $Profile),
    ('Generated UTC: `{0}`' -f $summary.GeneratedAtUtc),
    ('Output directory: `{0}`' -f $runDirectory),
    '',
    '## Scope',
    '',
    '1. This package records read-only script output for compliance review.',
    '2. It does not sign in, write the database, read secret files, or store passwords, tokens, cookies, connection strings, certificate private keys, or production configuration.',
    '3. Test and production evidence must still be interpreted with the actual deployment environment.',
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
