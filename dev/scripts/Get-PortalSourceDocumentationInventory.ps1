<#
.SYNOPSIS
    Generates a read-only source and documentation coverage inventory for P15.

.DESCRIPTION
    中文：本脚本只读取 Git 已追踪的源码、脚本和主要 Markdown，用启发式规则统计源码结构、
    注释形态、高风险区域和文档化补强候选。它不读取未跟踪生成目录、不改写源码、不构建项目、
    不生成 API 文档、不访问数据库或网络。
    English: This script reads only Git-tracked source, script, and main Markdown files, then
    uses heuristic rules to inventory source structure, comment styles, high-risk areas, and
    documentation-improvement candidates. It does not read untracked generated output, rewrite
    source files, build the project, generate API docs, or access databases or the network.
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
$trackedFiles = @(& git -C $repoRoot ls-files)
if ($LASTEXITCODE -ne 0) {
    throw '无法读取 Git 已追踪文件，无法生成源码文档化盘点。'
}

$includedExtensions = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
@('.cs', '.aspx', '.ascx', '.master', '.js', '.ps1', '.md') | ForEach-Object { [void]$includedExtensions.Add($_) }

$excludedPrefixes = @(
    'temp/',
    'src/Documentation/',
    'src/DoxyGen/',
    'src/Portal/Documentation/',
    'src/Portal.Components.Data/Documentation/',
    'src/Portal/bin/',
    'src/Portal/obj/',
    'src/packages/',
    'node_modules/'
)

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

function Test-IsIncludedTrackedFile {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $repoPath = ConvertTo-RepoPath -Path $RelativePath
    foreach ($prefix in $excludedPrefixes) {
        if ($repoPath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $false
        }
    }

    return $includedExtensions.Contains([System.IO.Path]::GetExtension($repoPath))
}

function Get-AbsolutePath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    return Join-Path $repoRoot ((ConvertTo-RepoPath -Path $RelativePath) -replace '/', '\')
}

function Get-TextMetric {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text,
        [Parameter(Mandatory = $true)][string]$Pattern
    )

    if ([string]::IsNullOrEmpty($Text)) {
        return 0
    }

    return [System.Text.RegularExpressions.Regex]::Matches(
        $Text,
        $Pattern,
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::Multiline).Count
}

function Get-RiskCategories {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text
    )

    $joined = "$RelativePath`n$Text"
    $categories = New-Object 'System.Collections.Generic.List[string]'

    $rules = @(
        @{ Name = 'Configuration'; Pattern = '(web\.config|appSettings|connectionStrings|ExternalCfgPath|SystemSettings|PortalSettings|UnityCfg|machineKey)' },
        @{ Name = 'Security'; Pattern = '(Security|Encrypt|Decrypt|Password|Credential|AuthCookie|FormsAuthentication|HttpOnly|SameSite|CSP|X-Frame-Options|ValidateRequest)' },
        @{ Name = 'IdentityRole'; Pattern = '(User|Users|Role|Roles|Membership|Login|Register|EmployeeCode|SysUser|BizUser)' },
        @{ Name = 'PathUpload'; Pattern = '(Upload|Uploaded|FileName|PhysicalPath|MapPath|Directory|Path\.|HttpPostedFile|Document)' },
        @{ Name = 'ExceptionDiagnostics'; Pattern = '(Exception|Application_Error|Diagnostics|Diagnostic|Log|GenericErrorPage|Trace\.Warn)' },
        @{ Name = 'Audit'; Pattern = '(Audit|OperationAudit|UpdatedBy|UpdatedUtc|Review|Approval|WorkItem)' },
        @{ Name = 'DataAccess'; Pattern = '(SqlConnection|SqlCommand|SqlDataReader|DataProvider|Repository|Database|Db|Migration|StoredProcedure)' },
        @{ Name = 'ReleaseScript'; Pattern = '(^dev/scripts/|Publish|Release|Deploy|Smoke|Readiness|Evidence|Hardening)' }
    )

    foreach ($rule in $rules) {
        if ([regex]::IsMatch($joined, $rule.Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
            $categories.Add($rule.Name)
        }
    }

    return @($categories | Select-Object -Unique)
}

function Get-FileArea {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = ConvertTo-RepoPath -Path $RelativePath
    if ($path.StartsWith('src/Portal/Components/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'Portal.Components.Runtime' }
    if ($path.StartsWith('src/Portal/Admin/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'Portal.Admin' }
    if ($path.StartsWith('src/Portal/DesktopModules/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'Portal.DesktopModules' }
    if ($path.StartsWith('src/Portal.Components.Data1/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'Portal.Components.Data1' }
    if ($path.StartsWith('src/Portal.Components.Data/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'Portal.Components.Data' }
    if ($path.StartsWith('src/Portal.DataProviderProof/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'Portal.DataProviderProof' }
    if ($path.StartsWith('src/Portal.HiaBoundaryProof/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'Portal.HiaBoundaryProof' }
    if ($path.StartsWith('src/Portal/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'Portal.Web' }
    if ($path.StartsWith('dev/scripts/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'Dev.Scripts' }
    if ($path.StartsWith('docs/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'PublicDocs' }
    if ($path.StartsWith('dev/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'PublicDev' }
    if ($path.EndsWith('.md', [System.StringComparison]::OrdinalIgnoreCase)) { return 'RootDocs' }
    return 'Other'
}

function Get-GeneratedDirectoryState {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$LikelySource,
        [Parameter(Mandatory = $true)][string]$Recommendation
    )

    $repoPath = (ConvertTo-RepoPath -Path $RelativePath).TrimEnd('/')
    $prefix = "$repoPath/"
    $trackedCount = @($trackedFiles | Where-Object {
            $_.Equals($repoPath, [System.StringComparison]::OrdinalIgnoreCase) -or
            $_.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)
        }).Count
    $absolutePath = Get-AbsolutePath -RelativePath $repoPath
    $exists = (Test-Path -LiteralPath $absolutePath)
    $fileCount = if ($exists) {
        @(Get-ChildItem -LiteralPath $absolutePath -Recurse -File -ErrorAction SilentlyContinue).Count
    }
    else {
        0
    }

    return [pscustomobject][ordered]@{
        Path = $repoPath
        Exists = $exists
        TrackedFileCount = $trackedCount
        FileCount = $fileCount
        LikelySource = $LikelySource
        Recommendation = $Recommendation
    }
}

$includedFiles = @($trackedFiles | Where-Object { Test-IsIncludedTrackedFile -RelativePath $_ })
$fileInventories = New-Object 'System.Collections.Generic.List[object]'

foreach ($relativePath in $includedFiles) {
    $absolutePath = Get-AbsolutePath -RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        continue
    }

    $text = [System.IO.File]::ReadAllText($absolutePath, [System.Text.Encoding]::UTF8)
    $extension = [System.IO.Path]::GetExtension($relativePath).ToLowerInvariant()
    $lineCount = if ([string]::IsNullOrEmpty($text)) { 0 } else { (($text -split "`r?`n").Count) }

    $xmlDocLineCount = if ($extension -eq '.cs') { Get-TextMetric -Text $text -Pattern '^\s*///' } else { 0 }
    $publicProtectedCandidates = if ($extension -eq '.cs') {
        Get-TextMetric -Text $text -Pattern '^\s*(?:public|protected|protected\s+internal|internal\s+protected)\b'
    }
    else {
        0
    }

    $jsdocBlockCount = if ($extension -eq '.js') { Get-TextMetric -Text $text -Pattern '/\*\*' } else { 0 }
    $commentHelpBlockCount = if ($extension -eq '.ps1') { Get-TextMetric -Text $text -Pattern '<#' } else { 0 }
    $markdownHeadingCount = if ($extension -eq '.md') { Get-TextMetric -Text $text -Pattern '^\s{0,3}#{1,6}\s+' } else { 0 }
    $riskCategories = @(Get-RiskCategories -RelativePath (ConvertTo-RepoPath -Path $relativePath) -Text $text)

    $hasMachineReadableLocale =
        (Get-TextMetric -Text $text -Pattern '<lang\b|<l\b|@lang\b') -gt 0
    $hasLegacyBilingualMarker =
        (Get-TextMetric -Text $text -Pattern '中文[:：]|English[:：]') -gt 0

    $candidateReasons = New-Object 'System.Collections.Generic.List[string]'
    if ($extension -eq '.cs' -and $publicProtectedCandidates -gt 0 -and $xmlDocLineCount -eq 0) {
        $candidateReasons.Add('PublicOrProtectedWithoutXmlDoc')
    }
    if ($extension -eq '.cs' -and $xmlDocLineCount -gt 0 -and -not $hasMachineReadableLocale -and $riskCategories.Count -gt 0) {
        $candidateReasons.Add('RiskAreaXmlDocWithoutLangMarker')
    }
    if ($extension -eq '.js' -and $jsdocBlockCount -eq 0) {
        $candidateReasons.Add('JavaScriptWithoutJSDoc')
    }
    if ($extension -eq '.ps1' -and $commentHelpBlockCount -eq 0) {
        $candidateReasons.Add('PowerShellWithoutCommentHelp')
    }
    if (($extension -eq '.aspx' -or $extension -eq '.ascx' -or $extension -eq '.master') -and $riskCategories.Count -gt 0) {
        $candidateReasons.Add('WebFormsSurfaceRiskReview')
    }
    if ($hasLegacyBilingualMarker -and -not $hasMachineReadableLocale) {
        $candidateReasons.Add('LegacyBilingualParagraphCandidate')
    }

    $candidateReasonArray = @($candidateReasons.ToArray())
    $priorityScore = 0
    $priorityScore += $riskCategories.Count * 3
    $priorityScore += $candidateReasonArray.Count * 2
    if ($extension -eq '.cs') { $priorityScore += [Math]::Min($publicProtectedCandidates, 10) }
    if ($extension -eq '.ps1' -and $relativePath.StartsWith('dev/scripts/', [System.StringComparison]::OrdinalIgnoreCase)) { $priorityScore += 3 }

    $fileInventories.Add([pscustomobject][ordered]@{
            Path = ConvertTo-RepoPath -Path $relativePath
            Area = Get-FileArea -RelativePath $relativePath
            Extension = $extension
            LineCount = $lineCount
            XmlDocLineCount = $xmlDocLineCount
            PublicProtectedCandidateCount = $publicProtectedCandidates
            JsdocBlockCount = $jsdocBlockCount
            CommentHelpBlockCount = $commentHelpBlockCount
            MarkdownHeadingCount = $markdownHeadingCount
            HasMachineReadableLocale = $hasMachineReadableLocale
            HasLegacyBilingualMarker = $hasLegacyBilingualMarker
            RiskCategories = $riskCategories
            CandidateReasons = $candidateReasonArray
            PriorityScore = $priorityScore
        })
}

$files = @($fileInventories.ToArray())
$summaryByExtension = @($files | Group-Object Extension | Sort-Object Name | ForEach-Object {
        [pscustomobject][ordered]@{
            Extension = $_.Name
            FileCount = $_.Count
            TotalLineCount = ($_.Group | Measure-Object LineCount -Sum).Sum
            FilesWithMachineReadableLocale = @($_.Group | Where-Object { $_.HasMachineReadableLocale }).Count
            CandidateFileCount = @($_.Group | Where-Object { $_.CandidateReasons.Count -gt 0 }).Count
        }
    })

$summaryByArea = @($files | Group-Object Area | Sort-Object Name | ForEach-Object {
        [pscustomobject][ordered]@{
            Area = $_.Name
            FileCount = $_.Count
            TotalLineCount = ($_.Group | Measure-Object LineCount -Sum).Sum
            CandidateFileCount = @($_.Group | Where-Object { $_.CandidateReasons.Count -gt 0 }).Count
            RiskFileCount = @($_.Group | Where-Object { $_.RiskCategories.Count -gt 0 }).Count
        }
    })

$summaryByRisk = @($files | ForEach-Object {
        $file = $_
        foreach ($category in $file.RiskCategories) {
            [pscustomobject][ordered]@{
                Category = $category
                Path = $file.Path
                PriorityScore = $file.PriorityScore
            }
        }
    } | Group-Object Category | Sort-Object Name | ForEach-Object {
        [pscustomobject][ordered]@{
            Category = $_.Name
            FileCount = $_.Count
            TopFiles = @($_.Group | Sort-Object PriorityScore -Descending | Select-Object -First 8 Path, PriorityScore)
        }
    })

$topCandidates = @($files |
    Where-Object { $_.CandidateReasons.Count -gt 0 } |
    Sort-Object -Property @{ Expression = 'PriorityScore'; Descending = $true }, @{ Expression = 'Path'; Descending = $false } |
    Select-Object -First 40)

$generatedDirectoryStates = @(
    Get-GeneratedDirectoryState -RelativePath 'src/Documentation' -LikelySource 'SHFB Website output' -Recommendation 'Generated output; keep ignored or delete after P15.4 strategy confirms no manual material is needed.'
    Get-GeneratedDirectoryState -RelativePath 'src/Portal/Documentation' -LikelySource 'Doxygen HTML/LaTeX output' -Recommendation 'Generated output; keep ignored or delete after P15.4 strategy confirms no manual material is needed.'
    Get-GeneratedDirectoryState -RelativePath 'src/Portal.Components.Data/Documentation' -LikelySource 'Doxygen HTML/LaTeX output' -Recommendation 'Generated output; keep ignored or delete after P15.4 strategy confirms no manual material is needed.'
    Get-GeneratedDirectoryState -RelativePath 'src/DoxyGen' -LikelySource 'Doxygen config/output placeholder' -Recommendation 'Zero-byte Doxyfile placeholder; evaluate in P15.4/P16 before deleting.'
    Get-GeneratedDirectoryState -RelativePath 'src/Portal.shfbproj' -LikelySource 'SHFB project configuration' -Recommendation 'Configuration input, not generated output; evaluate before adopting or deleting.'
)

$result = [pscustomobject][ordered]@{
    SchemaVersion = '1.0'
    GeneratedUtc = [DateTime]::UtcNow.ToString('O')
    RepositoryRoot = $repoRoot
    Scope = 'Git-tracked .cs, .aspx, .ascx, .master, .js, .ps1 and .md files; generated and temp directories are excluded.'
    IncludedFileCount = $files.Count
    SummaryByExtension = $summaryByExtension
    SummaryByArea = $summaryByArea
    SummaryByRiskCategory = $summaryByRisk
    TopDocumentationCandidates = $topCandidates
    GeneratedDirectoryStates = $generatedDirectoryStates
    Notes = @(
        'Counts are heuristic inventory facts, not documentation quality scores.',
        'C# <lang>/<l> readiness is assessed separately against HIA-Documentation-Sys DotNetDoc support.',
        'Generated directory states are based on current working tree existence and Git tracking.'
    )
}

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    Write-Utf8NoBomFile -Path $OutputJson -Content (($result | ConvertTo-Json -Depth 12) + [Environment]::NewLine)
    Write-Host ('JSON: {0}' -f $OutputJson)
}

if (-not [string]::IsNullOrWhiteSpace($OutputMarkdown)) {
    $markdown = New-Object 'System.Collections.Generic.List[string]'
    $markdown.Add('# Portal 源码与文档化覆盖盘点')
    $markdown.Add('')
    $markdown.Add(('生成时间 UTC：`{0}`' -f $result.GeneratedUtc))
    $markdown.Add('')
    $markdown.Add('## 范围')
    $markdown.Add('')
    $markdown.Add($result.Scope)
    $markdown.Add('')
    $markdown.Add(('纳入文件数：`{0}`。' -f $result.IncludedFileCount))
    $markdown.Add('')
    $markdown.Add('## 按扩展名汇总')
    $markdown.Add('')
    $markdown.Add('| 扩展名 | 文件数 | 总行数 | 含机器可读 locale | 候选文件 |')
    $markdown.Add('| --- | ---: | ---: | ---: | ---: |')
    foreach ($item in $summaryByExtension) {
        $markdown.Add(('| `{0}` | {1} | {2} | {3} | {4} |' -f $item.Extension, $item.FileCount, $item.TotalLineCount, $item.FilesWithMachineReadableLocale, $item.CandidateFileCount))
    }
    $markdown.Add('')
    $markdown.Add('## 按区域汇总')
    $markdown.Add('')
    $markdown.Add('| 区域 | 文件数 | 总行数 | 风险文件 | 候选文件 |')
    $markdown.Add('| --- | ---: | ---: | ---: | ---: |')
    foreach ($item in $summaryByArea) {
        $markdown.Add(('| `{0}` | {1} | {2} | {3} | {4} |' -f $item.Area, $item.FileCount, $item.TotalLineCount, $item.RiskFileCount, $item.CandidateFileCount))
    }
    $markdown.Add('')
    $markdown.Add('## 高优先级候选')
    $markdown.Add('')
    $markdown.Add('| 文件 | 区域 | 分数 | 原因 | 风险类别 |')
    $markdown.Add('| --- | --- | ---: | --- | --- |')
    foreach ($item in $topCandidates | Select-Object -First 25) {
        $markdown.Add(('| `{0}` | `{1}` | {2} | {3} | {4} |' -f $item.Path, $item.Area, $item.PriorityScore, (($item.CandidateReasons) -join ', '), (($item.RiskCategories) -join ', ')))
    }
    $markdown.Add('')
    $markdown.Add('## 生成目录状态')
    $markdown.Add('')
    $markdown.Add('| 路径 | 存在 | 已追踪 | 文件数 | 判断 | 建议 |')
    $markdown.Add('| --- | --- | ---: | ---: | --- | --- |')
    foreach ($item in $generatedDirectoryStates) {
        $markdown.Add(('| `{0}` | {1} | {2} | {3} | {4} | {5} |' -f $item.Path, $item.Exists, $item.TrackedFileCount, $item.FileCount, $item.LikelySource, $item.Recommendation))
    }

    Write-Utf8NoBomFile -Path $OutputMarkdown -Content (($markdown -join [Environment]::NewLine) + [Environment]::NewLine)
    Write-Host ('MARKDOWN: {0}' -f $OutputMarkdown)
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 10
}
else {
    $result
}
