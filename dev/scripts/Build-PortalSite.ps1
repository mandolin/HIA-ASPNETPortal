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
    if ($navTitles.Count -gt 0 -and -not $navTitles.Contains($e.parent)) {
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

    $targetDir = Join-Path $siteDir $pathSeg
    if (-not (Test-Path -LiteralPath $targetDir)) { $null = New-Item -ItemType Directory -Path $targetDir }
    $zhFm = "---`nlayout: default`nlang: zh-CN`nparent: " + $e.parent + "`npermalink: /" + $pathSeg + "/`ntitle: " + $zhTitle + "`n---`n"
    [System.IO.File]::WriteAllText((Join-Path $targetDir 'index.md'), ($zhFm + $zhBody), $utf8)

    $enDir = Join-Path $targetDir 'en'
    if (-not (Test-Path -LiteralPath $enDir)) { $null = New-Item -ItemType Directory -Path $enDir }
    $enFm = "---`nlayout: default`nlang: en`nparent: " + $e.parent + "`npermalink: /en/" + $pathSeg + "/`ntitle: " + $enTitle + "`n---`n"
    [System.IO.File]::WriteAllText((Join-Path $enDir 'index.md'), ($enFm + $enBody), $utf8)

    $generated++
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
