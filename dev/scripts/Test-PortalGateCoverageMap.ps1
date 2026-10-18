#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>门禁覆盖地图门禁（L0，静态）：核对「哪些面有门禁、哪些没有」的地图与实际门禁集一致，并机器强制缺口的登记质量。</zh-CN>
  <en>Gate coverage map gate (L0, static): verifies that the map of "which surfaces have gates and which do not" agrees with the actual gate set, and machine-enforces the registration quality of gaps.</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>`C-anp-P27`（`W-anp-P147`）交付：把"门禁覆盖面说不清"从文档叙述变成**机器可核对的地图**。
  地图文件 `dev\gate-coverage-map.json` 是人工登记的事实源；本门禁负责让它**不会漂移、不会漏登记**：

  M1 编排门禁必须登记：`Invoke-PortalGateSuite.ps1` 中全部 `File = '...'` 条目（含 L0/L1/L2 与 `$slowGates`）
     必须被某个面引用（gate 或 gates 数组）——新增门禁未登记即 Fail（default-deny，不允许"没人认领"的门禁）。
  M2 地图不漂移：地图引用的每个门禁必须在编排集合中且文件真实存在（幽灵条目 → Fail）。
  M3 未覆盖面必须带主（V3 机器强制）：`coverage=uncovered` 的面必须带 owner / reEntry / basis。
  M4 部分覆盖必须声明边界：`coverage=partial` 的面必须带 boundary（本周期硬纪律：只证明什么、不证明什么必须写清；
     依据 P82 教训——"通过但什么都没断言"的门禁比没有门禁更危险）。
  M5 无孤儿门禁脚本：`dev\scripts` 下 `Test-*` / `Build-*` / `Invoke-Portal*` 文件必须要么已接入编排，
     要么在地图 `tooling` 列表中并带 basis（工具/编排自身不是门禁，必须显式声明）。
  M6 证明边界必须显式声明（本周期新增）：地图 `boundaryRequiredGates` 清单内的门禁，脚本内必须含
     中英双语的「证明边界 / Proof boundary」小节——把散落在工作文档里的 caveat 变成机器核对的不变量。
     硬断言只针对该清单（本周期七个新门禁 + 今后新增）；既有老门禁未声明者**只作软指标上报**，
     避免一刀切把 L0 打红并掩盖真实缺陷信号（收编路径见软指标输出）。

  证明边界：只证明"地图与门禁集一致、缺口登记质量达标"；**不证明每个门禁的断言强度足够**
  （强度由各门禁自身的负向测试与证明边界声明承载，本门禁只管"有没有、登记没登记"）。

  用法（仓库根目录）：& 'dev\scripts\Test-PortalGateCoverageMap.ps1'
  产物：temp\gate-map\gate-map-stats.json（每次运行刷新；gitignore）。
  退出码：0 全过；1 任一违规。
  有效性验证（注入法，沿用 W83/W87/P112 惯例）：删掉某面的门禁引用（使编排门禁未登记）→ M1 Fail；
  清空某 uncovered 面的 owner → M3 Fail；清空某 partial 面的 boundary → M4 Fail；
  地图引用幽灵门禁名 → M2 Fail；还原 → Pass。</zh-CN>
  <en>`C-anp-P27` (`W-anp-P147`) deliverable: turns "unstateable gate coverage" from prose into a **machine-checkable map**.
  The map file `dev\gate-coverage-map.json` is the human-curated source of truth; this gate keeps it **drift-free and complete**:

  M1 every wired gate must be registered: all `File = '...'` entries in `Invoke-PortalGateSuite.ps1` (L0/L1/L2 plus
     `$slowGates`) must be referenced by some surface (its `gate` or `gates` array) — a new gate that is unregistered fails
     (default-deny: no unclaimed gates).
  M2 the map must not drift: every gate referenced by the map must exist in the wired set and on disk (phantom → Fail).
  M3 uncovered surfaces must be owned (V3 machine-enforced): `coverage=uncovered` requires owner / reEntry / basis.
  M4 partial coverage must declare its boundary: `coverage=partial` requires `boundary` (a hard rule of this cycle: state
     what is and is not proven; per the P82 lesson — a gate that passes while asserting nothing is worse than no gate).
  M5 no orphan gate scripts: files under `dev\scripts` named `Test-*` / `Build-*` / `Invoke-Portal*` must be wired in the
     suite or listed in the map's `tooling` with a basis (tooling and the orchestrator are not gates and must say so).
  M6 explicit proof boundary required (added this cycle): gates listed in the map's `boundaryRequiredGates` must contain a
     bilingual "证明边界 / Proof boundary" section — turning caveats scattered across work docs into a machine-checked
     invariant. The hard assertion covers only that list (this cycle's seven new gates plus any future one); older gates
     without it are **reported as a soft metric**, so a blanket rule cannot red-line L0 and bury the real signal.

  Proof boundary: proves only that "the map agrees with the gate set and gap registration meets the bar"; it does **not**
  prove that each gate's assertions are strong enough (strength belongs to each gate's own negative tests and declared
  proof boundary; this gate only answers "does it exist and is it registered").

  Usage (repository root): & 'dev\scripts\Test-PortalGateCoverageMap.ps1'
  Output: temp\gate-map\gate-map-stats.json (refreshed per run; gitignored).
  Exit codes: 0 all pass; 1 any violation.
  Effectiveness proof (injection, per the W83/W87/P112 convention): remove a surface's gate reference (leaving a wired gate
  unregistered) → M1 fails; clear an uncovered surface's owner → M3 fails; clear a partial surface's boundary → M4 fails;
  reference a phantom gate name → M2 fails; restore → Pass.</en>
</lang>
.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Test-PortalGateCoverageMap.ps1'   # 仓库根目录执行；exit 0 全过，exit 1 任一违规</zh-CN>
  <en>& 'dev\scripts\Test-PortalGateCoverageMap.ps1'   # run from the repository root; exit 0 pass, exit 1 any violation</en>
</lang>
.NOTES
<lang>
  <zh-CN>演进：`W-anp-P147` 交付并接入 L0（36→37）。地图新增面须同时满足 M2–M4，否则门禁失败。</zh-CN>
  <en>Provenance: delivered by `W-anp-P147` and wired into L0 (36→37). New map surfaces must satisfy M2–M4 or the gate fails.</en>
</lang>
#>

[CmdletBinding()]
param(
    [string] $StatsPath = ''
)

$ErrorActionPreference = 'Stop'

# <lang><zh-CN>仓库根 = 本脚本目录（dev\scripts）上两级。</zh-CN>
# <en>Repository root = two levels up from this script's directory (dev\scripts).</en></lang>
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$suitePath = Join-Path $repoRoot 'dev\scripts\Invoke-PortalGateSuite.ps1'
$mapPath = Join-Path $repoRoot 'dev\gate-coverage-map.json'
$scriptsDir = Join-Path $repoRoot 'dev\scripts'

if (-not (Test-Path -LiteralPath $mapPath)) {
    Write-Host "[FAIL] 覆盖地图不存在：$mapPath（地图是事实源，缺失即失败）"
    exit 1
}
$suiteText = [System.IO.File]::ReadAllText($suitePath)
$map = Get-Content -LiteralPath $mapPath -Raw -Encoding UTF8 | ConvertFrom-Json

# <lang>
#   <zh-CN>已接入门禁集：解析编排器全文件的 `File = '...'` 条目（含 L0/L1/L2 与 $slowGates）。
#   不按层块分段解析——块内多行 `Fixture/Args` 会让分段脆弱；全文件解析对"有没有被编排"更稳。</zh-CN>
#   <en>Wired gate set: parse every `File = '...'` entry in the suite (L0/L1/L2 plus $slowGates). Deliberately not parsed
#   per layer block — multi-line `Fixture/Args` blocks make block splitting fragile; whole-file parsing answers "is it wired"
#   more robustly.</en>
# </lang>
$wired = @([regex]::Matches($suiteText, "File\s*=\s*'([^']+\.(?:ps1|mjs))'") | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)

$failures = @()
$mappedGates = @()
$byCoverage = [ordered]@{ full = 0; partial = 0; uncovered = @() }

foreach ($surface in @($map.surfaces)) {
    $sid = [string]$surface.id
    $coverage = [string]$surface.coverage

    # <lang><zh-CN>M4：部分覆盖必须声明边界（只证明什么 / 不证明什么）。</zh-CN>
    # <en>M4: partial coverage must declare its boundary (what it proves and what it does not).</en></lang>
    if ($coverage -eq 'partial' -and [string]::IsNullOrWhiteSpace([string]$surface.boundary)) {
        $failures += "M4 [$sid] coverage=partial 但未声明 boundary（必须写清不证明什么）。"
    }
    # <lang><zh-CN>M3：未覆盖面必须带所有者 / 再入条件 / 判定依据（V3 机器强制）。</zh-CN>
    # <en>M3: uncovered surfaces must carry owner / re-entry condition / basis (V3 machine-enforced).</en></lang>
    if ($coverage -eq 'uncovered') {
        $byCoverage.uncovered += $sid
        foreach ($field in @('owner', 'reEntry', 'basis')) {
            if ([string]::IsNullOrWhiteSpace([string]$surface.$field)) {
                $failures += "M3 [$sid] coverage=uncovered 缺 $field（未覆盖面必须带所有者/再入条件/判定依据）。"
            }
        }
    } else {
        if ($coverage -eq 'full') { $byCoverage.full++ } elseif ($coverage -eq 'partial') { $byCoverage.partial++ }
        else { $failures += "M0 [$sid] coverage 取值非法：$coverage（只允许 full/partial/uncovered）。" }
    }

    # <lang><zh-CN>M2：地图引用的门禁必须在编排集合中且文件存在。</zh-CN>
    # <en>M2: every gate referenced by the map must be in the wired set and present on disk.</en></lang>
    # <lang>
    #   <zh-CN>按**属性存在性**取值：只有 `gate` 而无 `gates` 的面，`@($surface.gates)` 会得到 `@($null)`，
    #   产生空串门禁引用并被 M2 误判为"未接入编排"——首跑即暴露（18 处假阳性）。空串一律忽略。</zh-CN>
    #   <en>Read by **property existence**: for a surface with only `gate` and no `gates`, `@($surface.gates)` yields
    #   `@($null)`, producing an empty gate reference that M2 misread as "not wired" — exposed on the first run (18 false
    #   positives). Empty strings are ignored outright.</en>
    # </lang>
    $refs = @()
    if (-not [string]::IsNullOrWhiteSpace([string]$surface.gate)) { $refs += [string]$surface.gate }
    if (($surface.PSObject.Properties.Name -contains 'gates') -and $surface.gates) {
        foreach ($g in @($surface.gates)) {
            if (-not [string]::IsNullOrWhiteSpace([string]$g)) { $refs += [string]$g }
        }
    }
    foreach ($ref in $refs) {
        $mappedGates += $ref
        if ($wired -notcontains $ref) { $failures += "M2 [$sid] 引用的门禁未接入编排：$ref。" }
        if (-not (Test-Path -LiteralPath (Join-Path $scriptsDir $ref))) { $failures += "M2 [$sid] 引用的门禁文件不存在：$ref。" }
    }
}

# <lang><zh-CN>M1：已接入门禁必须被某个面登记（default-deny）。</zh-CN>
# <en>M1: every wired gate must be registered by some surface (default-deny).</en></lang>
foreach ($gate in $wired) {
    if ($mappedGates -notcontains $gate) { $failures += "M1 门禁已接入编排但未登记进覆盖地图：$gate（新增门禁必须先登记面）。" }
}

# <lang><zh-CN>M5：门禁类文件不得成孤儿——要么已接入编排，要么在 tooling 中显式声明（带 basis）。</zh-CN>
# <en>M5: no orphan gate scripts — either wired in the suite or explicitly declared in tooling (with a basis).</en></lang>
$toolingIds = @(@($map.tooling) | ForEach-Object { [string]$_.id })
foreach ($tool in @($map.tooling)) {
    if ([string]::IsNullOrWhiteSpace([string]$tool.basis)) { $failures += "M5 tooling 条目缺 basis：$([string]$tool.id)。" }
}
foreach ($f in (Get-ChildItem $scriptsDir -File | Where-Object { $_.Name -match '^(Test-|Build-|Invoke-Portal)' } | Sort-Object Name)) {
    if ($wired -contains $f.Name) { continue }
    if ($toolingIds -contains $f.Name) { continue }
    $failures += "M5 门禁类脚本既未接入编排也未登记为工具类：$($f.Name)（补接入编排或在地图 tooling 中声明）。"
}

# <lang>
#   <zh-CN>M6：清单内门禁必须含中英双语「证明边界 / Proof boundary」小节；清单外既有门禁未声明者只作软指标上报。
#   注意：清单条目本身漂移（文件已不存在）也要报，避免清单随时间腐坏。</zh-CN>
#   <en>M6: gates in the list must contain a bilingual "证明边界 / Proof boundary" section; older gates outside the list are
#   reported as a soft metric. Drift in the list itself (a file that no longer exists) is reported too, so the list cannot rot.</en>
# </lang>
$requiredBoundary = @(@($map.boundaryRequiredGates) | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
foreach ($gateName in $requiredBoundary) {
    $path = Join-Path $scriptsDir $gateName
    if (-not (Test-Path -LiteralPath $path)) {
        $failures += "M6 清单中的门禁文件不存在：$gateName（漂移，请清理 boundaryRequiredGates）。"
        continue
    }
    $text = [System.IO.File]::ReadAllText($path)
    if (-not $text.Contains('证明边界')) { $failures += "M6 门禁未声明中文「证明边界」小节：$gateName。" }
    if (-not $text.Contains('Proof boundary')) { $failures += "M6 门禁未声明英文「Proof boundary」小节：$gateName（双语纪律）。" }
}
$boundaryDebt = @()
foreach ($gate in $wired) {
    if ($requiredBoundary -contains $gate) { continue }
    $path = Join-Path $scriptsDir $gate
    if (-not (Test-Path -LiteralPath $path)) { continue }
    $text = [System.IO.File]::ReadAllText($path)
    if (-not ($text.Contains('证明边界') -or $text.Contains('Proof boundary'))) { $boundaryDebt += $gate }
}

# ---------------------------------------------------------------------------
# 统计落盘 + 输出
# ---------------------------------------------------------------------------
if ([string]::IsNullOrWhiteSpace($StatsPath)) {
    $StatsPath = Join-Path (Join-Path $repoRoot 'temp') 'gate-map\gate-map-stats.json'
}
$statsDir = Split-Path -Parent $StatsPath
if (-not (Test-Path -LiteralPath $statsDir)) { New-Item -ItemType Directory -Force -Path $statsDir | Out-Null }
$report = [pscustomobject]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    wiredGates = $wired.Count
    mappedGates = @($mappedGates | Sort-Object -Unique).Count
    surfaces = [pscustomobject]@{
        total = @($map.surfaces).Count
        full = $byCoverage.full
        partial = $byCoverage.partial
        uncovered = @($byCoverage.uncovered)
    }
    tooling = @($map.tooling).Count
    boundaryRequired = @($requiredBoundary).Count
    boundaryDebt = $boundaryDebt
    failures = $failures
}
[System.IO.File]::WriteAllText($StatsPath, ($report | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))

Write-Host ''
Write-Host '=== 门禁覆盖地图 ==='
Write-Host ("已接入门禁 {0} 个｜地图登记面 {1} 个（full {2} / partial {3} / uncovered {4}）｜工具类 {5} 个" -f $wired.Count, @($map.surfaces).Count, $byCoverage.full, $byCoverage.partial, @($byCoverage.uncovered).Count, @($map.tooling).Count)
foreach ($id in @($byCoverage.uncovered)) {
    $s = @($map.surfaces | Where-Object { $_.id -eq $id })[0]
    Write-Host ("    未覆盖：{0}（所有者：{1}）" -f $id, $s.owner)
}
Write-Host ("证明边界：清单内 {0} 个均已声明；清单外既有门禁未声明 {1} 个（软指标，逐步收编）" -f @($requiredBoundary).Count, @($boundaryDebt).Count)
Write-Host ("统计已写入：$StatsPath")

if ($failures.Count -gt 0) {
    Write-Host ''
    Write-Host ("[FAIL] 覆盖地图校验失败 {0} 处：" -f $failures.Count)
    foreach ($f in $failures) { Write-Host ("  - " + $f) }
    exit 1
}

Write-Host ''
Write-Host '[PASS] 地图与门禁集一致；未覆盖面带主；部分覆盖已声明边界。'
exit 0
