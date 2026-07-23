<#
.SYNOPSIS
    Generates a read-only documentation map and artifact-boundary inventory for W-anp-P15.4.

.DESCRIPTION
    中文：本脚本只读取 Git 已追踪文件、WorkZone 已追踪文件摘要和少量本机生成目录状态，
    用于整理公开文档、内部 WorkZone 文档、生成文档产物、历史 SHFB/Doxygen 现场、
    HIA 文档化通知和文档化脚本入口。它不删除文件、不提交生成物、不升级文档工具链、
    不访问数据库或网络。
    English: This script reads only Git-tracked files, a summary of WorkZone tracked files,
    and a small set of local generated-directory states. It maps public documentation,
    internal WorkZone documentation, generated documentation artifacts, historical SHFB/Doxygen
    footprints, HIA documentation notifications, and documentation-tool script entry points.
    It does not delete files, commit generated output, upgrade documentation tooling, access
    databases, or use the network.
#>
[CmdletBinding()]
param(
    [string]$OutputJson,

    [string]$OutputMarkdown,

    [switch]$AsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$workZoneRoot = Join-Path $repoRoot 'work-zone'

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function ConvertTo-RepoPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    return ($Path -replace '\\', '/')
}

function Get-GitTrackedFiles {
    param([Parameter(Mandatory = $true)][string]$Root)

    $files = @(& git -C $Root ls-files)
    if ($LASTEXITCODE -ne 0) {
        throw "无法读取 Git 已追踪文件：$Root"
    }

    return @($files | ForEach-Object { ConvertTo-RepoPath -Path $_ })
}

function Get-FileState {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $absolutePath = Join-Path $repoRoot $RelativePath
    $exists = Test-Path -LiteralPath $absolutePath
    return [pscustomobject][ordered]@{
        Path = ConvertTo-RepoPath -Path $RelativePath
        Exists = $exists
        Length = if ($exists -and (Test-Path -LiteralPath $absolutePath -PathType Leaf)) { (Get-Item -LiteralPath $absolutePath).Length } else { $null }
        LastWriteTime = if ($exists) { (Get-Item -LiteralPath $absolutePath).LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss') } else { $null }
    }
}

function Get-DirectoryState {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [string]$Owner,
        [string]$Decision,
        [string]$NextAction
    )

    $repoPath = ConvertTo-RepoPath -Path $RelativePath
    $absolutePath = Join-Path $repoRoot $RelativePath
    $exists = Test-Path -LiteralPath $absolutePath -PathType Container
    $fileCount = 0
    $sampleFiles = @()

    if ($exists) {
        $children = @(Get-ChildItem -LiteralPath $absolutePath -Recurse -File -ErrorAction SilentlyContinue)
        $fileCount = $children.Count
        $sampleFiles = @($children | Select-Object -First 8 | ForEach-Object {
            ConvertTo-RepoPath -Path ([System.IO.Path]::GetRelativePath($repoRoot, $_.FullName))
        })
    }

    return [pscustomobject][ordered]@{
        Path = $repoPath
        Exists = $exists
        FileCount = $fileCount
        TrackedFileCount = @($script:rootTrackedFiles | Where-Object { $_.StartsWith($repoPath.TrimEnd('/') + '/', [System.StringComparison]::OrdinalIgnoreCase) -or $_ -eq $repoPath }).Count
        Owner = $Owner
        Decision = $Decision
        NextAction = $NextAction
        SampleFiles = $sampleFiles
    }
}

function Get-PathGroup {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Boundary,
        [Parameter(Mandatory = $true)][string[]]$Paths
    )

    return [pscustomobject][ordered]@{
        Name = $Name
        Boundary = $Boundary
        Count = $Paths.Count
        Paths = @($Paths | Sort-Object)
    }
}

$script:rootTrackedFiles = Get-GitTrackedFiles -Root $repoRoot
$workZoneTrackedFiles = if (Test-Path -LiteralPath $workZoneRoot -PathType Container) {
    Get-GitTrackedFiles -Root $workZoneRoot
}
else {
    @()
}

$publicStableDocs = @($rootTrackedFiles | Where-Object {
    $_ -eq 'README.md' -or
    $_ -eq 'README-old.md' -or
    ($_.StartsWith('docs/', [System.StringComparison]::OrdinalIgnoreCase) -and $_.EndsWith('.md', [System.StringComparison]::OrdinalIgnoreCase))
})

$publicDevEntrypoints = @($rootTrackedFiles | Where-Object {
    $_ -in @(
        'dev/README.md',
        'dev/notify/README.md',
        'dev/documentation/jsdoc/README.md',
        'ai/README.md',
        'src/Setup/SystemReqs.md',
        'src/Libs/readme.md',
        'AGENTS.md',
        'TASK_STATE.md'
    )
})

$rootNotifySnapshots = @($rootTrackedFiles | Where-Object {
    $_.StartsWith('dev/notify/', [System.StringComparison]::OrdinalIgnoreCase) -and $_.EndsWith('.md', [System.StringComparison]::OrdinalIgnoreCase)
})

$documentationScripts = @($rootTrackedFiles | Where-Object {
    $_.StartsWith('dev/scripts/', [System.StringComparison]::OrdinalIgnoreCase) -and
    $_.EndsWith('.ps1', [System.StringComparison]::OrdinalIgnoreCase) -and
    ($_ -match '(Documentation|Doc|Doxy|Hia)')
})

$workZoneGroups = @(
    Get-PathGroup -Name 'WorkZone plans' -Boundary 'Internal planning, phase state, roadmaps, discussion questions, and closeout records.' -Paths @($workZoneTrackedFiles | Where-Object { $_.StartsWith('dev/plans/', [System.StringComparison]::OrdinalIgnoreCase) -and $_.EndsWith('.md', [System.StringComparison]::OrdinalIgnoreCase) })
    Get-PathGroup -Name 'WorkZone ADR' -Boundary 'Internal architecture decisions and route choices.' -Paths @($workZoneTrackedFiles | Where-Object { $_.StartsWith('docs/adr/', [System.StringComparison]::OrdinalIgnoreCase) -and $_.EndsWith('.md', [System.StringComparison]::OrdinalIgnoreCase) })
    Get-PathGroup -Name 'WorkZone evidence' -Boundary 'Internal validation evidence and generated JSON/Markdown summaries.' -Paths @($workZoneTrackedFiles | Where-Object { $_.StartsWith('dev/evidence/', [System.StringComparison]::OrdinalIgnoreCase) })
    Get-PathGroup -Name 'WorkZone AI logs' -Boundary 'Internal AI process logs and handoff context, not public documentation.' -Paths @($workZoneTrackedFiles | Where-Object { $_.StartsWith('ai/', [System.StringComparison]::OrdinalIgnoreCase) -and $_.EndsWith('.md', [System.StringComparison]::OrdinalIgnoreCase) })
)

$generatedDirectories = @(
    Get-DirectoryState -RelativePath 'src/Documentation' -Owner 'Historical SHFB website output' -Decision 'Ignored generated output; do not commit.' -NextAction 'After HIA documentation mechanism is mature, delete this local output directory unless a migration evidence snapshot is explicitly required.'
    Get-DirectoryState -RelativePath 'src/Portal/Documentation' -Owner 'Historical Doxygen output for Portal' -Decision 'Ignored generated output; do not commit.' -NextAction 'After HIA documentation mechanism is mature, delete this local output directory.'
    Get-DirectoryState -RelativePath 'src/Portal.Components.Data/Documentation' -Owner 'Historical Doxygen output for Portal.Components.Data' -Decision 'Ignored generated output; do not commit.' -NextAction 'After HIA documentation mechanism is mature, delete this local output directory.'
    Get-DirectoryState -RelativePath 'src/DoxyGen' -Owner 'Historical Doxygen configuration footprint' -Decision 'Untracked historical footprint; keep only as migration clue during P15/P16.' -NextAction 'If `Doxyfile` remains empty and HIA documentation replaces Doxygen, delete the directory.'
    Get-DirectoryState -RelativePath 'temp/documentation' -Owner 'JSDoc pilot generated output' -Decision 'Ignored local validation output.' -NextAction 'Keep generated output temporary; clear whenever local evidence is no longer needed.'
)

$toolConfigCandidates = @(
    Get-FileState -RelativePath 'src/Portal.shfbproj'
    Get-FileState -RelativePath 'src/DoxyGen/Doxyfile'
)

$notificationRoot = Join-Path (Split-Path -Parent $repoRoot) 'HIA-Documentation-Sys\work-zone\notify'
$notificationState = [pscustomobject][ordered]@{
    Path = $notificationRoot
    Exists = Test-Path -LiteralPath $notificationRoot -PathType Container
    LatestFiles = @()
}
if ($notificationState.Exists) {
    $notificationState.LatestFiles = @(Get-ChildItem -LiteralPath $notificationRoot -Recurse -File -Filter '*.md' |
        Where-Object { $_.Name -ne 'README.md' } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 5 |
        ForEach-Object {
            [pscustomobject][ordered]@{
                Name = $_.Name
                LastWriteTime = $_.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss')
                MentionsCurrentProject = ([System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8) -match 'HIA-ASPNETPortal')
            }
        })
}

$result = [pscustomobject][ordered]@{
    GeneratedAt = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    Scope = 'Documentation map, artifact ownership, generated-output boundary, and P16 input for HIA-ASPNETPortal.'
    RootTrackedFileCount = $rootTrackedFiles.Count
    WorkZoneTrackedFileCount = $workZoneTrackedFiles.Count
    PublicStableDocs = Get-PathGroup -Name 'Public stable documentation' -Boundary 'Root README and docs/*.md are suitable for public repository users and maintainers.' -Paths $publicStableDocs
    PublicDeveloperEntrypoints = Get-PathGroup -Name 'Public developer entrypoints' -Boundary 'Root-level rules, local dev entry documents, and historical notices that may remain in the public repo.' -Paths $publicDevEntrypoints
    RootNotifySnapshots = Get-PathGroup -Name 'Historical dev/notify snapshots' -Boundary 'Historical target-project notification snapshots; new HIA documentation notices are pulled from HIA-Documentation-Sys work-zone/notify.' -Paths $rootNotifySnapshots
    DocumentationScripts = Get-PathGroup -Name 'Documentation scripts' -Boundary 'Read-only or local-output tooling for public documentation, XML docs, JSDoc pilot, HIA notifications, and P15 inventories.' -Paths $documentationScripts
    WorkZoneGroups = $workZoneGroups
    GeneratedDirectories = $generatedDirectories
    ToolConfigCandidates = $toolConfigCandidates
    HiaDocumentationNotifications = $notificationState
    P16Inputs = @(
        'P16.1: Migrate legacy bilingual comments to <lang>/<l> and improve comment richness.',
        'P16.2: Decide DotNetDoc runner upgrade and generated documentation output contract.',
        'P16.3: Reconcile TODO/deferred comment markers and low-value restatement comments.',
        'P16.4: Strengthen high-risk PowerShell script comments and validation boundaries.',
        'P16.5: Complete annotation conditioning or register explicit deferred debt.'
    )
}

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    Write-Utf8NoBomFile -Path $OutputJson -Content ($result | ConvertTo-Json -Depth 12)
}

if (-not [string]::IsNullOrWhiteSpace($OutputMarkdown)) {
    $markdown = [System.Collections.Generic.List[string]]::new()
    $markdown.Add('# W-anp-P15.4 文档地图与生成边界 Inventory') | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add('生成时间：' + $result.GeneratedAt) | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add('## 总览') | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add('- 主仓库已追踪文件数：' + $result.RootTrackedFileCount) | Out-Null
    $markdown.Add('- WorkZone 已追踪文件数：' + $result.WorkZoneTrackedFileCount) | Out-Null
    $markdown.Add('- 稳定公开文档数：' + $result.PublicStableDocs.Count) | Out-Null
    $markdown.Add('- 文档化脚本入口数：' + $result.DocumentationScripts.Count) | Out-Null
    $markdown.Add('') | Out-Null

    $markdown.Add('## 公开文档入口') | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add('| 分组 | 数量 | 边界 |') | Out-Null
    $markdown.Add('| --- | ---: | --- |') | Out-Null
    foreach ($group in @($result.PublicStableDocs, $result.PublicDeveloperEntrypoints, $result.RootNotifySnapshots, $result.DocumentationScripts)) {
        $markdown.Add('| ' + $group.Name + ' | ' + $group.Count + ' | ' + $group.Boundary + ' |') | Out-Null
    }
    $markdown.Add('') | Out-Null

    $markdown.Add('## WorkZone 内部资料') | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add('| 分组 | 数量 | 边界 |') | Out-Null
    $markdown.Add('| --- | ---: | --- |') | Out-Null
    foreach ($group in $result.WorkZoneGroups) {
        $markdown.Add('| ' + $group.Name + ' | ' + $group.Count + ' | ' + $group.Boundary + ' |') | Out-Null
    }
    $markdown.Add('') | Out-Null

    $markdown.Add('## 生成目录与历史工具现场') | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add('| 路径 | 存在 | 文件数 | 已追踪 | 归属 | 当前决策 | 后续动作 |') | Out-Null
    $markdown.Add('| --- | --- | ---: | ---: | --- | --- | --- |') | Out-Null
    foreach ($directory in $result.GeneratedDirectories) {
        $markdown.Add('| `' + $directory.Path + '` | ' + $directory.Exists + ' | ' + $directory.FileCount + ' | ' + $directory.TrackedFileCount + ' | ' + $directory.Owner + ' | ' + $directory.Decision + ' | ' + $directory.NextAction + ' |') | Out-Null
    }
    $markdown.Add('') | Out-Null

    $markdown.Add('## 配置候选') | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add('| 文件 | 存在 | 大小 | 判断 |') | Out-Null
    $markdown.Add('| --- | --- | ---: | --- |') | Out-Null
    foreach ($candidate in $result.ToolConfigCandidates) {
        $judgment = switch ($candidate.Path) {
            'src/Portal.shfbproj' { '历史 SHFB 工程配置候选；P16 确认 HIA 文档化替代后可删除。' }
            'src/DoxyGen/Doxyfile' { 'Doxygen 配置足迹；若继续保持空文件，P16 可删除。' }
            default { '待判定。' }
        }

        $length = if ($null -eq $candidate.Length) { '' } else { [string]$candidate.Length }
        $markdown.Add('| `' + $candidate.Path + '` | ' + $candidate.Exists + ' | ' + $length + ' | ' + $judgment + ' |') | Out-Null
    }
    $markdown.Add('') | Out-Null

    $markdown.Add('## HIA 通知源') | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add('- 路径：`' + $result.HiaDocumentationNotifications.Path + '`') | Out-Null
    $markdown.Add('- 可读：' + $result.HiaDocumentationNotifications.Exists) | Out-Null
    $markdown.Add('- 说明：P15.4 继续读取通知，但不升级 DotNetDoc runner；升级和生成策略放入 P16。') | Out-Null
    $markdown.Add('') | Out-Null

    $markdown.Add('## P16 输入') | Out-Null
    $markdown.Add('') | Out-Null
    foreach ($input in $result.P16Inputs) {
        $markdown.Add('- ' + $input) | Out-Null
    }

    Write-Utf8NoBomFile -Path $OutputMarkdown -Content ($markdown -join "`r`n")
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 12
}
else {
    $result |
        Select-Object GeneratedAt, RootTrackedFileCount, WorkZoneTrackedFileCount,
            @{ Name = 'PublicStableDocCount'; Expression = { $_.PublicStableDocs.Count } },
            @{ Name = 'DocumentationScriptCount'; Expression = { $_.DocumentationScripts.Count } }
}
