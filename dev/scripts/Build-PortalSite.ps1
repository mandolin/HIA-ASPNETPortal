<#
.SYNOPSIS
  <lang>
    <zh-CN>由仓库 `docs/` + 英文译本生成 `site/` 站点页面（**同源生成，不手工复制**），并校验双语一致性与导航完整性。</zh-CN>
    <en>Generates `site/` pages from the repository's `docs/` plus English renders (**generated from one source, never hand-copied**), and verifies bilingual parity and navigation integrity.</en>
  </lang>
.DESCRIPTION
  <lang>
    <zh-CN>**要解决的问题**：官网若手工复制 `docs/` 的内容，两份文本会立刻开始漂移。原则4 要求官网内容源自 P21
    公开文档、**不另起一套脱离代码的文档**，故本脚本是**站点页面的唯一来源**：中文取自 `docs/`（权威源），
    英文取自 `site/_translations/`（译本）。**不要手工编辑生成的页面**。

    **双语并行（本周期硬约束）**：站点是两棵一一对应的树 —— 中文位于根、英文位于 `/en/`。
    强制点在流程而非口号：**一个页面只有在中英两版都就绪时，才允许被声明上架**；
    被声明却缺译 → 门禁 Fail。

    **路径单一来源（`W-anp-P132` 的教训）**：页面路径**只**由 `_data/published.yml` 的 `path` 决定，
    生成时注入 `permalink`。初版没有 path，脚本用文档文件名、而导航与首页写"理想路径" ——
    两套体系不一致导致**导航与首页链接全部指向 404**且门禁未发现。现在还有两项校验防复发：
    - **N1**：每个上架页面的 `parent` 必须在 `_data/navigation.yml` 中存在；
    - **N2**：`site/` 下（不含生成前的源码文件）Markdown 中的站内链接，目标页面必须存在。

    **分批上架，拒绝"一上线就红"**：只校验 `_data/published.yml` 中**已声明**的页面；
    未声明的计入"待译"信息，**不计 Fail** —— 否则门禁在翻译完成前长期红灯，
    正是 P20 closeout 批评的"门禁长期红着 → 被当成背景噪音 → 等于没写"。

    **前置条件**：只需源码与文档可读，属编排入口 L0 层；不装 gem、不构建 Jekyll、不连数据库。
    **验证方式**：退出码 0 = 已声明上架的页面双语齐备、parent 有效、站内链接无坏链；1 = 存在缺口。
    **低敏边界**：只读取 `docs/` 与 `site/_translations/` 的文本；不读取凭据、连接串或业务数据。

    **证明边界**：本脚本只保证"双语一一对应、parent 有效、站内链接可达"；
    **不保证**译本语义与原文等价 —— 译本质量仍需人工走查（见 `docs/documentation-review-checklist.md`）。</zh-CN>
    <en>**Problem addressed**: if the site hand-copies `docs/`, the two texts start drifting immediately. Principle 4 requires
    site content to come from P21's public docs and **not to create a separate, code-detached documentation set**, so this
    script is the **single source of site pages**: Chinese from `docs/` (authoritative), English from `site/_translations/`.
    **Do not hand-edit generated pages.**

    **Bilingual parity (hard constraint of this cycle)**: the site is two one-to-one trees — Chinese at the root, English
    under `/en/`. Enforcement is procedural: **a page may be declared published only when both its Chinese and English
    versions are ready**; declaring it without a translation fails the gate.

    **Single source for paths (lesson from `W-anp-P132`)**: a page path is determined **only** by `path` in
    `_data/published.yml`, injected as `permalink`. The first version had no path — the script used the document file name
    while navigation and the home page used "ideal" paths — the two incompatible systems made **every navigation and
    home-page link resolve to 404** with no gate catching it. Two checks now prevent regression:
    - **N1**: every published page's `parent` must exist in `_data/navigation.yml`;
    - **N2**: in-site links in Markdown under `site/` (excluding pre-generation source files) must resolve to an existing page.

    **Batch publishing, not "red from day one"**: only pages *declared* in `_data/published.yml` are validated; undeclared
    pages count as "pending translation" and do **not** fail — otherwise the gate would stay red for the whole translation
    period, the very "always-red gate → background noise → effectively absent" problem from the P20 closeout.

    **Prerequisites**: readable source and docs only — an L0-layer entry point; no gem install, no Jekyll build, no database.
    **Verification**: exit code 0 = declared-published pages have both language versions, a valid `parent`, and no broken
    in-site links; 1 = gaps exist.
    **Low-sensitivity boundary**: reads only text from `docs/` and `site/_translations/` — no credentials, connection strings
    or business data.

    **Proof boundary**: guarantees only "one-to-one parity, valid parent, reachable in-site links" — it does **not**
    guarantee a translation is semantically equivalent to its source; translation quality still needs a human walkthrough (see
    `docs/documentation-review-checklist.md`).</en>
  </lang>
.PARAMETER RepoRoot
  <lang>
    <zh-CN>仓库根目录；留空时取脚本所在目录的上两级目录（`dev\scripts` → 仓库根）。</zh-CN>
    <en>Repository root; when empty, two levels above the script directory (`dev\scripts` -> repository root).</en>
  </lang>
#>
param(
    [string] $RepoRoot
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}

$utf8 = [System.Text.UTF8Encoding]::new($false)
$docsDir = Join-Path $RepoRoot 'docs'
$siteDir = Join-Path $RepoRoot 'site'
$translationsDir = Join-Path $siteDir '_translations'
$dataDir = Join-Path $siteDir '_data'

if (-not (Test-Path -LiteralPath $docsDir)) { Write-Host 'ERROR: docs/ 不存在'; exit 1 }
if (-not (Test-Path -LiteralPath $siteDir)) { Write-Host 'ERROR: site/ 不存在'; exit 1 }

$gaps = New-Object 'System.Collections.Generic.List[string]'
$generated = 0
$pending = 0

# --- 解析 _data/published.yml（结构化：doc / path / parent） ---
$publishedPath = Join-Path $dataDir 'published.yml'
$entries = New-Object 'System.Collections.Generic.List[object]'
if (Test-Path -LiteralPath $publishedPath) {
    $current = $null
    foreach ($line in ([System.IO.File]::ReadAllText($publishedPath, $utf8) -split "`r?`n")) {
        $t = $line.Trim()
        if ($t -eq '' -or $t.StartsWith('#')) { continue }
        if ($t.StartsWith('- ')) {
            $current = @{ doc = ''; path = ''; parent = '' }
            $entries.Add($current)
            $t = $t.Substring(2).Trim()
        }
        if ($null -ne $current -and $t -match '^([A-Za-z_]+):\s*(.*)$') {
            $current[$Matches[1].Trim()] = $Matches[2].Trim()
        }
    }
}

# --- 解析 _data/navigation.yml（分区标题，供 N1 校验） ---
# <lang>
#   <zh-CN>分区标题的实际形态是 `- title: 产品概览`（带 `- ` 前缀），故正则须允许可选的前导 `- `；
#     同时用 `(?:-\s*)?title:` 而非 `title:`，以免误匹配 `title_en:`。
#     **解析结果为空时直接记为缺口** —— 初版在解析为空时静默跳过 N1，使该检查形同虚设
#     （负向测试插入无效 parent 竟仍通过）。解析失败必须暴露，不能让门禁悄悄失效。</zh-CN>
# </lang>
$navigationPath = Join-Path $dataDir 'navigation.yml'
$navTitles = New-Object 'System.Collections.Generic.List[string]'
if (Test-Path -LiteralPath $navigationPath) {
    foreach ($line in ([System.IO.File]::ReadAllText($navigationPath, $utf8) -split "`r?`n")) {
        if ($line.Trim() -match '^(?:-\s*)?title:\s*(.+)$') { $navTitles.Add($Matches[1].Trim()) }
    }
}
else {
    $gaps.Add('缺少 _data/navigation.yml，无法校验 parent')
}
if ((Test-Path -LiteralPath $navigationPath) -and $navTitles.Count -eq 0) {
    $gaps.Add('_data/navigation.yml 未解析出任何分区标题（正则或格式变更？）—— N1 校验无法进行')
}

# <lang>
#   <zh-CN>遍历已声明上架的页面：中文取自 `docs/`，英文取自 `site/_translations/`；
#     缺任一即登记为缺口并 Fail。路径由 `path` 唯一决定，并注入 front matter（含 permalink 与 parent）。</zh-CN>
# </lang>
foreach ($e in $entries) {
    if ([string]::IsNullOrWhiteSpace($e.doc)) { $gaps.Add('上架清单存在缺 doc 的条目'); continue }

    $zhPath = Join-Path $docsDir $e.doc
    $enSource = Join-Path $translationsDir $e.doc

    if (-not (Test-Path -LiteralPath $zhPath)) {
        $gaps.Add(('已声明上架但中文源缺失: docs/' + $e.doc))
        continue
    }
    if (-not (Test-Path -LiteralPath $enSource)) {
        $gaps.Add(('已声明上架但缺英文译本: docs/' + $e.doc + ' -> site/_translations/' + $e.doc))
        $pending++
        continue
    }

    # N1：parent 必须存在于导航骨架
    if ($navTitles.Count -gt 0 -and -not $navTitles.Contains($e.parent)) {
        $gaps.Add(('N1 parent 无效: ' + $e.doc + ' 的 parent="' + $e.parent + '" 不在 _data/navigation.yml 中'))
        continue
    }

    $pathSeg = if ([string]::IsNullOrWhiteSpace($e.path)) { [System.IO.Path]::GetFileNameWithoutExtension($e.doc) } else { $e.path }
    $zhTitle = if ($e.parent) { $e.parent } else { $pathSeg }

    $targetDir = Join-Path $siteDir $pathSeg
    if (-not (Test-Path -LiteralPath $targetDir)) { $null = New-Item -ItemType Directory -Path $targetDir }
    $zhBody = [System.IO.File]::ReadAllText($zhPath, $utf8)
    $zhFm = "---`nlayout: default`nlang: zh-CN`nparent: " + $e.parent + "`npermalink: /" + $pathSeg + "/`ntitle: " + $zhTitle + "`n---`n"
    [System.IO.File]::WriteAllText((Join-Path $targetDir 'index.md'), ($zhFm + $zhBody), $utf8)

    $enDir = Join-Path $targetDir 'en'
    if (-not (Test-Path -LiteralPath $enDir)) { $null = New-Item -ItemType Directory -Path $enDir }
    $enBody = [System.IO.File]::ReadAllText($enSource, $utf8)
    $enTitle = $e.parent
    $enFm = "---`nlayout: default`nlang: en`nparent: " + $e.parent + "`npermalink: /en/" + $pathSeg + "/`ntitle: " + $enTitle + "`n---`n"
    [System.IO.File]::WriteAllText((Join-Path $enDir 'index.md'), ($enFm + $enBody), $utf8)

    $generated++
}

# <lang>
#   <zh-CN>N2：站点页面中的站内链接必须可达 —— 这是 `W-anp-P132` 为"导航/首页链接全部 404 而门禁未发现"
#     补的检查。注意只检查 `site/**/index.md`（生成产物与手写页面），_config.yml 等源码不在其列。</zh-CN>
# </lang>
$pageFiles = @(Get-ChildItem -Path $siteDir -Recurse -Filter 'index.md' -File -ErrorAction SilentlyContinue)
foreach ($pf in $pageFiles) {
    $rel = $pf.FullName.Substring($siteDir.Length).Replace('\', '/')
    $text = [System.IO.File]::ReadAllText($pf.FullName, $utf8)
    foreach ($m in [regex]::Matches($text, '\]\((/[A-Za-z0-9/_-]*/?)\)')) {
        $target = $m.Groups[1].Value
        if ($target -eq '/' -or $target -eq '/en/') { continue }
        $seg = $target.Trim('/')
        if ($seg.StartsWith('en/')) { $seg = $seg.Substring(3) }
        $exists = Test-Path -LiteralPath (Join-Path $siteDir ($seg + '\index.md'))
        if (-not $exists) {
            $gaps.Add(('N2 站内坏链: site' + $rel + ' -> ' + $target))
        }
    }
}

if ($gaps.Count -gt 0) {
    Write-Host ('站点构建失败（共 ' + $gaps.Count + ' 处缺口）：')
    foreach ($g in $gaps) { Write-Host ('  - ' + $g) }
    Write-Host ''
    Write-Host 'RESULT: Fail'
    exit 1
}

Write-Host ('站点生成完成：已上架页面 ' + $generated + ' 个（中/英各一份）。检查页面 ' + $pageFiles.Count + ' 个：站内链接无坏链、parent 均有效。')
Write-Host '[GATE-NOTE] 双语并行：已上架页面必须中英两版齐备，缺译即 Fail（机制保证，非靠自觉）。'
Write-Host '[GATE-NOTE] 路径单一来源：页面路径只由 _data/published.yml 的 path 决定（W132 教训：曾因两套路径体系导致导航/首页链接全部 404）。'
Write-Host '[GATE-NOTE] 分批上架：只校验已声明页面，未声明的计入待译、不阻断（拒绝门禁一上线就长期红灯）。'
Write-Host 'RESULT: Pass'
exit 0
