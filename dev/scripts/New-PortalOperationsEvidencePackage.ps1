<#
.SYNOPSIS
    Builds a read-only operations evidence package for Portal maintenance review.

.DESCRIPTION
    <lang>
      <zh-CN>本脚本编排 P13.2 运维只读门禁，包括运维 readiness、日志维护 dry-run、发布资源、公开文档、基础合规和默认凭据风险检查。它不登录、不写数据库、不读取外置敏感配置、不创建计划任务，也不删除日志。</zh-CN>
      <en>This script orchestrates the P13.2 read-only operations gates: operations readiness, log-maintenance dry run, publish resources, public documentation, baseline compliance, and default-credential risk checks. It does not sign in, write databases, read external secrets, create scheduled tasks, or delete logs.</en>
    </lang>
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

# <lang>
#   <zh-CN>以 UTF-8 无 BOM 创建证据日志、JSON 和说明文件；只写本次证据目录，不修改门禁、数据库或源代码。</zh-CN>
#   <en>Write evidence logs, JSON, and readme files as UTF-8 without a BOM; write only under this run directory and do not modify gates, databases, or source.</en>
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
#   <zh-CN>仅为日志中的命令显示值添加引号；传给子进程的实际参数列表保持不变。</zh-CN>
#   <en>Add quoting only to command values displayed in logs; preserve the actual argument list passed to child processes.</en>
# </lang>
function Format-EvidenceArgument {
    param([string]$Value)

    if ($Value -match '\s|["'']') {
        return '"' + ($Value -replace '"', '\"') + '"'
    }

    return $Value
}

# <lang>
#   <zh-CN>优先返回项目约定的 PowerShell 7 执行器，找不到时回退到 PATH；未找到则抛出错误，本函数不启动子进程。</zh-CN>
#   <en>Prefer the project-mandated PowerShell 7 executable and fall back to PATH; throw when none is found, without starting a child process here.</en>
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
#   <zh-CN>运行一个运维只读门禁并捕获低敏输出；子步骤失败写入证据并参与汇总，不被转换为生产或目标环境通过。</zh-CN>
#   <en>Run one read-only operations gate and capture low-sensitivity output; child failure remains in evidence and summary and is not converted into production or target-environment success.</en>
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
#   <zh-CN>证据目录只在本脚本实际运行时创建；后续门禁仍由调用方环境和参数决定。</zh-CN>
#   <en>Create the evidence directory only when this script actually runs; subsequent gates remain determined by the caller's environment and arguments.</en>
# </lang>
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
# <lang>
#   <zh-CN>摘要保留 Profile、可选 URL/日志目录、每个门禁结果和失败数量；不保存凭据或外置秘密。</zh-CN>
#   <en>Keep the profile, optional URL/log directory, each gate result, and failure count in the summary; do not store credentials or external secrets.</en>
# </lang>
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

# <lang>
#   <zh-CN>AllowFailures 只改变最终退出码；失败步骤仍保留在日志、摘要和 README 中。</zh-CN>
#   <en>AllowFailures changes only the final exit code; failed steps remain in the logs, summary, and README.</en>
# </lang>
if ($failedSteps.Count -gt 0 -and -not $AllowFailures) {
    exit 1
}

exit 0
