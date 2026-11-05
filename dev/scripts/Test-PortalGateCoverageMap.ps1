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
  M7 注入证明索引不漂移：`dev\gate-injection-proof-index.json` 的 proved / notDetected / other / neverTouched /
     notApplicable **五类**清单里的每个门禁名，都必须在编排集合中且磁盘文件存在；去重总数必须等于已接入门禁数。
     `notApplicable`（已评估并裁定不适用）必须与事实源台账 `dev\gate-injection-proof-not-applicable.json` 逐项一致且带依据，
     且不得同时出现在 proved 或 neverTouched 中——防止把某门禁悄悄标成"不适用"来抬高覆盖率。
     起因是 `W-anp-P267` 曾按错误前缀（`Test-` vs `Invoke-`）搜漏，把确实存在的编排门禁误记为「幽灵门禁 / 文件缺失 /
     永远无法证明」——索引是覆盖结论的唯一机器事实源，一旦出现不存在的名字或漏项，结论就会静默失真。
     M7 让这类名字错配在产生当场即被拦住，而不是等到人工复盘才发现。

  证明边界：只证明"地图与门禁集一致、缺口登记质量达标、注入证明索引的名字与门禁集一致"；**不证明每个门禁的断言强度足够**
  （强度由各门禁自身的负向测试与证明边界声明承载，本门禁只管"有没有、登记没登记"）。

  用法（仓库根目录）：& 'dev\scripts\Test-PortalGateCoverageMap.ps1'
  产物：temp\gate-map\gate-map-stats.json（每次运行刷新；gitignore）。
  退出码：0 全过；1 任一违规。
  有效性验证（注入法，沿用 W83/W87/P112 惯例）：删掉某面的门禁引用（使编排门禁未登记）→ M1 Fail；
  清空某 uncovered 面的 owner → M3 Fail；清空某 partial 面的 boundary → M4 Fail；
  地图引用幽灵门禁名 → M2 Fail；注入证明索引写入不存在的门禁名或漏项 → M7 Fail；还原 → Pass。</zh-CN>
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
  reference a phantom gate name → M2 fails; write a non-existent gate name or drop an entry in the injection-proof index →
  M7 fails; restore → Pass.</en>
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

# <lang>
#   <zh-CN>M7：注入证明索引是覆盖结论的唯一机器事实源，其四类清单里的每个名字都必须是真实已接入的门禁。
#   起因是 W-anp-P267 按错误前缀搜漏，把确实存在的 `Invoke-PortalModuleRuntimeGate.ps1` 误记为幽灵门禁；
#   同时校验"四类清单去重后总数 == wired 总数"，防止漏项让某项门禁静默从覆盖结论里消失。
#   索引文件缺失直接判 Fail（结论无源），而非软跳过。</zh-CN>
#   <en>M7: the injection-proof index is the sole machine source of truth for coverage conclusions, so every name in its
#   four lists must be a real wired gate. Motivation: W-anp-P267 searched with the wrong prefix and mislabelled the
#   existing `Invoke-PortalModuleRuntimeGate.ps1` as a phantom gate. Also assert that the union of the four lists equals
#   the wired count, so a dropped entry cannot silently remove a gate from the conclusion. A missing index fails outright
#   (conclusions without a source), rather than being softly skipped.</en>
# </lang>
$indexPath = Join-Path $repoRoot 'dev\gate-injection-proof-index.json'
$ledgerPath = Join-Path $repoRoot 'dev\gate-injection-proof-not-applicable.json'
$indexGateCount = 0
$notApplicableCount = 0
if (-not (Test-Path -LiteralPath $indexPath)) {
    $failures += "M7 注入证明索引不存在：$indexPath（覆盖结论无机器事实源）。"
}
else {
    $proofIndex = Get-Content -LiteralPath $indexPath -Raw -Encoding UTF8 | ConvertFrom-Json
    # <lang>
    #   <zh-CN>五类清单：proved / notDetected / other / neverTouched 是覆盖结论的四种状态，
    #   `notApplicable` 是「已评估并裁定不适用」——它也是结论的一种，必须纳入并集核对，
    #   否则这类门禁会被误报成"覆盖结论漏项"。同时不能把 notApplicable 混进 proved 去凑覆盖率。</zh-CN>
    #   <en>Five lists: proved / notDetected / other / neverTouched are the four coverage states, while `notApplicable`
    #   means "evaluated and adjudicated not applicable" — also a conclusion, so it must join the union check, otherwise
    #   such a gate is misreported as a dropped entry. It must never be mixed into proved to inflate coverage.</en>
    # </lang>
    $indexNames = @()
    $byList = [ordered]@{}
    foreach ($listName in @('proved', 'notDetected', 'other', 'neverTouched', 'notApplicable')) {
        $names = @(@($proofIndex.$listName) | ForEach-Object { [string]$_ })
        $byList[$listName] = $names
        foreach ($name in $names) { $indexNames += $name }
    }
    $notApplicableCount = @($byList['notApplicable']).Count
    $indexNames = @($indexNames | Sort-Object -Unique)
    $indexGateCount = $indexNames.Count
    foreach ($name in $indexNames) {
        if ($wired -notcontains $name) { $failures += "M7 注入证明索引引用的门禁未接入编排：$name。" }
        if (-not (Test-Path -LiteralPath (Join-Path $scriptsDir $name))) { $failures += "M7 注入证明索引引用的门禁文件不存在：$name。" }
    }
    foreach ($gate in $wired) {
        if ($indexNames -notcontains $gate) { $failures += "M7 已接入门禁未出现在注入证明索引五类清单中：$gate（覆盖结论漏项）。" }
    }
    if ($indexNames.Count -ne $wired.Count) {
        $failures += ("M7 注入证明索引五类清单去重 {0} 个 ≠ 已接入门禁 {1} 个（重复或漏项）。" -f $indexNames.Count, $wired.Count)
    }
    foreach ($name in @($byList['notApplicable'])) {
        if ($byList['proved'] -contains $name) { $failures += "M7 已裁定不适用的门禁同时出现在 proved 中：$name（不得计入覆盖）。" }
        if ($byList['neverTouched'] -contains $name) { $failures += "M7 已裁定不适用的门禁同时出现在 neverTouched 中：$name（结论自相矛盾）。" }
    }

    # <lang>
    #   <zh-CN>索引的 notApplicable 必须与事实源台账逐项一致，且台账条目带非空依据。
    #   这样「已裁定不适用」不是索引里的一句自述，而是可追溯到裁定依据的机器核对结论；
    #   后续批次不能悄悄把某门禁标成不适用来抬高覆盖率。</zh-CN>
    #   <en>The index's notApplicable list must match the fact-source ledger entry by entry, and each ledger entry must
    #   carry a non-empty basis. "Adjudicated not applicable" is then not a self-declaration in the index but a
    #   machine-checked conclusion traceable to its rationale; later batches cannot quietly mark a gate not-applicable
    #   to inflate coverage.</en>
    # </lang>
    $ledgerNames = @()
    if (-not (Test-Path -LiteralPath $ledgerPath)) {
        if ($notApplicableCount -gt 0) { $failures += 'M7 索引声明了 notApplicable，但事实源台账不存在：' + $ledgerPath }
    }
    else {
        $ledger = Get-Content -LiteralPath $ledgerPath -Raw -Encoding UTF8 | ConvertFrom-Json
        foreach ($entry in @($ledger.entries)) {
            $name = [string]$entry.gate
            $ledgerNames += $name
            if ([string]::IsNullOrWhiteSpace([string]$entry.basis)) { $failures += "M7 notApplicable 台账条目缺依据 basis：$name。" }
            if ($wired -notcontains $name) { $failures += "M7 notApplicable 台账条目不在已接入门禁集中：$name。" }
            if ($byList['notApplicable'] -notcontains $name) { $failures += "M7 notApplicable 台账条目未出现在索引中：$name。" }
        }
        foreach ($name in @($byList['notApplicable'])) {
            if ($ledgerNames -notcontains $name) { $failures += "M7 索引里的 notApplicable 条目缺台账依据：$name。" }
        }
    }
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
    injectionProofIndexGates = $indexGateCount
    injectionProofNotApplicable = $notApplicableCount
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
Write-Host ("注入证明索引：五类清单去重 {0} 个 / 已接入门禁 {1} 个｜已裁定不适用 {2} 个（M7 核对名字、文件与台账依据）" -f $indexGateCount, $wired.Count, $notApplicableCount)
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
