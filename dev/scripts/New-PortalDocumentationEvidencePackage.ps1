<#
.SYNOPSIS
    Builds a documentation toolchain evidence package for P13.3.

.DESCRIPTION
    <lang>
      <zh-CN>本脚本编排文档化 readiness、文档化 baseline、公开文档门禁、.NET XML 文档验证、HIA JSDoc pilot 和 HIA-Documentation-Sys 通知读取。它不修改源码注释、不提交生成物、不读取敏感配置、不连接数据库，也不把 HIA-Documentation-Sys 变成本项目构建硬依赖。</zh-CN>
      <en>This script orchestrates documentation readiness, documentation baseline, public documentation gates, .NET XML documentation verification, the HIA JSDoc pilot, and HIA-Documentation-Sys notification pull evidence. It does not modify source comments, commit generated output, read secret configuration, connect to databases, or make HIA-Documentation-Sys a hard build dependency.</en>
    </lang>
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

# <lang>
#   <zh-CN>以 UTF-8 无 BOM 创建文档证据日志、JSON 和 README；只写本次运行产物，不修改源码或文档工具。</zh-CN>
#   <en>Write documentation evidence logs, JSON, and README files as UTF-8 without a BOM; write only run artifacts and do not modify source or documentation tools.</en>
# </lang>
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

# <lang>
#   <zh-CN>仅规范化证据日志中的命令显示文本；不改变实际传递的子进程参数。</zh-CN>
#   <en>Normalize only command text displayed in evidence logs; do not change the arguments actually passed to child processes.</en>
# </lang>
function Format-EvidenceArgument {
    param([string]$Value)

    if ($Value -match '\s|["'']') {
        return '"' + ($Value -replace '"', '\"') + '"'
    }

    return $Value
}

# <lang>
#   <zh-CN>优先使用 PowerShell 7 固定路径并回退到 PATH；未找到时抛错，本函数不执行门禁。</zh-CN>
#   <en>Prefer the fixed PowerShell 7 path and fall back to PATH; throw when missing, without executing a gate here.</en>
# </lang>
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

# <lang>
#   <zh-CN>执行一个文档证据步骤，Optional 失败映射为 Pending；普通失败仍为 Failed，并捕获低敏输出。</zh-CN>
#   <en>Run one documentation evidence step, mapping an Optional failure to Pending; ordinary failure remains Failed while low-sensitivity output is captured.</en>
# </lang>
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

# <lang>
#   <zh-CN>证据目录只在实际运行时创建，Skip 开关仅跳过对应步骤，不代表该步骤已通过。</zh-CN>
#   <en>Create the evidence directory only during execution; Skip switches omit their steps and do not mean those steps passed.</en>
# </lang>
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
# <lang>
#   <zh-CN>摘要同时保留 Failed 与 Pending，区分实际失败和可选通知源不可用；不写凭据或生产证明。</zh-CN>
#   <en>Keep Failed and Pending distinct in the summary so optional notification-source unavailability is not confused with failure; do not write credentials or production proof.</en>
# </lang>
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

# <lang>
#   <zh-CN>AllowFailures 只控制 Failed 是否转为非零退出；Pending 和已记录的失败不会被静默删除。</zh-CN>
#   <en>AllowFailures controls only whether Failed results produce a non-zero exit; Pending and recorded failures are never silently removed.</en>
# </lang>
if ($failedSteps.Count -gt 0 -and -not $AllowFailures) {
    exit 1
}

exit 0
