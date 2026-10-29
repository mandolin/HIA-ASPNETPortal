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
.PARAMETER DryRun
<lang>
  <zh-CN>**dry-run：只报告将生成什么，不落盘**（`W-anp-P191`，解备忘 D4）。`site/` 一个字节都不改——包括**目录创建**也受同一开关控制（否则 dry-run 会留下空目录，那仍然是落盘）。报告区分三类：新增 / 内容变更 / 无变化。dry-run 在**生成循环之后、N1/N2 校验之前**显式收口退出：那些校验读的是**当前**站点内容，在 dry-run 下运行会把当前站点的状态误呈现成本次构建的结论（假绿灯）。**同源原则**：内容计算路径完全不变，只有最后一步"写不写"分叉——不引入第二套生成逻辑。</zh-CN>
  <en>**dry-run: report what would be generated without writing anything** (`W-anp-P191`, resolves memo D4). Not one byte of `site/` changes — **directory creation** is governed by the same switch (otherwise dry-run would leave empty directories behind, which is still a write). The report distinguishes three states: new / content changed / unchanged. Dry-run **exits explicitly after the generation loop and before the N1/N2 checks**: those checks read the **current** site content, so running them under dry-run would present the current site's state as this build's conclusion (a false green light). **Same-source principle**: the content computation path is unchanged and only the final "write or not" step branches — no second generation logic is introduced.</en>
</lang>
.PARAMETER OutputRoot
<lang>
  <zh-CN>**输出根目录重定向**（`W-anp-P192`，解备忘 D10 的底座）。留空 = 写入真实 `site/`（默认行为，一字不变）；给定目录 = 把生成结果写到那里，**输入仍读真实 `site/`**（`_data/published.yml`、`_translations/` 不随输出走）。用于**镜像新鲜度门禁**：它把构建输出导到临时目录，再与仓库内 `site/` 逐字节比对——"合法变换"由构建自身定义，门禁不需要复述哪些变换被允许。该模式下**跳过 N1/N2 与双语成对校验**（它们读真实站点内容，对临时目录运行会把手写页面误判为缺失）。</zh-CN>
  <en>**Output root redirection** (`W-anp-P192`, the foundation for resolving memo D10). Empty = write to the real `site/` (default behaviour, unchanged); a given directory = write the generated result there while **inputs still come from the real `site/`** (`_data/published.yml` and `_translations/` do not follow the output). Used by the **mirror freshness gate**: it directs the build output into a temp directory and compares it byte-for-byte with the repository's `site/` — the "legitimate transformations" are defined by the build itself, so the gate need not restate which transformations are allowed. In this mode the **N1/N2 and bilingual-pairing checks are skipped** (they read the real site content and would misreport hand-written pages as missing against a temp directory).</en>
</lang>
#>
param(
    [string] $RepoRoot,
    [switch] $DryRun,
    [string] $OutputRoot
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}

$utf8 = [System.Text.UTF8Encoding]::new($false)
$docsDir = Join-Path $RepoRoot 'docs'
$siteDir = Join-Path $RepoRoot 'site'
# <lang>
#   <zh-CN>**输入与输出分离**：`$siteDir` 始终是**真实**站点目录（`_data`、`_translations` 从它读）；
#     `$outDir` 是生成产物去向——留空时等于 `$siteDir`（默认行为不变），给了 `-OutputRoot` 时指向该目录。
#     这是镜像新鲜度门禁的底座：门禁把输出导到临时目录，再与仓库内 `site/` 比对。</zh-CN>
#   <en>**Inputs and outputs separated**: `$siteDir` is always the **real** site directory (`_data` and `_translations` are read
#     from it); `$outDir` is where generated output goes — equal to `$siteDir` when empty (default behaviour unchanged), or the
#     given directory when `-OutputRoot` is supplied. This is the foundation of the mirror freshness gate: the gate directs
#     output into a temp directory and compares it with the repository's `site/`.</en>
# </lang>
$outDir = $siteDir
if (-not [string]::IsNullOrWhiteSpace($OutputRoot)) {
    $outDir = [System.IO.Path]::GetFullPath($OutputRoot)
    if (-not (Test-Path -LiteralPath $outDir)) { $null = New-Item -ItemType Directory -Path $outDir -Force }
}
$translationsDir = Join-Path $siteDir '_translations'
$dataDir = Join-Path $siteDir '_data'

if (-not (Test-Path -LiteralPath $docsDir)) { Write-Host 'ERROR: docs/ 不存在'; exit 1 }
if (-not (Test-Path -LiteralPath $siteDir)) { Write-Host 'ERROR: site/ 不存在'; exit 1 }

$gaps = New-Object 'System.Collections.Generic.List[string]'
$generated = 0
$pending = 0

# <lang>
#   <zh-CN>dry-run 计数器与**唯一写入入口**。同源原则落在这里：页面内容照原路径计算（一个字节都不改），
#     只有最后一步"写不写"分叉；目录创建也由同一开关控制（否则 dry-run 会留下空目录，那仍然是落盘）。</zh-CN>
#   <en>Dry-run counters and the **single write entry point**. The same-source principle lands here: page content is computed
#     along the original path (not one byte changed) and only the final "write or not" step branches; directory creation is
#     governed by the same switch (otherwise dry-run would leave empty directories behind, which is still a write).</en>
# </lang>
$dryNew = 0
$dryChanged = 0
$drySame = 0
$dryPlan = New-Object 'System.Collections.Generic.List[string]'

function Write-GeneratedPage {
    param([string] $Path, [string] $Content)
    $existing = $null
    if (Test-Path -LiteralPath $Path -PathType Leaf) { $existing = [System.IO.File]::ReadAllText($Path, $utf8) }
    if ($DryRun) {
        if ($null -eq $existing) { $script:dryNew++; $state = '新增' }
        elseif ($existing -eq $Content) { $script:drySame++; $state = '无变化' }
        else { $script:dryChanged++; $state = '内容变更' }
        $script:dryPlan.Add(($state + ' ' + $Path))
        return
    }
    [System.IO.File]::WriteAllText($Path, $Content, $utf8)
}

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

# <lang>
#   <zh-CN>**分区内的页面顺序也由上架清单决定**（`nav_order`）：同一分区的条目在清单里出现的先后，
#     就是它们在导航里的先后，并在生成时注入 front matter。
#     此前生成页只有 layout/lang/parent/permalink/title，**没有 nav_order**，导航只能落到主题默认序
#     （实测为标题 Unicode 码位序）——「参考」分区 32 页于是排成 "HTML → XML → 业 → 主 → 事 → 企 → 公 → 协 →
#     员 → 图 → 字 → 官" 的一长串：既不是上架顺序，也没有分组，中英文两棵树的顺序还不一致。
#     清单既然是路径与分区的单一事实源，**顺序同样应当只由它决定**；要调整顺序就调整清单里的位置。
#     编号在**解析阶段**一次算完（每个分区从 1 起），使编号只取决于清单，
#     而不取决于某一次构建里哪些页面恰好生成成功。</zh-CN>
#   <en>**Ordering within a section is decided by the publish manifest as well** (`nav_order`): the order in which a
#     section's entries appear in the manifest is their navigation order, injected into the generated front matter.
#     Generated pages previously carried only layout/lang/parent/permalink/title and **no nav_order**, so navigation fell
#     back to the theme's default order (observed: Unicode code-point order by title) — the Reference section's 32 pages
#     lined up as "HTML → XML → 业 → 主 → 事 → 企 → 公 → 协 → 员 → 图 → 字 → 官": neither the manifest order nor grouped,
#     and the two language trees disagreed. The manifest is already the single source for paths and sections, so the
#     **order must be decided by it alone**; to change the order, change the position in the manifest. The numbering is
#     computed **once during parsing** (per section, from 1), so it depends only on the manifest and never on which pages
#     happened to be generated in a particular run.</en>
# </lang>
$sectionOrder = @{}
foreach ($entry in $entries) {
    $sectionKey = if ([string]::IsNullOrWhiteSpace($entry.parent)) { '__root__' } else { [string]$entry.parent }
    if (-not $sectionOrder.ContainsKey($sectionKey)) { $sectionOrder[$sectionKey] = 0 }
    $sectionOrder[$sectionKey] = $sectionOrder[$sectionKey] + 1
    $entry['nav_order'] = $sectionOrder[$sectionKey]
}

# --- 解析 _data/navigation.yml（分区标题，供 N1 校验） ---
# <lang>
#   <zh-CN>分区标题的实际形态是 `- title: 产品概览`（带 `- ` 前缀），故正则须允许可选的前导 `- `；
#     同时用 `(?:-\s*)?title:` 而非 `title:`，以免误匹配 `title_en:`。
#     **解析结果为空时直接记为缺口** —— 初版在解析为空时静默跳过 N1，使该检查形同虚设
#     （负向测试插入无效 parent 竟仍通过）。解析失败必须暴露，不能让门禁悄悄失效。</zh-CN>
# </lang>
$navigationPath = Join-Path $dataDir 'navigation.yml'
# <lang>
#   <zh-CN>分区条目同时收集中文与英文标题（`W-anp-P148`）。初版只收中文 `title`，导致
#     `W-anp-P148` 要给英文页面注入英文分区名时无从取数。`Zh` 供 N1 校验与中文页 parent 使用，
#     `En` 供英文页 parent 使用。</zh-CN>
# </lang>
$navTitles = New-Object 'System.Collections.Generic.List[object]'
if (Test-Path -LiteralPath $navigationPath) {
    $cur = $null
    foreach ($line in ([System.IO.File]::ReadAllText($navigationPath, $utf8) -split "`r?`n")) {
        $t = $line.Trim()
        if ($t -match '^(?:-\s*)?title:\s*(.+)$') {
            if ($cur) { $navTitles.Add($cur) }
            $cur = [pscustomobject]@{ Zh = $Matches[1].Trim(); En = $null }
        }
        elseif ($cur -and $t -match '^title_en:\s*(.+)$') {
            $cur.En = $Matches[1].Trim()
        }
    }
    if ($cur) { $navTitles.Add($cur) }
}
else {
    $gaps.Add('缺少 _data/navigation.yml，无法校验 parent')
}
if ((Test-Path -LiteralPath $navigationPath) -and $navTitles.Count -eq 0) {
    $gaps.Add('_data/navigation.yml 未解析出任何分区标题（正则或格式变更？）—— N1 校验无法进行')
}
$navTitleZhList = @($navTitles | ForEach-Object { $_.Zh })

# <lang>
#   <zh-CN>遍历已声明上架的页面：中文取自 `docs/`，英文取自 `site/_translations/`；
#     缺任一即登记为缺口并 Fail。路径由 `path` 唯一决定，并注入 front matter（含 permalink 与 parent）。</zh-CN>
# </lang>
# <lang>
#   <zh-CN>**把 `docs/` 里的相对 Markdown 链接改写成站点可用形态**（`W-anp-P144`）。
#     站点把页面拍平为 `site/<path>/index.md`，而 `docs/` 是嵌套目录，所以源码里正确的相对链接
#     在站点上大多 404。按当前文档所在目录解析后分三种情况处理（详见调用处的说明）。
#     注意：**源文件不被改动**，改写只发生在生成侧。</zh-CN>
#   <en>**Rewrite relative Markdown links from `docs/` into a form the site can use** (`W-anp-P144`).
#     The site flattens pages to `site/<path>/index.md` while `docs/` is nested, so links that are correct in the
#     source mostly 404 on the site. Resolve against the current document's directory, then handle three cases
#     (see the call site for details). Note: **source files are never modified** — rewriting happens only on the
#     generation side.</en>
# </lang>
function Convert-DocLink {
    param(
        [string] $Body,
        [string] $CurDoc
    )
    $docsRoot = (Join-Path $RepoRoot 'docs')
    return [regex]::Replace($Body, '\]\(([^)]+)\)', {
        param($m)
        $raw = $m.Groups[1].Value.Trim()
        # 拆出锚点：链接可能是 `doc.md#anchor` 形态
        $hash = $raw.IndexOf('#')
        $link = if ($hash -ge 0) { $raw.Substring(0, $hash) } else { $raw }
        $anchor = if ($hash -ge 0) { $raw.Substring($hash) } else { '' }
        # 外链、站内已带 baseurl 的、纯锚点、绝对路径：不处理
        if ($link -match '^(https?:|mailto:|#|//)' -or $link -match '^\{\{' -or $link -match '^/') { return $m.Value }
        if ($link -eq '') { return $m.Value }
        $curDir = Split-Path $CurDoc -Parent
        $combined = if ($curDir) { Join-Path $curDir $link } else { $link }
        try { $full = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($docsRoot, $combined)) }
        catch { return $m.Value }
        if ($full.StartsWith($docsRoot + [System.IO.Path]::DirectorySeparatorChar)) {
            $relDoc = $full.Substring($docsRoot.Length + 1).Replace('\', '/')
            $mapped = $null
            foreach ($e in $entries) { if ($e.doc -eq $relDoc -and $e.path) { $mapped = $e.path; break } }
            if ($mapped) { return ']({{ site.baseurl }}/' + $mapped + '/' + $anchor + ')' }
            return $m.Value
        }
        # 指向 docs/ 之外（仓库根）→ GitHub 绝对地址
        $repoRel = $full.Substring($RepoRoot.Length + 1).Replace('\', '/')
        return '](https://github.com/mandolin/HIA-ASPNETPortal/blob/main/' + $repoRel + $anchor + ')'
    })
}

# <lang>
#   <zh-CN>**发布面文本过滤（`W-anp-P145`）**：官网是给**外部访客**（使用者、评估者）看的，
#     而 `docs/` 的源文本带有内部协作色彩 —— 工作项编号（`W-anp-P71`、`C-anp-P20`）、
#     "（内部计划，未公开）"、批次上架状态、构建脚本名、"原则4" 之类。
#     这些对内部协作者是必要的溯源信息，**对外部读者只是噪音**，还会让官网显得像内部协作平台。
#     此前（`W-anp-P133`）已把"是否过滤"登记为待办并**刻意未处置**，现在由用户明确要求去掉。
#
#     **处置原则（避免误删）**：
#       - 删"元信息"：工作项编号、"（内部计划，未公开）"、构建流程说明；
#       - 留"内容"：`docs/` 里讲产品本身怎么用的文字（命令示例、契约说明、权威源指向）。
#     所以过滤是**外科手术式**的——逐条列出要删的形态并保留其余文本，不是"见编号就删整行"。
#     源文件保持内部友好（不被改动），适配只发生在生成侧，与 `Convert-DocLink` 同一思路。
#
#     注意：`site/index.md` 等**手写页面**不经本函数（它们不走生成流程），其内部文字直接在手写源里改。</zh-CN>
#   <en>**Publication-surface text filtering (`W-anp-P145`)**: the website is for **external visitors** (users, evaluators),
#     while the `docs/` source text carries internal-collaboration colour — work-item IDs (`W-anp-P71`, `C-anp-P20`),
#     "(internal planning, not public)", batch-publishing status, build script names, "Principle 4" and the like.
#     These are necessary provenance for internal collaborators but are **noise to external readers**, and they make the
#     site look like an internal collaboration platform. Whether to filter was registered as a to-do back in `W-anp-P133`
#     and was **deliberately left undone**; the user has now explicitly asked for it to be removed.
#
#     **Principle (avoid over-deletion)**:
#       - remove "meta-information": work-item IDs, "(internal planning, not public)", build-process notes;
#       - keep "content": text in `docs/` about how to use the product itself (command examples, contract notes,
#         authoritative-source pointers).
#     The filtering is therefore **surgical** — each form to remove is listed explicitly and the rest of the text is kept,
#     not "delete the whole line when an ID appears".
#     Source files stay internally friendly (untouched); adaptation happens only on the generation side, the same idea as
#     `Convert-DocLink`.
#
#     Note: hand-written pages such as `site/index.md` do not go through this function (they bypass generation); their
#     internal wording is fixed directly in the hand-written source.</en>
# </lang>
function Convert-PublicText {
    param([string] $Body)
    $b = $Body
    # <lang>
    #   <zh-CN>**顺序很关键**：先删"含编号的完整括号组/标题后缀"，再删散落编号。
    #     若反过来（先删散号），组内剩下的文字会变成「（ ， 据源码订正）」这类残迹 —— 比不过滤更糟。
    #     这是本函数第一次实现时踩的坑：编号删干净了，却留出一堆空括号和双空格。</zh-CN>
    # </lang>
    # ① 含工作项编号的完整括号组 → 整组删除（中文/英文括号都要覆盖）
    $b = [regex]::Replace($b, '\s*[（(][^）)]*`(?:C|W)-anp-P[\d.]+`[^）)]*[）)]', '')
    # ② 标题行的出处后缀
    $b = [regex]::Replace($b, '(?m)(^#{1,6}\s+[^\r\n]*?)（[^）\r\n]*(?:C|W)-anp-P[\d.]+[^）\r\n]*）', '$1')
    $b = [regex]::Replace($b, '(?m)(^#{1,6}\s+[^\r\n]*?)\s*\([^)\r\n]*(?:C|W)-anp-P[\d.]+[^)\r\n]*\)', '$1')
    # ③ 出处前缀整句
    $b = $b -replace '\*\*A\s+P21\s+deliverable\.\*\*\s*', ''
    $b = $b -replace '\*\*P21\s+交付物\*\*。\s*', ''
    $b = $b -replace '\*\*P21\s*`W-anp-P[\d.]+`\s*交付物\*\*。\s*', ''
    $b = $b -replace '\*\*P21\s*`W-anp-P[\d.]+`\s*交付物\*\*\.\s*', ''
    # 整段删除：出处/阶段说明里的"（内部计划，未公开）"
    $b = $b -replace '（内部计划，未公开）', ''
    $b = $b -replace '\(internal planning, not public\)', ''
    # 整段删除：「归属：`W-anp-P71` 状态机显式化。」这类溯源前缀
    $b = $b -replace '归属：`W-anp-P[\d.]+`\s*状态机显式化。\s*', ''
    $b = $b -replace 'Source: `W-anp-P[\d.]+` state-machine explicitisation\.\s*', ''
    # 整段删除：「阶段：`W-anp-P62` `P62.4`」
    $b = $b -replace '阶段：`W-anp-P[\d.]+`\s*`P[\d.]+`\s*', ''
    $b = $b -replace 'Phase: `W-anp-P[\d.]+`\s*`P[\d.]+`\s*', ''
    # 门禁编号 → 通用说法（保留"有自动化门禁守护"这个有用信息）
    $b = $b -replace '`C-anp-P18`\s*/\s*`W-anp-P100`', '自动化门禁'
    $b = $b -replace '`C-anp-P18`\s*/\s*`W-anp-P99`', '主题契约门禁'
    $b = $b -replace 'C-anp-P18\s*/\s*W-anp-P100', 'the automated gates'
    $b = $b -replace 'C-anp-P18\s*/\s*W-anp-P99', 'the theme-contract gates'
    $b = $b -replace '`C-anp-P23`\s*D3\s*裁定新增', '主题工程裁定新增'
    $b = $b -replace 'added in C-anp-P23', 'added by the theme engineering decision'
    # 正文中的裸工作项编号：删编号、保留描述
    $b = $b -replace '`W-anp-P[\d.]+`\s*的\s*', ''
    $b = $b -replace '`C-anp-P\d+(?:\.\d+)?`\s*交付', ''
    $b = $b -replace '由\s*`C-anp-P\d+(?:\.\d+)?`\s*交付', ''
    $b = $b -replace '`W-anp-P\d+(?:\.\d+)?`\s*产出', ''
    $b = $b -replace 'by\s*`C-anp-P\d+(?:\.\d+)?`', ''
    $b = $b -replace '`W-anp-P\d+(?:\.\d+)?`\s*已把', ''
    # 括号内的交付方标注：删整个括号组
    $b = $b -replace '\s*[（(]\s*`W-anp-P[\d.]+`\s*交付\s*[）)]', ''
    $b = $b -replace '\s*[（(]\s*`W-anp-P[\d.]+`\s*补强\s*[）)]', ''
    $b = $b -replace '\s*[（(]\s*a\s*`W-anp-P[\d.]+`\s*deliverable\s*[）)]', ''
    $b = $b -replace '\s*[（(]\s*strengthened by\s*`W-anp-P[\d.]+`\s*[）)]', ''
    $b = $b -replace '\s*[（(]\s*`W-anp-P[\d.]+`\s*已建\s*[）)]', ''
    $b = $b -replace '\s*[（(]\s*`docs/contracts/`\s*built by\s*`W-anp-P[\d.]+`\s*[）)]', ''
    $b = $b -replace '\s*[（(]\s*→\s*`C-anp-P\d+`\s*ROP[，,]\s*源码注释\s*[）)]', '（源码注释）'
    $b = $b -replace '\s*[（(]\s*→\s*`C-anp-P\d+`\s*ROP,\s*source comments\s*[）)]', ' (source comments)'
    $b = $b -replace '\s*[（(]\s*衔接\s*`C-anp-P\d+`\s*[）)]', ''
    $b = $b -replace '\s*[（(]\s*links to\s*`C-anp-P\d+`\s*[）)]', ''
    # 归属/来源短语
    $b = $b -replace '`W-anp-P[\d.]+`\s*has created\s*', 'The project has created '
    $b = $b -replace 'is the output of\s*`C-anp-P\d+(?:\.\d+)?`\s*/\s*`W-anp-P\d+(?:\.\d+)?`\s*and registers', 'documents'
    $b = $b -replace '`W-anp-P\d+(?:\.\d+)?`\s*and registers', 'documents'
    $b = $b -replace 'logged for\s*`W-anp-P\d+(?:\.\d+)?`\s*to review and fix', 'logged for review and correction'
    $b = $b -replace 'was added by the\s*`C-anp-P\d+`\s*D3 ruling', 'was added by the theme engineering decision'
    # 编号区间（待建清单）
    $b = $b -replace '`W-anp-P1\d\d`–`P1\d\d`', '相关规划'
    $b = $b -replace '`W-anp-P1\d\d`–`P1\d\d`;\s*formerly\s*D3–D6,\s*renumbered to W on\s*20\d\d-\d\d-\d\d', '相关规划'
    # 测试清单里的内部阶段编号
    $b = $b -replace 'P1\d/P2\d/P2\d/P2\d\s*scenario SQL', 'scenario SQL'
    $b = $b -replace 'P1\d/P2\d/P2\d/P2\d\s*场景 SQL', '场景 SQL'
    # 复合出处前缀：「**P21 `W-anp-P124` 交付物**。」
    $b = $b -replace '\*\*P21\s*`W-anp-P[\d.]+`\s*交付物\*\*。\s*', ''
    $b = $b -replace '\*\*`W-anp-P[\d.]+`\s*deliverable\*\*\.\s*', ''
    # 括号内的实测/出处标注
    $b = $b -replace '\s*[（(]\s*`C-anp-P\d+(?:\.\d+)?`\s*实测\s*[）)]', ''
    $b = $b -replace '\s*[（(]\s*verified in\s*`C-anp-P\d+(?:\.\d+)?`[^）)]*[）)]', ''
    $b = $b -replace '\s*[（(]\s*`C-anp-P\d+(?:\.\d+)?`\s*/\s*`W-anp-P\d+(?:\.\d+)?`\s*[）)]', ''
    $b = $b -replace '\s*[（(]\s*`C-anp-P\d+(?:\.\d+)?`\s*/\s*P21\s*[）)]', ''
    $b = $b -replace '\s*[（(]\s*`C-anp-P\d+(?:\.\d+)?`\s*D3\s*裁定新增\s*[）)]', ''
    $b = $b -replace '\s*[（(]\s*added by the\s*`C-anp-P\d+`\s*D3 ruling\s*[）)]', ''
    # 标题里的出处后缀
    $b = $b -replace '(##\s*[^\r\n]*?)（`C-anp-P\d+(?:\.\d+)?`[^）\r\n]*）', '$1'
    $b = $b -replace '(##\s*[^\r\n]*?)\s*\(`C-anp-P\d+(?:\.\d+)?`[^)\r\n]*\)', '$1'
    # 零散裸编号（保底）：删掉剩余的 `W-anp-Pxxx` / `C-anp-Pxxx` 标记本身
    $b = $b -replace '`W-anp-P[\d.]+`', ''
    $b = $b -replace '`C-anp-P\d+(?:\.\d+)?`', ''
    # 交付物前缀 → 通用说法
    $b = $b -replace '`P21`\s*`W-anp-P127`\s*交付物', '文档复核清单'
    $b = $b -replace 'A P21 `W-anp-P127` deliverable', 'Documentation review checklist'
    $b = $b -replace '`P21\s*D1\s*交付物`', '文档工程登记表'
    $b = $b -replace 'A P21 D1 deliverable', 'Documentation engineering registry'
    $b = $b -replace '`P21\s*D2\s*交付物`', '文档导航地图'
    $b = $b -replace 'A P21 D2 deliverable', 'Documentation navigation map'
    $b = $b -replace '`P21\s*`W-anp-P125`\s*交付物`', '技术契约目录'
    $b = $b -replace 'A P21 `W-anp-P125` deliverable', 'Technical contracts'
    # <lang>
    #   <zh-CN>**无反引号的编号形态（第三批补漏）**：前两批都假设编号带反引号 `C-anp-P18`，
    #     但实际文档里还有大量**裸写**形态 —— 「（衔接 C-anp-P18）」「（C-anp-P19 / P21）」
    #     「（C-anp-P23 D3 裁定新增）」「**P21 D1 交付物**」「原则四（……）落地载体」「P28 起」。
    #     这些对外读者同样只是噪音。括号组整删仍放前面（避免残迹）。</zh-CN>
    # </lang>
    $b = [regex]::Replace($b, '\s*[（(][^）)]*(?:C|W)-anp-P[\d.]+[^）)]*[）)]', '')
    $b = $b -replace '\s*衔接\s*(?:C|W)-anp-P[\d.]+', ''
    $b = $b -replace '\s*links to\s*(?:C|W)-anp-P[\d.]+', ''
    $b = $b -replace '\*\*P21\s*D\d\s*交付物\*\*。?\s*', ''
    $b = $b -replace '\*\*A\s+P21\s+D\d\s+deliverable\*\*\.?\s*', ''
    $b = $b -replace '（D1，登记适用/不适用）', '（登记适用/不适用）'
    $b = $b -replace '\(D1, registering applicability[^)]*\)', '(registering applicability)'
    $b = $b -replace '原则四（[^）]*）落地载体：\s*', ''
    $b = $b -replace 'Vehicle for Principle 4 \([^)]*\):\s*', ''
    $b = $b -replace '\s*\(Principle 4\)', ''
    $b = $b -replace '\s*[（(]2026-\d\d-\d\d 起本 Cycle 改用 W 编号[^）)]*[）)]', ''
    $b = $b -replace 'P28 起新增', '新增'
    $b = $b -replace 'From P28 there is a formal MSTest runner', 'There is a formal MSTest runner'
    # <lang>
    #   <zh-CN>**残迹清理（必须在所有删号规则之后）**：先删散号会让「**A P21 `W-anp-P125` deliverable.**」
    #     变成「**A P21  deliverable.**」—— 编号删干净了，却留下孤立前缀与空括号，比不过滤更难看。
    #     这些形态只有删号后才出现，故放在末尾统一收口。
    #     **刻意不做全局双空格压缩**（`[ \t]{2,}` → ' '）：那会破坏代码块的对齐。</zh-CN>
    # </lang>
    $b = $b -replace '\*\*A\s+P21\s+deliverable\.\*\*\s*', ''
    $b = $b -replace '\*\*P21\s+交付物\*\*。\s*', ''
    $b = $b -replace '（\s*[，,、]\s*）', ''
    $b = $b -replace '\(\s*,\s*\)', ''
    return $b
}

foreach ($e in $entries) {    if ([string]::IsNullOrWhiteSpace($e.doc)) { $gaps.Add('上架清单存在缺 doc 的条目'); continue }

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
    if ($navTitles.Count -gt 0 -and -not $navTitleZhList.Contains($e.parent)) {
        $gaps.Add(('N1 parent 无效: ' + $e.doc + ' 的 parent="' + $e.parent + '" 不在 _data/navigation.yml 中'))
        continue
    }

    $pathSeg = if ([string]::IsNullOrWhiteSpace($e.path)) { [System.IO.Path]::GetFileNameWithoutExtension($e.doc) } else { $e.path }

    # <lang>
    #   <zh-CN>**页面标题取自文档自身，不取分区名**（`W-anp-P136` 修正，由上线后实测发现）：
    #     初版把中英两页的 `title` 都写成 `parent`（分区名，中文），导致**英文页面的浏览器标签与
    #     `<title>` 显示中文**（实测 `/en/overview/` 的 title 为"产品概览"）。
    #     现改为：中文页取其正文首个 `#` 一级标题；英文页取译本 front matter 的 `title`。
    #     两者都取不到时才回退到 `parent`。`parent` 仍只用于**导航分组**，不兼任页面标题。</zh-CN>
    # </lang>
    # 先读取两侧正文，再从正文里取标题（顺序不可颠倒：标题提取依赖已读入的正文）
    $zhBody = [System.IO.File]::ReadAllText($zhPath, $utf8)
    $enBody = [System.IO.File]::ReadAllText($enSource, $utf8)

    $zhTitle = $e.parent
    $zhH1 = [regex]::Match($zhBody, '(?m)^#\s+(.+?)\s*$')
    if ($zhH1.Success) { $zhTitle = $zhH1.Groups[1].Value.Trim() }

    $enTitle = $e.parent
    $enFmTitle = [regex]::Match($enBody, '(?m)^title:\s*(.+?)\s*$')
    if ($enFmTitle.Success) { $enTitle = $enFmTitle.Groups[1].Value.Trim() }
    else {
        $enH1 = [regex]::Match($enBody, '(?m)^#\s+(.+?)\s*$')
        if ($enH1.Success) { $enTitle = $enH1.Groups[1].Value.Trim() }
    }

    # <lang>
    #   <zh-CN>**剥离源文本自带的 front matter**（`W-anp-P136` 修正，由上线后直接读生成产物发现）：
    #     英文译本（`site/_translations/*.md`）本身带 front matter，若不剥离，生成的页面会有**两份 front matter**
    #     （`--- … ---` 紧接着又一组 `--- … ---`），Jekyll 只解析第一份，第二份的 YAML 会被**当正文渲染到页面上**。
    #     标题已在上面先提取完成，故此处剥离不会丢失信息。中文源同样做防御性剥离（当前 `docs/` 无 front matter）。</zh-CN>
    # </lang>
    $zhBody = [regex]::Replace($zhBody, '(?s)\A\s*---\r?\n.*?\r?\n---\s*\r?\n?', '')
    $enBody = [regex]::Replace($enBody, '(?s)\A\s*---\r?\n.*?\r?\n---\s*\r?\n?', '')

    # <lang>
    #   <zh-CN>**链接重写（`W-anp-P144`）**：`docs/` 下的相对 Markdown 链接在 GitHub 上浏览源码时是正确的，
    #     但**在站点结构下大多 404** —— 站点把页面拍平为 `site/<path>/index.md`，而 `docs/` 是嵌套目录。
    #     此前靠"在译本里手动改成 GitHub 绝对地址"绕过，但中文源没改，于是生成的中文页全是坏链
    #     （`contracts-*` 三页首次上架即暴露 31 处）。
    #     现按**当前文档所在目录**解析相对链接，再分三种情况处理：
    #       ① 解析结果命中 `_data/published.yml` 的 doc → path 映射 → 改写为 `{{ site.baseurl }}/<path>/`
    #          （站点内跳转，必须带 baseurl，否则浏览器丢失 Pages 注入的项目前缀而 404）；
    #       ② 解析结果在 `docs/` 内但未上架 → 保持原样（站点本就没有对应页面可指）；
    #       ③ 解析结果在 `docs/` **之外**（如 `../LICENSE`、`../README-old.md`，指向仓库根）→
    #          改写为 GitHub 绝对地址（站点没有这些文件，只能回源码仓库看）。
    #     **源文件保持 GitHub 友好**（不被改动），适配只发生在生成侧。</zh-CN>
    #   <en>**Link rewriting (`W-anp-P144`)**: relative Markdown links under `docs/` are correct when browsing the source on
    #     GitHub, but **mostly 404 under the site structure** — the site flattens pages to `site/<path>/index.md` while
    #     `docs/` is nested. This was previously worked around by manually changing translations to GitHub absolute URLs,
    #     but the Chinese sources were left alone, so every generated Chinese page had broken links (the three `contracts-*`
    #     pages exposed 31 of them the moment they were published).
    #     Relative links are now resolved **against the current document's directory**, then handled in three cases:
    #       (1) the resolved target matches a doc → path mapping in `_data/published.yml` → rewritten to
    #           `{{ site.baseurl }}/<path>/` (in-site navigation; baseurl is mandatory, otherwise the browser loses the
    #           Pages-injected project prefix and 404s);
    #       (2) the resolved target is inside `docs/` but not published → left as-is (the site has no such page to point at);
    #       (3) the resolved target is **outside** `docs/` (e.g. `../LICENSE`, `../README-old.md`, pointing at the repo root)
    #           → rewritten to a GitHub absolute URL (the site does not carry those files).
    #     **Source files stay GitHub-friendly** (untouched); adaptation happens only on the generation side.</en>
    # </lang>
    $zhBody = Convert-DocLink -Body $zhBody -CurDoc $e.doc
    $enBody = Convert-DocLink -Body $enBody -CurDoc $e.doc
    # 发布面文本过滤：去掉内部协作色彩（工作项编号、"内部计划，未公开"等），源文件不动
    $zhBody = Convert-PublicText -Body $zhBody
    $enBody = Convert-PublicText -Body $enBody

# <lang>
#   <zh-CN>**源指纹必须先归一化行尾再哈希**（`W-anp-P199` 验证时发现的缺陷）。原因：仓库的
#     `core.autocrlf=true` 让 **CI 检出的是 CRLF、本地工作区是 LF**；若按**原始字节**哈希，
#     同一个源文件在两处会得到不同指纹——于是**本地复核永远无法确认 CI 发布出去的页面**
#     （实测：52 个页面被报"构建戳未更新"，而内容一字未改）。归一化后指纹只反映**内容**，
#     与行尾表示无关。构建侧与线上复核侧必须用同一算法，这是戳能对上的前提。</zh-CN>
#   <en>**The source fingerprint must normalise line endings before hashing** (a defect found while verifying
#     `W-anp-P199`). Reason: the repository's `core.autocrlf=true` means **CI checks out CRLF while the local working
#     tree is LF**; hashing **raw bytes** would give different fingerprints for the same source in the two places —
#     so a **local re-check could never confirm a page published by CI** (measured: 52 pages reported "build stamp not
#     updated" while not one byte of content had changed). After normalisation the fingerprint reflects **content
#     only**, independent of line-ending representation. The build side and the live re-check side must use the same
#     algorithm; that is the precondition for stamps ever matching.</en>
# </lang>
function Get-Stamp12 {
    param([string] $Path, $Alg)
    $text = [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))
    $text = $text -replace "`r`n", "`n"
    $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes($text)
    return ([BitConverter]::ToString($Alg.ComputeHash($bytes))).Replace('-', '').Substring(0, 12)
}
    # <lang>
    #   <zh-CN>**构建戳**：把**源文档指纹**（SHA-256 前 12 位）写进生成页末尾的 HTML 注释里。
    #     为什么是"源"的指纹而不是"生成结果"的指纹：线上复核要判断的是"**线上这一页是不是由当前
    #     `docs/` 构建出来的**"，所以它必须能在**不重新构建**的前提下被复算——读源文件算哈希即可，
    #     而生成结果的指纹要跑一遍构建才能拿到（那是镜像新鲜度门禁的做法，职责不同）。
    #     为什么用 HTML 注释：markdown 里的原始 HTML 会被 Jekyll 原样输出，于是戳**随页面发到线上**
    #     且对读者不可见；放在 front matter 里则不进 HTML，线上读不到。
    #     中英两页各自取自己那一侧的源（中文 `docs/<doc>`、英文 `site/_translations/<doc>`），
    #     因此某一侧改了、另一侧没改，能被分别看出来。</zh-CN>
    #   <en>**Build stamp**: the **source document fingerprint** (first 12 hex chars of SHA-256) is written into an HTML
    #     comment at the end of each generated page. Why the *source* fingerprint rather than the *generated result*'s:
    #     the live re-check asks "**is this live page built from the current `docs/`**", so it must be recomputable
    #     **without re-running the build** — hashing the source file suffices, whereas the result's fingerprint would
    #     require a build (that is the mirror-freshness gate's approach, a different responsibility). Why an HTML comment:
    #     raw HTML inside markdown is passed through by Jekyll, so the stamp **ships with the page** while staying
    #     invisible to readers; front matter, by contrast, does not reach the HTML. Each language page stamps its own
    #     source (Chinese `docs/<doc>`, English `site/_translations/<doc>`), so a change on one side only is visible
    #     per side.</en>
    # </lang>
    $stampAlg = [System.Security.Cryptography.SHA256]::Create()
    try {
        $zhStamp = (Get-Stamp12 -Path $zhPath -Alg $stampAlg)
        $enStamp = (Get-Stamp12 -Path $enSource -Alg $stampAlg)
    }
    finally { $stampAlg.Dispose() }
    $zhBody = $zhBody.TrimEnd() + "`n`n<!-- build-stamp: " + $zhStamp + " -->`n"
    $enBody = $enBody.TrimEnd() + "`n`n<!-- build-stamp: " + $enStamp + " -->`n"

    $targetDir = Join-Path $outDir $pathSeg
    if (-not ($DryRun) -and -not (Test-Path -LiteralPath $targetDir)) { $null = New-Item -ItemType Directory -Path $targetDir }
    # <lang>
    #   <zh-CN>**parent 必须按页面语言注入对应语言的分区名**（`W-anp-P148`，由用户截图发现）。
    #     初版中英两页都注入 `$e.parent`（中文分区名），于是**英文内容页全挂到了中文分区下**，
    #     而英文分区页（title: Reference）名下没有任何子页 —— 侧边栏里中英节点混在同一个分区下展开。
    #     Just the Docs 的 children 收集逻辑是 `nav_parenthood | where: "name", node.title`，
    #     即**用父页面的 title 去匹配子页面的 parent 字符串**，二者必须**同语言**才能对上。
    #     分区名映射来自 `_data/navigation.yml`（title ↔ title_en），与 N1 校验同源。</zh-CN>
    #   <en>**parent must be injected in the matching language for each page** (`W-anp-P148`, found from a user screenshot).
    #     The first version injected `$e.parent` (the Chinese section name) into both language versions, so **all English
    #     content pages hung under the Chinese section**, while the English section page (title: Reference) had no children at
    #     all — Chinese and English nodes then expanded mixed together under one section in the sidebar.
    #     Just the Docs collects children with `nav_parenthood | where: "name", node.title`, i.e. it **matches the child's
    #     parent string against the parent page's title**, so the two must be **in the same language** to line up.
    #     Section names come from `_data/navigation.yml` (title ↔ title_en), the same source as the N1 check.</en>
    # </lang>
    # <lang>
    #   <zh-CN>**parent 按语言映射到分区页的实际标题**（`W-anp-P148`）。`published.yml` 的 `parent`
    #     是**中文裸分区名**（与 `_data/navigation.yml` 的 `title` 对应，人类可读，N1 也按它校验）；
    #     而 Just the Docs 要求子页的 parent **字符串等于父页的 title**，分区页 title 就是裸分区名
    #     （中文）/ `title_en`（英文）。
    #     注意：分区页标题**不加任何后缀** —— 曾有版本加「（分区）/(section)」来避开"页面 title 与父页 title
    #     相同"的主题保护分支，结果侧边栏每个分区都显示成「产品概览（分区）」，用户明确指出难看。
    #     正确做法是让**冲突的那一个页面**让路（本站仅 `en/overview` 的标题与英文分区页相同，
    #     已改为 `Product overview and positioning`），而不是给全部分区页加后缀。</zh-CN>
    #   <en>**parent is mapped per language to the section page's actual title** (`W-anp-P148`). The `parent` in
    #     `published.yml` is the **bare Chinese section name** (matching `title` in `_data/navigation.yml`, human-readable,
    #     and what N1 checks); Just the Docs requires a child's parent **string to equal the parent page's title**, and the
    #     section page title is exactly the bare section name (Chinese) / `title_en` (English).
    #     Note: section page titles carry **no suffix** — an earlier version appended "（分区）/ (section)" to avoid the
    #     theme's "page title equals parent page title" guard, but that made every sidebar entry read "产品概览（分区）",
    #     which the user explicitly called ugly. The right fix is to let **the one conflicting page** yield (here only
    #     `en/overview`'s title collided with the English section page, and it is now
    #     `Product overview and positioning`), not to suffix every section page.</en>
    # </lang>
    $zhParent = $e.parent
    $enBare = $e.parent
    foreach ($nv in $navTitles) {
        if ($nv.Zh -eq $e.parent -and $nv.En) { $enBare = $nv.En; break }
    }
    $enParent = $enBare
    $zhParent = $e.parent
    $zhFm = "---`nlayout: default`nlang: zh-CN`nparent: " + $zhParent + "`nnav_order: " + $e['nav_order'] + "`npermalink: /" + $pathSeg + "/`ntitle: " + $zhTitle + "`n---`n"
    Write-GeneratedPage -Path (Join-Path $targetDir 'index.md') -Content ($zhFm + $zhBody)

    $enFm = "---`nlayout: default`nlang: en`nparent: " + $enParent + "`nnav_order: " + $e['nav_order'] + "`npermalink: /en/" + $pathSeg + "/`ntitle: " + $enTitle + "`n---`n"

    $enDir = Join-Path $targetDir 'en'
    if (-not ($DryRun) -and -not (Test-Path -LiteralPath $enDir)) { $null = New-Item -ItemType Directory -Path $enDir }
    Write-GeneratedPage -Path (Join-Path $enDir 'index.md') -Content ($enFm + $enBody)

    $generated++
}

# <lang>
#   <zh-CN>**dry-run 在此显式收口退出**。后面的 N1/N2 与双语成对校验读的是**当前**站点内容，
#     在 dry-run 下运行会把"当前站点的状态"误呈现成"本次构建的结论"——那正是一种假绿灯
#     （dry-run 绿了，实跑却可能坏）。故 dry-run 只报告将写入什么，然后退出。</zh-CN>
#   <en>**Dry-run exits explicitly here.** The N1/N2 and bilingual-pairing checks that follow read the **current** site
#     content, so running them under dry-run would present the current site's state as this build's conclusion — a false
#     green light (dry-run green while a real run could fail). Dry-run therefore only reports what would be written, then
#     exits.</en>
# </lang>
if ($DryRun) {
    Write-Host ('[DRY-RUN] 将生成 ' + $generated + ' 个页面（中/英各一份）：新增 ' + $dryNew + '｜内容变更 ' + $dryChanged + '｜无变化 ' + $drySame + '。')
    foreach ($p in $dryPlan) { Write-Host ('  ' + $p) }
    Write-Host '[DRY-RUN] 未写入任何文件（含目录创建）；N1/N2 与双语成对校验未运行（它们读当前站点内容，会把当前状态误呈现成本次结论）。'
    Write-Host 'RESULT: Pass'
    exit 0
}

# <lang>
#   <zh-CN>`-OutputRoot` 模式同样在此收口：N1/N2 与双语成对校验读的是**真实**站点内容（含手写页面），
#     对临时输出目录运行会把"手写页面不在这里"误判成缺口——**假失败**。本模式的唯一目的就是
#     "产出可比对的一组生成文件"，校验由调用方（镜像新鲜度门禁）负责。</zh-CN>
#   <en>`-OutputRoot` mode exits here for the same reason: the N1/N2 and bilingual-pairing checks read the **real** site
#     content (including hand-written pages), so running them against a temp output directory would misreport "hand-written
#     pages are not here" as a gap — a **false failure**. This mode's only purpose is to produce a comparable set of generated
#     files; verification is the caller's job (the mirror freshness gate).</en>
# </lang>
if (-not [string]::IsNullOrWhiteSpace($OutputRoot)) {
    Write-Host ('[OUTPUT-REDIRECT] 已生成 ' + $generated + ' 个页面（中/英各一份）到：' + $outDir)
    Write-Host '[OUTPUT-REDIRECT] N1/N2 与双语成对校验未运行（它们读真实站点内容，对临时目录会误判手写页面缺失）。'
    Write-Host 'RESULT: Pass'
    exit 0
}
# <lang>
#   <zh-CN>N2：站点页面中的站内链接必须**既形态合法、又目标存在** —— 这是 `W-anp-P132` 为
#     "导航/首页链接全部 404 而门禁未发现"补的检查。注意只检查 `site/**/index.md`
#     （生成产物与手写页面），_config.yml 等源码不在其列。
#
#     **`W-anp-P144` 口径修正（本项此前是"假通过"的典型）**：初版只判断
#     "`site/<seg>/index.md` 这个文件在不在"，**完全不看链接的写法**。于是首页手写的
#     裸绝对路径 `/overview/` 被判为"可达"（因为 `site/overview/index.md` 确实存在）——
#     而浏览器实际把它解析成 `https://mandolin.github.io/overview/`，**丢失 Pages 注入的
#     `/HIA-ASPNETPortal` 前缀**，点击即 404。线上 31 个首页链接全是这么坏的，
#     而门禁一路绿灯（`W-anp-P132` 修过一次同类问题，本项却没拦住第二次）。
#     根因与本项目反复出现的教训同源：**门禁的"可达"定义 ≠ 用户点击后的真实结果**。
#     现要求站内链接必须是两种合法形态之一：
#       ① `{{ site.baseurl }}/...`（推荐；Pages 会自动把 baseurl 注入为 `/HIA-ASPNETPortal`）；
#       ② 同目录相对路径（如 `foo.md`、`./foo.md`）。
#     裸绝对路径 `/xxx/` 一律判坏链 —— 在项目子路径站点下它必然 404。</zh-CN>
# </lang>
$pageFiles = @(Get-ChildItem -Path $siteDir -Recurse -Filter 'index.md' -File -ErrorAction SilentlyContinue)

# <lang>
#   <zh-CN>**手写页面的双语成对检查（`W-anp-P146`）**：`site/index.md`、`site/en/index.md` 与
#     `site/sections/**` 这些分区索引页是**手写**的，不经上面的生成流程，因此不受"缺译即 Fail"保护。
#     若只写中文忘写英文，双语并行就成了空话 —— 而这个缺口此前没有任何检查覆盖。
#     规则：`site/**/index.md`（非生成目录）必须同时存在 `en/index.md`，反之亦然。
#     生成目录（`site/<path>/`，其下直接有 index.md 与 en/index.md 且由 published.yml 声明）天然成对，
#     故只检查**不在 published.yml path 列表中**的目录，避免重复校验。</zh-CN>
#   <en>**Pairing check for hand-written pages (`W-anp-P146`)**: `site/index.md`, `site/en/index.md` and the section index
#     pages under `site/sections/**` are **hand-written** and bypass the generation flow above, so they are not protected by
#     "a missing translation fails". If Chinese is written and English forgotten, bilingual parity becomes an empty phrase —
#     and no check covered that gap before.
#     Rule: every `site/**/index.md` (outside generated directories) must have a matching `en/index.md`, and vice versa.
#     Generated directories (which hold both `index.md` and `en/index.md` and are declared in published.yml) are naturally
#     paired, so only directories **not in the published.yml path list** are checked, avoiding duplicate validation.</en>
# </lang>
# <lang>
#   <zh-CN>**手写页面的双语成对检查（`W-anp-P146`）**：`site/index.md`、`site/en/index.md`、
#     `site/sections/**` 这些分区索引页是**手写**的，不经上面的生成流程，因此不受"缺译即 Fail"保护。
#     若只写中文忘写英文，双语并行就成了空话 —— 而这个缺口此前没有任何检查覆盖。
#
#     **站点结构约定（`W-anp-P147` 修正后）**：英文树统一挂在 `site/en/` 下，即
#       中文 `site/sections/<slug>/index.md` ↔ 英文 `site/en/sections/<slug>/index.md`。
#     初版按"同目录下的 en/ 子目录"配对，在我把英文分区页移到 `site/en/sections/` 后**立刻误报 8 处** ——
#     这正说明该检查是活的（不是橡皮章）：结构约定变了，检查必须跟着变，否则它会用过期口径制造假失败。
#     规则：凡 `site/**/index.md` 且不在 published.yml path 列表中，
#       ① 中文页（`site/sections/<slug>/index.md`）→ 须有 `site/en/sections/<slug>/index.md`；
#       ② 站点根 `site/index.md` → 须有 `site/en/index.md`；
#       ③ 反向同理。</zh-CN>
#   <en>**Pairing check for hand-written pages (`W-anp-P146`)**: `site/index.md`, `site/en/index.md` and the section index
#     pages under `site/sections/**` are **hand-written** and bypass the generation flow above, so they are not protected by
#     "a missing translation fails". If Chinese is written and English forgotten, bilingual parity becomes an empty phrase —
#     and no check covered that gap before.
#
#     **Site structure convention (after the `W-anp-P147` fix)**: the English tree hangs uniformly under `site/en/`, i.e.
#       Chinese `site/sections/<slug>/index.md` ↔ English `site/en/sections/<slug>/index.md`.
#     The first version paired by "an `en/` subdirectory in the same directory" and **immediately produced 8 false reports**
#     after I moved the English section pages to `site/en/sections/` — which shows the check is alive (not a rubber stamp):
#     when the structure convention changes, the check must change with it, or it manufactures false failures from a stale
#     convention. Rule: for any `site/**/index.md` not in the published.yml path list,
#       (1) a Chinese page (`site/sections/<slug>/index.md`) must have `site/en/sections/<slug>/index.md`;
#       (2) the site root `site/index.md` must have `site/en/index.md`;
#       (3) and vice versa.</en>
# </lang>
$publishedPaths = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($e in $entries) { if ($e.path) { $null = $publishedPaths.Add($e.path) } }

foreach ($pf in $pageFiles) {
    $rel = $pf.FullName.Substring($siteDir.Length).Replace('\', '/').TrimStart('/')
    if ($rel -notmatch '^sections/[^/]+/index\.md$' -and $rel -ne 'index.md' -and $rel -notmatch '^[a-z0-9-]+/index\.md$') { continue }
    if ($rel -match '^en/') { continue }  # en/ 树由下面的反向循环检查，这里只查中文侧

    # rel 形如 sections/<slug>/index.md、<顶层目录>/index.md 或 index.md；slug 直接从 rel 取，
    # 避免对根路径再 Split-Path。顶层目录手写页（overview/features/compare 等营销页）也纳入成对检查 ——
    # 但若该目录是 published.yml 声明的**生成目录**（如 product-overview），则跳过（生成流程已保证成对）。
    if ($rel -eq 'index.md') {
        $enRel = 'en/index.md'
    }
    elseif ($rel -match '^sections/([^/]+)/index\.md$') {
        $enRel = 'en/sections/' + $Matches[1] + '/index.md'
    }
    else {
        $seg = ($rel -split '/')[0]
        if ($publishedPaths.Contains($seg)) { continue }
        $enRel = 'en/' + $seg + '/index.md'
    }
    $enPair = Join-Path $siteDir ($enRel -replace '/', '\')
    if (-not (Test-Path -LiteralPath $enPair)) {
        $gaps.Add(('手写页面缺英文版: site/' + $rel + ' -> 应有 site/' + $enRel))
    }
}
# 反向：英文手写页存在但缺中文
foreach ($pf in $pageFiles) {
    $rel = $pf.FullName.Substring($siteDir.Length).Replace('\', '/').TrimStart('/')
    if ($publishedPaths.Contains((Split-Path $rel -Parent))) { continue }
    if ($rel -notmatch '^en/sections/[^/]+/index\.md$' -and $rel -ne 'en/index.md' -and $rel -notmatch '^en/[a-z0-9-]+/index\.md$') { continue }

    if ($rel -eq 'en/index.md') {
        $zhRel = 'index.md'
    }
    elseif ($rel -match '^en/sections/([^/]+)/index\.md$') {
        $zhRel = 'sections/' + $Matches[1] + '/index.md'
    }
    else {
        # rel 形如 en/<顶层目录>/index.md；若该目录是 published.yml 声明的生成目录则跳过
        $seg = ($rel -split '/')[1]
        if ($publishedPaths.Contains($seg)) { continue }
        $zhRel = $seg + '/index.md'
    }
    $zhPair = Join-Path $siteDir ($zhRel -replace '/', '\')
    if (-not (Test-Path -LiteralPath $zhPair)) {
        $gaps.Add(('手写页面缺中文版: site/' + $rel + ' -> 应有 site/' + $zhRel))
    }
}

foreach ($pf in $pageFiles) {
    $rel = $pf.FullName.Substring($siteDir.Length).Replace('\', '/')
    $text = [System.IO.File]::ReadAllText($pf.FullName, $utf8)
    foreach ($m in [regex]::Matches($text, '\]\(([^)]+)\)')) {
        $target = $m.Groups[1].Value.Trim()
        if ($target -eq '' -or $target -eq '/' -or $target -eq '/en/') { continue }
        # 外链、锚点、mailto 不在本站内链接范畴
        if ($target -match '^(https?:|mailto:|#|//)') { continue }
        # 形态校验：必须带 {{ site.baseurl }} 或是相对路径
        if ($target -notmatch '^\{\{\s*site\.baseurl\s*\}\}') {
            if ($target -notmatch '^\.{0,2}/?[A-Za-z0-9._-]+\.md(#.*)?$' -and $target -notmatch '^\.{1,2}/[A-Za-z0-9._/-]+$') {
                $gaps.Add(('N2 站内链接未带 baseurl（项目子路径站点下浏览器解析必 404）: site' + $rel + ' -> ' + $target))
                continue
            }
        }
        $seg = ($target -replace '^\{\{\s*site\.baseurl\s*\}\}', '')
        $seg = ($seg -split '#')[0].Trim('/')
        if ($seg.StartsWith('en/')) { $seg = $seg.Substring(3) }
        if ($seg -eq '') { continue }

        # <lang>
        #   <zh-CN>**静态资源与页面分开校验**：静态资源（图片等）按**文件是否存在**判定，页面仍按 `<seg>/index.md` 判定。
        #     旧实现把图片链接也当页面查 `site/<seg>/index.md`，于是**任何**图片链接都被判坏链 —— 既误报、
        #     又完全放过了"引用了并不存在的图片"这种真坏链。分开校验后：图片链接必须真的在仓库里，
        #     少一张就报错（比原来更强，不是放水）。</zh-CN>
        #   <en>**Validate static assets and pages separately**: static assets (images and the like) are judged by whether the
        #     **file exists**, while pages are still judged by `<seg>/index.md`. The previous implementation looked up
        #     `site/<seg>/index.md` for image links too, so **every** image link was reported as broken — both a false
        #     positive and a complete blind spot for "links to an image that does not exist". With the split, an image link
        #     must resolve to a real file in the repository: a missing image fails the build (stronger, not weaker).</en>
        # </lang>
        if ($seg -match '\.(png|jpe?g|svg|webp|gif|pdf|ico|css|js)$') {
            $assetPath = Join-Path $siteDir ($seg.Replace('/', '\'))
            if (-not (Test-Path -LiteralPath $assetPath)) {
                $gaps.Add(('N2 站内静态资源缺失: site' + $rel + ' -> ' + $target))
            }
            continue
        }

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
