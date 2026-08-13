<#
.SYNOPSIS
.LANG en
Creates a read-only documentation baseline for tracked portal sources.

.LANG zh-CN
为已追踪门户源码生成只读文档化基线。

.DESCRIPTION
<lang>
  <zh-CN>统计已追踪源码文件、C# XML 文档行、public/protected 声明候选和已知文档边界目录。输出只是 inventory 快照，不判定最终文档质量，也不采纳生成目录或本机专属目录。</zh-CN>
  <en>Counts tracked source files, C# XML documentation lines, public/protected declaration candidates, and known documentation-boundary directories. The output is an inventory snapshot only; it does not judge final documentation quality or adopt generated or local-only directories.</en>
</lang>

.PARAMETER AsJson
.LANG en
Writes the baseline object to the pipeline as JSON.

.LANG zh-CN
以 JSON 形式将基线对象写入管道。

.PARAMETER OutputJson
.LANG en
Optional file path for a UTF-8 no BOM JSON baseline artifact.

.LANG zh-CN
可选的 UTF-8 无 BOM JSON 基线证据输出路径。
#>
[CmdletBinding()]
param(
    [switch]$AsJson,

    [string]$OutputJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>基线只读取 Git 已追踪源码，避免将本机资料或历史生成物误纳入公开文档范围。</zh-CN>
#   <en>The baseline reads tracked sources only, so local material and generated history are never adopted implicitly.</en>
# </lang>
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$trackedFiles = @(& git -C $repositoryRoot ls-files)
if ($LASTEXITCODE -ne 0) {
    throw '无法读取 Git 已追踪文件，无法生成文档化基线。'
}

# <lang>
#   <zh-CN>从 Git 已追踪路径中筛选指定扩展名，保持基线不纳入未追踪生成物。</zh-CN>
#   <en>Filters Git-tracked paths by extension so the baseline excludes untracked generated output.</en>
# </lang>
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

# <lang>
#   <zh-CN>读取文档边界目录的追踪文件数和存在性摘要，不采纳本机专属目录正文。</zh-CN>
#   <en>Reads tracked-count and existence summaries for documentation-boundary directories without adopting local-only contents.</en>
# </lang>
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

# <lang>
#   <zh-CN>按源码区域汇总 C# 文件、XML 文档和 public/protected 声明候选，输出仅供盘点。</zh-CN>
#   <en>Summarizes C# files, XML documentation, and public/protected declaration candidates by source area for inventory only.</en>
# </lang>
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

        # <lang>
        #   <zh-CN>这是 inventory 启发式统计，不等价于 API 完整度或注释质量百分比。</zh-CN>
        #   <en>This is an inventory heuristic, not an API-completeness or documentation-quality percentage.</en>
        # </lang>
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

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    $outputDirectory = Split-Path -Parent $OutputJson
    if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path -LiteralPath $outputDirectory)) {
        New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
    }

    [System.IO.File]::WriteAllText(
        $OutputJson,
        (($result | ConvertTo-Json -Depth 8) + [Environment]::NewLine),
        [System.Text.UTF8Encoding]::new($false))
    Write-Host ('JSON: {0}' -f $OutputJson)
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 6
}
else {
    $result
}
