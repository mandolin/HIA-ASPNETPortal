<#
.SYNOPSIS
<lang>
  <zh-CN>代码文档发布门禁：官网 `/api/jsdoc/` 快照必须与源码输入一致、形状合格，且说明页已上架并链接到它。</zh-CN>
  <en>Code documentation publishing gate: the `/api/jsdoc/` snapshot on the site must agree with its source inputs, be
  shape-clean, and be linked from a published explainer page.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>
    背景：`C-anp-P32`（`W-anp-P180`）把 HIA-Documentation-Sys 的 **JSDoc 半边**产物发布到官网
    （`site/api/jsdoc/`，3 个双语 HTML + 主题资源）。官网由签入的静态文件发布，因此这是**快照**：
    源码改了而快照没刷新，官网就会展示过期代码文档，且没有任何机制会发现。

    本门禁把四件事交给机器：
    A1 **快照存在且完整**：`site/api/jsdoc/` 必须含清单声明的全部产物（3 个 HTML + 主题资源）；
    A2 **源指纹一致（新鲜度）**：对 JSDoc 的 2 个输入文件计算 SHA256 组合指纹，与
       `code-docs-manifest.json` 记录值比对——不一致即失败（这正是"源码变了快照没刷"的形态）；
    A3 **公开面形状**：快照 HTML 不得含内部工作项编号（`W-anp-P*` / `C-anp-P*`）、不得含本地绝对路径；
    A4 **说明页已上架且链接到位**：`docs/code-documentation.md` 必须已被 `published.yml` 上架，
       且文中链接到 `/api/jsdoc/`（否则读者从官网找不到代码文档入口）。
  </zh-CN>
  <en>
    Background: `C-anp-P32` (`W-anp-P180`) publishes the **JSDoc half** of the HIA-Documentation-Sys output to the site
    (`site/api/jsdoc/`: 3 bilingual HTML pages + theme assets). The site is published from committed static files, so this
    is a **snapshot**: if the sources change but the snapshot is not refreshed, the site serves stale code documentation
    and nothing would notice.

    This gate puts four things in machine hands:
    A1 **the snapshot exists and is complete**: `site/api/jsdoc/` must contain every artifact declared by the manifest
       (3 HTML pages + theme assets);
    A2 **source fingerprint agreement (freshness)**: compute the combined SHA-256 over the two JSDoc input files and
       compare it with the value recorded in `code-docs-manifest.json` — a mismatch fails (exactly the shape of
       "sources changed, snapshot not refreshed");
    A3 **public-surface shape**: snapshot HTML must contain no internal work-item numbers (`W-anp-P*` / `C-anp-P*`) and no
       local absolute paths;
    A4 **the explainer page is published and links in**: `docs/code-documentation.md` must be published by
       `published.yml` and must link to `/api/jsdoc/` (otherwise readers cannot find the entry point from the site).
  </en>
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
    证明边界：只证明"快照与声明一致、源指纹匹配、公开面形状合格、说明页已上架并链接"；
    **不证明快照等于一次全新生成的逐字节输出**（指纹只覆盖输入文件，不覆盖 JSDoc 工具链版本与主题包版本——
    这两者变化时需人工重跑刷新）；**不覆盖 DotNetDoc 半边**（其产物仍是中间 JSON、未上架，
    已在 `code-docs-manifest.json` 的 `dotnetdocStatus` 如实登记）。
  </zh-CN>
  <en>
    Proof boundary: proves only that "the snapshot matches its declaration, the source fingerprint agrees, the
    public-surface shape is clean, and the explainer page is published and linked"; does **not** prove the snapshot is
    byte-identical to a fresh generation (the fingerprint covers only the input files, not the JSDoc toolchain or theme
    package versions — when those change, a human refresh run is required); does **not** cover the DotNetDoc half (its
    output is still intermediate JSON and is not published, honestly recorded in `code-docs-manifest.json`'s
    `dotnetdocStatus`).
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
if (Test-Path -LiteralPath $snapshotDir) {
    foreach ($f in (Get-ChildItem $snapshotDir -File -Filter '*.html' -ErrorAction SilentlyContinue)) {
        $text = [System.IO.File]::ReadAllText($f.FullName, $utf8)
        $ids = [regex]::Matches($text, '(?:W|C)-anp-P\d')
        if ($ids.Count -gt 0) {
            $failures += "A3 快照含内部工作项编号 $($ids.Count) 处（公开面不得外泄）：$($f.Name)。"
        }
        if ($text -match 'K:\\' -or $text -match 'K:/' -or $text -match 'C:\\Users') {
            $failures += "A3 快照含本地绝对路径（公开面不得外泄）：$($f.Name)。"
        }
    }
}

# ---------- A4 说明页已上架且链接到位 ----------
if (-not (Test-Path -LiteralPath $docPath -PathType Leaf)) {
    $failures += 'A4 代码文档说明页缺失：docs/code-documentation.md。'
}
else {
    $docText = [System.IO.File]::ReadAllText($docPath, $utf8)
    if ($docText -notmatch '/api/jsdoc/') {
        $failures += 'A4 说明页未链接到代码文档：docs/code-documentation.md 必须出现 /api/jsdoc/ 入口链接。'
    }
    $publishedText = [System.IO.File]::ReadAllText($publishedPath, $utf8)
    if ($publishedText -notmatch '(?m)^\s*-\s*doc:\s*code-documentation\.md\s*$') {
        $failures += 'A4 说明页未上架：site/_data/published.yml 缺少 code-documentation.md 条目（读者从官网找不到入口）。'
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
    snapshotFiles = @(Get-ChildItem $snapshotDir -File -ErrorAction SilentlyContinue | ForEach-Object { $_.Name })
    sourceFingerprint = Get-SourceFingerprint
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
Write-Host '[PASS] 代码文档发布：快照完整、源指纹一致、公开面形状合格、说明页已上架并链接。' -ForegroundColor Green
Write-Host ('统计已写入：' + $StatsPath)
exit 0
