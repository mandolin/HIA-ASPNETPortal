<#
<lang>
  <zh-CN>版本一致性门禁脚本。校验「CHANGELOG 最新条目版本」「最新 git 标签版本」「程序集版本」三者一致，防止版本锚点漂移。本脚本只读取文件与 git 标签，不连接网络、不写入任何文件、不创建或修改标签。</zh-CN>
  <en>Version-consistency gate script. Verifies that the latest CHANGELOG entry, the latest git tag, and the assembly versions all agree so version anchors cannot drift. It only reads files and git tags: no network, no file writes, and no tag creation or modification.</en>
</lang>
#>

param(
    # <lang><zh-CN>仓库根目录；留空时取脚本所在目录的上级目录。</zh-CN><en>Repository root; when empty, the parent of the script directory is used.</en></lang>
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent $PSScriptRoot
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
#   <zh-CN>读取所有 AssemblyInfo.cs 中的 AssemblyVersion 值集合。</zh-CN>
#   <en>Reads the set of AssemblyVersion values from all AssemblyInfo.cs files.</en>
# </lang>
function Get-AssemblyVersions {
    param([string]$Root)

    $files = Get-ChildItem -Path (Join-Path $Root 'src') -Recurse -Filter 'AssemblyInfo.cs' -File -ErrorAction SilentlyContinue
    $values = @()
    foreach ($file in $files) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        if ($content -match 'AssemblyVersion\("(\d+\.\d+\.\d+)\.\d+"\)') {
            $values += $Matches[1]
        }
    }
    return ($values | Sort-Object -Unique)
}

$changelog = Get-LatestChangelogVersion -Root $RepoRoot
$tag = Get-LatestTagVersion -Root $RepoRoot
$assemblies = Get-AssemblyVersions -Root $RepoRoot

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

if ($problems.Count -gt 0) {
    Write-Output 'RESULT: Fail'
    foreach ($problem in $problems) { Write-Output ('  - ' + $problem) }
    exit 1
}

Write-Output 'RESULT: Pass'
exit 0
