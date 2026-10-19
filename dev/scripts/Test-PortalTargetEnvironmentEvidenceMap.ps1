#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>看守 L7 目标环境证据清单台账的完整性与覆盖面（L0）。</zh-CN>
  <en>Guards the completeness and coverage of the L7 target-environment evidence registry (L0).</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>`C-anp-P28`（`W-anp-P155`）交付，对应验收判据 V5「拿到目标环境后，一条命令产出 L7 所需全部证据」。
  一键证据包 `Invoke-PortalTargetEnvironmentEvidencePack.ps1` 按台账 `dev/target-environment-evidence.json`
  逐项出证；本门禁保证这份台账**不漏、不假、不腐**：

    A1 条目字段齐备：id / criterion / name / mode / owner / reEntry / basis 必填，id 不得重复，
       mode 只能取 local / targetEnv / manual。
    A2 有 generator 的条目，其脚本必须在磁盘存在、args 必须是数组（禁止登记跑不起来的命令）。
    A3 覆盖面：里程碑 `M-ANP-TRUSTED-PORTAL` 的判据与阻断项必须都有条目——
       当前要求 T1 / T4 / T5 / blocker-1 / blocker-2 / blocker-3 / L8-handoff 全覆盖，
       缺一项即失败（防止"清单漏登记 = 环境到了也出不全证"）。
    A4 模式语义自洽：local 必须有 generator；targetEnv 必须写清 prereqs（否则"跳过"没有理由）；
       manual 必须有 owner 与 reEntry。
    A5 可发现性：公开运维文档必须点名一键证据包与台账文件名，否则"有工具没人知道"。

  证明边界：只证明台账结构、覆盖面与可执行性；**不证明任何证据本身已取得**（targetEnv 项当前一律
  显式跳过，由证据包运行时如实呈现），也不证明证据内容满足 L7 判据（那需要目标环境与人工判读）。

  用法（仓库根目录）：& 'dev\scripts\Test-PortalTargetEnvironmentEvidenceMap.ps1'
  产物：temp\p155\evidence-map-stats.json（gitignore）。
  退出码：0 通过；1 断言失败；2 台账或文档前置缺失。
  </zh-CN>
  <en>`C-anp-P28` (`W-anp-P155`) deliverable, matching acceptance criterion V5: "once the target environment is available, one
  command produces all evidence required by L7". The one-command pack `Invoke-PortalTargetEnvironmentEvidencePack.ps1` produces
  evidence item by item from the registry `dev/target-environment-evidence.json`; this gate keeps that registry **complete,
  truthful and unrotted**:

    A1 item fields: id / criterion / name / mode / owner / reEntry / basis are required, ids must be unique, and mode must be
       one of local / targetEnv / manual.
    A2 for items with a generator, the script must exist on disk and args must be an array (registering a command that cannot run
       is forbidden).
    A3 coverage: every criterion and blocker of the milestone `M-ANP-TRUSTED-PORTAL` must have an item — currently T1 / T4 / T5 /
       blocker-1 / blocker-2 / blocker-3 / L8-handoff must all be covered; a missing one fails (so "an unregistered item" cannot
       mean "the environment arrives but the evidence is incomplete").
    A4 mode semantics: local requires a generator; targetEnv must state prereqs (otherwise the "skip" has no reason); manual
       requires owner and reEntry.
    A5 discoverability: the public operations doc must name the one-command pack and the registry file (otherwise the tool exists
       but nobody knows).

  Proof boundary: proves only the registry's structure, coverage and executability; does **not** prove that any evidence has been
  obtained (targetEnv items are skipped explicitly, as the pack reports at runtime), and does **not** prove the evidence content
  satisfies the L7 criteria (that needs the target environment and human judgement).

  Usage (repository root): & 'dev\scripts\Test-PortalTargetEnvironmentEvidenceMap.ps1'
  Output: temp\p155\evidence-map-stats.json (gitignored).
  Exit codes: 0 pass; 1 assertion failure; 2 missing registry or doc prerequisite.
  </en>
</lang>
.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Test-PortalTargetEnvironmentEvidenceMap.ps1'   # 核对证据清单台账</zh-CN>
  <en>& 'dev\scripts\Test-PortalTargetEnvironmentEvidenceMap.ps1'   # validate the evidence registry</en>
</lang>
.NOTES
<lang>
  <zh-CN>演进：`W-anp-P155` 交付；配套一键证据包为 `Invoke-PortalTargetEnvironmentEvidencePack.ps1`。</zh-CN>
  <en>Provenance: delivered by `W-anp-P155`; the companion one-command pack is `Invoke-PortalTargetEnvironmentEvidencePack.ps1`.</en>
</lang>
#>

[CmdletBinding()]
param(
    [string] $StatsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$registryPath = Join-Path $repoRoot 'dev\target-environment-evidence.json'
$docPath = Join-Path $repoRoot 'docs\operations-runbook.md'
$packPath = Join-Path $repoRoot 'dev\scripts\Invoke-PortalTargetEnvironmentEvidencePack.ps1'

$failures = @()
$softNotes = @()

# <lang><zh-CN>要求覆盖的判据/阻断项（与 M-ANP-TRUSTED-PORTAL 的阻断项清单一一对应）。</zh-CN>
# <en>Required criteria/blockers (one-to-one with the blocker list of M-ANP-TRUSTED-PORTAL).</en></lang>
$REQUIRED_CRITERIA = @('T1', 'T4', 'T5', 'blocker-1', 'blocker-2', 'blocker-3', 'L8-handoff')

if (-not (Test-Path -LiteralPath $registryPath)) {
    Write-Host '[PREREQ-MISSING] 证据清单台账不存在：' + $registryPath + '（exit 2）'
    exit 2
}
if (-not (Test-Path -LiteralPath $packPath)) {
    Write-Host '[PREREQ-MISSING] 一键证据包脚本不存在：' + $packPath + '（exit 2）'
    exit 2
}
if (-not (Test-Path -LiteralPath $docPath)) {
    Write-Host '[PREREQ-MISSING] 运维文档不存在：' + $docPath + '（exit 2）'
    exit 2
}

$registry = Get-Content -LiteralPath $registryPath -Raw -Encoding UTF8 | ConvertFrom-Json
$items = @($registry.items)

# <lang><zh-CN>A1：字段齐备、id 唯一、mode 合法。</zh-CN><en>A1: fields present, ids unique, mode valid.</en></lang>
$seenIds = @{}
foreach ($item in $items) {
    foreach ($field in @('id', 'criterion', 'name', 'mode', 'owner', 'reEntry', 'basis')) {
        $value = [string]$item.$field
        if ([string]::IsNullOrWhiteSpace($value)) { $failures += "A1 条目缺字段 $field：$([string]$item.id)。" }
    }
    if ($seenIds.ContainsKey([string]$item.id)) { $failures += "A1 条目 id 重复：$([string]$item.id)。" }
    $seenIds[[string]$item.id] = $true
    $mode = [string]$item.mode
    if (@('local', 'targetEnv', 'manual') -notcontains $mode) { $failures += "A1 非法 mode：$mode（条目 $([string]$item.id)）。" }
}

# <lang><zh-CN>A2：generator 脚本必须存在、args 必须是数组。</zh-CN><en>A2: generator scripts must exist and args must be arrays.</en></lang>
foreach ($item in $items) {
    if ($null -eq $item.generator) { continue }
    $scriptRel = [string]$item.generator.script
    if ([string]::IsNullOrWhiteSpace($scriptRel)) { $failures += "A2 条目有 generator 但缺 script：$([string]$item.id)。"; continue }
    if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $scriptRel))) {
        $failures += "A2 生成器脚本不存在：$scriptRel（条目 $([string]$item.id)）——不得登记跑不起来的命令。"
    }
    if ($null -ne $item.generator.args -and ($item.generator.args -isnot [System.Array])) {
        $failures += "A2 generator.args 必须是数组：$([string]$item.id)。"
    }
}

# <lang><zh-CN>A3：判据/阻断项全覆盖。</zh-CN><en>A3: full coverage of criteria and blockers.</en></lang>
$covered = @($items | ForEach-Object { [string]$_.criterion } | Sort-Object -Unique)
foreach ($criterion in $REQUIRED_CRITERIA) {
    if ($covered -notcontains $criterion) {
        $failures += "A3 里程碑判据/阻断项未登记：$criterion（环境到了也出不全证）。"
    }
}

# <lang><zh-CN>A4：模式语义自洽。</zh-CN><en>A4: mode semantics must be self-consistent.</en></lang>
foreach ($item in $items) {
    $mode = [string]$item.mode
    if ($mode -eq 'local' -and $null -eq $item.generator) { $failures += "A4 local 条目必须有 generator：$([string]$item.id)。" }
    if ($mode -eq 'targetEnv') {
        if ([string]::IsNullOrWhiteSpace([string]$item.prereqs)) { $failures += "A4 targetEnv 条目必须写清 prereqs：$([string]$item.id)。" }
        if ($null -eq $item.generator) { $softNotes += "[soft] targetEnv 条目无生成器（$([string]$item.id)）——将来需人工执行，请确认是否有对应脚本。" }
    }
}

# <lang><zh-CN>A5：公开运维文档必须点名一键证据包与台账。</zh-CN><en>A5: the public operations doc must name the pack and the registry.</en></lang>
$docText = [System.IO.File]::ReadAllText($docPath)
if ($docText -notmatch 'Invoke-PortalTargetEnvironmentEvidencePack\.ps1') {
    $failures += 'A5 运维文档未点名一键证据包 Invoke-PortalTargetEnvironmentEvidencePack.ps1（有工具没人知道 = 没交付）。'
}
if ($docText -notmatch 'target-environment-evidence\.json') {
    $failures += 'A5 运维文档未点名证据清单台账 target-environment-evidence.json。'
}

# <lang><zh-CN>统计落盘 + 输出。</zh-CN><en>Stats output and reporting.</en></lang>
if ([string]::IsNullOrWhiteSpace($StatsPath)) {
    $statsDir = Join-Path $repoRoot 'temp\p155'
    New-Item -ItemType Directory -Force -Path $statsDir | Out-Null
    $StatsPath = Join-Path $statsDir 'evidence-map-stats.json'
}
else {
    $parent = Split-Path -Parent $StatsPath
    if ($parent -and -not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
}
$byMode = @{}
foreach ($item in $items) { $m = [string]$item.mode; if (-not $byMode.ContainsKey($m)) { $byMode[$m] = 0 }; $byMode[$m] += 1 }
$report = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    itemCount = $items.Count
    byMode = $byMode
    coveredCriteria = $covered
    requiredCriteria = $REQUIRED_CRITERIA
    softNotes = $softNotes
    failures = $failures
}
[System.IO.File]::WriteAllText($StatsPath, ($report | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))

Write-Host ''
Write-Host '=== L7 目标环境证据清单（W-anp-P155）==='
$modeSummary = (@($byMode.Keys) | ForEach-Object { $_ + ' ' + $byMode[$_] }) -join '｜'
Write-Host ('条目 ' + $items.Count + '（' + $modeSummary + '）｜判据覆盖 ' + $covered.Count + '/' + $REQUIRED_CRITERIA.Count)
foreach ($note in $softNotes) { Write-Host ('  ' + $note) }
Write-Host ("统计已写入：$StatsPath")

if ($failures.Count -gt 0) {
    Write-Host "[FAIL] 证据清单台账校验失败 $($failures.Count) 处：" -ForegroundColor Red
    foreach ($failure in $failures) { Write-Host ('  - ' + $failure) -ForegroundColor Red }
    exit 1
}
Write-Host '[PASS] 证据清单：字段齐备、脚本可执行、判据与阻断项全覆盖、运维文档已点名。' -ForegroundColor Green
exit 0
