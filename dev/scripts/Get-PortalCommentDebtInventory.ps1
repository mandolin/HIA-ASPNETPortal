<#
.SYNOPSIS
    Generates a read-only old-comment debt inventory for W-anp-P15.3.

.DESCRIPTION
<lang>
  <zh-CN>本脚本只读取 Git 已追踪的 C#、Web Forms 标记文件和少量高风险 PowerShell 候选，用启发式规则识别旧双语格式、乱码、客户端 HTML 注释、明确 TODO/延期标记、低价值复述、节点文档缺失和 P16.1 迁移候选；业务“待办”名词和正常后续生命周期描述不按债务计数，且不改写源码、不构建项目、不访问数据库或网络。</zh-CN>
  <en>This script reads only Git-tracked C#, Web Forms markup, and a limited set of high-risk PowerShell candidates. It heuristically identifies legacy bilingual, garbled, client-visible HTML, explicit TODO/deferred markers, low-value, missing-node, and P16.1 migration findings; domain work-item nouns and normal follow-on lifecycle descriptions are not counted as debt, and the script does not rewrite source, build the project, or access databases or the network.</en>
</lang>
#>
[CmdletBinding()]
param(
    [string]$OutputJson,

    [string]$OutputMarkdown,

    [ValidateRange(1, 200)]
    [int]$Top = 30,

    [switch]$AsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$trackedFiles = @(& git -C $repoRoot ls-files)
if ($LASTEXITCODE -ne 0) {
    throw '无法读取 Git 已追踪文件，无法生成旧注释债务盘点。'
}

$primaryExtensions = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
@('.cs', '.aspx', '.ascx', '.master') | ForEach-Object { [void]$primaryExtensions.Add($_) }

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

$commentDebtWeights = @{
    GarbledOrMojibake = 80
    LegacyBilingualFormat = 20
    ClientVisibleHtmlComment = 18
    TodoOrDeferredMarker = 15
    HighRiskScriptCandidate = 12
    MissingNodeDocumentation = 8
    LowValueRestatement = 4
}

# <lang>
#   <zh-CN>只匹配带明确待办或延期意图的标记，避免把“待办”业务名词和普通后续流程描述误报为债务。</zh-CN>
#   <en>Matches only explicit TODO or deferred intent so domain work-item nouns and ordinary follow-on flow descriptions are not reported as debt.</en>
# </lang>
$todoMarkerPattern = '(?i)(TODO|FIXME|HACK|待办\s*[:：]|待处理\s*[:：]|待确认|临时处理|后续\s*(?:任务|治理|实现|规划|确认|补齐|迁移|专题)|债务)'

# <lang>
#   <zh-CN>以 UTF-8 无 BOM 写入可选债务清单产物，并按需创建父目录。</zh-CN>
#   <en>Writes optional debt-inventory artifacts as UTF-8 without BOM and creates the parent directory when needed.</en>
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
#   <zh-CN>将仓库路径统一为正斜杠形式，供过滤和输出稳定复用。</zh-CN>
#   <en>Normalizes repository paths to slash-separated values reused by filtering and stable output.</en>
# </lang>
function ConvertTo-RepoPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    return ($Path -replace '\\', '/')
}

# <lang>
#   <zh-CN>按固定生成目录和临时目录前缀排除不应进入债务盘点的路径。</zh-CN>
#   <en>Excludes paths under fixed generated and temporary prefixes from the debt inventory.</en>
# </lang>
function Test-IsExcludedPath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $repoPath = ConvertTo-RepoPath -Path $RelativePath
    foreach ($prefix in $excludedPrefixes) {
        if ($repoPath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

# <lang>
#   <zh-CN>识别 P25 高风险 PowerShell 候选，保持脚本候选范围显式而有限。</zh-CN>
#   <en>Identifies the bounded P25 high-risk PowerShell candidate set explicitly.</en>
# </lang>
function Test-IsHighRiskScriptCandidate {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $repoPath = ConvertTo-RepoPath -Path $RelativePath
    if (-not $repoPath.StartsWith('dev/scripts/', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $false
    }

    if ([System.IO.Path]::GetExtension($repoPath) -ne '.ps1') {
        return $false
    }

    return $repoPath -match '(Credential|Password|Secret|Token|Cert|Security|Compliance|Publish|Deploy|Release|IIS|Sql|Smoke|Evidence|Hardening)'
}

# <lang>
#   <zh-CN>按主扩展名、排除前缀和高风险脚本例外决定文件是否进入盘点。</zh-CN>
#   <en>Decides inventory inclusion from primary extensions, excluded prefixes, and the high-risk script exception.</en>
# </lang>
function Test-IsIncludedFile {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    if (Test-IsExcludedPath -RelativePath $RelativePath) {
        return $false
    }

    $extension = [System.IO.Path]::GetExtension($RelativePath)
    if ($primaryExtensions.Contains($extension)) {
        return $true
    }

    return Test-IsHighRiskScriptCandidate -RelativePath $RelativePath
}

# <lang>
#   <zh-CN>为一个文件建立稳定的低敏债务状态容器，供后续发现项聚合。</zh-CN>
#   <en>Creates a stable low-sensitivity debt-state container for one file before findings are aggregated.</en>
# </lang>
function New-FileDebtState {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Extension
    )

    return [pscustomobject][ordered]@{
        Path = ConvertTo-RepoPath -Path $RelativePath
        Extension = $Extension
        IsGeneratedOrDesigner = $RelativePath -match '\.(Designer|generated|g)\.cs$'
        IsHighRiskArea = $RelativePath -match '(^src[\\/]+Portal[\\/]+(Components|Admin|DesktopModules)|Global\.asax\.cs$|src[\\/]+Portal\.Components\.Data|^dev[\\/]+scripts[\\/]+)'
        HasMachineReadableLocale = $false
        FindingCounts = [ordered]@{}
        Samples = [System.Collections.Generic.List[object]]::new()
        PriorityScore = 0
    }
}

# <lang>
#   <zh-CN>把分类、严重度、行号和摘要加入文件债务状态，保持发现项字段统一。</zh-CN>
#   <en>Adds category, severity, line, and summary data to a file debt state with a uniform finding shape.</en>
# </lang>
function Add-Finding {
    param(
        [Parameter(Mandatory = $true)]$State,
        [Parameter(Mandatory = $true)][string]$Type,
        [Parameter(Mandatory = $true)][string]$Severity,
        [Parameter(Mandatory = $true)][int]$LineNumber,
        [Parameter(Mandatory = $true)][string]$Message,
        [string]$Text
    )

    if (-not $State.FindingCounts.Contains($Type)) {
        $State.FindingCounts[$Type] = 0
    }

    $State.FindingCounts[$Type] = [int]$State.FindingCounts[$Type] + 1
    $weight = if ($commentDebtWeights.ContainsKey($Type)) { [int]$commentDebtWeights[$Type] } else { 1 }
    $State.PriorityScore += $weight

    if ($State.Samples.Count -lt 10) {
        $trimmedText = if ([string]::IsNullOrWhiteSpace($Text)) { '' } else { $Text.Trim() }
        if ($trimmedText.Length -gt 180) {
            $trimmedText = $trimmedText.Substring(0, 180) + '...'
        }

        $State.Samples.Add([pscustomobject][ordered]@{
            Type = $Type
            Severity = $Severity
            Line = $LineNumber
            Message = $Message
            Text = $trimmedText
        }) | Out-Null
    }
}

# <lang>
#   <zh-CN>识别注释债务扫描器自身的规则文本；这些行保留在源码中供维护，但不应成为被盘点的业务债务。</zh-CN>
#   <en>Identifies rule text belonging to the comment-debt scanner itself; those lines remain maintainable source but must not become reported business debt.</en>
# </lang>
function Test-IsScannerRuleText {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Line
    )

    if ($RelativePath -ne 'dev/scripts/Get-PortalCommentDebtInventory.ps1') {
        return $false
    }

    return $Line -match '(?i)(TODO|FIXME|HACK|TodoOrDeferredMarker|Add-Finding|待办|待确认|临时处理|后续|债务|scanner|扫描器)'
}

# <lang>
#   <zh-CN>检查 C# 节点前方是否存在相邻 XML 文档，避免把已有节点说明重复列为缺失。</zh-CN>
#   <en>Checks for nearby XML documentation before a C# node so existing node coverage is not reported as missing.</en>
# </lang>
function Test-HasNearbyXmlDocumentation {
    param(
        [string[]]$Lines,
        [Parameter(Mandatory = $true)][int]$Index
    )

    if ($null -eq $Lines -or $Lines.Length -eq 0) {
        return $false
    }

    $cursor = $Index - 1
    $checkedMeaningfulLines = 0
    while ($cursor -ge 0 -and $checkedMeaningfulLines -lt 5) {
        $previous = $Lines[$cursor].Trim()
        if ([string]::IsNullOrWhiteSpace($previous)) {
            $cursor--
            continue
        }

        if ($previous.StartsWith('///', [System.StringComparison]::Ordinal)) {
            return $true
        }

        if ($previous -match '^\s*\[.+\]\s*$') {
            $cursor--
            $checkedMeaningfulLines++
            continue
        }

        return $false
    }

    return $false
}

# <lang>
#   <zh-CN>用受限正则识别需要人工复核的 C# 类型和成员声明。</zh-CN>
#   <en>Uses bounded patterns to identify C# type and member declarations requiring review.</en>
# </lang>
function Test-IsCSharpNodeDeclaration {
    param([string]$Line)

    $trimmed = $Line.Trim()
    if ($trimmed.StartsWith('//', [System.StringComparison]::Ordinal) -or
        $trimmed.StartsWith('///', [System.StringComparison]::Ordinal) -or
        $trimmed.StartsWith('[', [System.StringComparison]::Ordinal)) {
        return $false
    }

    if ($trimmed -match '^\s*(public|protected|internal|private\s+protected|protected\s+internal)\s+(static\s+|sealed\s+|abstract\s+|partial\s+|virtual\s+|override\s+|async\s+|readonly\s+|unsafe\s+)*((class|struct|interface|enum|delegate)\b|[A-Za-z_][\w<>,\[\]\.?]+\s+[A-Za-z_]\w*\s*(\(|\{|=>|;))') {
        return $true
    }

    if ($trimmed -match '^\s*(public|protected|internal)\s+[A-Za-z_]\w*\s*\(') {
        return $true
    }

    return $false
}

# <lang>
#   <zh-CN>识别仅复述下一行代码的低价值注释，保持启发式结果可解释。</zh-CN>
#   <en>Identifies comments that merely restate the next code line while keeping the heuristic explainable.</en>
# </lang>
function Test-IsLowValueComment {
    param([string]$CommentText)

    $text = $CommentText.Trim()
    if ($text.Length -gt 80) {
        return $false
    }

    return $text -match '^(//+\s*)?(获取|设置|初始化|调用|返回|循环|遍历|判断|检查|创建|删除|更新|保存|读取|绑定|按钮|控件|字段|属性|方法|事件|区域性|命名空间|导入)\b'
}

# <lang>
#   <zh-CN>按发现类别和安全影响映射稳定严重度，供排序和汇总使用。</zh-CN>
#   <en>Maps finding categories to stable severities for sorting and aggregation.</en>
# </lang>
function Get-Severity {
    param([Parameter(Mandatory = $true)][string]$Type)

    switch ($Type) {
        'GarbledOrMojibake' { 'High' }
        'ClientVisibleHtmlComment' { 'Medium' }
        'LegacyBilingualFormat' { 'Medium' }
        'TodoOrDeferredMarker' { 'Medium' }
        'HighRiskScriptCandidate' { 'Medium' }
        default { 'Low' }
    }
}

$includedFiles = @($trackedFiles | Where-Object { Test-IsIncludedFile -RelativePath $_ })
$fileSummaries = [System.Collections.Generic.List[object]]::new()
$globalCounts = [ordered]@{}

foreach ($relativePath in $includedFiles) {
    $absolutePath = Join-Path $repoRoot $relativePath
    $extension = [System.IO.Path]::GetExtension($relativePath)
    $content = [System.IO.File]::ReadAllText($absolutePath, [System.Text.Encoding]::UTF8)
    $lines = $content -split "`r?`n"
    $state = New-FileDebtState -RelativePath $relativePath -Extension $extension
    $state.HasMachineReadableLocale = $content -match '(<lang>|<l\s+locale=|@lang)'

    if (Test-IsHighRiskScriptCandidate -RelativePath $relativePath) {
        Add-Finding -State $state -Type 'HighRiskScriptCandidate' -Severity 'Medium' -LineNumber 1 -Message '高风险脚本候选，P16 或脚本专项需要补齐脚本注释和验证说明。' -Text $relativePath
    }

    for ($index = 0; $index -lt $lines.Length; $index++) {
        $line = $lines[$index]
        $lineNumber = $index + 1

        if ($line -match '(�|锟斤拷|Ã.|æ|ä¸|Â)') {
            Add-Finding -State $state -Type 'GarbledOrMojibake' -Severity 'High' -LineNumber $lineNumber -Message '疑似乱码或 mojibake，需要按上下文恢复；恢复不了则删除并记录。' -Text $line
        }

        if ($line -match '(中文\s*[:：]\s*|English\s*:\s*|中文\s*/\s*English)') {
            Add-Finding -State $state -Type 'LegacyBilingualFormat' -Severity 'Medium' -LineNumber $lineNumber -Message '旧双语格式，P16.1 迁移时应转为 `<lang>` / `<l>`。' -Text $line
        }

        if ($extension -in @('.aspx', '.ascx', '.master') -and
            $line -match '<!--' -and
            $line -notmatch '<!\[endif\]' -and
            $line -notmatch '<!--\[if') {
            Add-Finding -State $state -Type 'ClientVisibleHtmlComment' -Severity 'Medium' -LineNumber $lineNumber -Message '客户端可见 HTML 注释；如为开发说明，应改为 Web Forms 服务端注释。' -Text $line
        }

        if ([regex]::IsMatch($line, $todoMarkerPattern) -and
            -not (Test-IsScannerRuleText -RelativePath $relativePath -Line $line)) {
            Add-Finding -State $state -Type 'TodoOrDeferredMarker' -Severity 'Medium' -LineNumber $lineNumber -Message '存在待办或延期标记，需要在 P15.3/P16 输入中分类。' -Text $line
        }

        if ($line.TrimStart().StartsWith('//', [System.StringComparison]::Ordinal) -and (Test-IsLowValueComment -CommentText $line)) {
            Add-Finding -State $state -Type 'LowValueRestatement' -Severity 'Low' -LineNumber $lineNumber -Message '疑似低价值复述注释，后续应改为解释意图、风险或边界。' -Text $line
        }

        if ($extension -eq '.cs' -and -not $state.IsGeneratedOrDesigner -and (Test-IsCSharpNodeDeclaration -Line $line)) {
            if (-not (Test-HasNearbyXmlDocumentation -Lines $lines -Index $index)) {
                Add-Finding -State $state -Type 'MissingNodeDocumentation' -Severity 'Low' -LineNumber $lineNumber -Message '公开/受保护/内部节点缺少相邻 XML 文档注释。' -Text $line
            }
        }
    }

    foreach ($key in $state.FindingCounts.Keys) {
        if (-not $globalCounts.Contains($key)) {
            $globalCounts[$key] = 0
        }

        $globalCounts[$key] = [int]$globalCounts[$key] + [int]$state.FindingCounts[$key]
    }

    if ($state.IsHighRiskArea) {
        $state.PriorityScore += 10
    }

    if ($state.FindingCounts.Count -gt 0) {
        $fileSummaries.Add($state) | Out-Null
    }
}

$priorityFiles = @($fileSummaries | Sort-Object -Property @{ Expression = 'PriorityScore'; Descending = $true }, @{ Expression = 'Path'; Ascending = $true } | Select-Object -First $Top)

$result = [pscustomobject][ordered]@{
    GeneratedAt = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    Scope = 'Git-tracked .cs/.aspx/.ascx/.master plus high-risk dev/scripts/*.ps1 candidates'
    IncludedFileCount = $includedFiles.Count
    FilesWithFindings = $fileSummaries.Count
    FindingCounts = $globalCounts
    CommentConditioningDeadline = 'W-anp-P16.5 验收前完成全量注释调理，或逐项登记为延期债务。'
    P16MigrationRule = 'W-anp-P16.1 启动 `<lang>` / `<l>` 全量迁移与注释丰富度提升；P15.3/P15.4 只提供输入。'
    PriorityFiles = $priorityFiles
    AllFilesWithFindings = $fileSummaries
}

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    Write-Utf8NoBomFile -Path $OutputJson -Content ($result | ConvertTo-Json -Depth 12)
}

if (-not [string]::IsNullOrWhiteSpace($OutputMarkdown)) {
    $markdown = [System.Collections.Generic.List[string]]::new()
    $markdown.Add('# W-anp-P15.3 旧注释债务 Inventory') | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add("生成时间：$($result.GeneratedAt)") | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add('## 范围') | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add("- $($result.Scope)") | Out-Null
    $markdown.Add("- 纳入文件数：$($result.IncludedFileCount)") | Out-Null
    $markdown.Add("- 有债务命中的文件数：$($result.FilesWithFindings)") | Out-Null
    $markdown.Add("- 截止约束：$($result.CommentConditioningDeadline)") | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add('## 债务类型统计') | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add('| 类型 | 命中数 | 处理建议 |') | Out-Null
    $markdown.Add('| --- | ---: | --- |') | Out-Null
    foreach ($entry in $globalCounts.GetEnumerator() | Sort-Object Name) {
        $advice = switch ($entry.Key) {
            'GarbledOrMojibake' { 'P15.3 可少量直接恢复；无法恢复的列入待确认。' }
            'LegacyBilingualFormat' { 'P16.1 转为 `<lang>` / `<l>`。' }
            'ClientVisibleHtmlComment' { '若为开发说明，迁移为 `<%-- --%>` 服务端注释。' }
            'TodoOrDeferredMarker' { '分类为真实待办、历史残留或可删除说明。' }
            'HighRiskScriptCandidate' { 'P16 或脚本专项补脚本说明和验证边界。' }
            'MissingNodeDocumentation' { '按优先级补节点级文档化注释。' }
            'LowValueRestatement' { '改成意图、风险、边界或删除。' }
            default { '人工复核。' }
        }

        $markdown.Add('| `' + $entry.Key + '` | ' + $entry.Value + ' | ' + $advice + ' |') | Out-Null
    }
    $markdown.Add('') | Out-Null
    $markdown.Add('## 优先候选文件') | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add('| 优先级 | 文件 | 分数 | 主要债务 | 样例 |') | Out-Null
    $markdown.Add('| ---: | --- | ---: | --- | --- |') | Out-Null
    $rank = 1
    foreach ($file in $priorityFiles) {
        $counts = @($file.FindingCounts.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join '<br>'
        $sample = ''
        if ($file.Samples.Count -gt 0) {
            $first = $file.Samples[0]
            $sample = "L$($first.Line) $($first.Type): $($first.Text)"
            $sample = $sample.Replace('|', '\|')
        }

        $markdown.Add('| ' + $rank + ' | `' + $file.Path + '` | ' + $file.PriorityScore + ' | ' + $counts + ' | ' + $sample + ' |') | Out-Null
        $rank++
    }
    $markdown.Add('') | Out-Null
    $markdown.Add('## 使用说明') | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add('1. 本清单是启发式输入，不等同于最终代码审查结论。') | Out-Null
    $markdown.Add('2. `LegacyBilingualFormat` 不是错误，而是 P16.1 迁移输入。') | Out-Null
    $markdown.Add('3. `MissingNodeDocumentation` 以公开、受保护和内部 C# 节点为主，Designer/generated 文件不参与节点缺失扫描。') | Out-Null
    $markdown.Add('4. PowerShell 本轮只列高风险候选，不做全量注释质量判断。') | Out-Null

    Write-Utf8NoBomFile -Path $OutputMarkdown -Content ($markdown -join "`r`n")
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 12
}
else {
    $result | Select-Object GeneratedAt, IncludedFileCount, FilesWithFindings, FindingCounts, CommentConditioningDeadline
}
