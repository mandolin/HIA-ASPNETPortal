#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>看守计划账本一致性：周期状态在三处（状态块 / 状态总表 / 编号登记册）是否一致、收口文档是否存在（L0）。</zh-CN>
  <en>Guards plan-ledger consistency: whether cycle status agrees across three places (state block / status table / numbering
  register) and whether closeout documents exist (L0).</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>`C-anp-P30`（`W-anp-P165`）交付。起因：2026-10-20 跨周期盘点发现**账本漂移 3 处**且**无任何门禁能发现**——
  `cycle-status.md` 第五节的"当前线 / 下一步"长期停在 `C-anp-P26` / `W-anp-P139`（而 P26–P29 均已收口）、
  第三节跳跃记录停在 2026-10-15、`works-index.md` 把 P26/P27/P28 标"待开始"、P29 标"进行中"。
  编号门禁实测通过，因为它只核对"编号不冲突 / 不悬空 / 不外泄"，**不核对周期状态与实际是否一致**。
  本项目反复出现的教训是"登记会过期，必须机器兜底"——本门禁即该教训的直接产物。

  断言（状态标记映射：✅ 已收口/已完成/已上线 → closed；✅ 已裁定关闭 → disposition；🟢 进行中 → active；⏸ 待开始 → pending）：
    A1 `cycle-status.md` 必须存在机器可读状态块（`LEDGER-STATE-BEGIN/END`），且可解析为 JSON；
       块内每个周期必须在状态总表中有对应行，反之亦然（两边不得各说各话）。
    A2 状态总表每行的状态标记必须与状态块的 `state` 一致。
    A3 `works-index.md` 中每个周期号的状态标记必须与状态块一致。
    A4 状态为 `closed` 的周期必须有收口文档（`cycles/C-anp-P<id>-closeout.md`）；
       `disposition` 必须有裁定文档（`-disposition.md`）——防止"标完成但无收口"。
    A5 状态总表/第五节「下一步唯一动作」中出现的 `W-anp-Pnnn` 编号，若其**归属周期已收口**，即失败
       （"下一步动作"不能指向已完成周期的工作包——这正是原来那处漂移的形态）。

  证明边界：只证明三处账本的**状态一致性与收口文档存在性**；不证明状态所反映的交付内容真实完成
  （编号门禁已说明"不证明被引用的工作包确实已完成"），不检查工作包内部质量，不核对证据目录完整性。

  用法（仓库根目录）：& 'dev\scripts\Test-PortalPlanLedgerConsistency.ps1'
  产物：temp\p165\ledger-consistency-stats.json（gitignore）。
  退出码：0 通过；1 断言失败；2 账本文件前置缺失。
  </zh-CN>
  <en>`C-anp-P30` (`W-anp-P165`) deliverable. Origin: the 2026-10-20 cross-cycle audit found **three ledger drifts with no gate able
  to catch them** — section 5 of `cycle-status.md` ("current line / next step") had long been stuck on `C-anp-P26` /
  `W-anp-P139` while P26–P29 were all closed, the jump log stopped at 2026-10-15, and `works-index.md` marked P26/P27/P28 as
  "not started" and P29 as "in progress". The numbering gate passed throughout, because it only checks "no conflicts / no
  dangling / no public leak" and **never checks whether cycle status matches reality**. This project's recurring lesson is
  "registrations go stale, a machine must back them up" — this gate is the direct product of that lesson.

  Assertions (marker mapping: ✅ closed/completed/live → closed; ✅ disposition-closed → disposition; 🟢 in progress → active;
  ⏸ not started → pending):
    A1 `cycle-status.md` must contain a machine-readable state block (`LEDGER-STATE-BEGIN/END`) that parses as JSON; every
       cycle in the block must have a row in the status table, and vice versa (the two must not tell different stories).
    A2 each status-table row's marker must agree with the block's `state`.
    A3 each cycle's marker in `works-index.md` must agree with the block.
    A4 a `closed` cycle must have a closeout document (`cycles/C-anp-P<id>-closeout.md`); a `disposition` cycle must have a
       disposition document (`-disposition.md`) — so "marked done" cannot mean "no closeout".
    A5 any `W-anp-Pnnn` referenced by the "next single action" must not belong to an already-closed cycle (a next step cannot
       point at a finished cycle's work package — exactly the shape of the original drift).

  Proof boundary: proves only **status consistency across the three ledgers and the existence of closeout documents**; does
  not prove that the delivery behind a status is genuinely complete (the numbering gate already states it does not prove a
  referenced work package is done), does not inspect work-package quality, and does not verify evidence directory completeness.

  Usage (repository root): & 'dev\scripts\Test-PortalPlanLedgerConsistency.ps1'
  Output: temp\p165\ledger-consistency-stats.json (gitignored).
  Exit codes: 0 pass; 1 assertion failure; 2 missing ledger-file prerequisite.
  </en>
</lang>
.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Test-PortalPlanLedgerConsistency.ps1'   # 核对计划账本三处一致</zh-CN>
  <en>& 'dev\scripts\Test-PortalPlanLedgerConsistency.ps1'   # check the three plan ledgers agree</en>
</lang>
.NOTES
<lang>
  <zh-CN>演进：`W-anp-P165` 交付；机器可读状态块位于 `cycle-status.md` 统计行之后。</zh-CN>
  <en>Provenance: delivered by `W-anp-P165`; the machine-readable state block sits right after the stats line in `cycle-status.md`.</en>
</lang>
#>

[CmdletBinding()]
param(
    [string] $StatsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$statusPath = Join-Path $repoRoot 'work-zone\dev\plans\cycle-status.md'
$indexPath = Join-Path $repoRoot 'work-zone\dev\plans\works-index.md'
$cyclesDir = Join-Path $repoRoot 'work-zone\dev\plans\cycles'

$failures = @()
$softNotes = @()

if (-not (Test-Path -LiteralPath $statusPath)) { Write-Host '[PREREQ-MISSING] 缺 cycle-status.md（exit 2）'; exit 2 }
if (-not (Test-Path -LiteralPath $indexPath)) { Write-Host '[PREREQ-MISSING] 缺 works-index.md（exit 2）'; exit 2 }

$statusText = [System.IO.File]::ReadAllText($statusPath)
$indexText = [System.IO.File]::ReadAllText($indexPath)

# <lang><zh-CN>状态标记 → 规范状态（按"最具体优先"判定，避免 ✅ 被笼统匹配成 closed 而漏掉裁定关闭）。</zh-CN>
# <en>Marker → canonical state (most specific first, so a disposition is not swallowed by the generic ✅ rule).</en></lang>
function Get-CanonicalState {
    param([string] $Cell)
    if ([string]::IsNullOrWhiteSpace($Cell)) { return 'unknown' }
    if ($Cell -match '裁定关闭') { return 'disposition' }
    if ($Cell -match '✅') { return 'closed' }
    if ($Cell -match '🟢') { return 'active' }
    if ($Cell -match '⏸') { return 'pending' }
    return 'unknown'
}

# <lang><zh-CN>A1：状态块存在且可解析。</zh-CN><en>A1: the state block exists and parses.</en></lang>
$blockMatch = [regex]::Match($statusText, '(?s)LEDGER-STATE-BEGIN(.*?)LEDGER-STATE-END')
if (-not $blockMatch.Success) {
    $failures += 'A1 cycle-status.md 缺少机器可读状态块（LEDGER-STATE-BEGIN/END）——账本没有机器唯一事实源。'
    $blockCycles = @()
}
else {
    try {
        $block = ($blockMatch.Groups[1].Value -replace '(?s)<!--', '' -replace '(?s)-->', '') | ConvertFrom-Json
        $blockCycles = @($block.cycles)
    }
    catch {
        $failures += 'A1 状态块不是合法 JSON：' + $_.Exception.Message
        $blockCycles = @()
    }
}

# <lang><zh-CN>状态总表行：解析「| P<序号> | 名称 | 状态 | ...」。</zh-CN>
# <en>Status table rows: parse "| P<n> | name | status | ...".</en></lang>
$tableStates = @{}
foreach ($m in [regex]::Matches($statusText, '(?m)^\|\s*\**P(\d+)\**\s*\|([^|]*)\|([^|]*)\|')) {
    $tableStates['P' + $m.Groups[1].Value] = Get-CanonicalState -Cell $m.Groups[3].Value
}

# <lang><zh-CN>A1（续）两边集合一致；A2 状态一致。</zh-CN><en>A1 (cont.) the two sets agree; A2 states agree.</en></lang>
if ($blockCycles.Count -gt 0) {
    $blockIds = @($blockCycles | ForEach-Object { 'P' + ([string]$_.id -replace '^P', '') })
    foreach ($id in $blockIds) {
        if (-not $tableStates.ContainsKey($id)) { $failures += "A1 状态块的周期 $id 在状态总表中没有对应行。" }
    }
    foreach ($id in @($tableStates.Keys)) {
        if ($blockIds -notcontains $id) { $failures += "A1 状态总表的周期 $id 未登记进机器可读状态块。" }
        else {
            $blockState = [string]($blockCycles | Where-Object { ('P' + ([string]$_.id -replace '^P', '')) -eq $id } | Select-Object -First 1).state
            if ($blockState -ne $tableStates[$id]) {
                $failures += "A2 周期 $id 状态不一致：状态块=$blockState，状态总表=$($tableStates[$id])。"
            }
        }
    }
}

# <lang><zh-CN>A3：编号登记册中的周期状态与状态块一致。</zh-CN><en>A3: cycle status in the numbering register agrees with the block.</en></lang>
foreach ($m in [regex]::Matches($indexText, '(?m)^\|\s*\**P(\d+)\**\s*\|([^|]*)\|([^|]*)\|')) {
    $id = 'P' + $m.Groups[1].Value
    if (-not $blockCycles -or ($blockIds -notcontains $id)) { continue }
    $canonical = Get-CanonicalState -Cell $m.Groups[3].Value
    $blockState = [string]($blockCycles | Where-Object { ('P' + ([string]$_.id -replace '^P', '')) -eq $id } | Select-Object -First 1).state
    if ($canonical -ne 'unknown' -and $canonical -ne $blockState) {
        $failures += "A3 周期 $id 状态不一致：编号登记册=$canonical，状态块=$blockState（登记册状态列曾整体过期，故需机器核对）。"
    }
}

# <lang><zh-CN>A4：已收口/已裁定的周期必须有对应文档。</zh-CN><en>A4: closed/disposition cycles must have the matching document.</en></lang>
foreach ($c in $blockCycles) {
    $id = 'P' + ([string]$c.id -replace '^P', '')
    $state = [string]$c.state
    if ($state -eq 'closed') {
        if (-not (Test-Path -LiteralPath (Join-Path $cyclesDir ("C-anp-{0}-closeout.md" -f $id)))) {
            $failures += "A4 周期 $id 标记为 closed，但缺收口文档 C-anp-$id-closeout.md（不得标完成而无收口）。"
        }
    }
    elseif ($state -eq 'disposition') {
        if (-not (Test-Path -LiteralPath (Join-Path $cyclesDir ("C-anp-{0}-disposition.md" -f $id)))) {
            $failures += "A4 周期 $id 标记为 disposition，但缺裁定文档 C-anp-$id-disposition.md。"
        }
    }
}

# <lang><zh-CN>A5：「下一步唯一动作」不得指向已收口周期的工作包——先建立 W 编号 → 归属周期（读各周期计划的「## 二、W 工作包」节）。</zh-CN>
# <en>A5: the "next single action" must not point at a closed cycle's package — first build W number → owning cycle by reading
# each cycle plan's "## 二、W 工作包" section.</en></lang>
$ownerByW = @{}
foreach ($plan in (Get-ChildItem $cyclesDir -Filter 'C-anp-P*.md' -File -ErrorAction SilentlyContinue)) {
    $m = [regex]::Match($plan.Name, '^C-anp-P(\d+)\.md$')
    if (-not $m.Success) { continue }
    $ownerId = 'P' + $m.Groups[1].Value
    $text = [System.IO.File]::ReadAllText($plan.FullName)
    $sec = [regex]::Match($text, '(?s)##\s*二、W 工作包(.*?)(?=\r?\n##\s|\z)')
    if (-not $sec.Success) { continue }
    foreach ($w in [regex]::Matches($sec.Groups[1].Value, 'W-anp-P(\d+)')) { $ownerByW['W-anp-P' + $w.Groups[1].Value] = $ownerId }
}

# <lang><zh-CN>只取「下一步唯一动作」这一行——散文里引用历史编号（如说明旧漂移形态）不应被判为"下一步"。
# 首跑即暴露：整节扫描会把我自己写的说明文字里的旧编号也抓进来，属误报。</zh-CN>
# <en>Only the "next single action" line is scanned — a prose mention of a historical number (for example when describing an old
# drift) is not a "next step". Exposed on the first run: scanning the whole section also flagged the old number inside my own
# explanatory note, i.e. a false positive.</en></lang>
$nextActionLine = [regex]::Match($statusText, '(?m)^-\s*\*\*下一步唯一动作\*\*.*$')
$nextActionText = if ($nextActionLine.Success) { $nextActionLine.Value } else { '' }
if (-not $nextActionLine.Success) { $softNotes += '[soft] 未找到「下一步唯一动作」行，A5 无输入可核。' }
$referencedW = @([regex]::Matches($nextActionText, 'W-anp-P(\d+)') | ForEach-Object { 'W-anp-P' + $_.Groups[1].Value } | Sort-Object -Unique)
foreach ($w in $referencedW) {
    if (-not $ownerByW.ContainsKey($w)) { $softNotes += "[soft] 下一步动作引用的 $w 未在任何周期计划的「## 二、W 工作包」节中登记（无法判定归属）。"; continue }
    $owner = $ownerByW[$w]
    $ownerState = [string]($blockCycles | Where-Object { ('P' + ([string]$_.id -replace '^P', '')) -eq $owner } | Select-Object -First 1).state
    if ($ownerState -eq 'closed' -or $ownerState -eq 'disposition') {
        $failures += "A5 下一步唯一动作指向 $w，但其归属周期 $owner 已是 $ownerState —— 下一步不得指向已完成周期的工作包（原漂移形态）。"
    }
}

# <lang><zh-CN>统计落盘 + 输出。</zh-CN><en>Stats output and reporting.</en></lang>
if ([string]::IsNullOrWhiteSpace($StatsPath)) {
    $statsDir = Join-Path $repoRoot 'temp\p165'
    New-Item -ItemType Directory -Force -Path $statsDir | Out-Null
    $StatsPath = Join-Path $statsDir 'ledger-consistency-stats.json'
}
else {
    $parent = Split-Path -Parent $StatsPath
    if ($parent -and -not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
}
$stateSummary = @{}
foreach ($c in $blockCycles) { $s = [string]$c.state; if (-not $stateSummary.ContainsKey($s)) { $stateSummary[$s] = 0 }; $stateSummary[$s] += 1 }
$report = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    blockCycles = @($blockCycles).Count
    tableRows = $tableStates.Count
    byState = $stateSummary
    nextActionReferences = $referencedW
    softNotes = $softNotes
    failures = $failures
}
[System.IO.File]::WriteAllText($StatsPath, ($report | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))

Write-Host ''
Write-Host '=== 计划账本一致性（W-anp-P165）==='
$summaryText = (@($stateSummary.Keys) | ForEach-Object { $_ + ' ' + $stateSummary[$_] }) -join '｜'
Write-Host ('状态块周期 ' + @($blockCycles).Count + ' 个（' + $summaryText + '）｜状态总表行 ' + $tableStates.Count + '｜下一步引用 ' + $referencedW.Count + ' 个')
foreach ($note in $softNotes) { Write-Host ('  ' + $note) }
Write-Host ("统计已写入：$StatsPath")

if ($failures.Count -gt 0) {
    Write-Host "[FAIL] 账本一致性校验失败 $($failures.Count) 处：" -ForegroundColor Red
    foreach ($failure in $failures) { Write-Host ('  - ' + $failure) -ForegroundColor Red }
    exit 1
}
Write-Host '[PASS] 账本一致：状态块 / 状态总表 / 编号登记册三处一致，收口文档齐备，下一步动作未指向已收口周期。' -ForegroundColor Green
exit 0
