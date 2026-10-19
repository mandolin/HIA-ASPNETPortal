#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>看守 L7（`M-ANP-TRUSTED-PORTAL`）判据表：条目化、可逐条复核（L0）。</zh-CN>
  <en>Guards the L7 (`M-ANP-TRUSTED-PORTAL`) criteria table: itemised and reviewable criterion by criterion (L0).</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>`C-anp-P28`（`W-anp-P156`）交付：把里程碑 `M-ANP-TRUSTED-PORTAL` 的 T1–T7 判据从叙述变成
  **条目化、可逐条复核**的台账 `dev/l7-criteria-registry.json`，并由本门禁看守其质量：

    A1 字段齐备：每条判据必须有 id / name / requirement / status / statusNote / verify / invalidation /
       owner / reEntry；id 不得重复；status 只能取 met-dev-side / needs-target-env / not-met。
    A2 复核方式可用：verify.kind=script 时脚本必须存在且 args 为数组（**不得登记跑不起来的复核命令**）；
       verify.kind=manual 时人工程序必须写得具体（≥20 字，说清怎么做），否则等于没写复核方式。
    A3 覆盖完整：T1–T7 七条判据必须全部在册（漏一条 = 有判据没人复核）。
    A4 状态自洽：`needs-target-env` 的判据必须在 `dev/target-environment-evidence.json` 中有对应
       证据条目（防止"需要环境"却没人登记怎么出证）；`met-dev-side` 的判据必须至少有 1 条证据指针。
    A5 交叉核对（软）：若 work-zone 的里程碑文档存在，则其 T1–T7 编号集合应与台账一致；不一致仅作软指标
       ——不因私有仓缺失而把 L0 打红。

  证明边界：只证明"判据表结构完整、复核命令可执行、状态与证据登记自洽"；
  **不证明判据本身已被满足**（`met-dev-side` 是开发侧结论，需按 verify 逐条跑/复核才算数），
  **不替代人工复核**（T2 等 manual 项必须由人执行并留结论）。

  用法（仓库根目录）：& 'dev\scripts\Test-PortalL7CriteriaRegistry.ps1'
  产物：temp\p156\l7-criteria-stats.json（gitignore）。
  退出码：0 通过；1 断言失败；2 台账前置缺失。
  </zh-CN>
  <en>`C-anp-P28` (`W-anp-P156`) deliverable: turns the T1–T7 criteria of the milestone `M-ANP-TRUSTED-PORTAL` from prose into an
  **itemised, criterion-by-criterion reviewable** registry `dev/l7-criteria-registry.json`, guarded by this gate:

    A1 fields: every criterion must have id / name / requirement / status / statusNote / verify / invalidation / owner /
       reEntry; ids unique; status limited to met-dev-side / needs-target-env / not-met.
    A2 usable verification: with verify.kind=script the script must exist and args must be an array (**a review command that
       cannot run must not be registered**); with verify.kind=manual the procedure must be concrete (>= 20 characters saying how
       to do it), otherwise it is not a verification method at all.
    A3 completeness: all seven criteria T1–T7 must be registered (a missing one means a criterion nobody reviews).
    A4 status coherence: a `needs-target-env` criterion must have a matching evidence entry in
       `dev/target-environment-evidence.json` (so "needs the environment" cannot mean "nobody registered how to produce the
       evidence"); a `met-dev-side` criterion must have at least one evidence pointer.
    A5 cross-check (soft): when the work-zone milestone doc exists, its T1–T7 id set must match the registry; a mismatch is a
       soft metric only — a missing private repo must not red-line L0.

  Proof boundary: proves only that "the criteria table is structurally complete, review commands are executable, and statuses
  cohere with the evidence registrations"; does **not** prove that the criteria themselves are satisfied (`met-dev-side` is a
  development-side conclusion; each must actually be run or reviewed per verify), and does **not** replace human review
  (manual items such as T2 must be executed by a person with the conclusion recorded).

  Usage (repository root): & 'dev\scripts\Test-PortalL7CriteriaRegistry.ps1'
  Output: temp\p156\l7-criteria-stats.json (gitignored).
  Exit codes: 0 pass; 1 assertion failure; 2 missing registry prerequisite.
  </en>
</lang>
.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Test-PortalL7CriteriaRegistry.ps1'   # 核对 L7 判据表</zh-CN>
  <en>& 'dev\scripts\Test-PortalL7CriteriaRegistry.ps1'   # validate the L7 criteria table</en>
</lang>
.NOTES
<lang>
  <zh-CN>演进：`W-anp-P156` 交付；判据来源为 `work-zone/dev/milestones/M-ANP-TRUSTED-PORTAL.md`。</zh-CN>
  <en>Provenance: delivered by `W-anp-P156`; criteria come from `work-zone/dev/milestones/M-ANP-TRUSTED-PORTAL.md`.</en>
</lang>
#>

[CmdletBinding()]
param(
    [string] $StatsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$registryPath = Join-Path $repoRoot 'dev\l7-criteria-registry.json'
$evidencePath = Join-Path $repoRoot 'dev\target-environment-evidence.json'
$milestoneDoc = Join-Path $repoRoot 'work-zone\dev\milestones\M-ANP-TRUSTED-PORTAL.md'

$failures = @()
$softNotes = @()

if (-not (Test-Path -LiteralPath $registryPath)) {
    Write-Host '[PREREQ-MISSING] L7 判据台账不存在：' + $registryPath + '（exit 2）'
    exit 2
}

$registry = Get-Content -LiteralPath $registryPath -Raw -Encoding UTF8 | ConvertFrom-Json
$criteria = @($registry.criteria)
$REQUIRED_IDS = @('T1', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7')
$VALID_STATUS = @('met-dev-side', 'needs-target-env', 'not-met')

# <lang><zh-CN>A1：字段齐备、id 唯一、status 合法。</zh-CN><en>A1: fields present, ids unique, status valid.</en></lang>
$seenIds = @{}
foreach ($c in $criteria) {
    foreach ($field in @('id', 'name', 'requirement', 'status', 'statusNote', 'invalidation', 'owner', 'reEntry')) {
        if ([string]::IsNullOrWhiteSpace([string]$c.$field)) { $failures += "A1 判据缺字段 $field：$([string]$c.id)。" }
    }
    if ($seenIds.ContainsKey([string]$c.id)) { $failures += "A1 判据 id 重复：$([string]$c.id)。" }
    $seenIds[[string]$c.id] = $true
    if ($VALID_STATUS -notcontains [string]$c.status) { $failures += "A1 非法 status：$([string]$c.status)（判据 $([string]$c.id)）。" }
}

# <lang><zh-CN>A2：复核方式可用。</zh-CN><en>A2: verification methods must be usable.</en></lang>
foreach ($c in $criteria) {
    if ($null -eq $c.verify) { $failures += "A2 判据缺 verify：$([string]$c.id)。"; continue }
    $kind = [string]$c.verify.kind
    if ($kind -eq 'script') {
        $scriptRel = [string]$c.verify.script
        if ([string]::IsNullOrWhiteSpace($scriptRel)) { $failures += "A2 script 复核缺脚本路径：$([string]$c.id)。"; continue }
        if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $scriptRel))) {
            $failures += "A2 复核脚本不存在：$scriptRel（判据 $([string]$c.id)）——不得登记跑不起来的复核命令。"
        }
        if ($null -ne $c.verify.args -and ($c.verify.args -isnot [System.Array])) {
            $failures += "A2 verify.args 必须是数组：$([string]$c.id)。"
        }
    }
    elseif ($kind -eq 'manual') {
        if ([string]::IsNullOrWhiteSpace([string]$c.verify.manual) -or ([string]$c.verify.manual).Length -lt 20) {
            $failures += "A2 人工程序写得太虚（<20 字，等于没写复核方式）：$([string]$c.id)。"
        }
    }
    else {
        $failures += "A2 非法 verify.kind：$kind（判据 $([string]$c.id)）。"
    }
}

# <lang><zh-CN>A3：T1–T7 全覆盖。</zh-CN><en>A3: full coverage of T1-T7.</en></lang>
$ids = @($criteria | ForEach-Object { [string]$_.id })
foreach ($required in $REQUIRED_IDS) {
    if ($ids -notcontains $required) { $failures += "A3 判据未登记：$required（漏一条 = 有判据没人复核）。" }
}

# <lang><zh-CN>A4：状态自洽（needs-target-env 必须有对应证据条目；met-dev-side 必须有证据指针）。</zh-CN>
# <en>A4: status coherence (needs-target-env must have a matching evidence entry; met-dev-side must have evidence pointers).</en></lang>
$evidenceCriteria = @()
if (Test-Path -LiteralPath $evidencePath) {
    $evidenceRegistry = Get-Content -LiteralPath $evidencePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $evidenceCriteria = @($evidenceRegistry.items | ForEach-Object { [string]$_.criterion } | Sort-Object -Unique)
}
else {
    $softNotes += '[soft] 环境证据台账不存在（dev/target-environment-evidence.json），跳过 A4 的交叉核对。'
}
foreach ($c in $criteria) {
    $status = [string]$c.status
    $evidenceCount = @($c.evidence).Count
    if ($status -eq 'needs-target-env') {
        if (($evidenceCriteria.Count -gt 0) -and ($evidenceCriteria -notcontains [string]$c.id)) {
            $failures += "A4 判据 $([string]$c.id) 标记为 needs-target-env，但环境证据台账中没有对应条目（需要环境却没人登记怎么出证）。"
        }
    }
    if ($status -eq 'met-dev-side' -and $evidenceCount -lt 1) {
        $failures += "A4 判据 $([string]$c.id) 标记为 met-dev-side，但没有任何证据指针。"
    }
}

# <lang><zh-CN>A5：与里程碑文档交叉核对（软，work-zone 缺失时不阻断）。</zh-CN>
# <en>A5: cross-check against the milestone doc (soft; a missing work-zone does not block).</en></lang>
if (Test-Path -LiteralPath $milestoneDoc) {
    $docText = [System.IO.File]::ReadAllText($milestoneDoc)
    $docIds = @([regex]::Matches($docText, '\bT([1-7])\b') | ForEach-Object { 'T' + $_.Groups[1].Value } | Sort-Object -Unique)
    foreach ($required in $REQUIRED_IDS) {
        if ($docIds -notcontains $required) { $softNotes += "[soft] 里程碑文档中未见判据 $required 的编号（可能已改写表述）。" }
    }
}
else {
    $softNotes += '[soft] 里程碑文档不存在（work-zone 未就位），跳过 A5 交叉核对。'
}

# <lang><zh-CN>统计落盘 + 输出。</zh-CN><en>Stats output and reporting.</en></lang>
if ([string]::IsNullOrWhiteSpace($StatsPath)) {
    $statsDir = Join-Path $repoRoot 'temp\p156'
    New-Item -ItemType Directory -Force -Path $statsDir | Out-Null
    $StatsPath = Join-Path $statsDir 'l7-criteria-stats.json'
}
else {
    $parent = Split-Path -Parent $StatsPath
    if ($parent -and -not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
}
$byStatus = @{}
foreach ($c in $criteria) { $s = [string]$c.status; if (-not $byStatus.ContainsKey($s)) { $byStatus[$s] = 0 }; $byStatus[$s] += 1 }
$report = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    criteriaCount = $criteria.Count
    byStatus = $byStatus
    scriptVerified = @($criteria | Where-Object { [string]$_.verify.kind -eq 'script' }).Count
    manualVerified = @($criteria | Where-Object { [string]$_.verify.kind -eq 'manual' }).Count
    softNotes = $softNotes
    failures = $failures
}
[System.IO.File]::WriteAllText($StatsPath, ($report | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))

Write-Host ''
Write-Host '=== L7 判据表（W-anp-P156）==='
$statusSummary = (@($byStatus.Keys) | ForEach-Object { $_ + ' ' + $byStatus[$_] }) -join '｜'
Write-Host ('判据 ' + $criteria.Count + ' 条（' + $statusSummary + '）｜脚本复核 ' + $report.scriptVerified + '｜人工复核 ' + $report.manualVerified)
foreach ($note in $softNotes) { Write-Host ('  ' + $note) }
Write-Host ("统计已写入：$StatsPath")

if ($failures.Count -gt 0) {
    Write-Host "[FAIL] L7 判据表校验失败 $($failures.Count) 处：" -ForegroundColor Red
    foreach ($failure in $failures) { Write-Host ('  - ' + $failure) -ForegroundColor Red }
    exit 1
}
Write-Host '[PASS] L7 判据表：T1-T7 齐备、复核方式可用、状态与证据登记自洽。' -ForegroundColor Green
exit 0
