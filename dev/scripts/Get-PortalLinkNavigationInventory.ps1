<#
.SYNOPSIS
.LANG en
Generates a read-only entry-link and navigation inventory for the Portal project.

.LANG zh-CN
生成 Portal 项目的只读入口链接与导航链路盘点。

.LANG en
Scans Git-tracked Web Forms markup, C#, and JavaScript files for navigation-like
references such as href, NavigateUrl, PostBackUrl, form actions,
Response.Redirect, Server.Transfer, and common client-side location assignments.
The script does not modify source files, databases, IIS, or external configuration.

.LANG zh-CN
扫描 Git 已追踪的 Web Forms 标记、C# 和 JavaScript 文件，识别 href、NavigateUrl、
PostBackUrl、form action、Response.Redirect、Server.Transfer 以及常见客户端 location
赋值等导航痕迹。本脚本不修改源码、数据库、IIS 或外置配置。

.PARAMETER OutputJson
.LANG en
Optional UTF-8 no-BOM JSON output path.

.LANG zh-CN
可选 UTF-8 无 BOM JSON 输出路径。

.PARAMETER OutputMarkdown
.LANG en
Optional UTF-8 no-BOM Markdown summary output path.

.LANG zh-CN
可选 UTF-8 无 BOM Markdown 摘要输出路径。

.PARAMETER AsJson
.LANG en
Writes the full inventory object to stdout as JSON.

.LANG zh-CN
将完整盘点对象以 JSON 写到标准输出。
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

function Get-AbsolutePath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    return Join-Path $repoRoot ((ConvertTo-RepoPath -Path $RelativePath) -replace '/', '\')
}

function Test-IsIncludedFile {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    # <lang>
    #   <zh-CN>入口治理先以已追踪的业务源码为边界，避免把生成物、本机配置和依赖包噪声误判为正式入口。</zh-CN>
    #   <en>Entry governance starts from tracked application sources, avoiding generated files, local settings, and package noise.</en>
    # </lang>
    $repoPath = ConvertTo-RepoPath -Path $RelativePath
    $extension = [System.IO.Path]::GetExtension($repoPath)
    $fileName = [System.IO.Path]::GetFileName($repoPath)
    if ($fileName -in @('package.json', 'package-lock.json')) {
        return $false
    }

    $includedExtensions = @('.aspx', '.ascx', '.master', '.cs', '.js')
    if ($includedExtensions -notcontains $extension) {
        return $false
    }

    $excludedPrefixes = @(
        'temp/',
        'work-zone/',
        '.vscode/',
        'src/Documentation/',
        'src/DoxyGen/',
        'src/Portal/bin/',
        'src/Portal/obj/',
        'src/packages/',
        'node_modules/',
        'dev/documentation/jsdoc/node_modules/',
        'dev/documentation/dotnetdoc/node_modules/'
    )

    foreach ($prefix in $excludedPrefixes) {
        if ($repoPath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $false
        }
    }

    return $true
}

function Get-LineNumber {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][int]$Index
    )

    if ($Index -le 0) {
        return 1
    }

    return ([regex]::Matches($Text.Substring(0, $Index), "`n")).Count + 1
}

function Get-NormalizedTarget {
    param([AllowEmptyString()][string]$Value)

    if ($null -eq $Value) {
        return ''
    }

    $trimmed = $Value.Trim()
    $trimmed = $trimmed -replace '&amp;', '&'
    $trimmed = $trimmed -replace '\s+', ' '
    return $trimmed
}

function Get-TargetCategory {
    param([AllowEmptyString()][string]$Target)

    if ([string]::IsNullOrWhiteSpace($Target)) {
        return 'Empty'
    }

    if ($Target -match '^(?i)(https?:)?//') {
        return 'ExternalUrl'
    }

    if ($Target -match '^(?i)(mailto:|tel:|javascript:)') {
        return 'Protocol'
    }

    if ($Target.StartsWith('#')) {
        return 'Anchor'
    }

    if ($Target -match '(?i)\.(aspx|ascx|ashx|asmx)(\?|#|$)') {
        return 'PortalPage'
    }

    if ($Target -match '^(~?/|/)?Admin/|^(~?/|/)?DesktopModules/|^(~?/|/)?Mobile') {
        return 'PortalPath'
    }

    if ($Target -match '^(?i)(~/|/)') {
        return 'ApplicationPath'
    }

    if ($Target -match '^(?i)(\.{1,2}/)') {
        return 'RelativePath'
    }

    if ($Target -match '(<%|DataBinder|Eval\(|ResolveUrl|Get.*Url|Request\.|Response\.)') {
        return 'DynamicExpression'
    }

    return 'LiteralOrUnknown'
}

function Get-NavigationRecords {
    param(
        [Parameter(Mandatory = $true)]$RelativePath,
        [Parameter(Mandatory = $true)]$Text
    )

    $sourcePath = [string]$RelativePath
    $sourceText = [string]$Text
    # <lang>
    #   <zh-CN>这些规则只负责发现“疑似导航痕迹”，不直接判断业务可达性；后续阶段还要结合 Tab、Module、权限和 Profile gate 复核。</zh-CN>
    #   <en>These rules only detect navigation-like traces; later phases still correlate them with Tab, Module, permission, and Profile-gate data.</en>
    # </lang>
    $patterns = @(
        @{ Kind = 'MarkupHref'; Pattern = '(?i)\bhref\s*=\s*["'']([^"'']+)["'']' },
        @{ Kind = 'MarkupNavigateUrl'; Pattern = '(?i)\bNavigateUrl\s*=\s*["'']([^"'']+)["'']' },
        @{ Kind = 'MarkupPostBackUrl'; Pattern = '(?i)\bPostBackUrl\s*=\s*["'']([^"'']+)["'']' },
        @{ Kind = 'MarkupAction'; Pattern = '(?i)\baction\s*=\s*["'']([^"'']+)["'']' },
        @{ Kind = 'ResponseRedirect'; Pattern = '(?i)\bResponse\.Redirect\s*\(\s*["'']([^"'']+)["'']' },
        @{ Kind = 'ServerTransfer'; Pattern = '(?i)\bServer\.Transfer\s*\(\s*["'']([^"'']+)["'']' },
        @{ Kind = 'ClientLocation'; Pattern = '(?i)\b(?:window\.)?location(?:\.href)?\s*=\s*["'']([^"'']+)["'']' },
        @{ Kind = 'OpenWindow'; Pattern = '(?i)\bwindow\.open\s*\(\s*["'']([^"'']+)["'']' }
    )

    $records = New-Object 'System.Collections.Generic.List[object]'

    foreach ($rule in $patterns) {
        $pattern = [string]$rule.Pattern
        $options = [System.Text.RegularExpressions.RegexOptions](
            [int][System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor
            [int][System.Text.RegularExpressions.RegexOptions]::Multiline)
        $regex = [System.Text.RegularExpressions.Regex]::new($pattern, $options)
        $matches = $regex.Matches($sourceText)

        foreach ($match in $matches) {
            try {
                $target = Get-NormalizedTarget -Value $match.Groups[1].Value
                $lineNumber = Get-LineNumber -Text $sourceText -Index $match.Index
                $category = Get-TargetCategory -Target $target
                $record = [pscustomobject]([ordered]@{
                        Source = ConvertTo-RepoPath -Path $sourcePath
                        Line = $lineNumber
                        Kind = $rule.Kind
                        Target = $target
                        Category = $category
                        Snippet = ($match.Value -replace '\s+', ' ').Trim()
                    })
                $records.Add($record)
            }
            catch {
                throw ('Rule {0} failed at index {1}: {2}' -f $rule.Kind, $match.Index, $_.Exception.Message)
            }
        }
    }

    return $records.ToArray()
}

$trackedFiles = @(& git -C $repoRoot ls-files)
if ($LASTEXITCODE -ne 0) {
    throw '无法读取 Git 已追踪文件，无法生成入口链接盘点。'
}

$includedFiles = @($trackedFiles | Where-Object { Test-IsIncludedFile -RelativePath $_ })
$records = New-Object 'System.Collections.Generic.List[object]'

# <lang>
#   <zh-CN>逐文件读取采用 UTF-8 无 BOM 口径；若旧文件存在编码异常，应让扫描失败暴露问题，而不是静默吞掉入口证据。</zh-CN>
#   <en>Files are read as UTF-8 no-BOM; encoding issues should surface during scanning instead of silently hiding navigation evidence.</en>
# </lang>
foreach ($relativePath in $includedFiles) {
    $absolutePath = Get-AbsolutePath -RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        continue
    }

    $text = [System.IO.File]::ReadAllText($absolutePath, [System.Text.UTF8Encoding]::new($false))
    try {
        foreach ($record in (Get-NavigationRecords -RelativePath ([string]$relativePath) -Text ([string]$text))) {
            $records.Add($record)
        }
    }
    catch {
        throw ("Failed to scan navigation records in {0}: {1}`n{2}" -f $relativePath, $_.Exception.Message, $_.ScriptStackTrace)
    }
}

$byKind = @($records | Group-Object Kind | Sort-Object Name | ForEach-Object {
        [pscustomobject][ordered]@{ Kind = $_.Name; Count = $_.Count }
    })
$byCategory = @($records | Group-Object Category | Sort-Object Name | ForEach-Object {
        [pscustomobject][ordered]@{ Category = $_.Name; Count = $_.Count }
    })
$topSources = @($records |
    Group-Object Source |
    Sort-Object -Property @{ Expression = 'Count'; Descending = $true }, Name |
    Select-Object -First 20 |
    ForEach-Object {
        [pscustomobject][ordered]@{ Source = $_.Name; Count = $_.Count }
    })

$potentialManualEntries = @($records |
    Where-Object {
        $_.Category -in @('PortalPage', 'PortalPath', 'ApplicationPath') -and
        $_.Target -match '(?i)(Admin/|Diagnostic|ModuleCatalog|SystemHealth|ThemeSettings|CollaborationItems|BusinessApplications|WorkItems)'
    } |
    Sort-Object Source, Line, Target)

$inventory = [pscustomobject][ordered]@{
    GeneratedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Scope = 'Git-tracked Portal entry-link and navigation references.'
    FilesScanned = $includedFiles.Count
    LinkRecords = $records.Count
    ByKind = $byKind
    ByCategory = $byCategory
    TopSources = $topSources
    PotentialManualOrAdminEntries = @($potentialManualEntries)
    Records = @($records | Sort-Object Source, Line, Kind, Target)
}

$markdownLines = New-Object 'System.Collections.Generic.List[string]'
$markdownLines.Add('# Portal 入口链接与导航链路盘点')
$markdownLines.Add('')
$markdownLines.Add('生成时间 UTC：' + $inventory.GeneratedUtc)
$markdownLines.Add('')
$markdownLines.Add('## 摘要')
$markdownLines.Add('')
$markdownLines.Add('| 指标 | 数值 |')
$markdownLines.Add('| --- | --- |')
$markdownLines.Add('| 扫描文件 | ' + $inventory.FilesScanned + ' |')
$markdownLines.Add('| 链接记录 | ' + $inventory.LinkRecords + ' |')
$markdownLines.Add('| 潜在后台/手动入口记录 | ' + $inventory.PotentialManualOrAdminEntries.Count + ' |')
$markdownLines.Add('')
$markdownLines.Add('## 按类型')
$markdownLines.Add('')
$markdownLines.Add('| 类型 | 数量 |')
$markdownLines.Add('| --- | --- |')
foreach ($item in $byKind) {
    $markdownLines.Add('| ' + $item.Kind + ' | ' + $item.Count + ' |')
}

$markdownLines.Add('')
$markdownLines.Add('## 按目标分类')
$markdownLines.Add('')
$markdownLines.Add('| 分类 | 数量 |')
$markdownLines.Add('| --- | --- |')
foreach ($item in $byCategory) {
    $markdownLines.Add('| ' + $item.Category + ' | ' + $item.Count + ' |')
}

$markdownLines.Add('')
$markdownLines.Add('## 链接记录最多的文件')
$markdownLines.Add('')
$markdownLines.Add('| 文件 | 数量 |')
$markdownLines.Add('| --- | --- |')
foreach ($item in $topSources) {
    $markdownLines.Add('| `' + $item.Source + '` | ' + $item.Count + ' |')
}

$markdownLines.Add('')
$markdownLines.Add('## 潜在后台/手动入口样例')
$markdownLines.Add('')
$markdownLines.Add('| 文件 | 行 | 类型 | 目标 |')
$markdownLines.Add('| --- | --- | --- | --- |')
foreach ($item in @($potentialManualEntries | Select-Object -First 40)) {
    $target = ($item.Target -replace '\|', '\|')
    $markdownLines.Add('| `' + $item.Source + '` | ' + $item.Line + ' | ' + $item.Kind + ' | `' + $target + '` |')
}

$json = $inventory | ConvertTo-Json -Depth 8
$markdown = ($markdownLines -join [Environment]::NewLine) + [Environment]::NewLine

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    Write-Utf8NoBomFile -Path $OutputJson -Content $json
}

if (-not [string]::IsNullOrWhiteSpace($OutputMarkdown)) {
    Write-Utf8NoBomFile -Path $OutputMarkdown -Content $markdown
}

if ($AsJson) {
    $json
}
else {
    Write-Host ('Files scanned: {0}' -f $inventory.FilesScanned)
    Write-Host ('Link records: {0}' -f $inventory.LinkRecords)
    Write-Host ('Potential manual/admin entries: {0}' -f $inventory.PotentialManualOrAdminEntries.Count)
    if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
        Write-Host ('JSON: {0}' -f $OutputJson)
    }

    if (-not [string]::IsNullOrWhiteSpace($OutputMarkdown)) {
        Write-Host ('Markdown: {0}' -f $OutputMarkdown)
    }
}
