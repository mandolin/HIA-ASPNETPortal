<#
.SYNOPSIS
    Generates a read-only old-comment debt inventory for W-anp-P15.3.

.DESCRIPTION
    中文：本脚本只读取 Git 已追踪的 C#、Web Forms 标记文件，以及少量高风险 PowerShell 脚本候选，
    用启发式规则识别旧双语格式、乱码或 mojibake、客户端 HTML 注释、TODO/FIXME、低价值复述注释、
    节点文档缺失和 P16.1 全量迁移候选。脚本不改写源码、不构建项目、不访问数据库或网络。
    English: This script reads only Git-tracked C#, Web Forms markup, and a limited set of high-risk
    PowerShell script candidates. It uses heuristic rules to find legacy bilingual comments, garbled
    or mojibake text, client-visible HTML comments, TODO/FIXME markers, low-value restatement comments,
    missing node documentation, and P16.1 migration candidates. It does not rewrite source, build the
    project, access databases, or use the network.
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

function Test-IsLowValueComment {
    param([string]$CommentText)

    $text = $CommentText.Trim()
    if ($text.Length -gt 80) {
        return $false
    }

    return $text -match '^(//+\s*)?(获取|设置|初始化|调用|返回|循环|遍历|判断|检查|创建|删除|更新|保存|读取|绑定|按钮|控件|字段|属性|方法|事件|区域性|命名空间|导入)\b'
}

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

        if ($line -match '(TODO|FIXME|HACK|待办|待确认|临时处理|后续|债务)') {
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
