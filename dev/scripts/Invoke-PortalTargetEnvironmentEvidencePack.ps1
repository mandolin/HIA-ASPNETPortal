#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>一条命令产出 L7（`M-ANP-TRUSTED-PORTAL`）目标环境证据包。</zh-CN>
  <en>Produces the L7 (`M-ANP-TRUSTED-PORTAL`) target-environment evidence pack with a single command.</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>`C-anp-P28`（`W-anp-P155`）交付，对应验收判据 V5「拿到目标环境后，一条命令产出 L7 所需全部证据」。

  工作方式（读台账 `dev/target-environment-evidence.json`，逐项处置）：
    - `mode=local`   立即执行生成器，收日志进证据包；
    - `mode=targetEnv` **显式跳过**并写明缺失的前置条件（P139 口径：环境前置缺失 = exit 2 语义，
      不静默通过、不伪装失败）；目标环境就绪后同一条命令即自动出证；
    - `mode=manual`  登记为待人工出具（如业务签收），不执行任何脚本。

  边界：本脚本**不发布文件、不改 IIS、不连生产库、不写业务数据、不读取外置敏感配置**；
  只执行台账登记的只读生成器。任何生成器失败 → 整个证据包 exit 1；跳过与待人工项不计为失败，
  但会逐条列在摘要里（"没做"必须看得见）。

  用法（仓库根目录）：
    & 'dev\scripts\Invoke-PortalTargetEnvironmentEvidencePack.ps1'
    & 'dev\scripts\Invoke-PortalTargetEnvironmentEvidencePack.ps1' -OutputRoot <目录> -BaseUrl <目标终结点>
  产物：`<OutputRoot>\<时间戳>-target-environment\`（manifest.json + 各步骤日志 + summary.md）
  退出码：0 无失败（允许有跳过/待人工项）；1 有生成器失败；2 台账或前置缺失。
  </zh-CN>
  <en>`C-anp-P28` (`W-anp-P155`) deliverable, matching acceptance criterion V5: "once the target environment is available, one
  command produces all evidence required by L7".

  How it works (reads the registry `dev/target-environment-evidence.json` and handles each item):
    - `mode=local`   runs the generator immediately and collects its log into the pack;
    - `mode=targetEnv` **skips explicitly**, stating the missing prerequisites (the P139 convention: a missing environment
      prerequisite is exit-2 semantics — never a silent pass, never a disguised failure); once the target environment exists the
      same command produces the evidence automatically;
    - `mode=manual`  registers the item as pending human output (for example business sign-off) and runs nothing.

  Boundary: this script **does not publish files, change IIS, connect to production databases, write business data, or read
  external secret configuration**; it only runs the read-only generators registered in the registry. Any generator failure makes
  the whole pack exit 1; skips and manual items are not failures but are listed individually in the summary ("not done" must be
  visible).

  Usage (repository root):
    & 'dev\scripts\Invoke-PortalTargetEnvironmentEvidencePack.ps1'
    & 'dev\scripts\Invoke-PortalTargetEnvironmentEvidencePack.ps1' -OutputRoot <dir> -BaseUrl <target endpoint>
  Output: `<OutputRoot>\<timestamp>-target-environment\` (manifest.json + per-step logs + summary.md)
  Exit codes: 0 no failures (skips and manual items allowed); 1 a generator failed; 2 missing registry or prerequisite.
  </en>
</lang>
.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Invoke-PortalTargetEnvironmentEvidencePack.ps1'   # 本机一键出证（环境项显式跳过）</zh-CN>
  <en>& 'dev\scripts\Invoke-PortalTargetEnvironmentEvidencePack.ps1'   # one-command local evidence (environment items skipped explicitly)</en>
</lang>
.NOTES
<lang>
  <zh-CN>演进：`W-anp-P155` 交付；台账由 `Test-PortalTargetEnvironmentEvidenceMap.ps1` 看守。</zh-CN>
  <en>Provenance: delivered by `W-anp-P155`; the registry is guarded by `Test-PortalTargetEnvironmentEvidenceMap.ps1`.</en>
</lang>
#>

[CmdletBinding()]
param(
    [string] $RegistryPath,

    [string] $OutputRoot,

    [string] $BaseUrl
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($RegistryPath)) {
    $RegistryPath = Join-Path $repoRoot 'dev\target-environment-evidence.json'
}
if (-not (Test-Path -LiteralPath $RegistryPath)) {
    Write-Host '[PREREQ-MISSING] 证据清单台账不存在：' + $RegistryPath + '（exit 2）'
    exit 2
}
$registry = Get-Content -LiteralPath $RegistryPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = if (Test-Path -LiteralPath (Join-Path $repoRoot 'work-zone')) {
        Join-Path $repoRoot 'work-zone\dev\evidence\p155'
    }
    else {
        Join-Path $repoRoot 'temp\evidence\p155'
    }
}
# <lang><zh-CN>先确保输出根存在，再取全路径——`Resolve-Path` 对不存在的目录返回空，会让 `Join-Path` 收到 null
# （首跑即暴露：目录尚未创建时直接解析会失败）。</zh-CN>
# <en>Ensure the output root exists before resolving it — `Resolve-Path` returns nothing for a missing directory, which would
# hand `Join-Path` a null (exposed on the first run: resolving before creating the directory fails).</en></lang>
New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null
$runDirectory = Join-Path ([System.IO.Path]::GetFullPath($OutputRoot)) ('{0}-target-environment' -f (Get-Date).ToString('yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Content
    )
    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

$pwshPath = if (Get-Command 'pwsh' -ErrorAction SilentlyContinue) { 'pwsh' } else { 'powershell' }
$results = New-Object 'System.Collections.Generic.List[object]'
$failed = 0

foreach ($item in @($registry.items)) {
    $id = [string]$item.id
    $mode = [string]$item.mode
    $record = [ordered]@{
        id = $id
        criterion = [string]$item.criterion
        name = [string]$item.name
        mode = $mode
        status = 'Skipped'
        exitCode = $null
        log = $null
        reason = $null
    }

    if ($mode -eq 'local') {
        $scriptRel = [string]$item.generator.script
        $scriptPath = Join-Path $repoRoot $scriptRel
        if (-not (Test-Path -LiteralPath $scriptPath)) {
            $record.status = 'Failed'
            $record.reason = "生成器脚本不存在：$scriptRel"
            $failed += 1
            $results.Add([pscustomobject]$record)
            continue
        }
        $generatorArgs = @()
        foreach ($arg in @($item.generator.args)) { $generatorArgs += [string]$arg }
        $logPath = Join-Path $runDirectory ($id + '.log.md')
        $record.log = $logPath
        $output = & $pwshPath -NoLogo -NoProfile -ExecutionPolicy Bypass -File $scriptPath @generatorArgs 2>&1
        $code = $LASTEXITCODE
        Write-Utf8NoBomFile -Path $logPath -Content (($output | ForEach-Object { $_.ToString() }) -join "`r`n")
        $record.exitCode = $code
        if ($code -eq 0) {
            $record.status = 'Collected'
        }
        else {
            $record.status = 'Failed'
            $record.reason = "生成器退出码 $code"
            $failed += 1
        }
    }
    elseif ($mode -eq 'targetEnv') {
        $record.reason = "需要目标环境，前置条件未满足：$([string]$item.prereqs)"
        if (-not [string]::IsNullOrWhiteSpace($BaseUrl)) {
            $scriptRel = [string]$item.generator.script
            if (-not [string]::IsNullOrWhiteSpace($scriptRel)) {
                $scriptPath = Join-Path $repoRoot $scriptRel
                if (Test-Path -LiteralPath $scriptPath) {
                    $generatorArgs = @()
                    foreach ($arg in @($item.generator.args)) { $generatorArgs += [string]$arg }
                    # <lang>
                    #   <zh-CN>**只对声明了 `BaseUrl` 的生成器追加该参数**：此前无条件追加，会让不接受
                    #     `BaseUrl` 的生成器（如 `Test-PortalSqlVersionMatrix.ps1`、
                    #     `New-PortalEnterpriseScanBaseline.ps1`）直接以
                    #     “找不到与参数名称 'BaseUrl' 匹配的参数” 失败——这不是环境缺失，而是接线错误，
                    #     会让本可出证的目标环境项被误记为 Failed。</zh-CN>
                    #   <en>**Append `BaseUrl` only for generators that declare it**: appending it
                    #     unconditionally made generators that do not accept `BaseUrl`
                    #     (e.g. `Test-PortalSqlVersionMatrix.ps1`, `New-PortalEnterpriseScanBaseline.ps1`)
                    #     fail immediately with “A parameter cannot be found that matches parameter name
                    #     'BaseUrl'”. That is a wiring error rather than a missing environment, and it
                    #     wrongly recorded collectible target-environment items as Failed.</en>
                    # </lang>
                    $generatorCommand = Get-Command -Name $scriptPath -ErrorAction SilentlyContinue
                    if ($generatorCommand -and $generatorCommand.Parameters.ContainsKey('BaseUrl')) {
                        $generatorArgs += @('-BaseUrl', $BaseUrl)
                    }
                    $logPath = Join-Path $runDirectory ($id + '.log.md')
                    $record.log = $logPath
                    $output = & $pwshPath -NoLogo -NoProfile -ExecutionPolicy Bypass -File $scriptPath @generatorArgs 2>&1
                    $code = $LASTEXITCODE
                    Write-Utf8NoBomFile -Path $logPath -Content (($output | ForEach-Object { $_.ToString() }) -join "`r`n")
                    $record.exitCode = $code
                    if ($code -eq 0) { $record.status = 'Collected' } else { $record.status = 'Failed'; $record.reason = "生成器退出码 $code"; $failed += 1 }
                }
            }
        }
    }
    elseif ($mode -eq 'manual') {
        $record.reason = "人工产物，待业务方出具：$([string]$item.prereqs)"
    }
    else {
        $record.status = 'Failed'
        $record.reason = "未知 mode：$mode"
        $failed += 1
    }
    $results.Add([pscustomobject]$record)
}

# <lang><zh-CN>注意：哈希字面量的**值**里不能直接写 `@($list)` 或 `@($list).Count`——该位置会被当作 splat 解析，
# 运行期报 "Argument types do not match"（本脚本首跑即暴露）。一律先算进变量，再作为值写入。</zh-CN>
# <en>Note: a hash literal's **value** must not contain `@($list)` or `@($list).Count` directly — that position is parsed as a
# splat and fails at runtime with "Argument types do not match" (exposed on this script's first run). Compute into a variable
# first, then use it as the value.</en></lang>
$itemList = $results.ToArray()
$totalCount = $itemList.Count
$collectedCount = @($itemList | Where-Object { $_.status -eq 'Collected' }).Count
$skippedCount = @($itemList | Where-Object { $_.status -eq 'Skipped' }).Count
$manualCount = @($itemList | Where-Object { $_.mode -eq 'manual' }).Count

$manifest = [ordered]@{
    schemaVersion = 1
    workItem = 'W-anp-P155'
    milestone = [string]$registry.milestone
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    baseUrl = $BaseUrl
    runDirectory = $runDirectory
    counts = [ordered]@{
        total = $totalCount
        collected = $collectedCount
        skipped = $skippedCount
        manual = $manualCount
        failed = $failed
    }
    items = $itemList
}
$manifestPath = Join-Path $runDirectory 'manifest.json'
Write-Utf8NoBomFile -Path $manifestPath -Content ($manifest | ConvertTo-Json -Depth 8)

$summary = New-Object 'System.Collections.Generic.List[string]'
$summary.Add('# L7 目标环境证据包（W-anp-P155）')
$summary.Add('')
$summary.Add(('里程碑：{0}' -f $manifest.milestone))
$summary.Add(('生成时间（UTC）：{0}' -f $manifest.generatedUtc))
$summary.Add(('输出目录：{0}' -f $runDirectory))
$summary.Add(('条目：共 {0}｜已出证 {1}｜显式跳过 {2}｜待人工 {3}｜失败 {4}' -f $manifest.counts.total, $manifest.counts.collected, $manifest.counts.skipped, $manifest.counts.manual, $manifest.counts.failed))
$summary.Add('')
foreach ($r in $results) {
    $summary.Add(('- **{0}**（{1} / {2}）→ {3}{4}' -f $r.id, $r.criterion, $r.mode, $r.status, $(if ($r.reason) { '：' + $r.reason } else { '' })))
}
$summary.Add('')
$summary.Add('> 跳过与待人工项**不是通过**：它们标明"尚未取得目标环境证据"。目标环境就绪后重跑同一命令即可出证。')
Write-Utf8NoBomFile -Path (Join-Path $runDirectory 'summary.md') -Content ($summary -join "`r`n")

Write-Host ''
Write-Host '=== L7 目标环境证据包（W-anp-P155）==='
Write-Host ('共 {0} 项｜已出证 {1}｜显式跳过 {2}｜待人工 {3}｜失败 {4}' -f $manifest.counts.total, $manifest.counts.collected, $manifest.counts.skipped, $manifest.counts.manual, $manifest.counts.failed)
foreach ($r in $results) {
    Write-Host ('  [{0}] {1}（{2}）{3}' -f $r.status, $r.id, $r.criterion, $(if ($r.reason) { ' — ' + $r.reason } else { '' }))
}
Write-Host ('证据包目录：' + $runDirectory)

if ($failed -gt 0) { exit 1 }
exit 0
