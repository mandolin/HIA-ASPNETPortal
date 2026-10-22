<#
.SYNOPSIS
<lang>
  <zh-CN>代码文档发布门禁：官网 `/api/jsdoc/`（JSDoc 半边）与 `/api/dotnetdoc/`（DotNetDoc 半边）两个快照都必须与源码输入一致、形状合格，且说明页已上架并链接到它们。</zh-CN>
  <en>Code documentation publishing gate: both snapshots on the site — `/api/jsdoc/` (the JSDoc half) and
  `/api/dotnetdoc/` (the DotNetDoc half) — must agree with their source inputs, be shape-clean, and be linked from a
  published explainer page.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>
    背景：`C-anp-P32`（`W-anp-P180`）把 HIA-Documentation-Sys 的 **JSDoc 半边**产物发布到官网
    （`site/api/jsdoc/`，3 个双语 HTML + 主题资源）。官网由签入的静态文件发布，因此这是**快照**：
    源码改了而快照没刷新，官网就会展示过期代码文档，且没有任何机制会发现。

    `C-anp-P36`（`W-anp-P201`）补上 **DotNetDoc 半边**（`site/api/dotnetdoc/`，7 个半边 × 各自声明的语言）。
    本门禁随之从"只守 JSDoc"扩为"两个半边都守"。

    **JSDoc 半边（A 组）**：
    A1 **快照存在且完整**：`site/api/jsdoc/` 必须含清单声明的全部产物（3 个 HTML + 主题资源）；
    A2 **源指纹一致（新鲜度）**：对 JSDoc 的 2 个输入文件计算 SHA256 组合指纹，与
       `code-docs-manifest.json` 记录值比对——不一致即失败（这正是"源码变了快照没刷"的形态）；
    A3 **公开面形状**：快照 HTML 不得含内部工作项编号（`W-anp-P*` / `C-anp-P*`）、不得含本地绝对路径；
    A4 **说明页已上架且链接到位**：`docs/code-documentation.md` 必须已被 `published.yml` 上架，
       且文中链接到 `/api/jsdoc/` **与** `/api/dotnetdoc/`（否则读者从官网找不到代码文档入口）。

    **DotNetDoc 半边（B 组）**：
    B1 **快照与清单互洽**：清单登记的每个"半边 × 语言"都必须有 `index.html`；反向地，快照里存在的每个
       "半边 × 语言"目录都必须在清单里——**两头都要查**，否则残留目录会变成读者点到 404 的幽灵产物；
    B2 **源指纹一致**：覆盖 `dotnetdoc.config.json` 及其声明的全部输入（4 个 XML 文档、54 个标记层文件、
       7 个页面表层文件、`src/master.sln`）；输入缺失时该行记 `<missing>`，指纹自然不一致并给出可执行提示，
       **不跳过**（跳过等于假装通过）；
    B3 **公开面形状**：对快照下**所有**文件（不只是 HTML）递归扫描内部编号与绝对路径；
    B4 **语言声明与产物一致**：每个半边声明的 `locales` 与实际产出的语言目录必须**完全相同**——
       少一种语言是漏渲染，多一种语言是**伪造双语**（把英文内容复制成中文目录尤其危险，读者看不出差别）；
    B5 **产物非空**：每个 `index.html` 不得是空壳（小于 1 KB 视为异常）。
  </zh-CN>
  <en>
    Background: `C-anp-P32` (`W-anp-P180`) publishes the **JSDoc half** of the HIA-Documentation-Sys output to the site
    (`site/api/jsdoc/`: 3 bilingual HTML pages + theme assets). The site is published from committed static files, so this
    is a **snapshot**: if the sources change but the snapshot is not refreshed, the site serves stale code documentation
    and nothing would notice.

    `C-anp-P36` (`W-anp-P201`) added the **DotNetDoc half** (`site/api/dotnetdoc/`: 7 halves × their own declared
    locales). This gate accordingly grew from "guard the JSDoc half" to "guard both halves".

    **JSDoc half (group A)**: A1 the snapshot exists and is complete (`site/api/jsdoc/` must contain every artifact
    declared by the manifest: 3 HTML pages + theme assets); A2 source fingerprint agreement — the combined SHA-256 over
    the two JSDoc input files is compared with the value recorded in `code-docs-manifest.json`, and a mismatch fails
    (exactly the shape of "sources changed, snapshot not refreshed"); A3 public-surface shape — snapshot HTML must
    contain no internal work-item numbers and no local absolute paths; A4 the explainer page is published and links to
    `/api/jsdoc/` **and** `/api/dotnetdoc/`.

    **DotNetDoc half (group B)**: B1 snapshot and manifest agree **in both directions** — every declared half × locale
    must have an `index.html`, and every half × locale directory present must be declared, otherwise a leftover
    directory becomes a ghost artifact readers hit as 404; B2 source fingerprint agreement over `dotnetdoc.config.json`
    and every input it declares (4 XML documents, 54 markup files, 7 surface files, `src/master.sln`), where a missing
    input records `<missing>` so the fingerprint naturally disagrees with an actionable message rather than being
    **skipped** (skipping is pretending to pass); B3 public-surface shape — a **recursive** scan of **all** files (not
    just HTML) for internal numbers and absolute paths; B4 declared `locales` must match the produced locale
    directories **exactly** — a missing language is a rendering miss, an extra language is **fabricated bilingualism**
    (copying English content into a Chinese directory is especially dangerous because readers cannot tell the
    difference); B5 artifacts are not empty shells (an `index.html` under 1 KB is treated as abnormal).
  </zh-CN>
</lang>

.PARAMETER StatsPath
<lang>
  <zh-CN>统计 JSON 的输出路径（可选）。</zh-CN>
  <en>Output path for the stats JSON (optional).</en>
</lang>

.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Test-PortalCodeDocs.ps1'</zh-CN>
  <en>& 'dev\scripts\Test-PortalCodeDocs.ps1'</en>
</lang>

.NOTES
<lang>
  <zh-CN>
    证明边界：只证明"两个快照与各自声明一致、源指纹匹配、公开面形状合格、语言声明与产物一致、说明页已上架并链接"；
    **不证明快照等于一次全新生成的逐字节输出**（指纹只覆盖输入文件，不覆盖工具链版本与主题包版本——
    这两者变化时需人工重跑刷新）；**不证明代码文档内容本身正确**（注释写得准不准属人工审校，`W-anp-P204`）。
    另：A3/B3 只扫 `site/api/` 下的发布产物，不扫 `src/` 源码注释——源码注释里的内部编号由另一套约定处理。
  </zh-CN>
  <en>
    Proof boundary: proves only that "both snapshots match their declarations, source fingerprints agree, the
    public-surface shape is clean, declared locales match the artifacts, and the explainer page is published and
    linked"; does **not** prove a snapshot is byte-identical to a fresh generation (the fingerprint covers only input
    files, not toolchain or theme package versions — when those change, a human refresh run is required); does **not**
    prove the code documentation's content is correct (whether the comments are accurate is human review, `W-anp-P204`).
    Also: A3/B3 scan only the published artifacts under `site/api/`, not the `src/` comments — internal numbers in
    source comments are governed by a separate convention.
  </en>
</lang>
#>
[CmdletBinding()]
param(
    [string] $StatsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$utf8 = [System.Text.UTF8Encoding]::new($false)
$snapshotDir = Join-Path $repoRoot 'site\api\jsdoc'
$manifestPath = Join-Path $snapshotDir 'code-docs-manifest.json'
$docPath = Join-Path $repoRoot 'docs\code-documentation.md'
$publishedPath = Join-Path $repoRoot 'site\_data\published.yml'

# JSDoc 输入（与 dev/documentation/jsdoc/jsdoc.conf.json 的 source.include 一致）
$jsdocInputs = @(
    'src\Portal\gulpfile.js',
    'src\Portal\Scripts\Security\PortalLoginPasswordEncryption.js'
)

$failures = @()
$softNotes = @()

function Get-SourceFingerprint {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $parts = @()
    foreach ($rel in $jsdocInputs) {
        $full = Join-Path $repoRoot $rel
        if (-not (Test-Path -LiteralPath $full)) { return $null }
        $bytes = [System.IO.File]::ReadAllBytes($full)
        $hash = [BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '').ToLowerInvariant()
        $parts += ($rel.Replace('\', '/') + '|' + $bytes.Length + '|' + $hash)
    }
    $combined = [BitConverter]::ToString($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes(($parts -join "`n")))).Replace('-', '').ToLowerInvariant()
    return $combined
}

# ---------- A1 快照存在且完整 ----------
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    $failures += 'A1 代码文档快照清单缺失：site/api/jsdoc/code-docs-manifest.json（未上架或未登记）。'
}
else {
    $manifest = [System.IO.File]::ReadAllText($manifestPath, $utf8) | ConvertFrom-Json
    foreach ($artifact in $manifest.artifacts) {
        $p = Join-Path $snapshotDir $artifact
        if (-not (Test-Path -LiteralPath $p -PathType Leaf)) {
            $failures += "A1 快照缺少清单声明的产物：$artifact（site/api/jsdoc/）。"
        }
    }
    $htmlCount = @(Get-ChildItem $snapshotDir -File -Filter '*.html' -ErrorAction SilentlyContinue).Count
    if ($htmlCount -lt 3) {
        $failures += "A1 快照 HTML 页面不足 3 个（中文 / 英文 / 默认入口），当前 $htmlCount 个。"
    }
}

# ---------- A2 源指纹一致 ----------
if (Test-Path -LiteralPath $manifestPath -PathType Leaf) {
    $manifest = [System.IO.File]::ReadAllText($manifestPath, $utf8) | ConvertFrom-Json
    $actual = Get-SourceFingerprint
    if ($null -eq $actual) {
        $failures += 'A2 无法计算源指纹：JSDoc 输入文件缺失（' + ($jsdocInputs -join '、') + '）。'
    }
    elseif ($actual -ne $manifest.sourceFingerprint) {
        $failures += ('A2 源指纹不一致（源码已变而快照未刷新）：清单 ' + $manifest.sourceFingerprint.Substring(0, 16) + '… vs 实际 ' + $actual.Substring(0, 16) + '…；请重跑 Build-PortalJsdocPilot.ps1 并更新快照与清单。')
    }
}

# ---------- A3 公开面形状 ----------
# <lang>
#   <zh-CN>**必须递归**（`W-anp-P202` 修的真实缺口）：原先只扫快照**顶层**的 `*.html`
#     （`Get-ChildItem $snapshotDir -File`）。JSDoc 半边恰好没有子目录，所以这个疏漏一直没暴露；
#     而 DotNetDoc 半边的产物全在 `site/api/dotnetdoc/<半边>/<语言>/` 子目录里——`W-anp-P201`
#     首次发布后，**正是人肉递归扫描才发现 6 个页面含内部编号，而本门禁全部通过**。
#     一个只在特定目录深度生效的检查，等于把正确性押在"产物恰好是平的"上。</zh-CN>
#   <en>**Must be recursive** (a real gap fixed by `W-anp-P202`): this previously scanned only the **top level** of the
#     snapshot (`Get-ChildItem $snapshotDir -File`). The JSDoc half happens to have no subdirectories, so the gap stayed
#     hidden; the DotNetDoc half's artifacts all live in `site/api/dotnetdoc/<half>/<locale>/` subdirectories — after
#     `W-anp-P201`'s first publish it was **a manual recursive scan that found six pages carrying internal numbers while
#     this gate passed**. A check that only works at one directory depth bets correctness on "the artifacts happen to be
#     flat".</en>
# </lang>
function Test-PublicShape {
    param([string] $Directory, [string] $Label)
    if (-not (Test-Path -LiteralPath $Directory)) { return 0 }
    $scanned = 0
    foreach ($f in (Get-ChildItem -LiteralPath $Directory -Recurse -File -ErrorAction SilentlyContinue)) {
        $scanned++
        $text = [System.IO.File]::ReadAllText($f.FullName, $utf8)
        $ids = [regex]::Matches($text, '(?:W|C)-anp-P\d')
        if ($ids.Count -gt 0) {
            # <lang>
            #   <zh-CN>**必须写 `$script:failures`**：PowerShell 中函数内的 `$failures += …` 会创建/修改**局部副本**，
            #     脚本级的 `$failures` 一个字都不会变——于是这个检查会"跑过了但什么都没报"。
            #     这不是假想：`W-anp-P202` 的注入验证第一次跑时就踩中了（I2 在子目录注入内部编号，
            #     门禁 exit 0 全绿通过）。**把逻辑提成函数的那个动作，静默关掉了整个检查。**</zh-CN>
            #   <en>**`$script:failures` is mandatory**: inside a function, `$failures += …` creates/updates a **local copy**
            #     and the script-level `$failures` does not change by a single entry — so the check "ran but reported
            #     nothing". This is not hypothetical: `W-anp-P202`'s injection verification hit it on the first run (I2
            #     injected an internal number into a subdirectory page and the gate exited 0, fully green).
            #     **Extracting the logic into a function silently switched the whole check off.**</en>
            # </lang>
            $script:failures += "A3 $Label 含内部工作项编号 $($ids.Count) 处（公开面不得外泄）：$($f.FullName.Substring($repoRoot.Length + 1).Replace('\', '/'))。"
        }
        if ($text -match 'K:\\' -or $text -match 'K:/' -or $text -match 'C:\\Users') {
            $script:failures += "A3 $Label 含本地绝对路径（公开面不得外泄）：$($f.FullName.Substring($repoRoot.Length + 1).Replace('\', '/'))。"
        }
    }
    return $scanned
}
$jsdocScanned = Test-PublicShape -Directory $snapshotDir -Label 'JSDoc 快照'

# ---------- A4 说明页已上架且链接到位 ----------
if (-not (Test-Path -LiteralPath $docPath -PathType Leaf)) {
    $failures += 'A4 代码文档说明页缺失：docs/code-documentation.md。'
}
else {
    $docText = [System.IO.File]::ReadAllText($docPath, $utf8)
    if ($docText -notmatch '/api/jsdoc/') {
        $failures += 'A4 说明页未链接到代码文档：docs/code-documentation.md 必须出现 /api/jsdoc/ 入口链接。'
    }
    if ($docText -notmatch '/api/dotnetdoc/') {
        $failures += 'A4 说明页未链接到 DotNetDoc 代码文档：docs/code-documentation.md 必须出现 /api/dotnetdoc/ 入口链接（半边已上架，入口缺失等于"发布了但找不到"）。'
    }
    $publishedText = [System.IO.File]::ReadAllText($publishedPath, $utf8)
    if ($publishedText -notmatch '(?m)^\s*-\s*doc:\s*code-documentation\.md\s*$') {
        $failures += 'A4 说明页未上架：site/_data/published.yml 缺少 code-documentation.md 条目（读者从官网找不到入口）。'
    }
}

# ---------- B 组：DotNetDoc 半边 ----------
$dotnetdocDir = Join-Path $repoRoot 'site\api\dotnetdoc'
$dotnetdocManifestPath = Join-Path $dotnetdocDir 'code-docs-manifest.json'
$dotnetdocConfigPath = Join-Path $repoRoot 'dotnetdoc.config.json'

# <lang>
#   <zh-CN>B2 的指纹算法与 JSDoc 半边**逐字一致**（`'<相对路径>|<字节数>|<sha256>'` 按 LF 连接后再取 SHA-256），
#     两边可用同一套判定；输入缺失记 `<missing>`，因此"没构建 XML"会表现为指纹不一致并附带可执行提示，
#     而不是被静默跳过。</zh-CN>
#   <en>B2's fingerprint algorithm is **word-for-word identical** to the JSDoc half's (`'<relativePath>|<byteLength>|<sha256>'
#     lines joined by LF, then SHA-256) so both halves are judged by one rule; a missing input records `<missing>`, so
#     "the XML was not built" surfaces as a fingerprint mismatch with an actionable message instead of being silently
#     skipped.</en>
# </lang>
function Get-DotNetDocSourceFingerprint {
    if (-not (Test-Path -LiteralPath $dotnetdocConfigPath -PathType Leaf)) { return $null }
    $inputs = @('dotnetdoc.config.json')
    $config = [System.IO.File]::ReadAllText($dotnetdocConfigPath, [System.Text.UTF8Encoding]::new($false)) | ConvertFrom-Json
    foreach ($input in @($config.inputs)) {
        if ($input.PSObject.Properties.Name -contains 'path' -and $input.path) { $inputs += $input.path }
        if ($input.PSObject.Properties.Name -contains 'paths') { foreach ($p in @($input.paths)) { $inputs += $p } }
    }
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $parts = @()
    $missing = @()
    foreach ($rel in ($inputs | Sort-Object -Unique)) {
        $full = Join-Path $repoRoot ($rel -replace '/', '\')
        if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { $parts += (($rel -replace '\\', '/') + '|<missing>|<missing>'); $missing += $rel; continue }
        $bytes = [System.IO.File]::ReadAllBytes($full)
        $hash = ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
        $parts += (($rel -replace '\\', '/') + '|' + $bytes.Length + '|' + $hash)
    }
    $combined = ([BitConverter]::ToString($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes(($parts -join "`n"))))).Replace('-', '').ToLowerInvariant()
    $sha.Dispose()
    return [pscustomobject]@{ Fingerprint = $combined; Missing = @($missing); InputCount = @($inputs | Sort-Object -Unique).Count }
}

if (-not (Test-Path -LiteralPath $dotnetdocManifestPath -PathType Leaf)) {
    $failures += 'B1 DotNetDoc 快照清单缺失：site/api/dotnetdoc/code-docs-manifest.json（半边未上架或未登记；若有意不上架，须在说明页如实写明并同步修改本门禁的证明边界）。'
}
else {
    $dnManifest = [System.IO.File]::ReadAllText($dotnetdocManifestPath, $utf8) | ConvertFrom-Json

    # B1 正反两向
    $declared = @()
    foreach ($h in @($dnManifest.halves)) { $declared += ($h.half + '/' + $h.locale) }
    foreach ($key in $declared) {
        $entry = Join-Path $dotnetdocDir ($key -replace '/', '\')
        if (-not (Test-Path -LiteralPath (Join-Path $entry 'index.html') -PathType Leaf)) {
            $failures += "B1 清单登记的半边×语言缺少 index.html：$key。"
        }
    }
    $ghost = @()
    if (Test-Path -LiteralPath $dotnetdocDir) {
        # <lang>
        #   <zh-CN>**目录是三层**：`<分组>/<半边>/<语言>/`（如 `api/Portal/en/`）——半边名本身含一个斜杠
        #     （`api/Portal`）。首版只走两层，于是把 7 个半边整体误判成"幽灵目录"：
        #     **检查逻辑写错比没有检查更糟**，它会用自信的语气报出假失败，让人先去修不存在的问题。</zh-CN>
        #   <en>**The tree is three levels**: `<group>/<half>/<locale>/` (e.g. `api/Portal/en/`) — the half name itself
        #     contains a slash (`api/Portal`). The first version walked only two levels and so misreported all seven halves
        #     as "ghost directories": **a wrong check is worse than no check**, because it reports false failures
        #     confidently and sends people to fix problems that do not exist.</en>
        # </lang>
        foreach ($group in (Get-ChildItem -LiteralPath $dotnetdocDir -Directory)) {
            foreach ($halfDir in (Get-ChildItem -LiteralPath $group.FullName -Directory)) {
                $halfKey = $group.Name + '/' + $halfDir.Name
                foreach ($locDir in (Get-ChildItem -LiteralPath $halfDir.FullName -Directory)) {
                    $key = $halfKey + '/' + $locDir.Name
                    if ($declared -notcontains $key) { $ghost += $key }
                }
            }
        }
    }
    if ($ghost.Count -gt 0) {
        $failures += ('B1 快照存在清单未登记的"半边×语言"目录（残留/幽灵产物，读者会点到 404）：' + ($ghost -join '、') + '。请重跑 Publish-PortalDotNetDoc.ps1（它整体重建发布目录）。')
    }

    # B2 源指纹
    $fp = Get-DotNetDocSourceFingerprint
    if ($null -eq $fp) {
        $failures += 'B2 无法计算源指纹：dotnetdoc.config.json 缺失。'
    }
    else {
        if ($fp.Missing.Count -gt 0) {
            $failures += ('B2 源指纹输入缺失 ' + $fp.Missing.Count + ' 项（' + ($fp.Missing | Select-Object -First 5) -join '、' + '…）：XML 文档由构建产出且不入库，请先运行 dev/scripts/Build-PortalDotNetDoc.ps1 再跑本门禁（不跳过、不假装通过）。')
        }
        elseif ($fp.Fingerprint -ne $dnManifest.sourceFingerprint) {
            $failures += ('B2 源指纹不一致（源码已变而快照未刷新）：清单 ' + [string]$dnManifest.sourceFingerprint + ' vs 实际 ' + $fp.Fingerprint + '；请重跑 Publish-PortalDotNetDoc.ps1 并提交快照。')
        }
    }

    # B3 公开面形状（递归、所有文件）
    $dnScanned = Test-PublicShape -Directory $dotnetdocDir -Label 'DotNetDoc 快照'

    # B4 语言声明与产物一致
    foreach ($entry in @($dnManifest.declaredLocales)) {
        $halfDir = Join-Path $dotnetdocDir ($entry.half -replace '/', '\')
        $actual = @()
        if (Test-Path -LiteralPath $halfDir) {
            $actual = @(Get-ChildItem -LiteralPath $halfDir -Directory | ForEach-Object { $_.Name })
        }
        $want = @($entry.locales)
        $missingLocales = @($want | Where-Object { $actual -notcontains $_ })
        $extraLocales = @($actual | Where-Object { $want -notcontains $_ })
        if ($missingLocales.Count -gt 0) {
            $failures += ('B4 半边 ' + $entry.half + ' 声明了语言 ' + ($want -join '/') + ' 但产物缺少：' + ($missingLocales -join '、') + '（漏渲染）。')
        }
        if ($extraLocales.Count -gt 0) {
            $failures += ('B4 半边 ' + $entry.half + ' 只声明 ' + ($want -join '/') + ' 却存在产物目录：' + ($extraLocales -join '、') + '——**伪造双语**（把英文内容复制成中文目录，读者看不出差别）；请删掉该目录并重跑 Publish-PortalDotNetDoc.ps1。')
        }
    }

    # B5 产物非空
    foreach ($key in $declared) {
        $entry = Join-Path $dotnetdocDir ($key -replace '/', '\') 'index.html'
        if (Test-Path -LiteralPath $entry -PathType Leaf) {
            $size = (Get-Item -LiteralPath $entry).Length
            if ($size -lt 1KB) {
                $failures += ('B5 产物疑似空壳：' + $key + '/index.html 仅 ' + $size + ' 字节（渲染失败或被清空）。')
            }
        }
    }
}

# ---------- 统计与输出 ----------
if ([string]::IsNullOrWhiteSpace($StatsPath)) {
    $statsDir = Join-Path $repoRoot 'temp\p180'
    New-Item -ItemType Directory -Force -Path $statsDir | Out-Null
    $StatsPath = Join-Path $statsDir 'code-docs-stats.json'
}
$report = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    jsdoc = [ordered]@{
        snapshotFiles  = @(Get-ChildItem $snapshotDir -File -ErrorAction SilentlyContinue | ForEach-Object { $_.Name })
        sourceFingerprint = Get-SourceFingerprint
        scannedFiles   = $jsdocScanned
    }
    dotnetdoc = [ordered]@{
        manifestPresent = (Test-Path -LiteralPath $dotnetdocManifestPath -PathType Leaf)
        halves          = if (Test-Path -LiteralPath $dotnetdocManifestPath -PathType Leaf) { @($dnManifest.halves | ForEach-Object { $_.half + '/' + $_.locale }) } else { @() }
        sourceFingerprint = if (Test-Path -LiteralPath $dotnetdocConfigPath -PathType Leaf) { (Get-DotNetDocSourceFingerprint).Fingerprint } else { $null }
        scannedFiles    = $dnScanned
    }
    failures = @($failures)
}
[System.IO.File]::WriteAllText($StatsPath, ($report | ConvertTo-Json -Depth 5), $utf8)

if ($failures.Count -gt 0) {
    Write-Host '[FAIL] 代码文档发布：' -ForegroundColor Red
    foreach ($f in $failures) { Write-Host ('  - ' + $f) -ForegroundColor Red }
    Write-Host ('统计已写入：' + $StatsPath)
    exit 1
}

foreach ($s in $softNotes) { Write-Host ('  ' + $s) }
$halfCount = if (Test-Path -LiteralPath $dotnetdocManifestPath -PathType Leaf) { @($dnManifest.halves).Count } else { 0 }
Write-Host ('[PASS] 代码文档发布：JSDoc 快照完整、源指纹一致、公开面形状合格；DotNetDoc ' + $halfCount + ' 个半边×语言完整、源指纹一致、形状合格、语言声明与产物一致；说明页已上架并链接两个入口。') -ForegroundColor Green
Write-Host ('统计已写入：' + $StatsPath)
exit 0
