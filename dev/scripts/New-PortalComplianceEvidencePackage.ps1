<#
.SYNOPSIS
.LANG en
Creates a compliance evidence package from existing read-only gates.

.LANG zh-CN
基于现有只读门禁生成合规证据包。

.DESCRIPTION
<lang>
  <en>Run the compliance baseline, default-credential risk gate, and public documentation gate, then write logs and a summary under a timestamped evidence directory. The script orchestrates evidence only; it does not sign in, modify a database, read secrets, or upload results.</en>
  <zh-CN>运行合规基线、默认凭据风险门禁和公开文档门禁，并在带时间戳的证据目录下写入日志和摘要。本脚本只编排证据，不登录、不修改数据库、不读取密钥，也不上传结果。</zh-CN>
</lang>

.PARAMETER Profile
.LANG en
Compliance profile name used by downstream gates and the evidence directory name.

.LANG zh-CN
传给下游门禁并用于证据目录命名的合规 profile。

.PARAMETER BaseUrl
.LANG en
Optional portal URL passed to gates that can inspect HTTP responses.

.LANG zh-CN
可选门户 URL，会传给能够检查 HTTP 响应的门禁。

.PARAMETER OutputRoot
.LANG en
Optional evidence root. Defaults to WorkZone evidence when WorkZone exists, otherwise temp.

.LANG zh-CN
可选证据根目录。存在 WorkZone 时默认写入 WorkZone evidence，否则写入 temp。

.PARAMETER AllowFailures
.LANG en
Writes the package even when one or more gates fail, instead of returning a failing exit code.

.LANG zh-CN
即使一个或多个门禁失败也写出证据包，而不是返回失败退出码。
#>
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

# <lang>
#   <zh-CN>以 UTF-8 无 BOM 写入门禁日志、JSON 和 README，并只创建调用方选择的证据目录。</zh-CN>
#   <en>Write gate logs, JSON, and README as UTF-8 without a BOM, creating only the evidence directory selected by the caller.</en>
# </lang>
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

# <lang>
#   <zh-CN>为下游门禁命令参数做最小显示转义，避免日志中的空格或引号破坏命令边界。</zh-CN>
#   <en>Apply minimal display escaping to downstream gate arguments so spaces or quotes do not break logged command boundaries.</en>
# </lang>
function Format-EvidenceArgument {
    param([string]$Value)

    if ($Value -match '\s|["'']') {
        return '"' + ($Value -replace '"', '\"') + '"'
    }

    return $Value
}

# <lang>
#   <zh-CN>解析 PowerShell 7 可执行文件；只用于受控子进程编排，不回退到 Windows PowerShell 5.1。</zh-CN>
#   <en>Resolve the PowerShell 7 executable for controlled child-process orchestration without falling back to Windows PowerShell 5.1.</en>
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
#   <zh-CN>运行一个只读门禁子步骤、保存低敏日志和退出事实；不把失败转化为成功。</zh-CN>
#   <en>Run one read-only gate step, persist low-sensitivity logs and exit facts, and never convert failure into success.</en>
# </lang>
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

# <lang>
#   <zh-CN>只创建本次证据包目录；下游脚本仍由显式步骤决定，不扫描或修改生产目录。</zh-CN>
#   <en>Create only the current evidence-package directory; downstream scripts remain explicit steps, with no production-directory scan or mutation.</en>
# </lang>
if (-not (Test-Path -LiteralPath $runDirectory)) {
    New-Item -ItemType Directory -Path $runDirectory | Out-Null
}

# <lang>
#   <zh-CN>本脚本只编排已有只读门禁，形成可留存证据包；不写数据库、不尝试登录、不读取 secret 文件。</zh-CN>
#   <en>This script only orchestrates existing read-only gates into an evidence package; it never writes databases, attempts sign-in, or reads secret files.</en>
# </lang>
# <lang>
#   <zh-CN>输出证据目录位置并保持低敏；不打印连接串、密码、Token、Cookie 或证书私钥。</zh-CN>
#   <en>Display the evidence-directory location while keeping output low-sensitivity; never print connection strings, passwords, tokens, cookies, or certificate private keys.</en>
# </lang>
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

# <lang>
#   <zh-CN>汇总每个子步骤的退出事实和失败数量；AllowFailures 只改变最终退出策略，不伪造步骤状态。</zh-CN>
#   <en>Summarize each child-step exit fact and failure count; AllowFailures changes only final exit policy and never falsifies step status.</en>
# </lang>
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

# <lang>
#   <zh-CN>生成合规证据 README，明确只读、低敏和环境解释边界，不上传结果。</zh-CN>
#   <en>Generate the compliance-evidence README with explicit read-only, low-sensitivity, and environment interpretation boundaries without uploading results.</en>
# </lang>
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

# <lang>
#   <zh-CN>存在失败且未显式 AllowFailures 时返回非零；AllowFailures 不改变日志和摘要中的失败事实。</zh-CN>
#   <en>Return non-zero when failures exist without explicit AllowFailures; AllowFailures does not alter failure facts in logs or summaries.</en>
# </lang>
if ($failedSteps.Count -gt 0 -and -not $AllowFailures) {
    exit 1
}

exit 0
