#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>landing 统计数字门禁（L0，静态）：文档子站首页的统计数字必须与可计算真值一致，杜绝手工过期。</zh-CN>
  <en>Landing statistics gate (L0, static): the statistics on the docs subsite landing page must match computable truth, so hand-maintained numbers can never go stale.</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>`C-anp-P27`（`W-anp-P148`）交付：landing（`site/index.md` 与 `site/en/index.md`）上的统计数字此前**手工维护**，
  实测已漂移——本轮手工把"篇文档"从 31 改到 29，而真值（已上架文档篇数）是 27。手工修过一次的数字仍会再次过期，
  唯一可靠的是**可重复计算的取数**。本门禁把五个数字全部改为机器取数并核对：

  A1 篇文档 / Documents = `docs/**/*.md` 中**在 `site/` 下存在对应页面**（`site/<slug>/index.md`）的篇数（已上架口径）；
  A2 套正式皮肤 / Official skins = `src/Portal/App_Themes` 主题目录数**排除 `ThemeProbe` 探针主题**
     （口径对齐 `AGENTS.md`：6 套正式 + `Default` 回退 = 7，探针主题不是皮肤）；
  A3 UI 语言数 = `App_GlobalResources/lang.*.resx` 语言资源数（zh 侧以"双语"呈现，en 侧以数字呈现）；
  A4 当前版本 / Current version = `docs/versioning.md`「六、当前版本」节声明的版本；
  A5 开源许可 / Licence = 根目录 `LICENSE` 可判定的许可证（当前为 MIT）；无法判定时不作硬断言。
  M0 default-deny：landing 上出现的**每一个**统计块都必须被本门禁认识并核对（未识别的统计项 → Fail，
     防止新增一个没人核对的数字）。

  软指标：未上架文档清单（当前 4 篇：内部/开发文档，非缺陷，仅上报）。

  证明边界：只证明 landing 声明的统计数字与**上述定义**的真值一致；不证明文档内容质量、
  不证明"未上架"是刻意决定（上架取舍属内容范围，已如实登记为软指标）。

  用法（仓库根目录）：& 'dev\scripts\Test-PortalLandingStats.ps1'
  产物：temp\landing-stats\landing-stats.json（每次运行刷新；gitignore）。
  退出码：0 全过；1 任一数字漂移或出现未识别统计项。
  有效性验证（注入法，沿用 W83/W87/P112 惯例）：把"篇文档"改成任意错值 → A1 Fail；改皮肤数 → A2 Fail；
  改版本号 → A4 Fail；新增一个未识别统计块 → M0 Fail；还原 → Pass。</zh-CN>
  <en>`C-anp-P27` (`W-anp-P148`) deliverable: the statistics on the landing pages (`site/index.md` and `site/en/index.md`) were
  **hand-maintained** and had already drifted — this cycle corrected "Documents" from 31 to 29 by hand, while the truth
  (published document count) is 27. A number corrected by hand once goes stale again; only a **repeatable computation** is
  reliable. This gate derives and cross-checks five numbers:

  A1 Documents / 篇文档 = the count of `docs/**/*.md` that **have a corresponding page** under `site/` (`site/<slug>/index.md`)
     (the published-docs definition);
  A2 Official skins / 套正式皮肤 = the number of theme directories under `src/Portal/App_Themes` **excluding the `ThemeProbe`
     probe theme** (aligned with `AGENTS.md`: 6 official + the `Default` fallback = 7; a probe theme is not a skin);
  A3 UI languages = the number of `App_GlobalResources/lang.*.resx` locale resources (the zh side renders "双语", the en side a number);
  A4 Current version / 当前版本 = the version declared in the "current version" section of `docs/versioning.md`;
  A5 Licence / 开源许可 = the licence decidable from the root `LICENSE` (currently MIT); when undecidable it is not asserted.
  M0 default-deny: **every** statistic block on the landing pages must be recognised and checked by this gate (an unrecognised
     statistic → Fail, so no new number can appear without a check).

  Soft metric: the list of unpublished documents (currently 4: internal/development docs — not a defect, reported only).

  Proof boundary: proves only that the landing's declared numbers equal the truth under **the definitions above**; does not
  prove document content quality, nor that "unpublished" is a deliberate decision (publishing scope is a content matter,
  reported honestly as a soft metric).

  Usage (repository root): & 'dev\scripts\Test-PortalLandingStats.ps1'
  Output: temp\landing-stats\landing-stats.json (refreshed per run; gitignored).
  Exit codes: 0 pass; 1 any drifted number or unrecognised statistic.
  Effectiveness proof (injection, per the W83/W87/P112 convention): set "Documents" to any wrong value → A1 fails; change the
  skin count → A2 fails; change the version → A4 fails; add an unrecognised statistic block → M0 fails; restore → Pass.</en>
</lang>
.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Test-PortalLandingStats.ps1'   # 仓库根目录执行；exit 0 全过，exit 1 任一漂移</zh-CN>
  <en>& 'dev\scripts\Test-PortalLandingStats.ps1'   # run from the repository root; exit 0 pass, exit 1 any drift</en>
</lang>
.NOTES
<lang>
  <zh-CN>演进：`W-anp-P148` 交付并接入 L0（37→38）。交付时把已漂移的"篇文档 29"修正为真值 27。</zh-CN>
  <en>Provenance: delivered by `W-anp-P148` and wired into L0 (37→38). On delivery the drifted "Documents 29" was corrected to the true value 27.</en>
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

# ---------------------------------------------------------------------------
# 取数：五个统计数字的可计算真值
# ---------------------------------------------------------------------------
$docsDir = Join-Path $repoRoot 'docs'
$siteDir = Join-Path $repoRoot 'site'

$failures = @()

# <lang>
#   <zh-CN>A1：已上架文档篇数 = docs 与 site 页面**按标题匹配**（docs 的 H1 ↔ site 页 front-matter `title`），
#   文件名匹配仅作回退。
#
#   **教训与修正（2026-10-19，W-anp-P150）**：初版按「文件名 = 站点目录名」映射，漏掉两个别名
#   （`module-development-guide.md` → `site/module-development/`、`README.md` → `site/docs-index/`），
#   把正确的 **29** 误判为 27，并据此把 landing 上原本正确的数字改错。
#   自动取数若映射规则有缺陷，比手工数字更危险——它带着"机器算的"权威感，错得更隐蔽。
#   故改为按标题匹配（别名天然成立，不依赖目录命名），并加三道保险：
#     ① 标题歧义（多篇文档/多页同标题）→ 失败（无法判定即失败）；
#     ② 未匹配文档必须在 `INTENTIONALLY_UNPUBLISHED` 白名单中并附依据 → 否则失败
#        （default-deny：新文档要么上架、要么显式登记"刻意不上架"及理由）；
#     ③ 白名单条目本身漂移（文档已删除仍留在白名单）→ 失败。
#   真值交叉验证（两种独立口径一致）：站点页 32 − 非文档页 3 = 29；docs 31 − 未上架 2 = 29。</zh-CN>
#   <en>A1: published docs = docs matched to site pages **by title** (docs H1 ↔ site front-matter `title`); file-name matching
#   is only a fallback.
#
#   **Lesson and correction (2026-10-19, W-anp-P150)**: the first version mapped "file name = site directory name" and missed two
#   aliases (`module-development-guide.md` → `site/module-development/`, `README.md` → `site/docs-index/`), reading the correct
#   **29** as 27 and "correcting" a landing number that had been right. A flawed automatic derivation is more dangerous than a
#   hand-written number: it borrows the authority of "computed by machine" and hides its error better. Matching is therefore
#   title-based (aliases hold naturally, independent of directory naming) with three safeguards:
#     ① title ambiguity (several docs or pages sharing a title) → fail (undecidable means failure);
#     ② an unmatched doc must appear in `INTENTIONALLY_UNPUBLISHED` with a basis → otherwise fail (default-deny: a new doc is
#        either published or explicitly registered as intentionally unpublished with a reason);
#     ③ allowlist drift (an entry whose doc no longer exists) → fail.
#   Truth cross-checked by two independent derivations: 32 site pages − 3 non-doc pages = 29; 31 docs − 2 unpublished = 29.</en>
# </lang>
function Get-FirstHeading([string] $path) {
    foreach ($line in [System.IO.File]::ReadAllLines($path)) {
        if ($line -match '^#\s+(.+?)\s*$') { return $Matches[1].Trim() }
    }
    return $null
}

# <lang><zh-CN>刻意不上架的文档（内部治理文档，非公开文档集）——必须附依据，default-deny。</zh-CN>
# <en>Deliberately unpublished docs (internal governance docs, not part of the public set) — a basis is required, default-deny.</en></lang>
$INTENTIONALLY_UNPUBLISHED = @{
    'documentation-registry.md' = '内部文档治理台账（登记与复核用），不属公开文档集。'
    'documentation-review-checklist.md' = '内部文档复核走查清单（流程工具），不属公开文档集。'
}

$sitePages = @()
foreach ($dir in (Get-ChildItem $siteDir -Directory | Where-Object { $_.Name -notmatch '^(_|en$)' })) {
    $indexPath = Join-Path $dir.FullName 'index.md'
    if (-not (Test-Path -LiteralPath $indexPath)) { continue }
    $text = [System.IO.File]::ReadAllText($indexPath)
    $title = $null
    if ($text -match '(?m)^title:\s*(.+?)\s*$') { $title = $Matches[1].Trim() }
    if (-not $title) { $title = Get-FirstHeading $indexPath }
    $sitePages += [pscustomobject]@{ dir = $dir.Name; title = $title }
}

$docs = @()
foreach ($md in (Get-ChildItem $docsDir -Recurse -File -Filter '*.md')) {
    $rel = $md.FullName.Substring($docsDir.Length + 1)
    $docs += [pscustomobject]@{ rel = $rel; name = $md.Name; title = (Get-FirstHeading $md.FullName) }
}

# <lang><zh-CN>① 标题歧义：多篇文档或多页同标题会让匹配不可判定 → 直接失败。</zh-CN>
# <en>① Title ambiguity: several docs or pages sharing a title makes matching undecidable → fail outright.</en></lang>
foreach ($g in @($docs | Where-Object { $_.title } | Group-Object -Property title | Where-Object { $_.Count -gt 1 })) {
    $failures += ("A1 docs 标题重复：「{0}」（{1} 篇）——按标题匹配会产生歧义，无法判定上架状态。" -f $g.Name, $g.Count)
}
foreach ($g in @($sitePages | Where-Object { $_.title } | Group-Object -Property title | Where-Object { $_.Count -gt 1 })) {
    $failures += ("A1 site 页面标题重复：「{0}」（{1} 页）——按标题匹配会产生歧义。" -f $g.Name, $g.Count)
}

$publishedDocs = @(); $unpublishedDocs = @()
foreach ($doc in $docs) {
    $match = $null
    if ($doc.title) { $match = @($sitePages | Where-Object { $_.title -eq $doc.title } | Select-Object -First 1)[0] }
    if (-not $match) {
        $slug = if ($doc.rel -like 'contracts*') { 'contracts-' + [IO.Path]::GetFileNameWithoutExtension($doc.name) } else { [IO.Path]::GetFileNameWithoutExtension($doc.name) }
        $match = @($sitePages | Where-Object { $_.dir -ieq $slug } | Select-Object -First 1)[0]
    }
    if ($match) {
        $publishedDocs += $doc.rel
        continue
    }
    $unpublishedDocs += $doc.rel
    # <lang><zh-CN>② default-deny：未匹配且未在白名单 → 失败。</zh-CN>
    # <en>② default-deny: unmatched and not allowlisted → fail.</en></lang>
    if (-not $INTENTIONALLY_UNPUBLISHED.ContainsKey($doc.rel)) {
        $failures += ("A1 文档既未匹配到站点页、也不在刻意不上架白名单中：{0}（新增文档必须上架，或显式登记「刻意不上架」依据）。" -f $doc.rel)
    }
}
# <lang><zh-CN>③ 白名单漂移：条目对应的文档已不存在 → 失败。</zh-CN>
# <en>③ Allowlist drift: an entry whose doc no longer exists → fail.</en></lang>
foreach ($key in @($INTENTIONALLY_UNPUBLISHED.Keys)) {
    if (-not ($docs | Where-Object { $_.rel -eq $key })) {
        $failures += ("A1 白名单条目已不存在（漂移，请清理）：{0}。" -f $key)
    }
}

# <lang><zh-CN>A2：皮肤数 = 主题目录排除 ThemeProbe（探针主题不是皮肤）。</zh-CN>
# <en>A2: skin count = theme directories excluding ThemeProbe (a probe theme is not a skin).</en></lang>
$skinCount = @(Get-ChildItem (Join-Path $repoRoot 'src\Portal\App_Themes') -Directory | Where-Object { $_.Name -ne 'ThemeProbe' }).Count

# <lang><zh-CN>A3：UI 语言数 = lang.*.resx 语言资源数。</zh-CN>
# <en>A3: UI languages = the number of lang.*.resx locale resources.</en></lang>
$localeCount = @(Get-ChildItem (Join-Path $repoRoot 'src\Portal\App_GlobalResources') -File -Filter 'lang.*.resx').Count

# <lang><zh-CN>A4：版本 = docs/versioning.md「当前版本」节声明的版本。</zh-CN>
# <en>A4: version = the version declared in the "current version" section of docs/versioning.md.</en></lang>
$versioningText = [System.IO.File]::ReadAllText((Join-Path $repoRoot 'docs\versioning.md'))
$version = $null
if ($versioningText -match '当前版本[\s\S]{0,200}?`\*{0,2}(v\d+\.\d+\.\d+)\*{0,2}`') { $version = $Matches[1] }

# <lang><zh-CN>A5：许可证 = 根 LICENSE 可判定时取用（无法判定则不硬断言）。</zh-CN>
# <en>A5: licence = taken from the root LICENSE when decidable (not asserted when undecidable).</en></lang>
$licence = $null
$licensePath = Join-Path $repoRoot 'LICENSE'
if (Test-Path -LiteralPath $licensePath) {
    $licenseHead = (Get-Content -LiteralPath $licensePath -TotalCount 5) -join "`n"
    if ($licenseHead -match 'MIT License') { $licence = 'MIT' }
}

# ---------------------------------------------------------------------------
# 核对：landing 声明值 vs 真值
# ---------------------------------------------------------------------------
# <lang><zh-CN>注意：`$failures` 在 A1 之前已声明（A1 的映射违规也登记在其中），此处**不可**再重置。</zh-CN>
# <en>Note: `$failures` is declared before A1 (A1 mapping violations register there too); it must **not** be reset here.</en></lang>
$zhText = [System.IO.File]::ReadAllText((Join-Path $repoRoot 'site\index.md'))
$enText = [System.IO.File]::ReadAllText((Join-Path $repoRoot 'site\en\index.md'))

# <lang><zh-CN>标签 → 期望真值（default-deny：出现在本表之外的统计块即未识别 → Fail）。</zh-CN>
# <en>Label → expected truth (default-deny: any statistic block outside this table is unrecognised → Fail).</en></lang>
$expectedByLabel = @{
    '篇文档' = [string]$publishedDocs.Count
    'Documents' = [string]$publishedDocs.Count
    '套正式皮肤' = [string]$skinCount
    'Official skins' = [string]$skinCount
    '中 / 英文资源' = '双语'
    'UI languages' = [string]$localeCount
    '当前版本' = $version
    'Current version' = $version
    '开源许可' = $licence
    'Licence' = $licence
}
$recognized = 0
foreach ($pair in @(@('site/index.md', $zhText), @('site/en/index.md', $enText))) {
    $file = $pair[0]; $text = $pair[1]
    foreach ($m in [regex]::Matches($text, 'oc-stat-val">([^<]+)<[\s\S]{0,160}?oc-stat-label">([^<]+)<')) {
        $declared = $m.Groups[1].Value.Trim()
        $label = $m.Groups[2].Value.Trim()
        if (-not $expectedByLabel.ContainsKey($label)) {
            $failures += ("M0 [{0}] 未识别的统计项「{1}」声明值 {2}（新增统计数字必须先在本门禁登记取数口径）" -f $file, $label, $declared)
            continue
        }
        $expected = $expectedByLabel[$label]
        # <lang><zh-CN>无法判定的项（如许可证不可判定）不硬断言，仅上报。</zh-CN>
        # <en>Undecidable items (e.g. an unrecognised licence) are not asserted, only reported.</en></lang>
        if ([string]::IsNullOrWhiteSpace($expected)) { continue }
        $recognized++
        if ($declared -ne $expected) {
            $failures += ("A [{0}] 「{1}」声明 {2} ≠ 真值 {3}（landing 数字已漂移，须按真值更新）" -f $file, $label, $declared, $expected)
        }
    }
}

# ---------------------------------------------------------------------------
# 统计落盘 + 输出
# ---------------------------------------------------------------------------
if ([string]::IsNullOrWhiteSpace($StatsPath)) {
    $StatsPath = Join-Path (Join-Path $repoRoot 'temp') 'landing-stats\landing-stats.json'
}
$statsDir = Split-Path -Parent $StatsPath
if (-not (Test-Path -LiteralPath $statsDir)) { New-Item -ItemType Directory -Force -Path $statsDir | Out-Null }
$report = [pscustomobject]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    truth = [pscustomobject]@{
        publishedDocs = $publishedDocs.Count
        totalDocs = @($publishedDocs).Count + @($unpublishedDocs).Count
        skins = $skinCount
        uiLanguages = $localeCount
        version = $version
        licence = $licence
    }
    unpublishedDocs = $unpublishedDocs
    recognizedStats = $recognized
    failures = $failures
}
[System.IO.File]::WriteAllText($StatsPath, ($report | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))

Write-Host ''
Write-Host '=== landing 统计数字（真值 vs 声明） ==='
Write-Host ("真值：已上架文档 {0}（docs 总数 {1}）｜皮肤 {2}｜UI 语言 {3}｜版本 {4}｜许可 {5}" -f $publishedDocs.Count, (@($publishedDocs).Count + @($unpublishedDocs).Count), $skinCount, $localeCount, $version, $licence)
Write-Host ("未上架文档 {0} 篇（软指标）：{1}" -f @($unpublishedDocs).Count, ($unpublishedDocs -join ', '))
Write-Host ("已核对统计项 {0} 个｜统计已写入：$StatsPath" -f $recognized)

if ($failures.Count -gt 0) {
    Write-Host ''
    Write-Host ("[FAIL] landing 统计校验失败 {0} 处：" -f $failures.Count)
    foreach ($f in $failures) { Write-Host ("  - " + $f) }
    exit 1
}

Write-Host ''
Write-Host '[PASS] landing 统计数字与真值一致；无未识别统计项。'
exit 0
