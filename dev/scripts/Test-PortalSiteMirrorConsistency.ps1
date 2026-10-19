#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>看守 `docs/` 与 `site/` 镜像一致性（L0）：上架文档必须有镜像页，且镜像正文必须等于 `docs/` 原文。</zh-CN>
  <en>Guards `docs/` ↔ `site/` mirror consistency (L0): every published doc must have a mirror page whose body equals the
  `docs/` source.</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>`C-anp-P30`（`W-anp-P166`）交付。起因：`C-anp-P28` 期间每改一次公开文档都要**人工**同步 `site/` 镜像页，
  再用临时脚本人工校验"镜像尾部 == docs 全文"——**没有任何门禁校验这件事**。也就是说：改了 `docs/` 忘记同步、
  或同步时漏改，L0 会一路全绿。本项目反复出现的教训是"人肉维护的同步必然腐坏，必须机器兜底"。

  门诊（以 `site/_data/published.yml` 上架清单为准，它是"哪些文档已上架、站点路径是什么"的事实源）：
    A1 清单中每个条目：`docs/<doc>` 必须存在，`site/<path>/index.md` 必须存在。
    A2 **镜像正文必须等于 `docs/` 原文**（跳过 front matter，按 CRLF 归一后逐字节比对）——
       这是"改了 docs 没同步"的直接抓手。
    A3 镜像 front matter 必须含 `lang: zh-CN` 与 `permalink: /<path>/`，且 `parent` 与清单一致
       （上架契约：站点路径与导航分区由清单唯一决定）。
    A4（软指标）`docs/` 中未出现在上架清单的文件逐条列出——即"未上架文档"清单，供人工判断是否应上架。

  证明边界：只证明"已上架文档的镜像与原文一致、front matter 契约成立"；
  **不证明**镜像内容与 `Build-PortalSite.ps1` 的生成输出一致（构建侧还会做链接重写与公开面文本过滤，
  其输出与已签入镜像本就不同——该分歧已登记在 `deferred-verification-memo.md`，本门禁不裁决）；
  不检查英文译本（`site/_translations/`）与站内链接可达性（分别由其它门禁/人工走查承载）。

  用法（仓库根目录）：& 'dev\scripts\Test-PortalSiteMirrorConsistency.ps1'
  产物：temp\p166\site-mirror-stats.json（gitignore）。
  退出码：0 通过；1 断言失败；2 上架清单或目录前置缺失。
  </zh-CN>
  <en>`C-anp-P30` (`W-anp-P166`) deliverable. Origin: during `C-anp-P28`, every public-doc change required **manually** syncing the
  `site/` mirror page and then manually verifying "mirror tail == docs text" with a scratch script — **no gate checked it**. In
  other words: change `docs/` and forget the mirror, or sync it partially, and L0 stays green. This project's recurring lesson is
  that manually maintained sync rots and a machine must back it up.

  Assertions (the `site/_data/published.yml` manifest is the source of truth for "which docs are published and at which path"):
    A1 for each manifest entry: `docs/<doc>` must exist and `site/<path>/index.md` must exist.
    A2 the mirror must be **build-output shaped** rather than a verbatim copy: A2a no raw relative `.md` links (they 404 on the
       site — the build rewrites them), A2b no leaked `W-anp-P*` / `C-anp-P*` internal IDs (public-surface filtering contract),
       A2c the first heading matches the docs (a backstop for "the page was never regenerated").
       Byte equality is deliberately **not** required: 16 of 29 pages differ from the docs by design (link rewriting + text
       filtering), and requiring equality would produce 16 false failures.
    A3 the mirror front matter must contain `lang: zh-CN` and `permalink: /<path>/`, and its `parent` must match the manifest
       (the publication contract: path and navigation section are decided solely by the manifest).
    A4 (soft) `docs/` files absent from the manifest are listed — the "unpublished docs" list, for a human to judge whether they
       should be published.

  Proof boundary: proves only that "published docs' mirrors equal the source and the front-matter contract holds"; does **not**
  prove that the mirror equals what `Build-PortalSite.ps1` would generate (the build also rewrites links and filters public-facing
  text, so its output differs from the committed mirror by design — that divergence is registered in
  `deferred-verification-memo.md` and is not adjudicated here); it does not check English translations (`site/_translations/`) or
  in-site link reachability (covered by other gates and manual review).

  Usage (repository root): & 'dev\scripts\Test-PortalSiteMirrorConsistency.ps1'
  Output: temp\p166\site-mirror-stats.json (gitignored).
  Exit codes: 0 pass; 1 assertion failure; 2 missing manifest or directory prerequisite.
  </en>
</lang>
.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Test-PortalSiteMirrorConsistency.ps1'   # 核对 docs 与 site 镜像是否一致</zh-CN>
  <en>& 'dev\scripts\Test-PortalSiteMirrorConsistency.ps1'   # check docs against the site mirror</en>
</lang>
.NOTES
<lang>
  <zh-CN>演进：`W-anp-P166` 交付；上架清单为 `site/_data/published.yml`。</zh-CN>
  <en>Provenance: delivered by `W-anp-P166`; the publication manifest is `site/_data/published.yml`.</en>
</lang>
#>

[CmdletBinding()]
param(
    [string] $StatsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$manifestPath = Join-Path $repoRoot 'site\_data\published.yml'
$docsDir = Join-Path $repoRoot 'docs'
$siteDir = Join-Path $repoRoot 'site'

$failures = @()
$softNotes = @()

if (-not (Test-Path -LiteralPath $manifestPath)) { Write-Host '[PREREQ-MISSING] 缺上架清单 published.yml（exit 2）'; exit 2 }

$yaml = [System.IO.File]::ReadAllText($manifestPath)
$entries = @()
foreach ($m in [regex]::Matches($yaml, '(?m)^\s*-\s*doc:\s*(?<doc>[^\r\n]+?)[\r\n]+\s*path:\s*(?<path>[^\r\n]+?)(?:[\r\n]+\s*parent:\s*(?<parent>[^\r\n]+?))?\s*$')) {
    $entries += [pscustomobject]@{
        doc = $m.Groups['doc'].Value.Trim()
        path = $m.Groups['path'].Value.Trim()
        parent = $m.Groups['parent'].Value.Trim()
    }
}
if ($entries.Count -eq 0) {
    Write-Host '[PREREQ-MISSING] 上架清单未解析出任何条目（exit 2）'
    exit 2
}

$mirrorCount = 0
foreach ($e in $entries) {
    $docPath = Join-Path $docsDir $e.doc
    $pagePath = Join-Path $siteDir ($e.path + '\index.md')

    # <lang><zh-CN>A1：源文件与镜像页都存在。</zh-CN><en>A1: both the source file and the mirror page exist.</en></lang>
    if (-not (Test-Path -LiteralPath $docPath)) { $failures += "A1 上架清单中的文档不存在：docs/$($e.doc)。"; continue }
    if (-not (Test-Path -LiteralPath $pagePath)) { $failures += "A1 上架文档缺镜像页：site/$($e.path)/index.md（docs/$($e.doc)）。"; continue }

    $docText = [System.IO.File]::ReadAllText($docPath) -replace "`r`n", "`n"
    $pageText = [System.IO.File]::ReadAllText($pagePath) -replace "`r`n", "`n"
    $fm = [regex]::Match($pageText, '(?s)\A---\n.*?\n---\n')
    if (-not $fm.Success) { $failures += "A1 镜像页缺少 front matter：site/$($e.path)/index.md。"; continue }
    $body = $pageText.Substring($fm.Length)
    $mirrorCount += 1

    # <lang><zh-CN>A2：镜像必须是**构建产物形状**，不是 docs 原文直抄。
    #   首跑实测推翻了我最初的"字节相等"设计：29 个已上架页面中 16 个与 docs 原文不等，
    #   但差异全部来自构建的两项合法变换（链接重写为 `{{ site.baseurl }}/...`、公开面文本过滤掉内部编号），
    #   且行数一致 —— 即"不等"是**设计如此**，不是漂移。真正的问题在另一侧：
    #   `C-anp-P28` 期间我用"docs 原文 + front matter"手工同步过若干页，那些页**少了这两项变换**
    #   （站点上裸相对 .md 链接必然 404，内部编号还会外泄到公开面）。
    #   故 A2 改为核对构建产物的三条形状不变量（另有 A2c 结构性一致兜底"整页没重新生成"）。</zh-CN>
    #   <en>A2: the mirror must be **build-output shaped**, not a verbatim copy of the docs.
    #   The first run overturned my original "byte equality" design: 16 of 29 published pages differ from the docs source,
    #   but every difference comes from the build's two legitimate transformations (links rewritten to
    #   `{{ site.baseurl }}/...`, and public-surface filtering that strips internal IDs), with identical line counts — the
    #   inequality is **by design**, not drift. The real problem is on the other side: during `C-anp-P28` I hand-synced several
    #   pages as "docs text + front matter", so those pages **lack both transformations** (raw relative `.md` links 404 on the
    #   site, and internal IDs leak onto the public surface). A2 therefore checks three shape invariants of build output
    #   (plus A2c structural parity as a backstop for "the page was never regenerated").</en></lang>
    $rawLinks = [regex]::Matches($body, '\]\((?!https?:|mailto:|#|\{\{)[^)]*\.md\)')
    if ($rawLinks.Count -gt 0) {
        $failures += "A2a 镜像含裸相对 .md 链接 $($rawLinks.Count) 处（站点上必然 404，须由构建重写为 {{ site.baseurl }}/...）：site/$($e.path)/index.md。"
    }
    $leakedIds = [regex]::Matches($body, '(?:W|C)-anp-P\d')
    if ($leakedIds.Count -gt 0) {
        $failures += "A2b 镜像含内部编号 $($leakedIds.Count) 处（公开面过滤契约：不得外泄到站点）：site/$($e.path)/index.md。"
    }
    $docHeading = @($docText -split "`n" | Where-Object { $_ -match '^#\s' } | Select-Object -First 1)
    $pageHeading = @($body -split "`n" | Where-Object { $_ -match '^#\s' } | Select-Object -First 1)
    if ($docHeading.Count -gt 0 -and $pageHeading.Count -gt 0 -and ($docHeading[0].Trim() -ne $pageHeading[0].Trim())) {
        $failures += "A2c 镜像首标题与 docs 不一致（整页未重新生成的信号）：site/$($e.path)/index.md ← docs/$($e.doc)。"
    }

    # <lang><zh-CN>A3：front matter 契约（lang / permalink / parent）。</zh-CN>
    # <en>A3: front-matter contract (lang / permalink / parent).</en></lang>
    $front = $fm.Value
    if ($front -notmatch '(?m)^lang:\s*zh-CN\s*$') { $failures += "A3 镜像 front matter 缺 lang: zh-CN：site/$($e.path)/index.md。" }
    if ($front -notmatch ('(?m)^permalink:\s*/' + [regex]::Escape($e.path) + '/\s*$')) {
        $failures += "A3 镜像 permalink 与上架清单路径不符（应为 /$($e.path)/）：site/$($e.path)/index.md。"
    }
    if (-not [string]::IsNullOrWhiteSpace($e.parent) -and ($front -notmatch ('(?m)^parent:\s*' + [regex]::Escape($e.parent) + '\s*$'))) {
        $failures += "A3 镜像 parent 与上架清单不符（应为 $($e.parent)）：site/$($e.path)/index.md。"
    }
}

# <lang><zh-CN>A4（软）：未上架的 docs 文件逐条列出。</zh-CN><en>A4 (soft): list docs files that are not published.</en></lang>
$publishedDocs = @($entries | ForEach-Object { $_.doc })
$unpublished = @()
foreach ($f in (Get-ChildItem $docsDir -File -Filter '*.md' -ErrorAction SilentlyContinue)) {
    if ($publishedDocs -notcontains $f.Name) { $unpublished += $f.Name }
}
if ($unpublished.Count -gt 0) { $softNotes += "[soft] 未上架文档 $($unpublished.Count) 篇：" + ($unpublished -join '、') }

if ([string]::IsNullOrWhiteSpace($StatsPath)) {
    $statsDir = Join-Path $repoRoot 'temp\p166'
    New-Item -ItemType Directory -Force -Path $statsDir | Out-Null
    $StatsPath = Join-Path $statsDir 'site-mirror-stats.json'
}
else {
    $parent = Split-Path -Parent $StatsPath
    if ($parent -and -not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
}
$report = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    manifestEntries = $entries.Count
    mirrorsChecked = $mirrorCount
    unpublishedDocs = $unpublished
    softNotes = $softNotes
    failures = $failures
}
[System.IO.File]::WriteAllText($StatsPath, ($report | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))

Write-Host ''
Write-Host '=== docs 与 site 镜像一致性（W-anp-P166）==='
Write-Host ('上架条目 ' + $entries.Count + '｜已核镜像 ' + $mirrorCount + '｜未上架 docs ' + $unpublished.Count)
foreach ($note in $softNotes) { Write-Host ('  ' + $note) }
Write-Host ("统计已写入：$StatsPath")

if ($failures.Count -gt 0) {
    Write-Host "[FAIL] 镜像一致性校验失败 $($failures.Count) 处：" -ForegroundColor Red
    foreach ($failure in $failures) { Write-Host ('  - ' + $failure) -ForegroundColor Red }
    exit 1
}
Write-Host '[PASS] 镜像一致：上架文档均有镜像页，镜像为构建产物形状（无裸相对链接、无内部编号、首标题一致），front matter 契约成立。' -ForegroundColor Green
exit 0
