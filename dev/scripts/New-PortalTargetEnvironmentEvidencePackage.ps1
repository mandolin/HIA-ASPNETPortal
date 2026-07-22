<#
.SYNOPSIS
    Builds a read-only P14.1 target-environment readiness evidence package.

.DESCRIPTION
    中文：本脚本用于 P14.1，把目标环境矩阵、当前可用证据和不阻塞 Pending 项汇总成证据包。
    它只读取仓库文件并调用现有只读门禁；不发布文件、不修改 IIS、不连接真实生产库、不写业务数据库、
    不读取外置敏感配置，也不伪造真实生产环境通过证据。
    English: This script builds the P14.1 target-environment readiness evidence package from the target matrix,
    currently available evidence, and non-blocking Pending items. It only reads repository files and invokes
    existing read-only gates; it does not publish files, change IIS, connect to real production databases, write
    business data, read external secret configuration, or claim unverified production evidence.
#>
[CmdletBinding()]
param(
    [ValidateSet('Dev', 'Test', 'Prod', 'Scan', 'LegacyIe')]
    [string]$Profile = 'Dev',

    [ValidatePattern('^https?://')]
    [string]$BaseUrl,

    [string]$MatrixPath,

    [string]$OutputRoot,

    [switch]$AllowFailures
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($MatrixPath)) {
    $MatrixPath = Join-Path $repoRoot 'work-zone/dev/plans/W-anp-P14.1-target-environment-matrix.md'
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = if (Test-Path -LiteralPath (Join-Path $repoRoot 'work-zone')) {
        Join-Path $repoRoot 'work-zone/dev/evidence/p14.1'
    }
    else {
        Join-Path $repoRoot 'temp/evidence/p14.1'
    }
}

$resolvedMatrixPath = (Resolve-Path -LiteralPath $MatrixPath).Path
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

function ConvertTo-RepoPath {
    param([string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootPrefix = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\') + '\'
    if ($fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ($fullPath.Substring($rootPrefix.Length) -replace '\\', '/')
    }

    return $fullPath
}

function Split-MarkdownTableRow {
    param([string]$Line)

    return @($Line.Trim().Trim('|') -split '\|' | ForEach-Object { $_.Trim() })
}

function Get-MatrixStatusLabels {
    param([string]$StatusCell)

    $matches = [regex]::Matches($StatusCell, '`([^`]+)`')
    if ($matches.Count -gt 0) {
        return @($matches | ForEach-Object { $_.Groups[1].Value.Trim() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    }

    return @($StatusCell -split '\+' | ForEach-Object { $_.Trim() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Read-TargetMatrix {
    param([string]$Path)

    $items = New-Object 'System.Collections.Generic.List[object]'
    $inMatrixTable = $false
    $lines = Get-Content -LiteralPath $Path -Encoding UTF8

    foreach ($line in $lines) {
        if ($line -match '^\|\s*领域\s*\|\s*证据项\s*\|\s*当前状态\s*\|') {
            $inMatrixTable = $true
            continue
        }

        if (-not $inMatrixTable) {
            continue
        }

        if ($line -match '^\|\s*-+') {
            continue
        }

        if ($line -notmatch '^\|') {
            break
        }

        $cells = Split-MarkdownTableRow -Line $line
        if ($cells.Count -lt 5) {
            continue
        }

        $statusLabels = @(Get-MatrixStatusLabels -StatusCell $cells[2])
        $items.Add([pscustomobject][ordered]@{
                Area = $cells[0]
                EvidenceItem = $cells[1]
                StatusCell = $cells[2]
                StatusLabels = $statusLabels
                CurrentEvidence = $cells[3]
                P14Handling = $cells[4]
            })
    }

    # 中文：显式转成 object[]，避免 PowerShell 动态枚举在单元素/多元素之间切换时触发绑定异常。
    # English: Convert explicitly to object[] so PowerShell dynamic enumeration does not fail on scalar/list transitions.
    return [object[]]$items.ToArray()
}

function Get-StatusCounts {
    param([object[]]$Items)

    $counts = [ordered]@{}
    foreach ($item in $Items) {
        foreach ($label in @($item.StatusLabels)) {
            if (-not $counts.Contains($label)) {
                $counts[$label] = 0
            }

            $counts[$label]++
        }
    }

    return [pscustomobject]$counts
}

function Find-LatestEvidence {
    param(
        [string]$Code,
        [string]$Description,
        [string]$RelativeRoot,
        [string]$FileName
    )

    $rootPath = Join-Path $repoRoot $RelativeRoot
    if (-not (Test-Path -LiteralPath $rootPath -PathType Container)) {
        return [pscustomobject][ordered]@{
            Code = $Code
            Description = $Description
            Status = 'Missing'
            FileName = $FileName
            Path = ''
            LastWriteUtc = ''
        }
    }

    $matches = @(Get-ChildItem -LiteralPath $rootPath -Directory -Recurse |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName $FileName) -PathType Leaf } |
        Sort-Object LastWriteTimeUtc -Descending)

    if ($matches.Count -eq 0) {
        return [pscustomobject][ordered]@{
            Code = $Code
            Description = $Description
            Status = 'Missing'
            FileName = $FileName
            Path = ''
            LastWriteUtc = ''
        }
    }

    $path = Join-Path $matches[0].FullName $FileName
    return [pscustomobject][ordered]@{
        Code = $Code
        Description = $Description
        Status = 'Present'
        FileName = $FileName
        Path = ConvertTo-RepoPath -Path $path
        LastWriteUtc = ([System.IO.FileInfo]$path).LastWriteTimeUtc.ToString('yyyy-MM-ddTHH:mm:ssZ')
    }
}

function Invoke-ReadinessStep {
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
        LogPath = ConvertTo-RepoPath -Path $LogPath
        StartedUtc = $startedAt.ToString('yyyy-MM-ddTHH:mm:ssZ')
        FinishedUtc = $finishedAt.ToString('yyyy-MM-ddTHH:mm:ssZ')
        Command = ($displayCommand -join ' ')
    }

    $steps.Add($result)
    Write-Host ('[{0}] {1} -> {2}' -f $status.ToUpperInvariant(), $Name, $result.LogPath)
}

New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null
Write-Host ('Target environment evidence directory: {0}' -f $runDirectory)

$matrixItems = @(Read-TargetMatrix -Path $resolvedMatrixPath)
$matrixStatusCounts = Get-StatusCounts -Items $matrixItems
$evidenceInputs = @(
    Find-LatestEvidence -Code 'P13.1-RELEASE-MANIFEST' -Description '发布包 manifest' -RelativeRoot 'work-zone/dev/evidence/p13.1' -FileName 'release-manifest.json'
    Find-LatestEvidence -Code 'P13.2-OPERATIONS' -Description '运维证据包' -RelativeRoot 'work-zone/dev/evidence/p13.2' -FileName 'run-summary.json'
    Find-LatestEvidence -Code 'P13.3-DOCUMENTATION' -Description '文档化 readiness 证据包' -RelativeRoot 'work-zone/dev/evidence/p13.3' -FileName 'run-summary.json'
    Find-LatestEvidence -Code 'P13.4-RELEASE-SUMMARY' -Description '版本节奏与 release summary' -RelativeRoot 'work-zone/dev/evidence/p13.4' -FileName 'release-summary.json'
    Find-LatestEvidence -Code 'P12.5-BUSINESS-SCENARIO' -Description '员工资料更正样板验收证据' -RelativeRoot 'work-zone/dev/evidence/p12.5' -FileName 'run-summary.json'
    Find-LatestEvidence -Code 'P10.5-COMPLIANCE' -Description '安全合规验收证据包' -RelativeRoot 'work-zone/dev/evidence/p10.5' -FileName 'run-summary.json'
)

$publishReadinessLog = Join-Path $runDirectory 'publish-readiness.log.md'
Invoke-ReadinessStep `
    -Name 'Publish readiness' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalPublishReadiness.ps1') `
    -Arguments @() `
    -LogPath $publishReadinessLog

$operationsJson = Join-Path $runDirectory 'operations-readiness.json'
Invoke-ReadinessStep `
    -Name 'Operations readiness' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalOperationsReadiness.ps1') `
    -Arguments @('-Profile', $Profile, '-OutputJson', $operationsJson) `
    -LogPath (Join-Path $runDirectory 'operations-readiness.log.md')

$sqlMatrixJson = Join-Path $runDirectory 'sql-version-matrix.json'
Invoke-ReadinessStep `
    -Name 'SQL Server version matrix static preflight' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalSqlVersionMatrix.ps1') `
    -Arguments @('-OutputJson', $sqlMatrixJson) `
    -LogPath (Join-Path $runDirectory 'sql-version-matrix.log.md')

$complianceJson = Join-Path $runDirectory 'compliance-baseline.json'
$complianceArgs = @('-Profile', $Profile, '-OutputJson', $complianceJson)
if (-not [string]::IsNullOrWhiteSpace($BaseUrl)) {
    $complianceArgs += @('-BaseUrl', $BaseUrl)
}

Invoke-ReadinessStep `
    -Name 'Compliance baseline' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalComplianceBaseline.ps1') `
    -Arguments $complianceArgs `
    -LogPath (Join-Path $runDirectory 'compliance-baseline.log.md')

Invoke-ReadinessStep `
    -Name 'Public documentation gate' `
    -ScriptPath (Join-Path $PSScriptRoot 'Test-PortalPublicDocumentation.ps1') `
    -Arguments @() `
    -LogPath (Join-Path $runDirectory 'public-documentation.log.md')

$failedSteps = @($steps | Where-Object { $_.ExitCode -ne 0 })
$pendingTargetItems = @($matrixItems | Where-Object { @($_.StatusLabels) -contains 'PendingTargetEnvironment' })
$readyLocalItems = @($matrixItems | Where-Object { @($_.StatusLabels) -contains 'ReadyLocal' })
$readyNearTargetItems = @($matrixItems | Where-Object { @($_.StatusLabels) -contains 'ReadyNearTarget' })
$deferredItems = @($matrixItems | Where-Object { @($_.StatusLabels) -contains 'Deferred' })

$summary = [pscustomobject][ordered]@{
    SchemaVersion = 'p14.1.target-environment-readiness.v1'
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Profile = $Profile
    BaseUrl = $BaseUrl
    RunDirectory = ConvertTo-RepoPath -Path $runDirectory
    Matrix = [pscustomobject][ordered]@{
        Path = ConvertTo-RepoPath -Path $resolvedMatrixPath
        ItemCount = $matrixItems.Count
        StatusCounts = $matrixStatusCounts
        Items = $matrixItems
    }
    EvidenceInputs = $evidenceInputs
    Steps = $steps
    Summary = [pscustomobject][ordered]@{
        FailedStepCount = $failedSteps.Count
        ReadyLocalItemCount = $readyLocalItems.Count
        ReadyNearTargetItemCount = $readyNearTargetItems.Count
        PendingTargetEnvironmentItemCount = $pendingTargetItems.Count
        DeferredItemCount = $deferredItems.Count
        ReadyForP14_2NearTargetDrill = ($failedSteps.Count -eq 0)
        RealProductionEvidenceClaimed = $false
    }
    Boundary = @(
        'This package is read-only.',
        'ReadyForP14_2NearTargetDrill means local or near-target drill input is available; it does not mean real production IIS/TLS/ACL, target SQL Server, enterprise scan, or business signoff passed.',
        'PendingTargetEnvironment items remain non-blocking unless a later P14 stage explicitly promotes them to required input.'
    )
}

$summaryJsonPath = Join-Path $runDirectory 'target-environment-readiness.json'
Write-Utf8NoBomFile -Path $summaryJsonPath -Content (($summary | ConvertTo-Json -Depth 12) + [Environment]::NewLine)

$markdownLines = @(
    '# Portal Target Environment Readiness Evidence',
    '',
    ('Profile: `{0}`' -f $Profile),
    ('Generated UTC: `{0}`' -f $summary.GeneratedAtUtc),
    ('Output directory: `{0}`' -f $summary.RunDirectory),
    '',
    '## Conclusion',
    '',
    ('Ready for P14.2 near-target drill: `{0}`' -f $summary.Summary.ReadyForP14_2NearTargetDrill),
    ('Failed read-only steps: `{0}`' -f $summary.Summary.FailedStepCount),
    ('Real production evidence claimed: `{0}`' -f $summary.Summary.RealProductionEvidenceClaimed),
    '',
    '## Matrix Summary',
    '',
    '| Status | Count |',
    '| --- | --- |'
)

foreach ($property in $matrixStatusCounts.PSObject.Properties) {
    $markdownLines += ('| `{0}` | {1} |' -f $property.Name, $property.Value)
}

$markdownLines += @(
    '',
    '## Read-Only Gates',
    '',
    '| Gate | Status | Evidence |',
    '| --- | --- | --- |'
)

foreach ($step in $steps) {
    $markdownLines += ('| {0} | {1} | `{2}` |' -f $step.Name, $step.Status, $step.LogPath)
}

$markdownLines += @(
    '',
    '## Existing Evidence Inputs',
    '',
    '| Code | Status | Evidence |',
    '| --- | --- | --- |'
)

foreach ($input in $evidenceInputs) {
    $path = if ([string]::IsNullOrWhiteSpace($input.Path)) { '(missing)' } else { $input.Path }
    $markdownLines += ('| `{0}` | {1} | `{2}` |' -f $input.Code, $input.Status, $path)
}

$markdownLines += @(
    '',
    '## Pending Target Environment Items',
    '',
    '| Area | Evidence Item | Handling |',
    '| --- | --- | --- |'
)

foreach ($item in $pendingTargetItems) {
    $markdownLines += ('| {0} | {1} | {2} |' -f $item.Area, $item.EvidenceItem, (($item.P14Handling -replace '\|', '/') -replace "`r?`n", ' '))
}

$markdownLines += @(
    '',
    '## Boundary',
    '',
    '1. This package does not publish files, modify IIS, connect to production databases, write business data, or read external secret configuration.',
    '2. `ReadyForP14_2NearTargetDrill` only means P14.2 can start near-target release rehearsal with current evidence.',
    '3. `PendingTargetEnvironment` items remain visible and must be supplemented when the corresponding target environment or report is available.',
    ''
)

Write-Utf8NoBomFile -Path (Join-Path $runDirectory 'README.md') -Content (($markdownLines -join [Environment]::NewLine) + [Environment]::NewLine)

Write-Host ('Target environment readiness JSON: {0}' -f (ConvertTo-RepoPath -Path $summaryJsonPath))
Write-Host ('Target environment readiness README: {0}' -f (ConvertTo-RepoPath -Path (Join-Path $runDirectory 'README.md')))

if ($failedSteps.Count -gt 0 -and -not $AllowFailures) {
    throw ('P14.1 target-environment readiness package contains failed read-only steps: {0}' -f $failedSteps.Count)
}
