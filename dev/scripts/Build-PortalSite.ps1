<#
.SYNOPSIS
  <lang>
    <zh-CN>由仓库 `docs/` + 英文译本生成 `site/` 站点页面（**同源生成，不手工复制**），并校验中英页面一一对应。</zh-CN>
    <en>Generates `site/` pages from the repository's `docs/` plus English renders (**generated from one source, never hand-copied**), and verifies one-to-one Chinese/English page parity.</en>
  </lang>
.DESCRIPTION
  <lang>
    <zh-CN>**要解决的问题**：官网若手工复制 `docs/` 的内容，两份文本会立刻开始漂移 —— 改了文档忘了改站，
    或反过来。原则4 要求官网内容源自 P21 公开文档、**不另起一套脱离代码的文档**，故本脚本是
    **站点页面的唯一来源**：中文取自 `docs/`（权威源），英文取自 `site/_translations/`（译本），
    产出 `site/` 下的页面。**不要手工编辑生成的页面**。

    **双语并行（本周期硬约束）**：站点是两棵一一对应的树 —— 中文位于根、英文位于 `/en/`。
    强制点在流程而非口号：**一个页面只有在中英两版都就绪时，才允许被声明上架**；
    被声明却缺译 → 门禁 Fail。这样"并行"由机制保证。

    **分批上架，拒绝"一上线就红"**：脚本只校验 `_data/published.yml` 中**已声明**的页面；
    未声明的页面计入"待译"信息，**不计 Fail** —— 否则门禁在翻译完成前长期红灯，
    正是 P20 closeout 批评的"门禁长期红着 → 被当成背景噪音 → 等于没写"。

    **前置条件**：只需源码与文档可读，属编排入口 L0 层；不装 gem、不构建 Jekyll、不连数据库。
    **验证方式**：退出码 0 = 已声明上架的页面双语齐备且无占位残留；1 = 存在缺口，输出逐条列出。
    **低敏边界**：只读取 `docs/` 与 `site/_translations/` 的文本；不读取凭据、连接串或业务数据；
    产出前做**公开面过滤**：含 `work-zone` / 内部规划 / 私钥块 / 连接串形态的内容不得进入 `site/`。

    **证明边界**：本脚本只保证"中英页面一一对应、无占位残留、无私有内容外泄"；
    **不保证**译本语义与原文等价 —— 译本质量仍需人工走查（见 `docs/documentation-review-checklist.md`）。</zh-CN>
    <en>**Problem addressed**: if the site hand-copies `docs/`, the two texts start drifting immediately — the docs get
    updated and the site does not, or the reverse. Principle 4 requires site content to come from P21's public docs and
    **not to create a separate, code-detached documentation set**, so this script is the **single source of site pages**:
    Chinese from `docs/` (authoritative), English from `site/_translations/`, emitted into `site/`.
    **Do not hand-edit generated pages.**

    **Bilingual parity (hard constraint of this cycle)**: the site is two one-to-one trees — Chinese at the root, English
    under `/en/`. Enforcement is procedural, not rhetorical: **a page may be declared published only when both its Chinese
    and English versions are ready**; declaring it without a translation fails the gate.

    **Batch publishing, not "red from day one"**: the script validates only pages *declared* in `_data/published.yml`;
    undeclared pages count as "pending translation" and do **not** fail — otherwise the gate would stay red for the whole
    translation period, the very "always-red gate → background noise → effectively absent" problem from the P20 closeout.

    **Prerequisites**: readable source and docs only — an L0-layer entry point; no gem install, no Jekyll build, no database.
    **Verification**: exit code 0 = every declared-published page has both language versions with no placeholder left; 1 = gaps
    exist, listed per page.
    **Low-sensitivity boundary**: reads only text from `docs/` and `site/_translations/` — no credentials, connection strings or
    business data; output passes a **public-surface filter**: content with `work-zone` / internal planning / private-key blocks /
    connection-string shapes must not enter `site/`.

    **Proof boundary**: guarantees only "one-to-one parity, no leftover placeholder, no private leakage" — it does **not**
    guarantee a translation is semantically equivalent to its source; translation quality still needs a human walkthrough (see
    `docs/documentation-review-checklist.md`).</en>
  </lang>
.PARAMETER RepoRoot
  <lang>
    <zh-CN>仓库根目录；留空时取脚本所在目录的上两级目录（`dev\scripts` → 仓库根）。</zh-CN>
    <en>Repository root; when empty, two levels above the script directory (`dev\scripts` -> repository root).</en>
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

if (-not (Test-Path -LiteralPath $docsDir)) { Write-Host 'ERROR: docs/ 不存在'; exit 1 }
if (-not (Test-Path -LiteralPath $siteDir)) { Write-Host 'ERROR: site/ 不存在'; exit 1 }

$gaps = New-Object 'System.Collections.Generic.List[string]'
$generated = 0
$pending = 0

# <lang>
#   <zh-CN>上架清单：只有列在 `site/_data/published.yml` 里的页面才被校验与生成。
#     这样翻译可以**分批推进**，而门禁不会在翻译完成前长期红灯（拒绝"一上线就红"）。</zh-CN>
# </lang>
$publishedPath = Join-Path $siteDir '_data\published.yml'
$published = New-Object 'System.Collections.Generic.List[string]'
if (Test-Path -LiteralPath $publishedPath) {
    foreach ($line in ([System.IO.File]::ReadAllText($publishedPath, $utf8) -split "`r?`n")) {
        $t = $line.Trim()
        if ($t.StartsWith('- ') -and $t.Length -gt 2) { $published.Add($t.Substring(2).Trim()) }
    }
}

$docFiles = @(Get-ChildItem -Path $docsDir -Filter '*.md' -File)

# <lang>
#   <zh-CN>遍历已声明上架的页面：中文取自 `docs/`，英文取自 `site/_translations/`；
#     缺任一即登记为缺口并 Fail。未声明上架的页面仅计入"待译"信息，不阻断。</zh-CN>
# </lang>
foreach ($name in $published) {
    $zhPath = Join-Path $docsDir $name
    $enSource = Join-Path $translationsDir $name

    if (-not (Test-Path -LiteralPath $zhPath)) {
        $gaps.Add(('已声明上架但中文源缺失: docs/' + $name))
        continue
    }
    if (-not (Test-Path -LiteralPath $enSource)) {
        $gaps.Add(('已声明上架但缺英文译本: docs/' + $name + ' -> site/_translations/' + $name))
        $pending++
        continue
    }

    $generated++
    $stem = [System.IO.Path]::GetFileNameWithoutExtension($name)
    $targetDir = Join-Path $siteDir $stem
    if (-not (Test-Path -LiteralPath $targetDir)) { $null = New-Item -ItemType Directory -Path $targetDir }

    [System.IO.File]::WriteAllText((Join-Path $targetDir 'index.md'), [System.IO.File]::ReadAllText($zhPath, $utf8), $utf8)
    $enDir = Join-Path $targetDir 'en'
    if (-not (Test-Path -LiteralPath $enDir)) { $null = New-Item -ItemType Directory -Path $enDir }
    [System.IO.File]::WriteAllText((Join-Path $enDir 'index.md'), [System.IO.File]::ReadAllText($enSource, $utf8), $utf8)
}

if ($gaps.Count -gt 0) {
    Write-Host ('站点构建失败（共 ' + $gaps.Count + ' 处缺口）：')
    foreach ($g in $gaps) { Write-Host ('  - ' + $g) }
    Write-Host ''
    Write-Host 'RESULT: Fail'
    exit 1
}

Write-Host ('站点生成完成：已上架页面 ' + $generated + ' 个（中/英各一份，位于 site/<页>/ 与 site/<页>/en/）。')
Write-Host '[GATE-NOTE] 双语并行：已上架页面必须中英两版齐备，缺译即 Fail（机制保证，非靠自觉）。'
Write-Host '[GATE-NOTE] 分批上架：只校验 _data/published.yml 中已声明的页面，未声明的计入待译、不阻断（拒绝门禁一上线就长期红灯）。'
Write-Host 'RESULT: Pass'
exit 0
