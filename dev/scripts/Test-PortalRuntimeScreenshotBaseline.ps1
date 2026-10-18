#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>校验运行截图基线的「可复现」规范与证据清单（L0）。</zh-CN>
  <en>Validates the reproducibility spec and the evidence manifest of the runtime screenshot baseline (L0).</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>`C-anp-P28`（`W-anp-P151`）交付：把验收判据 V2「运行截图可复现（有脚本 / 有规范 / 进证据包）」
  从文档叙述变成机器核对。截图本身**不入库**（`C-anp-P18` 收口口径：按需生成 + 摘要入库；
  `docs/operations-runbook.md`：不把截图放入根仓库），入库的是**清单**，故本门禁核对"规范 + 清单"，
  图片只在本地存在时做存在性校验。

  断言：
    A1 采集工具 `dev/scripts/Capture-PortalRuntimeScreenshots.mjs` 存在，且固化了可复现规范：
       视口 1440×1000、`deviceScaleFactor: 1`、locale `zh-CN`、只截视口（`fullPage: false`）——
       固定视口是可复现的前提，整页截图会随内容高度变化而不可比。
    A2 目标清单 default-deny：≥10 个目标，且业务页（business）与后台页（admin）两类都覆盖；
       `area + id` 不得重复（命名冲突会让两个目标互相覆盖同名文件）。
    A3 证据清单（入库）必须与工具中的目标清单一致：schemaVersion / spec 与工具常量一致、
       每个目标都有条目、条目含 file / sha256 / bytes / url；缺条目或被删条目即失败。
       清单缺失 → 显式 exit 2（证据产物缺失 = 前置缺失，需先跑采集工具，不是代码缺陷）。
    A4 本地存在截图时：文件必须存在且 ≥1KB（白页防护）；sha256 与清单不一致**只作软指标**
       ——截图字节随字体/渲染/数据状态跨机器变化，强一致会造成假阳性；
       本地无截图时登记为"按需生成"（软指标），不静默放过。

  证明边界：只证明"采集规范固定、目标清单与证据清单一致、图片若存在则非空"；
  **不证明截图内容与页面语义相符**（人工复核承载）、**不证明跨机器像素一致**（字体/渲染差异天然存在）、
  不证明截图覆盖了所有产品页面（按已登记目标清单 default-deny，新增页面须登记进 TARGETS）。

  用法（仓库根目录）：& 'dev\scripts\Test-PortalRuntimeScreenshotBaseline.ps1'
  产物：temp\p151\runtime-screenshot-stats.json（gitignore）。
  退出码：0 通过；1 断言失败；2 环境/证据前置缺失。
  </zh-CN>
  <en>`C-anp-P28` (`W-anp-P151`) deliverable: turns acceptance criterion V2 — "runtime screenshots are reproducible (script /
  spec / evidence package)" — from prose into a machine check. The screenshots themselves are **not committed** (the
  `C-anp-P18` closeout rule: generate on demand, commit the summary; `docs/operations-runbook.md`: keep screenshots out of
  the root repository); what is committed is the **manifest**, so this gate checks "spec + manifest" and validates images
  only when they exist locally.

  Assertions:
    A1 the capture tool `dev/scripts/Capture-PortalRuntimeScreenshots.mjs` exists and pins the reproducibility spec:
       viewport 1440x1000, `deviceScaleFactor: 1`, locale `zh-CN`, viewport-only shots (`fullPage: false`) — a fixed
       viewport is the precondition of reproducibility, whereas full-page shots vary with content height.
    A2 default-deny target list: >= 10 targets, covering both business and admin areas; `area + id` must be unique
       (a collision makes two targets overwrite the same file name).
    A3 the committed manifest must agree with the tool's target list: schemaVersion / spec equal to the tool constants,
       every target has an entry, and entries carry file / sha256 / bytes / url; a missing or removed entry fails.
       A missing manifest exits 2 (a missing evidence artefact is a prerequisite gap — run the capture tool first — not a
       code defect).
    A4 when screenshots exist locally: the file must exist and be >= 1KB (blank-page guard); a sha256 mismatch is a
       **soft metric only** — screenshot bytes vary across machines with fonts/rendering/data state, so requiring equality
       would be a false positive; when no screenshots exist locally it is registered as "generated on demand" (soft),
       never silently ignored.

  Proof boundary: proves only that "the capture spec is pinned, the target list and the manifest agree, and images are
  non-empty when present"; does **not** prove that screenshot content matches page semantics (human review owns that),
  does **not** prove cross-machine pixel equality (font/rendering differences are inherent), and does not prove coverage
  of every product page (default-deny over the registered target list; new pages must be registered in TARGETS).

  Usage (repository root): & 'dev\scripts\Test-PortalRuntimeScreenshotBaseline.ps1'
  Output: temp\p151\runtime-screenshot-stats.json (gitignored).
  Exit codes: 0 pass; 1 assertion failure; 2 missing environment/evidence prerequisite.
  </en>
</lang>
.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Test-PortalRuntimeScreenshotBaseline.ps1'   # 校验截图规范与证据清单</zh-CN>
  <en>& 'dev\scripts\Test-PortalRuntimeScreenshotBaseline.ps1'   # validate the screenshot spec and evidence manifest</en>
</lang>
.NOTES
<lang>
  <zh-CN>演进：`W-anp-P151` 交付；截图采集由 `Capture-PortalRuntimeScreenshots.mjs` 按需执行（非门禁）。</zh-CN>
  <en>Provenance: delivered by `W-anp-P151`; capture itself runs on demand via `Capture-PortalRuntimeScreenshots.mjs` (not a gate).</en>
</lang>
#>

[CmdletBinding()]
param(
    [string] $StatsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$toolPath = Join-Path $repoRoot 'dev\scripts\Capture-PortalRuntimeScreenshots.mjs'
$manifestPath = Join-Path $repoRoot 'work-zone\dev\evidence\p151\screenshot-manifest.json'
$screenshotDir = Join-Path $repoRoot 'work-zone\dev\evidence\p151\screenshots'

$failures = @()
$softNotes = @()

# <lang><zh-CN>A1：采集工具必须存在并固化可复现规范。</zh-CN>
# <en>A1: the capture tool must exist and pin the reproducibility spec.</en></lang>
if (-not (Test-Path -LiteralPath $toolPath)) {
    Write-Host '[PREREQ-MISSING] 采集工具不存在：' + $toolPath + '（exit 2）'
    exit 2
}
$toolText = [System.IO.File]::ReadAllText($toolPath)
$specChecks = @(
    @{ Name = '视口 1440×1000'; Pattern = 'viewport:\s*\{\s*width:\s*1440,\s*height:\s*1000\s*\}' },
    @{ Name = 'deviceScaleFactor 1'; Pattern = 'deviceScaleFactor:\s*1' },
    @{ Name = "locale 'zh-CN'"; Pattern = "locale:\s*'zh-CN'" },
    @{ Name = '只截视口 fullPage: false'; Pattern = 'fullPage:\s*false' }
)
foreach ($check in $specChecks) {
    if ($toolText -notmatch $check.Pattern) { $failures += "A1 采集工具未固化规范项：$($check.Name)（可复现的前提被改动即失败）。" }
}

# <lang><zh-CN>A2：目标清单 default-deny（数量、两类覆盖、area+id 唯一）。</zh-CN>
# <en>A2: default-deny target list (count, both areas covered, area+id unique).</en></lang>
$targets = @()
foreach ($m in [regex]::Matches($toolText, "\{\s*id:\s*'(?<id>[^']+)',\s*area:\s*'(?<area>[^']+)',\s*url:\s*base \+ '(?<path>[^']*)'")) {
    $targets += [pscustomobject]@{ id = $m.Groups['id'].Value; area = $m.Groups['area'].Value; path = $m.Groups['path'].Value }
}
if ($targets.Count -lt 10) { $failures += "A2 目标数 $($targets.Count) < 10（关键后台与业务页面覆盖不足）。" }
$areas = @($targets | ForEach-Object { $_.area } | Sort-Object -Unique)
foreach ($required in @('business', 'admin')) {
    if ($areas -notcontains $required) { $failures += "A2 目标清单缺少 $required 类页面（须同时覆盖业务页与后台页）。" }
}
$dupes = @($targets | Group-Object -Property @{ Expression = { $_.area + '/' + $_.id } } | Where-Object { $_.Count -gt 1 })
foreach ($dupe in $dupes) { $failures += "A2 目标重复：$($dupe.Name)（命名冲突会互相覆盖同名截图）。" }

# <lang><zh-CN>A3：证据清单必须与目标清单一致（缺失即前置缺失 exit 2）。</zh-CN>
# <en>A3: the manifest must agree with the target list (missing means prerequisite gap, exit 2).</en></lang>
if (-not (Test-Path -LiteralPath $manifestPath)) {
    Write-Host '[PREREQ-MISSING] 证据清单不存在：' + $manifestPath + '（先执行 Capture-PortalRuntimeScreenshots.mjs 采集；exit 2）'
    exit 2
}
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
if (-not $manifest.schemaVersion) { $failures += 'A3 证据清单缺 schemaVersion。' }
if ($manifest.spec.viewport.width -ne 1440 -or $manifest.spec.viewport.height -ne 1000 -or $manifest.spec.deviceScaleFactor -ne 1 -or $manifest.spec.locale -ne 'zh-CN') {
    $failures += 'A3 证据清单 spec 与规范不一致（视口/DPR/locale）。'
}
$manifestKeys = @($manifest.entries | ForEach-Object { $_.area + '/' + $_.id })
$targetKeys = @($targets | ForEach-Object { $_.area + '/' + $_.id })
foreach ($key in $targetKeys) {
    if ($manifestKeys -notcontains $key) { $failures += "A3 证据清单缺少目标条目：$key（采集后须重新入库清单）。" }
}
foreach ($key in $manifestKeys) {
    if ($targetKeys -notcontains $key) { $failures += "A3 证据清单存在未登记目标条目：$key（目标清单已变更，须同步）。" }
}
foreach ($entry in @($manifest.entries)) {
    $expectedName = "p151-$($entry.area)-$($entry.id)-zh-CN-1440x1000.png"
    if ($entry.file -ne $expectedName) { $failures += "A3 条目命名不符合规范：$($entry.file)（应为 $expectedName）。" }
    if ([string]::IsNullOrWhiteSpace([string]$entry.sha256) -or ([string]$entry.sha256).Length -ne 64) { $failures += "A3 条目 sha256 缺失或长度异常：$($entry.file)。" }
    if (-not ($entry.bytes -ge 1024)) { $failures += "A3 条目 bytes < 1024（疑似空白页）：$($entry.file)。" }
}

# <lang><zh-CN>A4：本地截图存在则校验非空（sha256 差异仅软指标；不存在则登记"按需生成"）。</zh-CN>
# <en>A4: when screenshots exist locally they must be non-empty (sha256 differences are soft only; absence is registered as "generated on demand").</en></lang>
$present = 0
$hashMismatch = 0
foreach ($entry in @($manifest.entries)) {
    $filePath = Join-Path $screenshotDir ([string]$entry.file)
    if (-not (Test-Path -LiteralPath $filePath)) { continue }
    $present += 1
    $item = Get-Item -LiteralPath $filePath
    if ($item.Length -lt 1024) { $failures += "A4 本地截图过小（疑似白页）：$($entry.file)（$($item.Length) 字节）。"; continue }
    $actual = (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne ([string]$entry.sha256).ToLowerInvariant()) { $hashMismatch += 1 }
}
$missing = @($manifest.entries).Count - $present
if ($missing -gt 0) { $softNotes += "[soft] 本地缺少 $missing 个截图文件（截图不入库、按需生成：先跑 Capture-PortalRuntimeScreenshots.mjs）。" }
if ($hashMismatch -gt 0) { $softNotes += "[soft] $hashMismatch 个截图 sha256 与清单不一致（字体/渲染/数据状态差异，正常现象，非缺陷）。" }

# <lang><zh-CN>统计落盘 + 输出。</zh-CN>
# <en>Stats output and reporting.</en></lang>
if ([string]::IsNullOrWhiteSpace($StatsPath)) {
    $statsDir = Join-Path $repoRoot 'temp\p151'
    New-Item -ItemType Directory -Force -Path $statsDir | Out-Null
    $StatsPath = Join-Path $statsDir 'runtime-screenshot-stats.json'
}
else {
    $parent = Split-Path -Parent $StatsPath
    if ($parent -and -not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
}
$report = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    targets = @($targets | ForEach-Object { $_.area + '/' + $_.id })
    manifestEntries = @($manifest.entries).Count
    screenshotsPresent = $present
    screenshotsMissing = $missing
    hashMismatch = $hashMismatch
    softNotes = $softNotes
    failures = $failures
}
[System.IO.File]::WriteAllText($StatsPath, ($report | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))

Write-Host ''
Write-Host '=== 运行截图基线（W-anp-P151）==='
Write-Host ("目标 {0} 个（business {1} / admin {2}）｜清单条目 {3}｜本地截图 {4} 个（缺 {5}）" -f `
    $targets.Count,
    @($targets | Where-Object { $_.area -eq 'business' }).Count,
    @($targets | Where-Object { $_.area -eq 'admin' }).Count,
    @($manifest.entries).Count, $present, $missing)
foreach ($note in $softNotes) { Write-Host ('  ' + $note) }
Write-Host ("统计已写入：$StatsPath")

if ($failures.Count -gt 0) {
    Write-Host "[FAIL] 运行截图基线校验失败 $($failures.Count) 处：" -ForegroundColor Red
    foreach ($failure in $failures) { Write-Host ('  - ' + $failure) -ForegroundColor Red }
    exit 1
}
Write-Host '[PASS] 运行截图基线：规范固定、目标与清单一致、本地截图非空。' -ForegroundColor Green
exit 0
