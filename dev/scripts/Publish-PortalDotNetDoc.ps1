<#
.SYNOPSIS
<lang>
  <zh-CN>把 DotNetDoc 的 HIA 工件渲染成 HTML 并发布为 `site/api/dotnetdoc/` 快照。</zh-CN>
  <en>Renders the DotNetDoc HIA artifacts into HTML and publishes them as a snapshot under `site/api/dotnetdoc/`.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>**为什么需要这个脚本**（`C-anp-P36` / `W-anp-P201`）：`C-anp-P32` 交付代码文档时，DotNetDoc 半边只生成
    **中间 JSON**（`temp/documentation/dotnetdoc/*.hia.json`）而未上架，说明页与 manifest 都写着"待
    HIA-Documentation-Sys 的渲染/发布组件就位"。本周期启动前实测发现**组件早已存在**：试点工作区已安装
    `@hia-doc/renderer-html` + `@hia-doc/theme-default`，且 `hia docs build --input <工件> --out <目录>
    --locale <语言>` 可把单个 HIA 工件渲染成自包含的 `index.html` + `assets/`。缺的只是**接线与上架机制**——
    这正是本脚本补的那一环。

  **与 JSDoc 半边的关系**：JSDoc 半边是"跑生成脚本 → 人工拷产物 → 手改 manifest"。DotNetDoc 半边有
    **7 个半边 × 每种声明语言** 的组合（4 个 API 工程 + 标记层 + 页面表层 + 工程发现），人工拷贝既慢又易错，
    因此拷贝与 manifest 登记**脚本化**；生成步骤仍复用 `Build-PortalDotNetDocPilot.ps1`（不改变试点口径）。

  **三条刻意的设计决定**：
  1. **按工件自己声明的 `locales` 渲染**，不按我们想要的语言渲染。实测：`api/*` 与 `markup/*` 声明
     `["en","zh-CN"]`（C# XML 注释本身是双语的，用 `<lang><zh-CN>…<en>…</lang>` 惯例），而 `surface/*`、
     `project/*` 只声明 `["en"]`（它们是结构描述、不是人工注释）。**纯英文半边如实只出英文**，
     不为了"看起来双语"而伪造中文入口。
  2. **顶层入口页由脚本生成**（列出每个半边 × 每种语言的链接），因为 `/api/dotnetdoc/` 必须有个 index
     才能被导航与外链指向；手写入口页会与产物漂移。
  3. **产物自包含**：每个半边目录各自带 `assets/`，互不依赖，删一个半边不影响其他半边。</zh-CN>
  <en>**Why this script** (`C-anp-P36` / `W-anp-P201`): when `C-anp-P32` delivered the code documentation, the DotNetDoc
    half produced **intermediate JSON only** (`temp/documentation/dotnetdoc/*.hia.json`) and was not published; the
    explainer page and manifest both said "pending the HIA-Documentation-Sys rendering/publishing component".
    Investigation before this cycle found **the component already exists**: the pilot workspace has
    `@hia-doc/renderer-html` + `@hia-doc/theme-default` installed, and `hia docs build --input <artifact> --out <dir>
    --locale <locale>` renders a single HIA artifact into a self-contained `index.html` + `assets/`. What was missing
    is the **wiring and publishing mechanism** — exactly the gap this script fills.

  **Relationship to the JSDoc half**: the JSDoc half is "run the generator, copy artifacts by hand, edit the manifest
    by hand". The DotNetDoc half has **7 halves × each declared locale** (four API projects + markup + surface +
    project discovery), so manual copying is slow and error-prone; copying and manifest registration are therefore
    **scripted**, while generation still reuses `Build-PortalDotNetDocPilot.ps1` (the pilot's scope is unchanged).

  **Three deliberate decisions**: (1) render according to **each artifact's own declared `locales`**, not according to
    the languages we might prefer — `api/*` and `markup/*` declare `["en","zh-CN"]` (the C# XML comments are
    themselves bilingual, using the `<lang><zh-CN>…<en>…</lang>` convention) while `surface/*` and `project/*` declare
    `["en"]` only (they are structural descriptions, not human comments), so an English-only half **honestly renders
    English only** rather than faking a Chinese entry; (2) the top-level entry page is **generated** (listing every
    half × locale) because `/api/dotnetdoc/` needs an index for the navigation and external link to point at, and a
    hand-written entry would drift from the artifacts; (3) artifacts are **self-contained** — each half directory
    carries its own `assets/`, so removing one half cannot break the others.</en>
</lang>

.PARAMETER SkipGenerate
<lang>
  <zh-CN>跳过 `hia-dotnetdoc` 生成步骤，直接使用 `temp/documentation/dotnetdoc` 下现有工件。</zh-CN>
  <en>Skips the `hia-dotnetdoc` generation step and consumes the existing artifacts under `temp/documentation/dotnetdoc`.</en>
</lang>

.PARAMETER WhatIf
<lang>
  <zh-CN>只报告将发布哪些半边与语言、不写任何文件（与 `Build-PortalSite -DryRun` 同型的只读开关）。</zh-CN>
  <en>Reports which halves and locales would be published without writing any file (the same read-only switch shape as `Build-PortalSite -DryRun`).</en>
</lang>
#>
[CmdletBinding()]
param(
    [switch]$SkipGenerate,
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>**脚本自身可运行性的前置**：本机 `node` 常不在 PowerShell 的 PATH 里（实测 `C:\Program Files\nodejs\node.exe`
#     存在但 `node` 术语找不到），而 `hia` 的 `.cmd` 垫片内部要调 `node`。因此在调用前把 node 目录补进 PATH，
#     找不到就**明确失败**并说明原因，不让错误以"'node' 不是内部或外部命令"这种难以归因的形式冒出来。</zh-CN>
#   <en>**Prerequisite for this script to run at all**: on this machine `node` is frequently absent from PowerShell's PATH
#     (measured: `C:\Program Files\nodejs\node.exe` exists yet the term `node` is not found), while the `hia` `.cmd` shim
#     invokes `node` internally. So the node directory is added to PATH before any call, and if it cannot be found the
#     script **fails explicitly with a reason** rather than letting a hard-to-attribute "'node' is not recognized" error
#     surface.</en>
# </lang>
$nodeDirectory = $null
$nodeCommand = Get-Command node -ErrorAction SilentlyContinue
if ($nodeCommand) { $nodeDirectory = Split-Path -Parent $nodeCommand.Source }
else {
    foreach ($candidate in @("$env:ProgramFiles\nodejs", "${env:ProgramFiles(x86)}\nodejs", "$env:LOCALAPPDATA\Programs\nodejs")) {
        if (Test-Path -LiteralPath (Join-Path $candidate 'node.exe') -PathType Leaf) { $nodeDirectory = $candidate; break }
    }
}
if (-not $nodeDirectory) { throw '找不到 node.exe（试过 PATH 与常见安装目录）。DotNetDoc 渲染依赖 Node 运行时。' }
if (-not (($env:PATH -split ';') -contains $nodeDirectory)) { $env:PATH = $nodeDirectory + ';' + $env:PATH }

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$toolDirectory = Join-Path $repositoryRoot 'dev\documentation\dotnetdoc'
$hiaCommand = Join-Path $toolDirectory 'node_modules\.bin\hia.cmd'
$artifactsDirectory = Join-Path $repositoryRoot 'temp\documentation\dotnetdoc'
$publishRoot = Join-Path $repositoryRoot 'site\api\dotnetdoc'
$stagingRoot = Join-Path $repositoryRoot 'temp\documentation\dotnetdoc-render'

if (-not (Test-Path -LiteralPath $hiaCommand -PathType Leaf)) { throw "找不到 hia CLI：$hiaCommand（请先在 dev/documentation/dotnetdoc 还原依赖）。" }

# <lang>
#   <zh-CN>生成步骤复用既有试点脚本，**不在这里重跑 npm ci 或改配置**——试点口径由 `Build-PortalDotNetDocPilot.ps1`
#     独占，本脚本只负责"生成之后"的那一半。</zh-CN>
#   <en>Generation reuses the existing pilot script and **never re-runs npm ci or changes configuration here** — the
#     pilot's scope belongs to `Build-PortalDotNetDocPilot.ps1` alone; this script owns only the "after generation"
#     half.</en>
# </lang>
if (-not $SkipGenerate) {
    $pilotScript = Join-Path $PSScriptRoot 'Build-PortalDotNetDocPilot.ps1'
    & $pilotScript -SkipRestore
    if ($LASTEXITCODE -ne 0) { throw "DotNetDoc 生成失败，退出代码：$LASTEXITCODE" }
}

$hiaArtifacts = @(Get-ChildItem -LiteralPath $artifactsDirectory -Recurse -File -Filter '*.hia.json' | Sort-Object FullName)
if ($hiaArtifacts.Count -eq 0) { throw "在 $artifactsDirectory 下没有找到 *.hia.json（先跑生成，或用 -SkipGenerate 指向已有产物）。" }

# <lang>
#   <zh-CN>读每个工件**自己声明的**语言表。缺省为 `en`：宁可少渲染一种语言，也不要凭猜渲染出一种工件没有的语言
#     （那会产出空壳页面）。</zh-CN>
#   <en>Read each artifact's **own declared** locale list, defaulting to `en`: it is better to render one language fewer
#     than to guess a language the artifact does not declare (which would produce an empty shell page).</en>
# </lang>
$plan = @()
foreach ($artifact in $hiaArtifacts) {
    $relative = $artifact.FullName.Substring($artifactsDirectory.Length + 1)
    # <lang>
    #   <zh-CN>半边名取**相对路径去扩展名**（如 `api/Portal`、`api/Portal.Components`）而不是父目录名——
    #     四个 API 工程的父目录都是 `api`，用父目录会把它们显示成同名四项，读者无法区分。</zh-CN>
    #   <en>The half name is the **relative path without extension** (e.g. `api/Portal`, `api/Portal.Components`) rather
    #     than the parent directory name — all four API projects share the parent `api`, so the parent would render them
    #     as four identically named entries that readers cannot tell apart.</en>
    # </lang>
    $half = ($relative -replace '\.hia\.json$', '') -replace '\\', '/'
    $json = [System.IO.File]::ReadAllText($artifact.FullName, [System.Text.UTF8Encoding]::new($false)) | ConvertFrom-Json
    $locales = @()
    if ($json.PSObject.Properties.Name -contains 'locales') {
        $declared = $json.locales
        if ($declared -is [string]) { $locales = @($declared) } else { $locales = @($declared) }
    }
    if ($locales.Count -eq 0) { $locales = @('en') }
    $plan += [pscustomobject]@{
        Half     = ($half -replace '\\', '/')
        Artifact = $artifact.FullName
        Locales  = $locales
        Title    = [string]$json.title
    }
}

# <lang>
#   <zh-CN>**源指纹**（`W-anp-P202` 新增）：覆盖 `dotnetdoc.config.json` 及其声明的**全部输入**——
#     4 个 XML 文档、54 个标记层文件、7 个页面表层文件、`src/master.sln`。算法与 JSDoc 半边一致
#     （`'<相对路径>|<字节数>|<sha256>'` 各行按 LF 连接后再取 SHA-256），使两侧可用同一套判定。
#     为什么连配置本身也算进去：输入集合变了（增删文件）产物就该变，只算文件不算声明会漏掉这一类漂移。
#     输入缺失时该行记 `<missing>` 而不是跳过——跳过会让指纹"看起来正常"，那是假装通过。</zh-CN>
#   <en>**Source fingerprint** (added by `W-anp-P202`): covers `dotnetdoc.config.json` and **every input it declares** —
#     the four XML documents, the 54 markup files, the 7 surface files and `src/master.sln`. The algorithm matches the
#     JSDoc half (`'<relativePath>|<byteLength>|<sha256>'` lines joined by LF, then SHA-256), so both halves can be
#     judged by one rule. Why the config itself is included: when the input set changes (files added or removed) the
#     output should change, and hashing only the files would miss that class of drift. A missing input records
#     `<missing>` on its line rather than being skipped — skipping would make the fingerprint "look fine", which is
#     pretending to pass.</en>
# </lang>
$configPath = Join-Path $repositoryRoot 'dotnetdoc.config.json'
$fingerprintInputs = @()
if (Test-Path -LiteralPath $configPath -PathType Leaf) {
    $fingerprintInputs += 'dotnetdoc.config.json'
    $config = [System.IO.File]::ReadAllText($configPath, [System.Text.UTF8Encoding]::new($false)) | ConvertFrom-Json
    foreach ($input in @($config.inputs)) {
        if ($input.PSObject.Properties.Name -contains 'path' -and $input.path) { $fingerprintInputs += $input.path }
        if ($input.PSObject.Properties.Name -contains 'paths') { foreach ($p in @($input.paths)) { $fingerprintInputs += $p } }
    }
}
$fingerprintInputs = @($fingerprintInputs | Sort-Object -Unique)
$sha = [System.Security.Cryptography.SHA256]::Create()
$fingerprintParts = @()
foreach ($rel in $fingerprintInputs) {
    $full = Join-Path $repositoryRoot ($rel -replace '/', '\')
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { $fingerprintParts += (($rel -replace '\\', '/') + '|<missing>|<missing>'); continue }
    $bytes = [System.IO.File]::ReadAllBytes($full)
    $hash = ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    $fingerprintParts += (($rel -replace '\\', '/') + '|' + $bytes.Length + '|' + $hash)
}
$sourceFingerprint = ([BitConverter]::ToString($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes(($fingerprintParts -join "`n"))))).Replace('-', '').ToLowerInvariant()
$sha.Dispose()


$totalLocales = @($plan | ForEach-Object { $_.Locales.Count } | Measure-Object -Sum).Sum
Write-Host ('[INFO] DotNetDoc 半边 ' + $plan.Count + ' 个｜声明语言组合 ' + $totalLocales + ' 个')
foreach ($entry in $plan) {
    Write-Host ('       ' + $entry.Half.PadRight(26) + ' → ' + ($entry.Locales -join ' + '))
}

if ($WhatIf) {
    Write-Host ('[INFO] -WhatIf：不写任何文件。将发布到 ' + $publishRoot)
    exit 0
}

if (Test-Path -LiteralPath $stagingRoot -PathType Container) { Remove-Item -LiteralPath $stagingRoot -Recurse -Force }
$null = New-Item -ItemType Directory -Force -Path $stagingRoot

# <lang>
#   <zh-CN>**渲染方式（W-anp-P225 起改为多页）**：不再逐半边 `--input` 单页渲染（那样一个 API
#     工程整面挤在一页，Portal 中文版实测 4.58 MB），而是用 **`--project-manifest` 一次渲染成
#     split-site 多页站点**（`index.html` 壳 + `entries/<逐成员>` + `navigation/` + `search/` +
#     `assets/`）。`hia.config.json` 的 `docs.renderer.projectLayout = "split-site"` 决定布局，
#     `docs.theme.colors.accent` 把主色覆写为门户蓝 `#0f4f82`（工具在默认 CSS 之后追加
#     高特异性 `:root[data-hia-skin]` 覆盖块）。`--locale zh-CN` 只决定**默认显示语言**——
#     条目页本身含中英双语内容块，站内语言切换器可切英文，故一套产物即双语。
#   <en>**Rendering (multi-page since W-anp-P225)**: instead of rendering each half with `--input`
#     (which crams an entire API project onto one page — 4.58 MB measured for the Chinese Portal half),
#     we now render **once via `--project-manifest` into a split-site** (`index.html` shell +
#     `entries/<per member>` + `navigation/` + `search/` + `assets/`). `docs.renderer.projectLayout =
#     "split-site"` in `hia.config.json` selects the layout, and `docs.theme.colors.accent` overrides
#     the accent with the portal blue `#0f4f82` (the tool appends a higher-specificity `:root[data-hia-skin]`
#     block after the default CSS). `--locale zh-CN` only sets the **initially displayed language** — entry
#     pages carry both zh and en content blocks and the in-site switcher toggles English, so one artefact
#     set serves both languages.</en>
# </lang>
$configPath = Join-Path $repositoryRoot 'hia.config.json'
$projectManifestPath = Join-Path $repositoryRoot 'hia.project.json'
foreach ($required in @($configPath, $projectManifestPath)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "缺少多页渲染所需配置：$required" }
}
$renderTarget = Join-Path $stagingRoot 'api/dotnetdoc'
$null = New-Item -ItemType Directory -Force -Path $renderTarget

Push-Location $toolDirectory
try {
    & $hiaCommand docs build --config $configPath --project-manifest $projectManifestPath --out $renderTarget --locale 'zh-CN' 2>&1 |
        Where-Object { $_ -match '\[error|\[warning:PRESENTATION' } |
        ForEach-Object { Write-Host ('    ' + $_) }
    if ($LASTEXITCODE -ne 0) { throw ("多页渲染失败（exit " + $LASTEXITCODE + '）') }
    if (-not (Test-Path -LiteralPath (Join-Path $renderTarget 'index.html') -PathType Leaf)) {
        throw ('多页渲染未产出 index.html：' + $renderTarget)
    }
    $entryPages = @(Get-ChildItem -LiteralPath (Join-Path $renderTarget 'entries') -File -Filter '*.html' -ErrorAction SilentlyContinue)
    if ($entryPages.Count -eq 0) { throw ('多页渲染未产出任何条目页：' + (Join-Path $renderTarget 'entries')) }
}
finally { Pop-Location }

# ---------- 发布：先清后建，整体重建 ----------
$u = [System.Text.UTF8Encoding]::new($false)
# <lang>
#   <zh-CN>**先清后建**：发布目录整体重建，避免上一版残留的半边/语言目录变成"幽灵产物"（读者点到 404，
#     而 manifest 里又没有它——那种不一致最难查）。</zh-CN>
#   <en>**Clear then rebuild**: the publish directory is rebuilt wholesale so a previous half/locale directory cannot
#     survive as a "ghost artifact" (a reader hitting 404 while the manifest does not list it — the hardest kind of
#     inconsistency to track down).</en>
# </lang>
if (Test-Path -LiteralPath $publishRoot -PathType Container) { Remove-Item -LiteralPath $publishRoot -Recurse -Force }
$null = New-Item -ItemType Directory -Force -Path $publishRoot
Copy-Item -Path (Join-Path $renderTarget '*') -Destination $publishRoot -Recurse -Force

$publishedFiles = @(Get-ChildItem -LiteralPath $publishRoot -Recurse -File)
$publishedBytes = ($publishedFiles | Measure-Object Length -Sum).Sum
$entryPageCount = @(Get-ChildItem -LiteralPath (Join-Path $publishRoot 'entries') -File -Filter '*.html' -ErrorAction SilentlyContinue).Count

# ---------- manifest：站点级（多页 split-site） ----------
# <lang>
#   <zh-CN>**为什么 manifest 从 halves[] 改成站点级**：多页 split-site 是**一个自包含站点**
#     （index.html + entries/ + navigation/ + search/），不再有"半边 × 语言"的目录矩阵。若继续按
#      halves[] 登记，manifest 会描述一个不存在的结构——那正是"看起来通过、实际不一致"。
#     因此改为站点级：登记入口、布局、语言、条目数与源指纹。</zh-CN>
#   <en>**Why the manifest changed from halves[] to site-level**: the multi-page split-site is **one
#     self-contained site** (index.html + entries/ + navigation/ + search/), so the "half × locale"
#     directory matrix no longer exists. Keeping halves[] would describe a structure that is not there —
#     exactly the "looks passing, actually inconsistent" failure. It now records the entrypoint,
#     layout, locales, entry count and source fingerprint at site level.</en>
# </lang>
$projectIndexPath = Join-Path $renderTarget 'project-index.json'
$entryIds = @()
if (Test-Path -LiteralPath $projectIndexPath -PathType Leaf) {
    $projectIndex = [System.IO.File]::ReadAllText($projectIndexPath, $u) | ConvertFrom-Json
    if ($null -ne $projectIndex.entries) { $entryIds = @($projectIndex.entries | ForEach-Object { $_.id }) }
}

$manifest = [ordered]@{
    schemaVersion    = 2
    layout           = 'split-site'
    generator        = '@hia-doc/dotnetdoc-runner（hia-dotnetdoc）+ @hia-doc/renderer-html（hia docs build），由 dev/scripts/Publish-PortalDotNetDoc.ps1 编排'
    publishedBy      = 'HIA-ASPNETPortal 文档发布流程（Publish-PortalDotNetDoc.ps1）'
    generatedUtc     = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    entrypoint       = 'index.html'
    defaultLocale    = 'zh-CN'
    locales          = @('zh-CN', 'en')
    localeNote       = '条目页内含中英双语内容块，站内语言切换器切换显示语言；默认显示中文。已知限制：搜索索引按默认语言（中文）建立。'
    sourceFingerprint    = $sourceFingerprint
    fingerprintAlgorithm = "SHA256 over '<relativePath>|<byteLength>|<sha256(content)>' lines, joined by LF；输入缺失记 <missing>。覆盖 dotnetdoc.config.json 及其声明的全部输入。"
    fingerprintInputs    = @($fingerprintInputs)
    counts               = [ordered]@{
        entryPages = $entryPageCount
        files      = $publishedFiles.Count
        bytes      = $publishedBytes
    }
    declaredInputs = @($plan | ForEach-Object {
        [ordered]@{ half = $_.Half; locales = @($_.Locales); title = $_.Title }
    })
}
[System.IO.File]::WriteAllText((Join-Path $publishRoot 'code-docs-manifest.json'), ($manifest | ConvertTo-Json -Depth 6), $u)

Write-Host ('[OK] 已发布多页站点（split-site）到 site/api/dotnetdoc/：条目页 ' + $entryPageCount + ' 个，共 ' + $publishedFiles.Count + ' 文件 / ' + [Math]::Round($publishedBytes / 1MB, 2) + ' MB')
