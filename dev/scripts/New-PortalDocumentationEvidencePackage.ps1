<#
.SYNOPSIS
    Builds a documentation toolchain evidence package for P13.3.

.DESCRIPTION
    中文：本脚本编排文档化 readiness、文档化 baseline、公开文档门禁、.NET XML 文档验证、
    HIA JSDoc pilot 和 HIA-Documentation-Sys 通知读取。它不修改源码注释、不提交生成物、不读取敏感配置、
    不连接数据库，也不把 HIA-Documentation-Sys 变成本项目构建硬依赖。
    English: This script orchestrates documentation readiness, documentation baseline, public documentation gates,
    .NET XML documentation verification, the HIA JSDoc pilot, and HIA-Documentation-Sys notification pull evidence.
    It does not modify source comments, commit generated output, read secret configuration, connect to databases,
    or make HIA-Documentation-Sys a hard build dependency.
#>
[CmdletBinding()]
param(
    [string]$HiaDocumentationRoot,

    [string]$OutputRoot,

    [switch]$SkipXmlDocumentation,

    [switch]$SkipJsdocPilot,

    [switch]$AllowFailures
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = if (Test-Path -LiteralPath (Join-Path $repoRoot 'work-zone')) {
        Join-Path $repoRoot 'work-zone/dev/evidence/p13.3'
    }
    else {
        Join-Path $repoRoot 'temp/evidence/p13.3'
    }
}

$resolvedOutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
$runId = (Get-Date).ToString('yyyyMMdd-HHmmss')
$runDirectory = Join-Path $resolvedOutputRoot $runId
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
        [string]$LogPath,
        [switch]$Optional
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
    $status = if ($exitCode -eq 0) {
        'Passed'
    }
    elseif ($Optional) {
        'Pending'
    }
    else {
        'Failed'
    }
    $logLines = @(
        ('# {0}' -f $Name),
        '',
        ('Started UTC: {0}' -f $startedAt.ToString('yyyy-MM-ddTHH:mm:ssZ')),
        ('Finished UTC: {0}' -f $finishedAt.ToString('yyyy-MM-ddTHH:mm:ssZ')),
        ('ExitCode: {0}' -f $exitCode),
        ('Status: {0}' -f $status),
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
Write-Host ('Documentation evidence directory: {0}' -f $runDirectory)

$readinessJson = Join-Path $runDirectory 'documentation-readiness.json'
$readinessArgs = @('-OutputJson', $readinessJson)
if (-not [string]::IsNullOrWhiteSpace($HiaDocumentationRoot)) {
    $readinessArgs += @('-HiaDocumentationRoot', $HiaDocumentationRoot)
}
Invoke-EvidenceStep `
    -Name 'Documentation readiness' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalDocumentationReadiness.ps1') `
    -Arguments $readinessArgs `
    -LogPath (Join-Path $runDirectory 'documentation-readiness.log.md')

Invoke-EvidenceStep `
    -Name 'Documentation baseline inventory' `
    -ScriptPath (Join-Path $PSScriptRoot 'Get-PortalDocumentationBaseline.ps1') `
    -Arguments @('-OutputJson', (Join-Path $runDirectory 'documentation-baseline.json')) `
    -LogPath (Join-Path $runDirectory 'documentation-baseline.log.md')

Invoke-EvidenceStep `
    -Name 'Public documentation gate' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalPublicDocumentation.ps1') `
    -Arguments @() `
    -LogPath (Join-Path $runDirectory 'public-documentation.log.md')

if (-not $SkipXmlDocumentation) {
    Invoke-EvidenceStep `
        -Name '.NET XML documentation verification' `
        -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalXmlDocumentation.ps1') `
        -Arguments @() `
        -LogPath (Join-Path $runDirectory 'xml-documentation.log.md')
}

if (-not $SkipJsdocPilot) {
    Invoke-EvidenceStep `
        -Name 'HIA JSDoc pilot' `
        -ScriptPath (Join-Path $PSScriptRoot 'Build-PortalJsdocPilot.ps1') `
        -Arguments @('-SkipRestore') `
        -LogPath (Join-Path $runDirectory 'jsdoc-pilot.log.md')
}

$notificationArgs = @('-Latest', '20')
if (-not [string]::IsNullOrWhiteSpace($HiaDocumentationRoot)) {
    $notificationArgs += @('-HiaDocumentationRoot', $HiaDocumentationRoot)
}
Invoke-EvidenceStep `
    -Name 'HIA-Documentation-Sys notifications' `
    -ScriptPath (Join-Path $PSScriptRoot 'Get-HiaDocumentationNotifications.ps1') `
    -Arguments $notificationArgs `
    -LogPath (Join-Path $runDirectory 'hia-documentation-notifications.log.md') `
    -Optional

$failedSteps = @($steps | Where-Object { $_.Status -eq 'Failed' })
$pendingSteps = @($steps | Where-Object { $_.Status -eq 'Pending' })
$summary = [pscustomobject][ordered]@{
    RunDirectory = $runDirectory
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    HiaDocumentationRoot = $HiaDocumentationRoot
    Steps = $steps
    FailedStepCount = $failedSteps.Count
    PendingStepCount = $pendingSteps.Count
}

Write-Utf8NoBomFile -Path (Join-Path $runDirectory 'run-summary.json') -Content (($summary | ConvertTo-Json -Depth 8) + [Environment]::NewLine)

$markdownLines = @(
    '# Portal Documentation Evidence Run',
    '',
    ('Generated UTC: `{0}`' -f $summary.GeneratedAtUtc),
    ('Output directory: `{0}`' -f $runDirectory),
    '',
    '## Scope',
    '',
    '1. This package records documentation readiness, baseline inventory, public-doc, XML, JSDoc, and notification evidence.',
    '2. It does not modify source comments, commit generated output, read secret configuration, or connect to databases.',
    '3. Pending notification steps mean the local HIA-Documentation-Sys notify source was unavailable; they do not make the Portal build depend on that project.',
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
    ('Pending steps: `{0}`' -f $pendingSteps.Count),
    ''
)

Write-Utf8NoBomFile -Path (Join-Path $runDirectory 'README.md') -Content (($markdownLines -join [Environment]::NewLine) + [Environment]::NewLine)

Write-Host ('SUMMARY: Steps={0}; Failed={1}; Pending={2}' -f $steps.Count, $failedSteps.Count, $pendingSteps.Count)
Write-Host ('README: {0}' -f (Join-Path $runDirectory 'README.md'))

if ($failedSteps.Count -gt 0 -and -not $AllowFailures) {
    exit 1
}

exit 0
