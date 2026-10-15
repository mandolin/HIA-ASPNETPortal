<#
.SYNOPSIS
<lang>
  <zh-CN>版本一致性门禁：校验「CHANGELOG 最新条目版本」「最新 git 标签版本」「程序集版本」三者一致，防止版本锚点漂移。</zh-CN>
  <en>Version-consistency gate: verifies that the latest CHANGELOG entry, the latest git tag and the assembly versions all agree so version anchors cannot drift.</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>**前置条件**：Git 仓库可读、根 `CHANGELOG.md` 存在；本脚本不连接网络、不写入任何文件、不创建或修改标签。
  **验证方式**：退出码 0 表示三者一致、1 表示存在漂移；输出逐项列出不一致的版本锚点。
  **低敏边界**：只读取版本文本与 git 标签名，不读取凭据、连接串或业务数据。</zh-CN>
  <en>**Prerequisites**: a readable Git repository and an existing root `CHANGELOG.md`; this script does not use the network, write any file, or create or modify tags.
  **Verification**: exit code 0 means all three agree, 1 means drift; the output lists each inconsistent version anchor.
  **Low-sensitivity boundary**: only version text and git tag names are read — no credentials, connection strings or business data.</en>
</lang>
.PARAMETER RepoRoot
<lang>
  <zh-CN>仓库根目录；留空时取脚本所在目录的上两级目录（`dev\scripts` 的上两级即仓库根）。</zh-CN>
  <en>Repository root; when empty, two levels above the script directory (`dev\scripts` -> repository root).</en>
</lang>
#>

param(
    # <lang><zh-CN>仓库根目录；留空时取脚本所在目录的上两级目录（dev\\scripts 的上两级即仓库根）。</zh-CN><en>Repository root; when empty, two levels above the script directory (dev\\scripts -> repository root).</en></lang>
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

# <lang>
#   <zh-CN>从 CHANGELOG.md 读取第一个正式版本条目（跳过 Unreleased）的语义化版本号。</zh-CN>
#   <en>Reads the semantic version of the first non-Unreleased release entry from CHANGELOG.md.</en>
# </lang>
function Get-LatestChangelogVersion {
    param([string]$Root)

    $path = Join-Path $Root 'CHANGELOG.md'
    if (-not (Test-Path $Path)) { return $null }
    foreach ($line in Get-Content -LiteralPath $path) {
        if ($line -match '^##\s+\[v?(\d+\.\d+\.\d+)\]') {
            return $Matches[1]
        }
    }
    return $null
}

# <lang>
#   <zh-CN>读取仓库中最新的 vX.Y.Z 语义化标签版本；无标签时返回空值。</zh-CN>
#   <en>Reads the latest vX.Y.Z semantic tag version in the repository; returns null when no tag exists.</en>
# </lang>
function Get-LatestTagVersion {
    param([string]$Root)

    $tags = & git -C $Root tag --list 'v*.*.*' 2>$null
    if (-not $tags) { return $null }
    $versions = @()
    foreach ($tag in $tags) {
        $name = [string]$tag
        if ($name -match '^v?(\d+\.\d+\.\d+)$') {
            $versions += [Version]$Matches[1]
        }
    }
    if ($versions.Count -eq 0) { return $null }
    return ($versions | Sort-Object -Descending | Select-Object -First 1).ToString()
}

# <lang>
#   <zh-CN>C-anp-P16 / B1（W95，2026-10-07）：同时读取所有 AssemblyInfo.cs 中的 AssemblyVersion 与
#   AssemblyFileVersion，并核对单个文件内两者是否一致。返回去重后的两个集合，供主逻辑与 CHANGELOG 比对。</zh-CN>
#   <en>C-anp-P16 / B1 (W95, 2026-10-07): reads both AssemblyVersion and AssemblyFileVersion from every
#   AssemblyInfo.cs and checks intra-file agreement, returning two de-duplicated sets for the main logic to
#   compare against CHANGELOG.</en>
# </lang>
function Get-AssemblyVersionSets {
    param([string]$Root)

    $files = Get-ChildItem -Path (Join-Path $Root 'src') -Recurse -Filter 'AssemblyInfo.cs' -File -ErrorAction SilentlyContinue
    $av = @(); $fv = @()
    foreach ($file in $files) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        $mAv = [regex]::Match($content, 'AssemblyVersion\("(\d+\.\d+\.\d+)\.\d+"\)')
        $mFv = [regex]::Match($content, 'AssemblyFileVersion\("(\d+\.\d+\.\d+)\.\d+"\)')
        if ($mAv.Success) { $av += $mAv.Groups[1].Value }
        if ($mFv.Success) { $fv += $mFv.Groups[1].Value }
        if ($mAv.Success -and $mFv.Success -and $mAv.Groups[1].Value -ne $mFv.Groups[1].Value) {
            Write-Host ('  note: ' + $file.Name + ' AssemblyVersion=' + $mAv.Groups[1].Value + ' 与 AssemblyFileVersion=' + $mFv.Groups[1].Value + ' 不一致（文件内漂移）')
        }
    }
    return @{ AV = @($av | Sort-Object -Unique); FV = @($fv | Sort-Object -Unique) }
}

$changelog = Get-LatestChangelogVersion -Root $RepoRoot
$tag = Get-LatestTagVersion -Root $RepoRoot
$sets = Get-AssemblyVersionSets -Root $RepoRoot
$assemblies = $sets.AV
$fileVersions = $sets.FV

Write-Output ('CHANGELOG latest : ' + $(if ($changelog) { $changelog } else { '<none>' }))
Write-Output ('Git tag latest   : ' + $(if ($tag) { $tag } else { '<none>' }))
Write-Output ('Assembly versions: ' + $(if ($assemblies) { ($assemblies -join ', ') } else { '<none>' }))

$problems = @()
if (-not $changelog) { $problems += 'CHANGELOG.md 中没有正式版本条目' }
if (-not $tag) { $problems += '仓库中没有 vX.Y.Z 语义化标签' }
if ($changelog -and $tag -and $changelog -ne $tag) {
    $problems += ('CHANGELOG 版本 ' + $changelog + ' 与标签版本 ' + $tag + ' 不一致')
}
foreach ($assembly in $assemblies) {
    if ($changelog -and $assembly -ne $changelog) {
        $problems += ('程序集版本 ' + $assembly + ' 与 CHANGELOG 版本 ' + $changelog + ' 不一致')
    }
}
# <lang>
#   <zh-CN>B1（W95）：AssemblyFileVersion 必须与 CHANGELOG 对齐（与 AssemblyVersion 同口径），且文件内
#   不应缺失。缺省即版本锚点未同步，属 R6 缺口。</zh-CN>
#   <en>B1 (W95): AssemblyFileVersion must align with CHANGELOG (same basis as AssemblyVersion) and must not
#   be missing per file; absence means the version anchor is unsynced, an R6 gap.</en>
# </lang>
if ($assemblies.Count -gt 0 -and $fileVersions.Count -eq 0) {
    $problems += 'AssemblyInfo.cs 缺少 AssemblyFileVersion（版本锚点未同步，R6 缺口）'
}
foreach ($fv in $fileVersions) {
    if ($changelog -and $fv -ne $changelog) {
        $problems += ('程序集文件版本 AssemblyFileVersion ' + $fv + ' 与 CHANGELOG 版本 ' + $changelog + ' 不一致')
    }
}

# <lang>
#   <zh-CN>**B2：公开面文档的"当前版本"声明必须与权威源一致**（`W-anp-P138` 新增）。
#     背景：本项目已三次出现过期版本号 —— 根 README 英文版 `v0.7.0`（`W-anp-P126` 修）、
#     `docs/versioning.md` 第七节 `v0.1.0`（`W-anp-P137` 修）、以及同文档第八节
#     "产品仅 `v0.1.0`"的矛盾表述（本包修）。既有三项检查（CHANGELOG / tag / 程序集）**都不看文档正文**，
#     所以文档里的版本号可以随便过期而门禁全绿。
#     本项只查**显式声明"当前版本"的位置**（中文"当前版本 … vX.Y.Z"、英文"Current version … vX.Y.Z"），
#     不查版本里程碑表里的历史轨迹与预留版本 —— 那些是**有意保留的历史记录**，
#     一律要求等于当前版本会把它们误判为过期（与 W123"辨明非过期项未误改"是同类教训）。</zh-CN>
#   <en>**B2: a "current version" declaration in public-facing docs must match the authoritative source** (new in
#     `W-anp-P138`). Background: this project has now had three stale version numbers — the English root README's `v0.7.0`
#     (fixed in `W-anp-P126`), section 7 of `docs/versioning.md` saying `v0.1.0` (fixed in `W-anp-P137`), and the
#     contradictory "product only at `v0.1.0`" in section 8 of the same file (fixed here). The existing three checks
#     (CHANGELOG / tag / assemblies) **never look at documentation prose**, so a stale version in docs can pass every gate.
#     This check only inspects positions that **explicitly declare the "current version"** (Chinese "当前版本 … vX.Y.Z",
#     English "Current version … vX.Y.Z") and deliberately ignores historical trajectories and reserved versions inside
#     version/milestone tables — those are **intentional historical records**, and demanding equality with the current
#     version would misflag them as stale (the same lesson as W123's "identify non-stale items instead of editing them").</en>
# </lang>
$declarationTargets = New-Object 'System.Collections.Generic.List[string]'
foreach ($rel in @('README.md', 'docs/versioning.md')) {
    $p = Join-Path $RepoRoot $rel
    if (Test-Path -LiteralPath $p) { $declarationTargets.Add($p) }
}

foreach ($p in $declarationTargets) {
    $rel = $p.Substring($RepoRoot.Length + 1).Replace('\', '/')
    $text = [System.IO.File]::ReadAllText($p, [System.Text.UTF8Encoding]::new($false))
    $declares = [regex]::Matches($text, '(?i)(当前版本|Current version)[^\r\n]{0,80}?(v\d+\.\d+\.\d+)')
    foreach ($d in $declares) {
        $declared = ($d.Groups[2].Value -replace '^v', '')
        $authoritative = ($tag -replace '^v', '')
        if ($authoritative -and $declared -ne $authoritative) {
            $problems += ('公开面文档 ' + $rel + ' 声明的当前版本 ' + $d.Groups[2].Value + ' 与权威版本 ' + $tag + ' 不一致（过期版本号）')
        }
    }
}

if ($problems.Count -gt 0) {
    Write-Output 'RESULT: Fail'
    foreach ($problem in $problems) { Write-Output ('  - ' + $problem) }
    exit 1
}

Write-Output 'RESULT: Pass'
exit 0
