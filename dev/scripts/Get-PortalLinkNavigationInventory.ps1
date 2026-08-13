<#
.SYNOPSIS
<lang>
  <en>Generates a read-only entry-link and navigation inventory for the Portal project.</en>
  <zh-CN>生成 Portal 项目的只读入口链接与导航链路盘点。</zh-CN>
</lang>

<lang>
  <en>Scans Git-tracked Web Forms markup, C#, and JavaScript files for navigation-like references such as href, NavigateUrl, PostBackUrl, form actions, Response.Redirect, Server.Transfer, and common client-side location assignments. The script does not modify source files, databases, IIS, or external configuration.</en>
  <zh-CN>扫描 Git 已追踪的 Web Forms 标记、C# 和 JavaScript 文件，识别 href、NavigateUrl、PostBackUrl、form action、Response.Redirect、Server.Transfer 以及常见客户端 location 赋值等导航痕迹。本脚本不修改源码、数据库、IIS 或外置配置。</zh-CN>
</lang>

.PARAMETER OutputJson
<lang>
  <en>Optional UTF-8 no-BOM JSON output path.</en>
  <zh-CN>可选 UTF-8 无 BOM JSON 输出路径。</zh-CN>
</lang>

.PARAMETER OutputMarkdown
<lang>
  <en>Optional UTF-8 no-BOM Markdown summary output path.</en>
  <zh-CN>可选 UTF-8 无 BOM Markdown 摘要输出路径。</zh-CN>
</lang>

.PARAMETER AsJson
<lang>
  <en>Writes the full inventory object to stdout as JSON.</en>
  <zh-CN>将完整盘点对象以 JSON 写到标准输出。</zh-CN>
</lang>
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

# <lang>
#   <zh-CN>以脚本所在目录推导仓库根目录，保证扫描和可选输出相对当前项目而非当前 shell 目录。</zh-CN>
#   <en>Derive the repository root from the script location so scanning and optional output stay relative to the project, not the caller's shell directory.</en>
# </lang>
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

# <lang>
#   <zh-CN>统一内部路径显示分隔符；该 helper 不解析路径、不访问文件系统。</zh-CN>
#   <en>Normalize internal path separators for display; this helper does not resolve paths or touch the file system.</en>
# </lang>
function ConvertTo-RepoPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    return ($Path -replace '\\', '/')
}

# <lang>
#   <zh-CN>将仓库相对路径解析为扫描用绝对路径；输入边界由调用方的 Git 文件列表限定。</zh-CN>
#   <en>Resolve a repository-relative path for scanning; the caller constrains inputs to the Git file list.</en>
# </lang>
function Get-AbsolutePath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    return Join-Path $repoRoot ((ConvertTo-RepoPath -Path $RelativePath) -replace '/', '\')
}

# <lang>
#   <zh-CN>只纳入已追踪的呈现与业务脚本扩展，排除 WorkZone、生成物、依赖包和本机配置以避免入口噪声。</zh-CN>
#   <en>Include only tracked presentation and application-script extensions, excluding WorkZone, generated output, packages, and local settings from entry noise.</en>
# </lang>
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

# <lang>
#   <zh-CN>依据匹配索引计算一基行号，供盘点证据定位；不改变源文本或匹配结果。</zh-CN>
#   <en>Compute a one-based line number from a match index for inventory evidence without changing source text or match results.</en>
# </lang>
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

# <lang>
#   <zh-CN>规范化目标文本的空白和 HTML 实体，保持原始目标语义供分类与摘要使用。</zh-CN>
#   <en>Normalize target whitespace and HTML entities while preserving target semantics for classification and summaries.</en>
# </lang>
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

# <lang>
#   <zh-CN>按固定优先级给目标分组；分类只是盘点标签，不代表路由可达、授权通过或运行时解析成功。</zh-CN>
#   <en>Classify targets using fixed precedence; categories are inventory labels, not proof of route reachability, authorization, or runtime resolution.</en>
# </lang>
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

# <lang>
#   <zh-CN>在单个已追踪文件内应用受控导航模式并保留行号、目标和短片段；匹配失败必须显式暴露而不静默丢证据。</zh-CN>
#   <en>Apply controlled navigation patterns to one tracked file while retaining line, target, and short snippet; matching failures must surface instead of dropping evidence silently.</en>
# </lang>
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

# <lang>
#   <zh-CN>只读取 Git 索引中的候选文件并集中生成低敏导航事实；脚本不执行修复、提交或运行时探测。</zh-CN>
#   <en>Read only Git-index candidates and aggregate low-sensitivity navigation facts; the script performs no repair, commit, or runtime probing.</en>
# </lang>
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

# <lang>
#   <zh-CN>将原始记录投影为稳定摘要与完整记录集合；PotentialManualOrAdminEntries 只是后续人工复核提示，不是权限结论。</zh-CN>
#   <en>Project raw records into stable summaries and complete records; PotentialManualOrAdminEntries only prompts later review and is not an authorization conclusion.</en>
# </lang>
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

# <lang>
#   <zh-CN>仅在显式提供输出参数时写入 UTF-8 无 BOM 文件；默认路径不落盘，避免扫描意外制造证据目录。</zh-CN>
#   <en>Write UTF-8 no-BOM files only for explicit output parameters; no default path is created, preventing accidental evidence directories.</en>
# </lang>
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
