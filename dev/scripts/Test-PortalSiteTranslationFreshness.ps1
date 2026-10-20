<#
.SYNOPSIS
<lang>
  <zh-CN>官网译本新鲜度门禁：上架文档的每个模块级小节，其英文译本必须有对应结构（而非只在散文里提一句）。</zh-CN>
  <en>Site translation freshness gate: every module-level section of a published doc must have a corresponding structure in its
  English translation, rather than a single prose mention.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>
    背景：`C-anp-P32` 的 `W-anp-P177` 机器化审计发现，`site/_translations/user-guide.md` 的
    `## Common modules` **只有一句散文**（"include announcements, contacts, ..."），而中文版有 16 个 `###`
    模块小节——即 `W-anp-P152` 的 7 个 + `C-anp-P31` 的 4 个模块小节**英文版一个都没有**。
    现有 `Build-PortalSite.ps1` 的"缺译即 Fail"只校验**翻译文件存在**，不校验**内容新鲜度**，
    所以这类滞后一路绿灯。

    做法（语言无关，因为中英标题不可直接比对）：
    A1 从中文源的小节标题中提取**结构锚点**——标题括号内的 ASCII 标识符
       （`公告（Announcements）` → `Announcements`；`登录（Signin）` → `Signin`）；
       要求每个锚点在英文译本的**标题行或反引号代码**中**大小写敏感**地出现。
       ——散文里提一句不算"逐项说明"。
    A2 导航分区必须中英标题齐备（按行解析）。
    A3 未上架文档仅作软指标列出（是否上架由内容决策，不属本门禁判定）。
  </zh-CN>
  <en>
    Background: the machine-driven audit in `W-anp-P177` (`C-anp-P32`) found that
    `site/_translations/user-guide.md`'s `## Common modules` was **a single prose sentence**
    ("include announcements, contacts, ...") while the Chinese source had 16 `###` module sections — i.e. the 7 module
    sections from `W-anp-P152` plus the 4 from `C-anp-P31` were **entirely absent** from the English version.
    The existing "missing translation fails" check in `Build-PortalSite.ps1` only verifies that a translation **file
    exists**, never its **freshness**, so this kind of lag stayed green.

    Method (language-independent, because Chinese and English headings cannot be compared directly):
    A1 extract **structural anchors** from the Chinese source's section headings — ASCII identifiers inside the heading's
       full-width parentheses (`公告（Announcements）` → `Announcements`; `登录（Signin）` → `Signin`); each anchor must
       appear **case-sensitively** in a **heading line** of the English translation.
       — a prose mention does not count as item-by-item documentation, and neither does a code reference elsewhere
       (an injection that renamed `### HtmlModule` to `### HTML text module` still passed while the anchor survived in a
       backticked `DesktopModules/HtmlModule.ascx` elsewhere, so coverage is heading-level only).
    A2 navigation sections must carry both Chinese and English titles (parsed line by line).
    A3 unpublished docs are listed as a soft metric only (whether to publish is a content decision, not this gate's call).
  </en>
</lang>

.PARAMETER StatsPath
<lang>
  <zh-CN>统计 JSON 的输出路径（可选）。</zh-CN>
  <en>Output path for the stats JSON (optional).</en>
</lang>

.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\Test-PortalSiteTranslationFreshness.ps1'</zh-CN>
  <en>& 'dev\scripts\Test-PortalSiteTranslationFreshness.ps1'</en>
</lang>

.NOTES
<lang>
  <zh-CN>
    证明边界：只证明"中文小节标题中的 ASCII 结构锚点在英文译本的**标题行**中可对应"与"导航分区中英标题齐备"；
    **不证明英文译文的语义正确或完整**（语义由人工复核承载）；**不证明锚点所在小节的其余内容已翻译**
    （只查标题级结构存在，这是有意的低门槛口径——先保证"节在"，再靠人工保证"译对"）；
    若某节翻译后**改写了标题措辞且不含原 ASCII 标识符**，会被误判为缺失（宁误报不漏报）。
  </zh-CN>
  <en>
    Proof boundary: proves only that "ASCII structural anchors in Chinese section headings have a counterpart in the English
    translation's headings/code" and that "navigation sections carry both titles"; does **not** prove the English prose is
    semantically correct or complete (semantics belong to human review); does **not** prove the rest of a section is
    translated (only structural presence — an intentionally low bar: first ensure the section exists, then rely on human
    review for translation quality); if a translated heading drops the original ASCII identifier, it is reported missing
    (prefer false positives over false negatives).
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
$docsDir = Join-Path $repoRoot 'docs'
$siteDir = Join-Path $repoRoot 'site'
$transDir = Join-Path $siteDir '_translations'

$failures = @()
$softNotes = @()
$staleDocs = @()

# ---------- A1 译本新鲜度 ----------
$yaml = [System.IO.File]::ReadAllText((Join-Path $siteDir '_data\published.yml'), $utf8)
$entries = @()
foreach ($m in [regex]::Matches($yaml, '(?m)^\s*-\s*doc:\s*(?<doc>[^\r\n]+?)\r?\n\s*path:\s*(?<path>[^\r\n]+?)(?:\r?\n\s*parent:\s*(?<parent>[^\r\n]+?))?\s*$')) {
    $entries += [pscustomobject]@{ doc = $m.Groups['doc'].Value.Trim(); path = $m.Groups['path'].Value.Trim() }
}

foreach ($e in $entries) {
    $zhPath = Join-Path $docsDir $e.doc
    $enPath = Join-Path $transDir $e.doc
    if (-not (Test-Path -LiteralPath $zhPath)) { continue }
    if (-not (Test-Path -LiteralPath $enPath)) {
        $failures += "A1 上架文档缺少英文译本：$($e.doc)（构建的缺译即 Fail 之外，本门禁同样要求译本存在）。"
        continue
    }

    $zh = [System.IO.File]::ReadAllText($zhPath, $utf8)
    $en = [System.IO.File]::ReadAllText($enPath, $utf8)

    # 结构锚点：中文小节标题括号内的 ASCII 标识符（语言无关）
    $anchors = @()
    foreach ($hm in [regex]::Matches($zh, '(?m)^#{2,4}\s+.*?（\s*([A-Za-z][A-Za-z0-9_.-]*)\s*）')) {
        $a = $hm.Groups[1].Value
        if ($anchors -notcontains $a) { $anchors += $a }
    }
    if ($anchors.Count -eq 0) { continue }

    # 覆盖口径（`W-anp-P177` / `W-anp-P178` 三处教训的固化）：
    # ①只认**标题行**——首版把"标题行或反引号代码"都算覆盖，结果注入把 `### HtmlModule` 改名为
    #   `### HTML text module` 时，锚点仍能靠别处的反引号代码 `DesktopModules/HtmlModule.ascx` 命中，
    #   门禁**放行**（exit 0）。"小节结构对应"的语义就是标题级，代码引用不算。
    # ②**大小写敏感**匹配——PowerShell `-notmatch` 默认忽略大小写，`W-anp-P177` 首版审计曾因此把
    #   6 个模块蒙成"已覆盖"（英文散文一句小写列举）。
    # ③中文侧只从**小节标题括号内**取 ASCII 标识符（语言无关），不扫描正文。
    $enHeadingLines = @()
    foreach ($line in ($en -split "`n")) {
        if ($line -match '^\s*#{1,6}\s') { $enHeadingLines += $line }
    }
    $enCoverageText = $enHeadingLines -join "`n"

    $missing = @($anchors | Where-Object {
        -not [regex]::IsMatch($enCoverageText, [regex]::Escape($_), [System.Text.RegularExpressions.RegexOptions]::None)
    })
    if ($missing.Count -gt 0) {
        $staleDocs += $e.doc
        $failures += ("A1 英文译本缺少小节结构：$($e.doc) 缺失锚点 $($missing.Count) 个（" + ($missing -join '、') + '）——中文版有该小节而英文版未对应（散文提及不计）。')
    }
}

# ---------- A2 导航分区中英标题齐备（按行解析，勿用负向预查找） ----------
# <lang><zh-CN>`W-anp-P177` 首版审计用负向预查找正则判断"缺 title_en"，因**回溯**把「产品概览」截成「产品概」，
# 误报 8 个分区缺英文标题（实测 8/8 齐全）。故此处按行解析：`- title:` 起一条，下一非空行须为 `title_en:`。</zh-CN>
# <en>The first audit version in `W-anp-P177` used a negative-lookahead regex to detect a missing `title_en`; **backtracking**
# truncated 「产品概览」 to 「产品概」, falsely reporting 8 sections without English titles (measurement showed 8/8 present).
# Hence line-by-line parsing here: `- title:` starts a section and the next non-empty line must be `title_en:`.</en></lang>
$nav = [System.IO.File]::ReadAllText((Join-Path $siteDir '_data\navigation.yml'), $utf8)
$sectionCount = 0
$pendingZh = $null
foreach ($raw in ($nav -split "`n")) {
    $line = $raw.TrimEnd("`r")
    if ($line -match '^\s*#') { continue }
    if ($line -match '^-\s*title:\s*(.+)$') {
        if ($pendingZh) { $failures += "A2 导航分区缺少英文标题：$($pendingZh)（双语并行要求 title 与 title_en 齐备）。" }
        $pendingZh = $Matches[1].Trim(); continue
    }
    if ($line -match '^\s*title_en:\s*(.+)$') {
        if ($pendingZh) { $sectionCount++; $pendingZh = $null }
        continue
    }
}
if ($pendingZh) { $failures += "A2 导航分区缺少英文标题：$($pendingZh)（双语并行要求 title 与 title_en 齐备）。" }

# ---------- A3 未上架文档（软指标） ----------
$publishedDocs = @($entries | ForEach-Object { $_.doc })
$unpublished = @()
foreach ($f in (Get-ChildItem $docsDir -File -Filter '*.md')) {
    if ($publishedDocs -notcontains $f.Name) { $unpublished += $f.Name }
}
if ($unpublished.Count -gt 0) {
    $softNotes += "[soft] 未上架文档 $($unpublished.Count) 篇：" + ($unpublished -join '、') + '（是否上架属内容决策，本门禁只提示）。'
}

# ---------- 统计与输出 ----------
if ([string]::IsNullOrWhiteSpace($StatsPath)) {
    $statsDir = Join-Path $repoRoot 'temp\p178'
    New-Item -ItemType Directory -Force -Path $statsDir | Out-Null
    $StatsPath = Join-Path $statsDir 'translation-freshness-stats.json'
}
$report = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    publishedDocs = $entries.Count
    staleDocs = @($staleDocs)
    navSections = $sectionCount
    unpublishedDocs = @($unpublished)
    failures = @($failures)
}
[System.IO.File]::WriteAllText($StatsPath, ($report | ConvertTo-Json -Depth 6), $utf8)

if ($failures.Count -gt 0) {
    Write-Host '[FAIL] 官网译本新鲜度：' -ForegroundColor Red
    foreach ($f in $failures) { Write-Host ('  - ' + $f) -ForegroundColor Red }
    Write-Host ('统计已写入：' + $StatsPath)
    exit 1
}

foreach ($s in $softNotes) { Write-Host ('  ' + $s) }
Write-Host '[PASS] 官网译本新鲜度：上架文档的小节结构在英文译本中均有对应，导航分区中英标题齐备。' -ForegroundColor Green
Write-Host ('统计已写入：' + $StatsPath)
exit 0
