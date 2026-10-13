<#
.SYNOPSIS
<lang>
  <zh-CN>ROP 注释基线门禁：把 ROP inventory 的**可检查断言**固化为可重复运行的阻断项，防止已治理成果回归。</zh-CN>
  <en>ROP comment-baseline gate: turns the ROP inventory's **checkable assertions** into repeatable blocking checks so remediated results cannot regress.</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>本门禁是 `C-anp-P19`（ROP 注释治理独立周期）的收口产物，把 `W-anp-P108`–`W-anp-P111` 达成的基线固化为机器可跑断言。
  **硬断言（阻断，退出码 1）**，任一不成立即失败：
    1. `ScriptsWithLegacyLangHelpSurface = 0` —— 无脚本仍带旧版 `.LANG` help 表面；
    2. `LegacyLangHelpSurfaceCount = 0` —— legacy 标记总数为 0；
    3. `ScriptsWithMixedLanguageSurfaces = 0` —— 不存在"旧 `.LANG` 与标准 `<lang>` 共存"的最易被粗口径掩盖集合；
    4. `HighRiskMissingHiaLanguageMarkers = 0` —— 高风险脚本全部带有 HIA 双语标记；
    5. `MissingCommentHelp = 0` —— 全部 Git 追踪的 `dev/scripts/*.ps1` 均具备 comment-based help 结构。
  **软指标（上报，不阻断）**：`HighRiskScriptCandidate` 是按**文件名关键词**生成的候选集、`TodoOrDeferredMarker` 是按四分法登记的延期项；二者都不是缺陷计数，阻断它们只会制造无意义的历史搅动，故只上报供规划参考。

  **证明边界（必读）**：本门禁只证明"结构标记存在或缺失"这一客观事实，**不证明注释的语义充分性**。
  标记齐备 ≠ 注释说得清楚；语义质量仍需人工抽样复核（见 `C-anp-P19` `W-anp-P111` 的双语对等抽样）。
  任何把本门禁通过外推为"历史注释已彻底达标"的结论都是错误的。

  **前置条件**：Git 仓库可读；只读取源码文本，不执行被扫描脚本、不连接数据库或网络、不访问凭据。
  **验证方式**：退出码 0 表示硬断言全部成立、1 表示有断言被破坏；`[GATE-NOTE]` 行只上报软指标与证明边界，不参与失败判定。
  **低敏边界**：只读取脚本文本与 Git 追踪清单，不读取连接串、凭据或业务数据。</zh-CN>
  <en>This gate is the closing artifact of `C-anp-P19` (the independent ROP comment-governance cycle) and pins the baseline reached in
  `W-anp-P108`–`W-anp-P111` into machine-runnable assertions.
  **Hard assertions (blocking, exit code 1)** — failure if any does not hold:
    1. `ScriptsWithLegacyLangHelpSurface = 0` — no script still carries a legacy `.LANG` help surface;
    2. `LegacyLangHelpSurfaceCount = 0` — zero legacy markers in total;
    3. `ScriptsWithMixedLanguageSurfaces = 0` — no "legacy `.LANG` coexisting with standard `<lang>`" set, the one most easily hidden by a coarse metric;
    4. `HighRiskMissingHiaLanguageMarkers = 0` — every high-risk script carries HIA bilingual markers;
    5. `MissingCommentHelp = 0` — every Git-tracked `dev/scripts/*.ps1` has comment-based help structure.
  **Soft metrics (reported, non-blocking)**: `HighRiskScriptCandidate` is a candidate set generated from **file-name keywords**, and
  `TodoOrDeferredMarker` is a set of items registered under the four-way split; neither is a defect count, and blocking on them would
  only create pointless historical churn, so they are reported for planning reference only.

  **Proof boundary (read this)**: this gate proves only the objective fact that structural markers exist or are absent; it does
  **not** prove that comments are semantically sufficient. Complete markers do not mean clear comments — semantic quality still
  requires human sampling review (see the bilingual parity sampling in `C-anp-P19` `W-anp-P111`). Any conclusion that extrapolates a
  pass here into "historical comments are finally compliant" is wrong.

  **Prerequisites**: a readable Git repository; source text only — scanned scripts are never executed, no database or network access, no credentials.
  **Verification**: exit code 0 means all hard assertions hold, 1 means one was broken; `[GATE-NOTE]` lines report soft metrics and the proof boundary only and never count as failure.
  **Low-sensitivity boundary**: only script text and the Git-tracked file list are read — no connection strings, credentials or business data.</en>
</lang>
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>仓库根取脚本所在目录的上两级（`dev\scripts` -> 仓库根）；inventory 以仓库根为工作目录运行，
#   保证它读到的 Git 追踪清单与本门禁判定的范围一致。</zh-CN>
#   <en>The repository root is two levels above the script directory (`dev\scripts` -> repo root); the inventory runs with the
#   repository root as its working directory so the Git-tracked list it reads matches the scope this gate judges.</en>
# </lang>
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Push-Location $repoRoot
try {
    $inventory = & (Join-Path $repoRoot 'dev\scripts\Get-PortalPowerShellDocumentationInventory.ps1')
}
finally {
    Pop-Location
}

if ($null -eq $inventory) {
    Write-Output 'RESULT: Fail'
    Write-Output '  - 无法取得 PowerShell 文档化 inventory 结果（inventory 返回空）'
    exit 1
}

# <lang>
#   <zh-CN>硬断言逐条比对；每条都把"实际读数"写进失败信息，便于直接定位是哪一类表面回归了，
#   而不是只告诉人"失败了"。</zh-CN>
#   <en>Hard assertions are compared one by one, and each failure message carries the measured value so the regressed surface can be
#   located directly instead of only learning that "it failed".</en>
# </lang>
$problems = @()

if ($inventory.ScriptsWithLegacyLangHelpSurface -ne 0) {
    $problems += ('仍有 ' + $inventory.ScriptsWithLegacyLangHelpSurface + ' 个脚本带旧版 .LANG help 表面（应为 0）')
}
if ($inventory.LegacyLangHelpSurfaceCount -ne 0) {
    $problems += ('旧版 .LANG 标记总数为 ' + $inventory.LegacyLangHelpSurfaceCount + '（应为 0）')
}
if ($inventory.ScriptsWithMixedLanguageSurfaces -ne 0) {
    $problems += ('存在 ' + $inventory.ScriptsWithMixedLanguageSurfaces + ' 个"旧 .LANG 与标准 <lang> 共存"的脚本（应为 0，该集合最易被粗口径掩盖）')
}
if ($inventory.HighRiskMissingHiaLanguageMarkers -ne 0) {
    $problems += ('有 ' + $inventory.HighRiskMissingHiaLanguageMarkers + ' 个高风险脚本缺少 HIA 双语标记（应为 0）')
}
if ($inventory.MissingCommentHelp -ne 0) {
    $problems += ('有 ' + $inventory.MissingCommentHelp + ' 个脚本缺少 comment-based help 结构（应为 0）')
}

# <lang>
#   <zh-CN>软指标只上报、不阻断：候选集与延期项都不是缺陷计数，阻断它们只会制造无意义的历史搅动
#   （续治备忘第 5 项明确要求"防新增漂移，且不对历史存量造成噪音式搅动"）。</zh-CN>
#   <en>Soft metrics are reported only, never blocking: candidate sets and deferred items are not defect counts, and blocking on
#   them would only create pointless historical churn (the continuation memo's fifth requirement explicitly asks for drift
#   prevention "without causing noisy historical churn").</en>
# </lang>
$scriptCount = [int]$inventory.TotalScripts
$highRiskCandidates = [int]$inventory.HighRiskScripts
$todoMarkers = 0
if ($inventory.PSObject.Properties.Name -contains 'TodoOrDeferredMarkerCount') {
    $todoMarkers = [int]$inventory.TodoOrDeferredMarkerCount
}

Write-Output ('[GATE-NOTE] 脚本总数 {0}；有 comment-based help {1}；标准 <lang> 块 {2} 个' -f $scriptCount, [int]$inventory.ScriptsWithCommentHelp, [int]$inventory.StandardLangBlockCount)
Write-Output ('[GATE-NOTE] 高风险脚本 {0} 个（其中缺 HIA 标记 {1} 个，硬断言要求为 0）' -f $highRiskCandidates, [int]$inventory.HighRiskMissingHiaLanguageMarkers)
Write-Output '[GATE-NOTE] 证明边界：本门禁只证明结构标记存在或缺失，不证明注释语义充分性；语义质量需人工抽样复核。'

if ($problems.Count -gt 0) {
    Write-Output 'RESULT: Fail'
    foreach ($problem in $problems) { Write-Output ('  - ' + $problem) }
    exit 1
}

Write-Output 'RESULT: Pass'
exit 0
