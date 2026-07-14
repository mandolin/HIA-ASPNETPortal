[CmdletBinding()]
param(
    [switch]$AsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# 基线只读取 Git 已追踪源码，避免将本机资料或历史生成物误纳入公开文档范围。
# The baseline reads tracked sources only, so local material and generated history are never adopted implicitly.
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$trackedFiles = @(& git -C $repositoryRoot ls-files)
if ($LASTEXITCODE -ne 0) {
    throw '无法读取 Git 已追踪文件，无法生成文档化基线。'
}

function Get-TrackedFilesByExtension {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Extension,

        [string]$Prefix = 'src/'
    )

    return @($trackedFiles | Where-Object {
            $_.StartsWith($Prefix, [System.StringComparison]::OrdinalIgnoreCase) -and
            [System.IO.Path]::GetExtension($_).Equals($Extension, [System.StringComparison]::OrdinalIgnoreCase)
        })
}

function Get-TrackedDirectoryState {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,

        [Parameter(Mandatory = $true)]
        [string]$Boundary
    )

    $normalizedPath = $RelativePath.TrimEnd('/', '\')
    $directoryPrefix = "$normalizedPath/"
    $trackedCount = @($trackedFiles | Where-Object {
            $_.Equals($normalizedPath, [System.StringComparison]::OrdinalIgnoreCase) -or
            $_.StartsWith($directoryPrefix, [System.StringComparison]::OrdinalIgnoreCase)
        }).Count
    $exists = Test-Path -LiteralPath (Join-Path $repositoryRoot $normalizedPath) -PathType Container

    $state = if (-not $exists) {
        'Absent'
    }
    elseif ($trackedCount -gt 0) {
        'ContainsTrackedFiles'
    }
    else {
        'PresentWithoutTrackedFiles'
    }

    return [pscustomobject][ordered]@{
        Path = $normalizedPath
        Exists = $exists
        TrackedFileCount = $trackedCount
        State = $state
        Boundary = $Boundary
    }
}

function Get-CSharpAreaSummary {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AreaName
    )

    $prefix = "src/$AreaName/"
    $sourceFiles = @(Get-TrackedFilesByExtension -Extension '.cs' -Prefix $prefix)
    $xmlDocumentationLineCount = 0
    $publicProtectedCandidateCount = 0
    $filesWithXmlDocumentation = 0

    foreach ($sourceFile in $sourceFiles) {
        $absolutePath = Join-Path $repositoryRoot ($sourceFile -replace '/', '\')
        $content = [System.IO.File]::ReadAllText($absolutePath)
        $xmlLineCount = [System.Text.RegularExpressions.Regex]::Matches($content, '(?m)^\s*///').Count

        if ($xmlLineCount -gt 0) {
            $filesWithXmlDocumentation++
            $xmlDocumentationLineCount += $xmlLineCount
        }

        # 这是 inventory 启发式统计，不等价于 API 完整度或注释质量百分比。
        # This is an inventory heuristic, not an API-completeness or documentation-quality percentage.
        $publicProtectedCandidateCount += [System.Text.RegularExpressions.Regex]::Matches(
            $content,
            '(?m)^\s*(?:public|protected)\b').Count
    }

    return [pscustomobject][ordered]@{
        Area = $AreaName
        CSharpFileCount = $sourceFiles.Count
        FilesWithXmlDocumentation = $filesWithXmlDocumentation
        XmlDocumentationLineCount = $xmlDocumentationLineCount
        PublicProtectedDeclarationCandidates = $publicProtectedCandidateCount
    }
}

$csharpAreas = @(
    'Portal',
    'Portal.Components',
    'Portal.Components.Data',
    'Portal.Components.Data1',
    'Portal.DataProviderProof',
    'Portal.HiaBoundaryProof'
) | ForEach-Object { Get-CSharpAreaSummary -AreaName $_ }

$sourceExtensions = @('.cs', '.aspx', '.ascx', '.master', '.js', '.css', '.md', '.config', '.xml')
$sourceCounts = foreach ($extension in $sourceExtensions) {
    [pscustomobject][ordered]@{
        Extension = $extension
        TrackedFileCount = @(Get-TrackedFilesByExtension -Extension $extension).Count
    }
}

$knownBoundaryPaths = @(
    @{ Path = 'src/Documentation'; Boundary = '历史生成或资料目录；P4.1 不自动采纳。' },
    @{ Path = 'src/DoxyGen'; Boundary = '历史生成或工具资料目录；P4.1 不自动采纳。' },
    @{ Path = 'src/Portal.Components.Data/Documentation'; Boundary = '历史生成或资料目录；P4.1 不自动采纳。' },
    @{ Path = 'src/Portal/Documentation'; Boundary = '历史生成或资料目录；P4.1 不自动采纳。' },
    @{ Path = 'src/Portal/js'; Boundary = '未完成所有权与提交策略确认；不作为 JSDoc pilot 输入。' },
    @{ Path = 'src/Portal/css'; Boundary = '未完成所有权与提交策略确认；不作为文档生成输入。' },
    @{ Path = 'temp'; Boundary = '仅可作为本机验证输出，例如 temp/documentation；不发布、不追踪。' }
) | ForEach-Object { Get-TrackedDirectoryState -RelativePath $_.Path -Boundary $_.Boundary }

$result = [pscustomobject][ordered]@{
    SchemaVersion = '1.0'
    GeneratedUtc = [DateTime]::UtcNow.ToString('O')
    RepositoryRoot = $repositoryRoot
    Scope = 'Git tracked source files under src/ only; counts are inventory facts, not documentation quality scores.'
    SourceCounts = $sourceCounts
    CSharpAreas = $csharpAreas
    DocumentationInputBoundaries = [pscustomobject][ordered]@{
        TrackedJavaScriptFiles = @(Get-TrackedFilesByExtension -Extension '.js')
        TrackedCssFiles = @(Get-TrackedFilesByExtension -Extension '.css')
        KnownDirectoryStates = $knownBoundaryPaths
    }
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 6
}
else {
    $result
}
