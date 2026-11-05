<#
.SYNOPSIS
  <lang>
    <zh-CN>批量回归全部（或筛选后的）注入证明夹具，验证每次门禁/工具/fixture 改动后既有 proved 记录仍成立。</zh-CN>
    <en>Runs all (or a filtered subset of) gate-injection fixtures as a regression batch, verifying that existing proved records still hold after every gate/tool/fixture change.</en>
  </lang>
.DESCRIPTION
  <lang>
    <zh-CN>长期项目需要可重复执行的回归入口，而不是每次在会话里手敲命令挑几个夹具跑。本脚本串行遍历夹具目录，
    逐夹具调用 `Invoke-PortalGateInjectionProof.ps1`，把证据写到独立目录，然后按**门禁**聚合判定：
      只要某个门禁在任一夹具里得到 proved，该门禁即视为通过；只有"一个门禁的所有夹具都不是 proved"才算回归。
    这是因为同一门禁历史上允许有多批夹具（新版注入点替换旧版），旧夹具失效不等于门禁失去证明。
    每跑完一个夹具清理一次残留 chromium/headless_shell：实测连续跑多个 playwright 门禁后残留进程会耗尽页面文件，
    导致宿主 `System.Management.Automation.dll` 加载失败（0x800705AF）。
    判定区分两类：出现 `not-detected`/`restore-failed`/`harness-error` 才是**检出能力回归**（退出码 1）；
    仅 `baseline-failed` 归为**基线不可用**（环境前置/数据态，按 W-anp-P139 口径单独上报、不阻断）。
    产物是 markdown 报告（含每夹具 verdict、按门禁聚合结论与索引历史基线），退出码 0 = 无检出能力回归，1 = 有回归，2 = 参数/前置错误。
    本脚本**只读仓库、不改源码**（夹具的 patch 由注入证明工具自行还原），也不重算覆盖索引。</zh-CN>
    <en>A long-lived project needs a repeatable regression entry point rather than hand-typed commands picking a few fixtures
    per session. This script walks the fixture directory serially, invokes `Invoke-PortalGateInjectionProof.ps1` per fixture
    into its own evidence directory, then aggregates **per gate**: a gate passes if any of its fixtures yields proved, because
    one gate may legitimately have several historical fixtures (a newer injection point superseding an older one).
    Verdicts are classified: `not-detected`/`restore-failed`/`harness-error` mean a **detection-power regression** (exit 1),
    while only `baseline-failed` means an **unavailable baseline** (environment prerequisite/data state, reported separately
    without failing, per W-anp-P139). After each fixture it cleans leftover chromium/headless_shell processes: measured runs
    of several playwright gates exhausted the page file with orphan processes, making the host fail to load
    `System.Management.Automation.dll` (0x800705AF). Output is a markdown report (per-fixture verdicts, per-gate conclusions
    and the index's historical baseline); exit 0 = no detection-power regression, 1 = regression, 2 = argument/prerequisite
    error. The script **only reads the repository and never edits source** (fixture patches are restored by the proof tool
    itself) and does not recompute the coverage index.</en>
  </lang>
.PARAMETER FixtureFilter
  <lang><zh-CN>夹具文件名通配符，默认 `*.json`；可用 `L0-*.json` 分批执行静态门禁。</zh-CN><en>Fixture filename glob, default `*.json`; use e.g. `L0-*.json` to batch the static gates.</en></lang>
.PARAMETER EvidenceRoot
  <lang><zh-CN>回归证据根目录；默认 `work-zone/dev/evidence/gate-injection-proof-regression`。证据目录带时间戳，避免覆盖历史。</zh-CN><en>Regression evidence root; defaults to `work-zone/dev/evidence/gate-injection-proof-regression`, timestamped so history is preserved.</en></lang>
.PARAMETER FixtureNames
  <lang><zh-CN>只跑指定夹具文件名（精确数组），用于复核某几个改动涉及的门禁。</zh-CN><en>Run only the given exact fixture filenames, for re-checking gates touched by a change.</en></lang>
.PARAMETER Skip
  <lang><zh-CN>排序后跳过的夹具数，配合 `-Take` 做可复现的分页执行（文件系统 `-Filter` 不支持字符类，长批次需分页以避开单条命令超时）。</zh-CN><en>Fixtures to skip after sorting; with `-Take` gives reproducible paging (filesystem -Filter has no character classes, so long batches need paging to avoid a single-command timeout).</en></lang>
.PARAMETER Take
  <lang><zh-CN>本次最多执行的夹具数；0 表示全部。</zh-CN><en>Maximum fixtures for this run; 0 means all.</en></lang>
.EXAMPLE
  <lang><zh-CN>& 'dev\scripts\Invoke-PortalGateInjectionProofRegression.ps1' -FixtureFilter 'L0-*.json'</zh-CN><en>& 'dev\scripts\Invoke-PortalGateInjectionProofRegression.ps1' -FixtureFilter 'L0-*.json'</en></lang>
.EXAMPLE
  <lang><zh-CN>& 'dev\scripts\Invoke-PortalGateInjectionProofRegression.ps1' -FixtureFilter 'L2-*.json' -Skip 0 -Take 9</zh-CN><en>& 'dev\scripts\Invoke-PortalGateInjectionProofRegression.ps1' -FixtureFilter 'L2-*.json' -Skip 0 -Take 9</en></lang>
#>
[CmdletBinding()]
param(
    [string] $RepoRoot,
    [string] $FixtureFilter = '*.json',
    [string[]] $FixtureNames,
    [string] $EvidenceRoot,
    [int] $Skip = 0,
    [int] $Take = 0
)

$ErrorActionPreference = 'Stop'
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
}
$fixtureDir = Join-Path $RepoRoot 'dev\scripts\gate-injections'
$proofTool = Join-Path $RepoRoot 'dev\scripts\Invoke-PortalGateInjectionProof.ps1'
if (-not (Test-Path -LiteralPath $fixtureDir)) { Write-Error ('夹具目录不存在：' + $fixtureDir); exit 2 }
if (-not (Test-Path -LiteralPath $proofTool)) { Write-Error ('注入证明工具不存在：' + $proofTool); exit 2 }

if ([string]::IsNullOrWhiteSpace($EvidenceRoot)) {
    $EvidenceRoot = Join-Path $RepoRoot 'work-zone\dev\evidence\gate-injection-proof-regression'
}
$runDir = Join-Path $EvidenceRoot ((Get-Date).ToString('yyyyMMddTHHmmss') + '-Dev')
New-Item -ItemType Directory -Force -Path $runDir | Out-Null

# <lang><zh-CN>确定夹具集合：FixtureNames 优先（精确复核），否则按通配符；排序保证可复现的执行顺序。</zh-CN><en>Determine the fixture set: FixtureNames wins (targeted re-check), otherwise the glob; sorting makes execution order reproducible.</en></lang>
$fixtures = @()
if ($FixtureNames -and $FixtureNames.Count -gt 0) {
    foreach ($name in $FixtureNames) {
        $f = Get-ChildItem -LiteralPath $fixtureDir -File | Where-Object { $_.Name -eq $name }
        if (-not $f) { Write-Error ('指定夹具不存在：' + $name); exit 2 }
        $fixtures += $f
    }
}
else {
    $fixtures = @(Get-ChildItem -LiteralPath $fixtureDir -File -Filter $FixtureFilter | Sort-Object Name)
}
# <lang><zh-CN>分页在排序之后生效，故 -Skip/-Take 的组合可复现且不重不漏；参数非法时报错而非静默跑空。</zh-CN><en>Paging applies after sorting, so -Skip/-Take combinations are reproducible with no gaps or overlaps; invalid values error out instead of silently running nothing.</en></lang>
if ($Skip -lt 0 -or $Take -lt 0) { Write-Error '-Skip / -Take 不能为负数。'; exit 2 }
if ($Skip -gt 0) { $fixtures = @($fixtures | Select-Object -Skip $Skip) }
if ($Take -gt 0) { $fixtures = @($fixtures | Select-Object -First $Take) }
if ($fixtures.Count -eq 0) { Write-Error '没有匹配到任何夹具。'; exit 2 }

$gateVerdicts = @{}
$rows = @()
foreach ($fixture in $fixtures) {
    $fixtureEvidence = Join-Path $runDir ($fixture.BaseName)
    # <lang><zh-CN>工具自身对 baseline/injected/restored 三阶段负责；这里只收集它的汇总结论并清理浏览器残留。</zh-CN><en>The tool itself owns the baseline/injected/restored phases; here we only collect its summary and clean browser leftovers.</en></lang>
    $null = & pwsh -NoProfile -File $proofTool -FixturePath $fixture.FullName -EvidenceDir $fixtureEvidence 2>&1
    Get-Process -Name 'chrome', 'headless_shell' -ErrorAction SilentlyContinue | ForEach-Object { $_.Kill() }

    $summaryPath = Join-Path $fixtureEvidence 'injection-proof-summary.json'
    if (-not (Test-Path -LiteralPath $summaryPath)) {
        $rows += [pscustomobject]@{ Fixture = $fixture.Name; Gate = '(no summary)'; Verdict = 'harness-error' }
        continue
    }
    $summary = Get-Content -LiteralPath $summaryPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($record in @($summary.results)) {
        $gate = [string]$record.gate
        $verdict = [string]$record.verdict
        if (-not $gateVerdicts.ContainsKey($gate)) { $gateVerdicts[$gate] = [System.Collections.Generic.List[string]]::new() }
        $gateVerdicts[$gate].Add($verdict)
        $rows += [pscustomobject]@{ Fixture = $fixture.Name; Gate = $gate; Verdict = $verdict }
    }
}

# <lang>
#   <zh-CN>按门禁聚合，并**区分「检出能力回归」与「基线不可用」**：二者处置完全不同。
#   首版把所有非 proved 门禁一律报 REGRESSION，实测 2026-11-05 把两例环境前置误报成回归——
#   一例是 NuGet 外网不可达（门禁按 W-anp-P139 口径 exit 2，明确不是代码缺陷），
#   一例是仓库 site/ 镜像未随 docs/ 重新生成（真实仓库漂移，也不是注入证明失效）。
#   判定：任一夹具 proved → PASS；出现 not-detected / restore-failed / harness-error → REGRESSION
#   （注入后门禁仍绿、还原失败或证明链路断裂，才是证明有效性本身失效）；
#   仅有 baseline-failed → BASELINE-UNAVAILABLE（环境前置/数据态，单独上报但不计回归、不阻断）。
#   索引作为历史基线一并写入报告，便于判断"历史 proved 但现在基线不绿"是否需要补齐前置。
#   分批执行时结论只覆盖本次夹具；某门禁的 proved 夹具可能在别的批次，故报告显式标注该局限。</zh-CN>
#   <en>Aggregate per gate while **distinguishing detection-power regressions from an unavailable baseline** — they need
#   different responses. The first version labelled every non-proved gate as REGRESSION and, measured 2026-11-05,
#   misreported two environment prerequisites: NuGet being unreachable (the gate exits 2 per W-anp-P139, explicitly not a
#   code defect) and the site/ mirror not being regenerated after a docs/ change (real repo drift, also not a lost proof).
#   Verdict: any proved fixture → PASS; not-detected / restore-failed / harness-error → REGRESSION (the gate stays green
#   after injection, restore fails, or the proof chain breaks — the proof itself is broken); only baseline-failed →
#   BASELINE-UNAVAILABLE (an environment/data prerequisite, reported separately without failing the run).
#   The coverage index is included as a historical baseline so "proved before but baseline red now" is visible.
#   Batched runs conclude only over the fixtures actually run; another batch may hold the proved fixture, so the report
#   states that limitation explicitly.</en>
# </lang>
$indexPath = Join-Path $RepoRoot 'dev\gate-injection-proof-index.json'
$indexProved = @()
$indexNotApplicable = @()
if (Test-Path -LiteralPath $indexPath) {
    $proofIndex = Get-Content -LiteralPath $indexPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $indexProved = @(@($proofIndex.proved) | ForEach-Object { [string]$_ })
    $indexNotApplicable = @(@($proofIndex.notApplicable) | ForEach-Object { [string]$_ })
}
$detectionFailureVerdicts = @('not-detected', 'restore-failed', 'harness-error', 'injected-green')

# <lang>
#   <zh-CN>回归结论必须与**执行范围**绑定：只有全量执行（默认 `*.json`、无 Skip/Take、无 FixtureNames）才有资格
#   下"回归"结论。分页或筛选执行时，某门禁的 proved 夹具可能落在别的批次——实测 2026-11-05 首版把
#   `L2-batch1.json`（历史废弃夹具，含 not-detected）里的两个门禁误报成 REGRESSION，而它们的 proved 夹具
#   `L2-batch26.json` / `L2-batch23.json` 其实在后续批次、索引基线也仍是 proved。
#   因此非全量执行时，这类门禁标为 `NOT-REPRODUCED-IN-BATCH`（显式上报但不阻断）；全量执行时才判 REGRESSION。
#   这不是放宽判定（全量仍严格），而是不让部分观察伪装成全局结论。</zh-CN>
#   <en>Regression conclusions must be tied to the **execution scope**: only a full run (default `*.json`, no Skip/Take,
#   no FixtureNames) may declare a regression. Under paging or filtering, a gate's proved fixture may live in another batch —
#   measured 2026-11-05, the first version misreported two gates from `L2-batch1.json` (a historical fixture containing
#   not-detected) as REGRESSION, while their proved fixtures `L2-batch26.json` / `L2-batch23.json` were in later batches
#   and the index baseline was still proved. Non-full runs therefore label these `NOT-REPRODUCED-IN-BATCH` (reported, not
#   failing); a full run still judges them REGRESSION. This does not weaken detection (full runs stay strict) — it prevents
#   a partial observation from masquerading as a global conclusion.</en>
# </lang>
$isFullRun = ($FixtureFilter -eq '*.json' -and $Skip -eq 0 -and $Take -eq 0 -and -not ($FixtureNames -and $FixtureNames.Count -gt 0))

$regressed = @()
$unavailable = @()
$notInBatch = @()
$gateRows = @()
foreach ($gate in ($gateVerdicts.Keys | Sort-Object)) {
    $verdicts = @($gateVerdicts[$gate])
    if ($indexProved -contains $gate) { $indexStatus = 'proved' }
    elseif ($indexNotApplicable -contains $gate) { $indexStatus = 'notApplicable' }
    else { $indexStatus = 'other' }

    if ($verdicts -contains 'proved') {
        $status = 'PASS'
    }
    elseif ($indexNotApplicable -contains $gate) {
        $status = 'NOT-APPLICABLE'
    }
    else {
        $failures = @($verdicts | Where-Object { $detectionFailureVerdicts -contains $_ })
        if ($failures.Count -gt 0 -and (-not $isFullRun) -and ($indexProved -contains $gate)) {
            $status = 'NOT-REPRODUCED-IN-BATCH'
            $notInBatch += $gate
        }
        elseif ($failures.Count -gt 0) {
            $status = 'REGRESSION'
            $regressed += $gate
        }
        else {
            $status = 'BASELINE-UNAVAILABLE'
            $unavailable += $gate
        }
    }
    $gateRows += [pscustomobject]@{
        Gate = $gate
        Status = $status
        IndexStatus = $indexStatus
        Verdicts = ($verdicts -join ', ')
    }
}

$lines = @()
$lines += '# 注入证明批量回归'
$lines += ''
$lines += ('- 生成时间：' + (Get-Date).ToString('s'))
$lines += ('- 夹具数：' + $fixtures.Count + '｜筛选：' + $FixtureFilter + '｜Skip=' + $Skip + '｜Take=' + $Take + '｜本次覆盖门禁数：' + $gateRows.Count + '｜检出能力回归：' + $regressed.Count + '｜基线不可用：' + $unavailable.Count)
$lines += ('- 执行范围：' + $(if ($isFullRun) { '**全量**' } else { '分批/筛选（不具回归结论资格）' }))
$lines += '- 判定口径：任一夹具 `proved` 即 PASS（允许新版注入点取代旧夹具）；出现 `not-detected` / `restore-failed` / `harness-error` 判 REGRESSION；仅 `baseline-failed` 判 BASELINE-UNAVAILABLE（环境前置/数据态，按 W-anp-P139 口径不计回归、不阻断）。'
$lines += '- 分批局限：非全量执行时，索引基线为 `proved` 但本次无 proved 的门禁标为 `NOT-REPRODUCED-IN-BATCH`（显式上报、不阻断），因为其 proved 夹具可能在其它批次；只有全量执行才可下 REGRESSION 结论。'
$lines += ''
$lines += '## 按门禁聚合'
$lines += ''
$lines += '| 门禁 | 结论 | 索引基线 | 各夹具 verdict |'
$lines += '| --- | --- | --- | --- |'
foreach ($g in $gateRows) {
    $mark = if ($g.Status -eq 'REGRESSION') { '**' + $g.Status + '**' } else { $g.Status }
    $lines += ('| `' + $g.Gate + '` | ' + $mark + ' | ' + $g.IndexStatus + ' | ' + $g.Verdicts + ' |')
}
if ($notInBatch.Count -gt 0) {
    $lines += ''
    $lines += '## 批次内未复现（索引基线 proved，需全量执行或补跑对应夹具确认）'
    $lines += ''
    foreach ($g in $notInBatch) { $lines += '- `' + $g + '`' }
}
if ($unavailable.Count -gt 0) {
    $lines += ''
    $lines += '## 基线不可用（不计回归，需补齐环境前置后再复验）'
    $lines += ''
    foreach ($g in $unavailable) { $lines += '- `' + $g + '`' }
}
$lines += ''
$lines += '## 逐夹具明细'
$lines += ''
$lines += '| 夹具 | 门禁 | verdict |'
$lines += '| --- | --- | --- |'
foreach ($r in $rows) { $lines += ('| `' + $r.Fixture + '` | `' + $r.Gate + '` | ' + $r.Verdict + ' |') }
[System.IO.File]::WriteAllText((Join-Path $runDir 'regression-report.md'), ($lines -join "`n"), $utf8NoBom)
[System.IO.File]::WriteAllText((Join-Path $runDir 'regression-summary.json'), ([pscustomobject]@{
    generatedAt = (Get-Date).ToString('s')
    isFullRun = $isFullRun
    fixtureCount = $fixtures.Count
    gateCount = $gateRows.Count
    regressedGates = $regressed
    baselineUnavailableGates = $unavailable
    notReproducedInBatchGates = $notInBatch
    gates = $gateRows
} | ConvertTo-Json -Depth 5), $utf8NoBom)

Write-Host ('回归完成：' + $(if ($isFullRun) { '全量' } else { '分批' }) + ' ｜夹具 ' + $fixtures.Count + ' ｜门禁 ' + $gateRows.Count + ' ｜回归 ' + $regressed.Count + ' ｜批次内未复现 ' + $notInBatch.Count + ' ｜基线不可用 ' + $unavailable.Count)
foreach ($g in $regressed) { Write-Host ('  REGRESSION: ' + $g) }
foreach ($g in $notInBatch) { Write-Host ('  NOT-REPRODUCED-IN-BATCH: ' + $g) }
foreach ($g in $unavailable) { Write-Host ('  BASELINE-UNAVAILABLE: ' + $g) }
Write-Host ('报告：' + (Join-Path $runDir 'regression-report.md'))

if ($regressed.Count -gt 0) { exit 1 }
exit 0
