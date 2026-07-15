[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# 中文：此门禁只读取公开 Markdown、Git 索引和已声明的生成目录边界；不访问 WorkZone、网络、数据库或文档生成工具。
# English: This gate reads only public Markdown, the Git index, and declared generated-directory boundaries. It never accesses
# WorkZone, the network, databases, or documentation generators.
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$checks = New-Object 'System.Collections.Generic.List[object]'
$linkPattern = [regex]'\[(?<label>[^\]]+)\]\((?<target>[^)]+)\)'
$absolutePathPattern = [regex]'(?<![A-Za-z0-9_])(?<path>[A-Za-z]:[\\/][^\r\n\x60"''<>]+)'
$secretAssignmentPattern = [regex]'(?im)^\s*(?:password|pwd|token|api[_-]?key|secret|connectionstring)\s*[:=]\s*["''][^"'']+'

function Add-PortalCheck {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [bool]$Passed,

        [Parameter(Mandatory = $true)]
        [string]$Detail
    )

    $checks.Add([pscustomobject][ordered]@{
            Name = $Name
            Passed = $Passed
            Detail = $Detail
        })
}

function Get-PublicMarkdownFiles {
    # 中文：只从 Git 已追踪文件筛选公开文档面，避免未跟踪草稿、WorkZone 资料或生成物改变门禁结果。
    # English: Select the public documentation surface from Git-tracked files only so untracked drafts, WorkZone material,
    # or generated output cannot change the gate result.
    $trackedFiles = @(& git -C $repositoryRoot ls-files -- '*.md')
    if ($LASTEXITCODE -ne 0) {
        throw "Git 无法列出已追踪 Markdown，退出代码：$LASTEXITCODE"
    }

    return @($trackedFiles | Where-Object {
            $_ -eq 'README.md' -or
            $_ -eq 'README-old.md' -or
            $_ -like 'docs/*.md' -or
            $_ -like 'dev/documentation/*.md'
        } | Sort-Object)
}

function Test-RelativeMarkdownLinks {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$MarkdownFiles
    )

    $invalidLinks = New-Object 'System.Collections.Generic.List[string]'
    $privateLinks = New-Object 'System.Collections.Generic.List[string]'
    $externalLinkCount = 0

    foreach ($relativePath in $MarkdownFiles) {
        $absolutePath = Join-Path $repositoryRoot ($relativePath -replace '/', '\')
        $content = [System.IO.File]::ReadAllText($absolutePath)
        $sourceDirectory = Split-Path -Parent $absolutePath

        foreach ($match in $linkPattern.Matches($content)) {
            $rawTarget = ([string]$match.Groups['target'].Value).Trim()
            $target = ($rawTarget -split '\s+')[0]
            if ([string]::IsNullOrWhiteSpace($target) -or $target.StartsWith('#')) {
                continue
            }

            if ($target -match '^(?i:https?|mailto):') {
                $externalLinkCount++
                continue
            }

            if ($target -match '^(?i:file):' -or $target.StartsWith('/') -or $target.StartsWith('\')) {
                $invalidLinks.Add($relativePath + ' -> ' + $rawTarget)
                continue
            }

            $targetPath = ($target -split '#', 2)[0] -replace '/', '\'
            if ([string]::IsNullOrWhiteSpace($targetPath)) {
                continue
            }

            if ($targetPath -match '(^|\\)(?i:work-zone|\.serena)(\\|$)') {
                $privateLinks.Add($relativePath + ' -> ' + $rawTarget)
                continue
            }

            $resolvedPath = [System.IO.Path]::GetFullPath((Join-Path $sourceDirectory $targetPath))
            if (-not $resolvedPath.StartsWith($repositoryRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
                -not (Test-Path -LiteralPath $resolvedPath -PathType Leaf)) {
                $invalidLinks.Add($relativePath + ' -> ' + $rawTarget)
            }
        }
    }

    $relativeLinkDetail = if ($invalidLinks.Count -eq 0) {
        'Checked public Markdown links.'
    }
    else {
        $invalidLinks -join '; '
    }
    Add-PortalCheck -Name 'Relative Markdown file links' -Passed ($invalidLinks.Count -eq 0) -Detail $relativeLinkDetail

    $privateLinkDetail = if ($privateLinks.Count -eq 0) {
        'No WorkZone or .serena link target.'
    }
    else {
        $privateLinks -join '; '
    }
    Add-PortalCheck -Name 'No private documentation links' -Passed ($privateLinks.Count -eq 0) -Detail $privateLinkDetail

    return $externalLinkCount
}

function Test-PublicDocumentationPrivacy {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$MarkdownFiles
    )

    $unexpectedPaths = New-Object 'System.Collections.Generic.List[string]'
    $secretAssignments = New-Object 'System.Collections.Generic.List[string]'
    $allowedAbsolutePaths = @(
        'C:\Program Files\PowerShell\7\pwsh.exe'
    )

    foreach ($relativePath in $MarkdownFiles) {
        $absolutePath = Join-Path $repositoryRoot ($relativePath -replace '/', '\')
        $content = [System.IO.File]::ReadAllText($absolutePath)

        foreach ($match in $absolutePathPattern.Matches($content)) {
            $candidate = ([string]$match.Groups['path'].Value).TrimEnd('.', ',', ';', ':', ')', ']')
            $isAllowed = $allowedAbsolutePaths | Where-Object {
                [string]::Equals($candidate, $_, [System.StringComparison]::OrdinalIgnoreCase)
            }
            if ($null -eq $isAllowed) {
                $unexpectedPaths.Add($relativePath + ': ' + $candidate)
            }
        }

        if ($secretAssignmentPattern.IsMatch($content)) {
            $secretAssignments.Add($relativePath)
        }
    }

    $absolutePathDetail = if ($unexpectedPaths.Count -eq 0) {
        'Only approved generic executable paths are present.'
    }
    else {
        $unexpectedPaths -join '; '
    }
    Add-PortalCheck -Name 'No unexpected absolute local paths' -Passed ($unexpectedPaths.Count -eq 0) -Detail $absolutePathDetail

    $secretAssignmentDetail = if ($secretAssignments.Count -eq 0) {
        'No password/token/key/connection-string assignments found.'
    }
    else {
        $secretAssignments -join '; '
    }
    Add-PortalCheck -Name 'No credential assignment patterns' -Passed ($secretAssignments.Count -eq 0) -Detail $secretAssignmentDetail
}

$markdownFiles = Get-PublicMarkdownFiles
if ($markdownFiles.Count -eq 0) {
    throw '未找到公开 Markdown 输入。'
}

$rootReadme = [System.IO.File]::ReadAllText((Join-Path $repositoryRoot 'README.md'))
Add-PortalCheck -Name 'Root documentation entry' -Passed $rootReadme.Contains('(docs/README.md)') -Detail 'README.md -> docs/README.md'

$docsIndexPath = Join-Path $repositoryRoot 'docs/README.md'
$docsIndex = [System.IO.File]::ReadAllText($docsIndexPath)
$publicDocs = @($markdownFiles | Where-Object { $_ -like 'docs/*.md' -and $_ -ne 'docs/README.md' })
$missingIndexEntries = @($publicDocs | Where-Object { -not $docsIndex.Contains((Split-Path -Leaf $_)) })
$indexCoverageDetail = if ($missingIndexEntries.Count -eq 0) {
    $publicDocs.Count.ToString() + ' indexed documents.'
}
else {
    'Missing: ' + ($missingIndexEntries -join ', ')
}
Add-PortalCheck -Name 'Public documentation index coverage' -Passed ($missingIndexEntries.Count -eq 0) -Detail $indexCoverageDetail

$externalLinkCount = Test-RelativeMarkdownLinks -MarkdownFiles $markdownFiles
Test-PublicDocumentationPrivacy -MarkdownFiles $markdownFiles

# 中文：这些目录尚未完成所有权或发布策略确认，任何已追踪文件都意味着公开边界被意外扩大。
# English: Ownership or publication policy is not yet confirmed for these directories; any tracked file would unexpectedly widen the public boundary.
$excludedGeneratedDirectories = @(
    'src/Documentation',
    'src/DoxyGen',
    'src/Portal/Documentation'
)
foreach ($relativeDirectory in $excludedGeneratedDirectories) {
    $trackedFiles = @(& git -C $repositoryRoot ls-files -- ($relativeDirectory + '/**'))
    if ($LASTEXITCODE -ne 0) {
        throw "Git 无法检查生成目录 '$relativeDirectory'，退出代码：$LASTEXITCODE"
    }

    $generatedDirectoryDetail = if ($trackedFiles.Count -eq 0) {
        'No tracked files.'
    }
    else {
        $trackedFiles -join ', '
    }
    Add-PortalCheck -Name ('Generated-directory boundary: ' + $relativeDirectory) -Passed ($trackedFiles.Count -eq 0) -Detail $generatedDirectoryDetail
}

$failedChecks = @($checks | Where-Object { -not $_.Passed })
$checks
[pscustomobject][ordered]@{
    MarkdownFiles = $markdownFiles.Count
    ExternalLinks = $externalLinkCount
    TotalChecks = $checks.Count
    FailedChecks = $failedChecks.Count
}

if ($failedChecks.Count -gt 0) {
    throw ('Portal public documentation gate failed: ' + (($failedChecks | ForEach-Object { $_.Name }) -join ', '))
}
