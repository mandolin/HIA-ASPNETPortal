<#
.SYNOPSIS
    版本锚点同步（半自动）：从单一来源（版本号 + 日期）把全部版本锚点一次性写入。

.DESCRIPTION
    `C-anp-P39` / `W-anp-P218`。把分散在 8 个文件里的版本锚点（CHANGELOG 条目 / 5 个程序集 / README 中英 /
    versioning §6 / 首页中英统计）从"人工逐处改"变成"一个脚本写"。
    - 升主版本（是否该 `v2.0.0`）仍由人决定——本脚本只消除"写了却漏写/写错"的漂移，不引入新版本策略。
    - 不做全自动封版：本脚本写锚点，打 tag 与发 Release 仍由人下令（`release-gate.yml` 是封版前检查点）。
    - `-WhatIf` 只打印将改的行、不落盘；重复运行同版本号不产生差异（幂等）。

.PARAMETER Version
    目标语义化版本号，形如 `v1.4.0`（含前导 `v`）。

.PARAMETER Date
    封版日期，形如 `2026-10-23`（短横线，与 CHANGELOG / versioning §6 现有格式一致）。

.PARAMETER WhatIf
    通用参数（由 `[CmdletBinding(SupportsShouldProcess)]` 提供）：预览模式，打印每个文件将改的行、不写入任何文件。

.PARAMETER RepoRoot
    仓库根（含 CHANGELOG.md、src/、docs/、site/）。默认取本脚本上两级目录。
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^v\d+\.\d+\.\d+$')]
    [string] $Version,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d{4}-\d{2}-\d{2}$')]
    [string] $Date,

    [string] $RepoRoot
)

# <lang>
#   <zh-CN>路径解析与前置校验：仓库根必须存在且含 CHANGELOG.md；目标版本段不能与已有 CHANGELOG 条目重复
#     （防止把已经封过的版本又封一次）。</zh-CN>
#   <en>Path resolution and preconditions: the repo root must exist with a CHANGELOG.md; the target version section
#     must not already exist in CHANGELOG (prevents re-sealing an already-sealed version).</en>
# </lang>
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
}
if (-not (Test-Path -LiteralPath $RepoRoot -PathType Container)) {
    throw ('仓库根不存在：' + $RepoRoot)
}
$changelogPath = Join-Path $RepoRoot 'CHANGELOG.md'
if (-not (Test-Path -LiteralPath $changelogPath -PathType Leaf)) {
    throw ('找不到 CHANGELOG.md：' + $changelogPath)
}
$semver = $Version.TrimStart('v')
$assemblyVersion = ($semver + '.0')

function Invoke-AnchorEdit {
    param(
        [string] $FilePath,
        [string] $DisplayLabel,
        [scriptblock] $Transform,
        [string] $FindPattern
    )
    if (-not (Test-Path -LiteralPath $FilePath -PathType Leaf)) {
        Write-Host ('  [SKIP] ' + $DisplayLabel + '：文件不存在 ' + $FilePath)
        return
    }
    $utf8 = [System.Text.UTF8Encoding]::new($false)
    $text = [System.IO.File]::ReadAllText($FilePath, $utf8)
    $newText = & $Transform $text
    if ($newText -eq $text) {
        Write-Host ('  [OK]   ' + $DisplayLabel + '：已是 ' + $Version + '，无需改动')
        return
    }
    if ($WhatIfPreference) {
        Write-Host ('  [DIFF] ' + $DisplayLabel + ' (' + $FilePath + ')')
        $oldLines = $text -split "`n"
        $newLines = $newText -split "`n"
        for ($i = 0; $i -lt [Math]::Max($oldLines.Count, $newLines.Count); $i++) {
            $o = if ($i -lt $oldLines.Count) { $oldLines[$i] } else { '' }
            $n = if ($i -lt $newLines.Count) { $newLines[$i] } else { '' }
            if ($o -ne $n) {
                Write-Host ('      - ' + $o.TrimEnd())
                Write-Host ('      + ' + $n.TrimEnd())
            }
        }
        return
    }
    if ($PSCmdlet.ShouldProcess($FilePath, ('写入版本 ' + $Version))) {
        [System.IO.File]::WriteAllText($FilePath, $newText, $utf8)
        Write-Host ('  [WRITE] ' + $DisplayLabel + ' → ' + $Version)
    }
}

Write-Host ('同步版本锚点 → ' + $Version + '（' + $Date + '）；程序集 ' + $assemblyVersion + ($WhatIf ? '；预览模式' : ''))

# <lang>
#   <zh-CN>A1 — CHANGELOG：把 `## [Unreleased]` 段的内容封为新版本段 `## [vX.Y.Z] - DATE`，Unreleased 段清空。
#     若 Unreleased 段为空，则仅插入一个空的新版本段（封版前已无新积累的正常状态）。</zh-CN>
#   <en>A1 — CHANGELOG: seal the `## [Unreleased]` section body into a new `## [vX.Y.Z] - DATE` section and empty Unreleased.
#     If Unreleased is empty, only insert an empty new version section (the normal state when nothing new has accumulated).</en>
# </lang>
Invoke-AnchorEdit -FilePath $changelogPath -DisplayLabel 'A1 CHANGELOG' -FindPattern 'Unreleased' -Transform {
    param($t)
    if ($t -match ('## \[' + [regex]::Escape($Version) + '\]')) {
        return $t  # 该版本段已存在，幂等跳过（不改）
    }
    $lines = $t -split "`n"
    $unreleasedIdx = -1
    $nextIdx = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^##\s+\[Unreleased\]') { $unreleasedIdx = $i }
        elseif ($unreleasedIdx -ge 0 -and $nextIdx -lt 0 -and $lines[$i] -match '^##\s+\[') { $nextIdx = $i; break }
    }
    if ($unreleasedIdx -lt 0) { return $t }
    if ($nextIdx -lt 0) { $nextIdx = $lines.Count }
    # Unreleased 段正文 = 两个标题之间的非空行
    $body = @()
    for ($i = $unreleasedIdx + 1; $i -lt $nextIdx; $i++) {
        if (-not [string]::IsNullOrWhiteSpace($lines[$i])) { $body += $lines[$i] }
    }
    $newSection = @('', ('## [' + $Version + '] - ' + $Date))
    if ($body.Count -gt 0) { $newSection += $body } else { $newSection += '' }
    # 组装：Unreleased 标题 + 新版本段 + 其余
    $result = @()
    $result += $lines[0..$unreleasedIdx]
    $result += $newSection
    $result += $lines[$nextIdx..($lines.Count - 1)]
    return ($result -join "`n")
}

# <lang>
#   <zh-CN>A3 — 5 个 AssemblyInfo.cs：AssemblyVersion / AssemblyFileVersion → X.Y.Z.0。</zh-CN>
#   <en>A3 — the 5 AssemblyInfo.cs files: AssemblyVersion / AssemblyFileVersion → X.Y.Z.0.</en>
# </lang>
$assemblyFiles = Get-ChildItem -LiteralPath $RepoRoot -Recurse -Filter 'AssemblyInfo.cs' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\temp\\' }
foreach ($af in $assemblyFiles) {
    Invoke-AnchorEdit -FilePath $af.FullName -DisplayLabel ('A3 ' + $af.Name) -FindPattern 'AssemblyVersion' -Transform {
        param($t)
        $t -replace '(AssemblyVersion|AssemblyFileVersion)\("\d+\.\d+\.\d+\.\d+"\)', ('$1("' + $assemblyVersion + '")')
    }
}

# <lang>
#   <zh-CN>A4/A5 — README 中英文的"当前版本"行：替换粗体版本与日期。</zh-CN>
#   <en>A4/A5 — the README zh/en "current version" lines: replace the bold version and the date.</en>
# </lang>
$readmePath = Join-Path $RepoRoot 'README.md'
Invoke-AnchorEdit -FilePath $readmePath -DisplayLabel 'A4 README 中文' -FindPattern '当前版本' -Transform {
    param($t)
    $t -replace '当前版本：\*\*v\d+\.\d+\.\d+\*\*\（\d{4}-\d{2}-\d{2}', ('当前版本：**' + $Version + '**（`' + $Date)
}
Invoke-AnchorEdit -FilePath $readmePath -DisplayLabel 'A5 README 英文' -FindPattern 'Current version' -Transform {
    param($t)
    $t -replace 'Current version: \*\*v\d+\.\d+\.\d+\*\*\ \(\d{4}-\d{2}-\d{2}', ('Current version: **' + $Version + '** (' + $Date)
}

# <lang>
#   <zh-CN>A6 — versioning.md §6 首行：替换 `vX.Y.Z` 与日期。</zh-CN>
#   <en>A6 — versioning.md section 6 first line: replace `vX.Y.Z` and the date.</en>
# </lang>
$versioningPath = Join-Path $RepoRoot 'docs/versioning.md'
Invoke-AnchorEdit -FilePath $versioningPath -DisplayLabel 'A6 versioning §6' -FindPattern '六、当前版本' -Transform {
    param($t)
    # 仅替换 §6 段内**当前版本锚点**（粗体 `vX.Y.Z` + 紧随的日期）；历史提及（如"上一版 `v1.1.0`（…）"）
    # 不带 `**` 粗体，不会被命中，故不会被误改。
    $lines = $t -split "`n"
    $inSection = $false
    $done = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^##\s+六、当前版本') { $inSection = $true; continue }
        if ($inSection -and -not $done -and $lines[$i] -match '\*\*`v\d+\.\d+\.\d+`\*\*\（\d{4}-\d{2}-\d{2}') {
            $lines[$i] = $lines[$i] -replace '\*\*`v\d+\.\d+\.\d+`\*\*\（\d{4}-\d{2}-\d{2}', ('**`' + $Version + '`**（`' + $Date)
            $done = $true
        }
        if ($inSection -and $lines[$i] -match '^##\s+七、') { break }
    }
    return ($lines -join "`n")
}

# <lang>
#   <zh-CN>A7/A8 — 首页中英统计数字：替换 oc-stat-val 内的版本。</zh-CN>
#   <en>A7/A8 — the homepage zh/en stat value: replace the version inside oc-stat-val.</en>
# </lang>
foreach ($idx in @('site/index.md', 'site/en/index.md')) {
    $p = Join-Path $RepoRoot $idx
    Invoke-AnchorEdit -FilePath $p -DisplayLabel ('A7/A8 ' + $idx) -FindPattern 'oc-stat-val' -Transform {
        param($t)
        $t -replace '<div class="oc-stat-val">v\d+\.\d+\.\d+</div>', ('<div class="oc-stat-val">' + $Version + '</div>')
    }
}

Write-Host ('完成。' + $(if ($WhatIfPreference) { '（预览模式，未写入任何文件）' } else { '已写入；请运行 Test-PortalVersionConsistency.ps1（扩面后）核对零漂移，再 git 提交。' }))
