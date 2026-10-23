<#
.SYNOPSIS
<lang>
  <zh-CN>从 `CHANGELOG.md` 的指定版本段自动生成 GitHub Release 说明正文（markdown）。</zh-CN>
  <en>Generates a GitHub Release body (markdown) from a given version section of `CHANGELOG.md`.</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>把 `CHANGELOG.md` 里 `## [vX.Y.Z] - DATE` 段的正文**逐字**提取为 Release 说明——粗体摘要与
  `### 新增/变更/修复/验证` 等子节原样保留。**忠实第一**：条目集合与 CHANGELOG 对应版本段一致，
  不编造、不遗漏、不改写（只做整段的空白修剪与一段来源标注）。

  **与 `New-PortalReleaseManifest.ps1` 的职责区分**：后者扫描 FileSystem 发布目录、产出文件清单 +
  SHA256 + 排除项核对，是"发布包内容物清单"；本脚本产出"给人看的版本变更说明"，二者互不替代。

  **前置条件**：仓库根 `CHANGELOG.md` 存在且含目标版本段。本脚本不连接网络、不创建或修改 Release
  （实际发 Release 由人/工作流调用 `gh`），只产出说明文本；不写入任何敏感信息。

  **退出码**：0 成功产出；1 目标版本段不存在或 CHANGELOG 缺失（明确报错并列出可用版本，不静默产出空说明）。</zh-CN>
  <en>Extracts the body of the `## [vX.Y.Z] - DATE` section in `CHANGELOG.md` **verbatim** as release notes — the
  bold summary and the `### Added/Changed/Fixed/Verified` subsections are preserved as-is. **Faithfulness first**:
  the entry set matches the corresponding CHANGELOG version section exactly — no fabrication, no omission, no rewrite
  (only whole-block whitespace trimming and a single provenance line).

  **Division of duty vs `New-PortalReleaseManifest.ps1`**: the latter scans a FileSystem publish directory and produces
  a file inventory + SHA256 + exclusion checks, i.e. a "what is inside the package" manifest; this script produces the
  "human-readable what changed" notes. The two do not substitute for each other.

  **Prerequisites**: an existing root `CHANGELOG.md` containing the target version section. This script does not use the
  network, does not create or modify a Release (actually publishing is done by a human/workflow via `gh`), only produces
  the notes text, and writes no sensitive information.

  **Exit codes**: 0 produced successfully; 1 the target version section is missing or CHANGELOG is absent (a clear error
  listing the available versions — never a silent empty output).</en>
</lang>
.PARAMETER Version
<lang>
  <zh-CN>目标版本号，形如 `v1.3.0` 或 `1.3.0`（`v` 前缀可省略，内部归一化）。</zh-CN>
  <en>Target version, for example `v1.3.0` or `1.3.0` (the `v` prefix is optional and normalised internally).</en>
</lang>
.PARAMETER OutputPath
<lang>
  <zh-CN>输出文件路径；省略时写到标准输出（便于管道或工作流重定向）。</zh-CN>
  <en>Output file path; when omitted, writes to standard output (handy for piping or redirection in a workflow).</en>
</lang>
.PARAMETER RepoRoot
<lang>
  <zh-CN>仓库根目录；留空时取脚本所在目录的上两级目录（`dev\scripts` 的上两级即仓库根）。</zh-CN>
  <en>Repository root; when empty, two levels above the script directory (`dev\scripts` -> repository root).</en>
</lang>
.PARAMETER NoProvenance
<lang>
  <zh-CN>默认在正文前加一行来源标注（版本段 + 发布日期）；指定本开关则只输出逐字段正文。</zh-CN>
  <en>By default a one-line provenance header (version section + release date) is prepended; pass this switch to emit
  only the verbatim section body.</en>
</lang>
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$OutputPath = '',

    [string]$RepoRoot = '',

    [switch]$NoProvenance
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
}

# <lang>
#   <zh-CN>以 UTF-8 无 BOM 写文件，与仓库文本约定一致（避免 BOM 进入 Release 正文）。</zh-CN>
#   <en>Write files as UTF-8 without a BOM, matching the repository text convention (no BOM leaking into the Release body).</en>
# </lang>
$utf8 = [System.Text.UTF8Encoding]::new($false)

$changelogPath = Join-Path $RepoRoot 'CHANGELOG.md'
if (-not (Test-Path -LiteralPath $changelogPath -PathType Leaf)) {
    Write-Error ('找不到 CHANGELOG.md：' + $changelogPath)
    exit 1
}

# <lang>
#   <zh-CN>归一化目标版本：去掉可选 `v` 前缀，得到 `X.Y.Z` 供精确匹配段标题。</zh-CN>
#   <en>Normalise the target version: strip the optional `v` prefix to obtain `X.Y.Z` for exact section-heading matching.</en>
# </lang>
$normalized = ($Version -replace '^[vV]', '').Trim()

$lines = [System.IO.File]::ReadAllLines($changelogPath, $utf8)

# <lang>
#   <zh-CN>定位 `## [v?X.Y.Z] - DATE` 段标题；记下其行号与发布日期（供来源标注）。</zh-CN>
#   <en>Locate the `## [v?X.Y.Z] - DATE` section heading; record its line index and release date (for the provenance header).</en>
# </lang>
$headingIndex = -1
$releaseDate = ''
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match ('^##\s+\[v?' + [regex]::Escape($normalized) + '\]\s*-\s*(\d{4}-\d{2}-\d{2})\s*$')) {
        $headingIndex = $i
        $releaseDate = $Matches[1]
        break
    }
}

if ($headingIndex -lt 0) {
    # <lang>
    #   <zh-CN>列出现有版本段帮调用者核对拼写——比一句"未找到"有用得多。</zh-CN>
    #   <en>List the available version sections to help the caller spot a typo — far more useful than a bare "not found".</en>
    # </lang>
    $available = @()
    foreach ($l in $lines) {
        if ($l -match '^##\s+\[([^\]]+)\]') { $available += $Matches[1] }
    }
    Write-Error ('CHANGELOG.md 中没有版本段 [' + $Version + ']（归一化 ' + $normalized + '）。现有版本段：' + ($available -join '、'))
    exit 1
}

# <lang>
#   <zh-CN>收集段正文：从标题的下一行起，直到下一个 `## [` 段标题或文件末尾。逐字保留子节与列表项，
#     不做任何改写——这是"无编造、无遗漏"的保证。</zh-CN>
#   <en>Collect the section body: from the line after the heading up to the next `## [` section heading or EOF. Sub-sections
#     and list items are kept verbatim, with no rewriting — this is the "no fabrication, no omission" guarantee.</en>
# </lang>
$body = New-Object 'System.Collections.Generic.List[string]'
for ($i = $headingIndex + 1; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^##\s+\[') { break }
    $body.Add($lines[$i])
}

# <lang>
#   <zh-CN>修剪整段的首尾空白行（保留内部结构），避免 Release 正文首尾出现多余空行。</zh-CN>
#   <en>Trim leading/trailing blank lines of the whole block (inner structure preserved), avoiding stray blank lines at the
#     start/end of the Release body.</en>
# </lang>
while ($body.Count -gt 0 -and $body[0].Trim() -eq '') { $body.RemoveAt(0) }
while ($body.Count -gt 0 -and $body[$body.Count - 1].Trim() -eq '') { $body.RemoveAt($body.Count - 1) }

if ($body.Count -eq 0) {
    Write-Error ('版本段 [' + $Version + '] 正文为空——没有可生成的 Release 说明。')
    exit 1
}

$sections = @()
if (-not $NoProvenance) {
    $dateText = if ($releaseDate) { '，发布日期 ' + $releaseDate } else { '' }
    $sections += ('> 本文由 `New-PortalReleaseNotes.ps1` 从 `CHANGELOG.md` 的 `[' + $Version + ']` 段自动提取' + $dateText + '；条目与源一致，未编造、未遗漏。')
    $sections += ''
}
$sections += ($body -join "`r`n")

$content = ($sections -join "`r`n")

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    Write-Output $content
}
else {
    $outFull = [System.IO.Path]::GetFullPath($OutputPath)
    $outDir = Split-Path -Parent $outFull
    if ($outDir -and -not (Test-Path -LiteralPath $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }
    [System.IO.File]::WriteAllText($outFull, ($content + "`r`n"), $utf8)
    Write-Host ('Release 说明已写入：' + $outFull + '（来源版本段 ' + $Version + ($(if ($releaseDate) { ' / ' + $releaseDate } else { '' })) + '）')
}

exit 0
