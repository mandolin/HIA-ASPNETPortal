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

Push-Location $toolDirectory
try {
    foreach ($entry in $plan) {
        foreach ($locale in $entry.Locales) {
            $destination = Join-Path $stagingRoot ($entry.Half + '/' + $locale)
            $null = New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination)
            & $hiaCommand docs build --input $entry.Artifact --out $destination --locale $locale 2>&1 |
                Where-Object { $_ -match '\[error' } |
                ForEach-Object { Write-Host ('    ' + $_) }
            if ($LASTEXITCODE -ne 0) { throw ("渲染失败：" + $entry.Half + ' / ' + $locale + '（exit ' + $LASTEXITCODE + '）') }
            if (-not (Test-Path -LiteralPath (Join-Path $destination 'index.html') -PathType Leaf)) {
                throw ('渲染未产出 index.html：' + $entry.Half + ' / ' + $locale)
            }
        }
    }
}
finally { Pop-Location }

# <lang>
#   <zh-CN>**先清后建**：发布目录整体重建，避免上一版残留的半边/语言目录变成"幽灵产物"（读者点到 404，
#     而 manifest 里又没有它——那种不一致最难查）。</zh-CN>
#   <en>**Clear then rebuild**: the publish directory is rebuilt wholesale so a previous half/locale directory cannot
#     survive as a "ghost artifact" (a reader hitting 404 while the manifest does not list it — the hardest kind of
#     inconsistency to track down).</en>
# </lang>
if (Test-Path -LiteralPath $publishRoot -PathType Container) { Remove-Item -LiteralPath $publishRoot -Recurse -Force }
$null = New-Item -ItemType Directory -Force -Path $publishRoot

$published = @()
foreach ($entry in $plan) {
    foreach ($locale in $entry.Locales) {
        $source = Join-Path $stagingRoot ($entry.Half + '/' + $locale)
        $target = Join-Path $publishRoot ($entry.Half + '/' + $locale)
        $null = New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target)
        Copy-Item -LiteralPath $source -Destination $target -Recurse -Force
        $files = @(Get-ChildItem -LiteralPath $target -Recurse -File)
        $bytes = ($files | Measure-Object Length -Sum).Sum
        $published += [pscustomobject]@{ Half = $entry.Half; Locale = $locale; Title = $entry.Title; Files = $files.Count; Bytes = $bytes }
    }
}

# <lang>
#   <zh-CN>顶层入口页由脚本生成：按半边分组、每行给出每种语言的链接。**中文标签与英文标签都由脚本写**，
#     与本站其余页面一样是双语入口；半边自身只有英文时，该行只出英文链接（与产物的 locale 声明一致）。</zh-CN>
#   <en>The top-level entry page is generated here: grouped by half, one row per declared locale. **Both the Chinese and
#     English labels are written by the script**, matching the bilingual entry convention used elsewhere on this site;
#     when a half declares English only, that row shows the English link only — consistent with the artifact's locale
#     declaration.</en>
# </lang>
$rows = New-Object 'System.Collections.Generic.List[string]'
foreach ($entry in ($plan | Sort-Object Half)) {
    $links = @()
    foreach ($locale in $entry.Locales) {
        $href = ($entry.Half + '/' + $locale + '/')
        if ($locale -eq 'zh-CN') { $links += ('<a href="' + $href + '">中文</a>') }
        elseif ($locale -eq 'en') { $links += ('<a href="' + $href + '">English</a>') }
        else { $links += ('<a href="' + $href + '">' + $locale + '</a>') }
    }
    $rows.Add('    <tr><td>' + $entry.Half + '</td><td>' + ($links -join ' · ') + '</td></tr>')
}
$entryHtml = @"
<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>代码文档（DotNetDoc）· Code documentation (DotNetDoc)</title>
<link rel="stylesheet" href="hia-default.css">
</head>
<body>
<main>
<h1>代码文档（DotNetDoc）</h1>
<p>由源码 XML 文档注释生成；本页为各半边的语言入口。<br>Generated from the source XML documentation comments; this page lists every half and its languages.</p>
<table>
<thead><tr><th>半边 / Half</th><th>语言 / Languages</th></tr></thead>
<tbody>
$($rows -join "`n")
</tbody>
</table>
</main>
</body>
</html>
"@
[System.IO.File]::WriteAllText((Join-Path $publishRoot 'index.html'), $entryHtml, [System.Text.UTF8Encoding]::new($false))

# <lang>
#   <zh-CN>入口页自带一份主题样式（从任一渲染产物拷贝），保证不依赖某个半边目录是否存在。</zh-CN>
#   <en>The entry page carries its own copy of the theme stylesheet (copied from any rendered artifact) so it does not
#     depend on a particular half directory existing.</en>
# </lang>
$assetSource = Get-ChildItem -LiteralPath $publishRoot -Recurse -File -Filter 'hia-default.css' | Select-Object -First 1
if ($assetSource) { Copy-Item -LiteralPath $assetSource.FullName -Destination (Join-Path $publishRoot 'hia-default.css') -Force }

$manifest = [ordered]@{
    schemaVersion  = 1
    generator      = '@hia-doc/dotnetdoc-runner（hia-dotnetdoc）+ @hia-doc/renderer-html（hia docs build），由 dev/scripts/Publish-PortalDotNetDoc.ps1 编排'
    generatedUtc   = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    publishedBy    = 'HIA-ASPNETPortal 文档发布流程（Publish-PortalDotNetDoc.ps1）'
    localePolicy   = '每个半边按其在 HIA 工件中声明的 locales 渲染；未声明 zh-CN 的半边（surface、project）为结构描述，只有英文。'
    halves         = @($published | ForEach-Object {
            [ordered]@{
                half       = $_.Half
                locale     = $_.Locale
                title      = $_.Title
                entrypoint = ($_.Half + '/' + $_.Locale + '/index.html')
                files      = $_.Files
                bytes      = $_.Bytes
            }
        })
    totalBytes     = ($published | Measure-Object Bytes -Sum).Sum
}
[System.IO.File]::WriteAllText((Join-Path $publishRoot 'code-docs-manifest.json'), ($manifest | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))

$totalBytes = ($published | Measure-Object Bytes -Sum).Sum
Write-Host ('[OK] 已发布 ' + $published.Count + ' 个半边×语言组合到 site/api/dotnetdoc/，共 ' + [Math]::Round($totalBytes / 1MB, 2) + ' MB')
foreach ($item in ($published | Sort-Object Half, Locale)) {
    Write-Host ('     ' + $item.Half.PadRight(26) + $item.Locale.PadRight(8) + [Math]::Round($item.Bytes / 1MB, 2).ToString().PadLeft(8) + ' MB  ' + $item.Files + ' 文件')
}
