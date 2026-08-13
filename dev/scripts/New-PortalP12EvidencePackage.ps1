<#
.SYNOPSIS
    Builds a P12 business-flow evidence package from read-only gates.

.DESCRIPTION
    <lang>
      <zh-CN>本脚本编排 P12.2 业务身份、P12.3 轻量待办、P12.4 业务权限审计等只读门禁，并可选执行解决方案构建，形成 P12.5 周期验收证据包。</zh-CN>
      <en>This script orchestrates the P12.2 business-identity, P12.3 lightweight work-item, and P12.4 business permission/audit read-only gates, with an optional solution build, into a P12.5 acceptance evidence package.</en>
    </lang>
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

# <lang>
#   <zh-CN>以 UTF-8 无 BOM 写入证据日志、JSON 和说明文件；本函数只写入本次运行产物，不修改源码或门禁脚本。</zh-CN>
#   <en>Write evidence logs, JSON, and readme files as UTF-8 without a BOM; this function writes only run artifacts and does not modify source or gate scripts.</en>
# </lang>
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

# <lang>
#   <zh-CN>仅为证据日志中的命令显示值添加引号；实际传给子进程的参数列表保持不变。</zh-CN>
#   <en>Add quoting only to command values displayed in evidence logs; the argument list passed to child processes remains unchanged.</en>
# </lang>
function Format-EvidenceArgument {
    param([string]$Value)

    if ($Value -match '\s|["'']') {
        return '"' + ($Value -replace '"', '\"') + '"'
    }

    return $Value
}

# <lang>
#   <zh-CN>优先使用项目约定的 PowerShell 7 路径，找不到时回退到 PATH 中的 pwsh；未找到则抛出明确错误，本函数本身不启动子进程。</zh-CN>
#   <en>Prefer the project-mandated PowerShell 7 path and fall back to pwsh on PATH; throw a clear error when neither exists, without starting a child process here.</en>
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
#   <zh-CN>执行一个配置的门禁或构建子进程并记录低敏感度元数据与输出；子进程失败会记录为 Failed，但不会被表述为真实环境已验收。</zh-CN>
#   <en>Run one configured gate or build child process and capture low-sensitivity metadata and output; a child failure is recorded as Failed and is not represented as proof of real-environment acceptance.</en>
# </lang>
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
            # <lang>
            #   <zh-CN>证据日志入库前去除行尾空白，避免自动生成物触发 Git whitespace 检查。</zh-CN>
            #   <en>Trim trailing whitespace before storing evidence logs so generated artifacts do not trigger Git whitespace checks.</en>
            # </lang>
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

# <lang>
#   <zh-CN>创建本次运行的证据目录；目录、日志和摘要只在脚本实际运行时生成，不会改变源代码或数据库。</zh-CN>
#   <en>Create the evidence directory for this run; the directory, logs, and summaries are generated only when the script runs and do not change source code or a database.</en>
# </lang>
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
# <lang>
#   <zh-CN>摘要只记录各门禁和可选构建的结果，并明确失败数量；不写入凭据、生产证明或其他秘密信息。</zh-CN>
#   <en>The summary records gate and optional-build results with an explicit failure count; it does not write credentials, production proof, or other secrets.</en>
# </lang>
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

# <lang>
#   <zh-CN>AllowFailures 只改变最终退出码；失败的子步骤仍保留在摘要和日志中，不能被静默视为通过。</zh-CN>
#   <en>AllowFailures changes only the final exit code; failed child steps remain in the summary and logs and must not be silently treated as passed.</en>
# </lang>
if ($failedSteps.Count -gt 0 -and -not $AllowFailures) {
    exit 1
}

exit 0
