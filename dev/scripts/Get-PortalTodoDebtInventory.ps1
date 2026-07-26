<#
.SYNOPSIS
    Generates a read-only TODO/deferred-comment debt inventory for W-anp-P16.3.

.DESCRIPTION
    中文：本脚本只读取 Git 已追踪的 C#、Web Forms 标记、JavaScript、PowerShell 和主要 Markdown，
    排除 designer/generated/temp/历史生成目录，并用启发式规则分类 TODO、FIXME、临时、延期、
    后续、待确认等标记。它不改写源码、不构建项目、不生成 API 文档、不访问数据库或网络。
    English: This script reads only Git-tracked C#, Web Forms markup, JavaScript, PowerShell, and
    primary Markdown files, excludes designer/generated/temp/historical generated directories, and
    classifies TODO, FIXME, temporary, deferred, follow-up, and confirmation-needed markers through
    heuristics. It does not rewrite source, build the project, generate API docs, or access databases
    or the network.
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
    throw '无法读取 Git 已追踪文件，无法生成 TODO/延期标记盘点。'
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
    'node_modules/',
    'dev/documentation/jsdoc/node_modules/',
    'dev/documentation/dotnetdoc/node_modules/'
)

$markerPattern = '(?i)(TODO|FIXME|HACK|XXX|UNDONE|待办\s*[:：]|待处理\s*[:：]|待确认|后续|延期|临时(?:策略|实现|方案|写法|处理|占位)|暂不|暂缓|以后|未来|后期|Pending|Deferred|temporary\s+(?:policy|implementation|workaround|placeholder)|follow[- ]?up|needs?\s+confirm|needs?\s+owner)'

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
    if ($repoPath -in @('TASK_STATE.md')) {
        return $true
    }

    foreach ($prefix in $excludedPrefixes) {
        if ($repoPath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    if ($repoPath -match '(?i)(\.designer\.cs$|\.generated\.cs$|/generated/|/obj/|/bin/)') {
        return $true
    }

    return $false
}

function Test-IsIncludedFile {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    if (Test-IsExcludedPath -RelativePath $RelativePath) {
        return $false
    }

    return $includedExtensions.Contains([System.IO.Path]::GetExtension($RelativePath))
}

function Get-AbsolutePath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    return Join-Path $repoRoot ((ConvertTo-RepoPath -Path $RelativePath) -replace '/', '\')
}

function Get-Area {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = ConvertTo-RepoPath -Path $RelativePath
    if ($path.StartsWith('src/Portal/Admin/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'Portal.Admin' }
    if ($path.StartsWith('src/Portal/DesktopModules/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'Portal.DesktopModules' }
    if ($path.StartsWith('src/Portal/Components/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'Portal.Web.Components' }
    if ($path.StartsWith('src/Portal.Components', [System.StringComparison]::OrdinalIgnoreCase)) { return 'Portal.Components' }
    if ($path.StartsWith('src/Portal/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'Portal.Web' }
    if ($path.StartsWith('dev/scripts/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'Dev.Scripts' }
    if ($path.StartsWith('docs/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'PublicDocs' }
    if ($path.StartsWith('dev/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'PublicDev' }
    if ($path.StartsWith('work-zone/', [System.StringComparison]::OrdinalIgnoreCase)) { return 'WorkZone' }
    if ($path.EndsWith('.md', [System.StringComparison]::OrdinalIgnoreCase)) { return 'RootDocs' }
    return 'Other'
}

function Get-RiskCategory {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Text
    )

    $joined = "$RelativePath`n$Text"
    $rules = @(
        @{ Name = 'Security'; Pattern = '(?i)(security|password|credential|auth|cookie|token|encrypt|decrypt|csrf|xss|html|权限|密码|认证|加密)' },
        @{ Name = 'Configuration'; Pattern = '(?i)(config|setting|appSettings|connectionStrings|ExternalCfgPath|配置|设置|连接串)' },
        @{ Name = 'IdentityRole'; Pattern = '(?i)(user|role|login|register|employee|用户|角色|登录|注册|员工)' },
        @{ Name = 'PathUpload'; Pattern = '(?i)(upload|file|path|directory|MapPath|上传|文件|路径|目录)' },
        @{ Name = 'DiagnosticsAudit'; Pattern = '(?i)(diagnostic|log|audit|trace|exception|日志|审计|异常|诊断)' },
        @{ Name = 'DataMigration'; Pattern = '(?i)(sql|database|migration|db|数据|迁移|数据库)' },
        @{ Name = 'ReleaseEnvironment'; Pattern = '(?i)(deploy|release|iis|sql server|target environment|发布|部署|真实环境|目标环境)' },
        @{ Name = 'Documentation'; Pattern = '(?i)(doc|documentation|dotnetdoc|jsdoc|comment|文档|注释)' }
    )

    foreach ($rule in $rules) {
        if ([regex]::IsMatch($joined, $rule.Pattern)) {
            return $rule.Name
        }
    }

    return 'General'
}

function Get-Classification {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Text
    )

    $path = ConvertTo-RepoPath -Path $RelativePath
    $sample = $Text.Trim()

    # <lang>
    #   <zh-CN>扫描器自身会包含待办、修复和延期等匹配规则文本，这些是检测逻辑，不是真实债务。</zh-CN>
    #   <en>The scanner itself contains matching-rule text for debt, fix and deferred markers; those lines describe detection logic rather than real debt.</en>
    # </lang>
    if ($path -eq 'dev/scripts/Get-PortalTodoDebtInventory.ps1' -and
        $sample -match '(?i)(TODO\|FIXME\|HACK\|XXX|markerPattern|Get-Classification)') {
        return 'resolved-stale'
    }

    # <lang>
    #   <zh-CN>目标环境、真实 IIS、企业扫描等无法在本机直接完成，默认归为外部环境依赖。</zh-CN>
    #   <en>Target environment, real IIS, and enterprise scanning work cannot be completed locally and is classified as external-environment dependent by default.</en>
    # </lang>
    if ($sample -match '(?i)(真实\s*IIS|目标环境|生产环境|企业扫描|绿盟|SQL Server 2016|SQL Server 2017|SQL Server 2019|Win7|WinXP|IE6|IE8|IE9|external|target environment|real IIS)') {
        return 'external-env'
    }

    if ($sample -match '(?i)(待确认|需要确认|人工|owner|needs?\s+confirm|needs?\s+owner|TBD|待讨论)') {
        return 'needs-owner-confirmation'
    }

    if ($sample -match '(?i)(后续|后期|未来|远期|延期|暂不|暂缓|later|future|deferred|follow[- ]?up|pending)' -or $path.StartsWith('work-zone/dev/plans/', [System.StringComparison]::OrdinalIgnoreCase)) {
        return 'deferred-plan'
    }

    if ($sample -match '(?i)(已完成|已迁移|已处理|旧|legacy|obsolete|resolved|stale|不再|deprecated)') {
        return 'resolved-stale'
    }

    if ($sample -match '(?i)(TODO|FIXME|HACK|XXX|UNDONE|待办\s*[:：]|待处理\s*[:：])') {
        return 'active'
    }

    if ($sample -match '(?i)(临时(?:策略|实现|方案|写法|处理|占位)|temporary\s+(?:policy|implementation|workaround|placeholder))') {
        return 'active'
    }

    return 'deferred-plan'
}

function Get-Severity {
    param(
        [Parameter(Mandatory = $true)][string]$Classification,
        [Parameter(Mandatory = $true)][string]$RiskCategory
    )

    if ($Classification -eq 'active' -and $RiskCategory -in @('Security', 'Configuration', 'IdentityRole', 'PathUpload', 'DataMigration')) {
        return 'High'
    }

    if ($Classification -in @('needs-owner-confirmation', 'external-env')) {
        return 'Medium'
    }

    if ($Classification -eq 'resolved-stale') {
        return 'Low'
    }

    return 'Normal'
}

function Get-LineCommentContext {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string[]]$Lines,
        [Parameter(Mandatory = $true)][int]$Index
    )

    $line = $Lines[$Index]
    $start = [Math]::Max(0, $Index - 1)
    $end = [Math]::Min($Lines.Length - 1, $Index + 1)
    $context = New-Object 'System.Collections.Generic.List[string]'
    for ($i = $start; $i -le $end; $i++) {
        $context.Add($Lines[$i].Trim()) | Out-Null
    }

    $sample = ($context -join ' ')
    if ($sample.Length -gt 260) {
        $sample = $sample.Substring(0, 260) + '...'
    }

    return [pscustomobject][ordered]@{
        Line = $Index + 1
        Text = $line.Trim()
        Context = $sample
    }
}

$findings = New-Object 'System.Collections.Generic.List[object]'
foreach ($relativePath in $trackedFiles) {
    if (-not (Test-IsIncludedFile -RelativePath $relativePath)) {
        continue
    }

    $absolutePath = Get-AbsolutePath -RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        continue
    }

    $text = [System.IO.File]::ReadAllText($absolutePath, [System.Text.Encoding]::UTF8)
    if (-not [regex]::IsMatch($text, $markerPattern)) {
        continue
    }

    $lines = $text -split "`r?`n"
    for ($index = 0; $index -lt $lines.Length; $index++) {
        $line = $lines[$index]
        if (-not [regex]::IsMatch($line, $markerPattern)) {
            continue
        }

        $context = Get-LineCommentContext -Lines $lines -Index $index
        $classification = Get-Classification -RelativePath $relativePath -Text $context.Context
        $riskCategory = Get-RiskCategory -RelativePath $relativePath -Text $context.Context
        $findings.Add([pscustomobject][ordered]@{
            Path = ConvertTo-RepoPath -Path $relativePath
            Line = $context.Line
            Area = Get-Area -RelativePath $relativePath
            Classification = $classification
            RiskCategory = $riskCategory
            Severity = Get-Severity -Classification $classification -RiskCategory $riskCategory
            Marker = ([regex]::Match($line, $markerPattern).Value)
            Text = $context.Text
            Context = $context.Context
        }) | Out-Null
    }
}

$findingsArray = @($findings.ToArray())
$summaryByClassification = @($findingsArray | Group-Object Classification | Sort-Object Name | ForEach-Object {
        [pscustomobject][ordered]@{ Classification = $_.Name; Count = $_.Count }
    })
$summaryByRisk = @($findingsArray | Group-Object RiskCategory | Sort-Object Name | ForEach-Object {
        [pscustomobject][ordered]@{ RiskCategory = $_.Name; Count = $_.Count }
    })
$summaryByArea = @($findingsArray | Group-Object Area | Sort-Object Name | ForEach-Object {
        [pscustomobject][ordered]@{ Area = $_.Name; Count = $_.Count }
    })

$result = [pscustomobject][ordered]@{
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Scope = 'Git-tracked C#, WebForms markup, JavaScript, PowerShell and Markdown; generated/designer/temp excluded.'
    TotalFindings = $findingsArray.Count
    SummaryByClassification = $summaryByClassification
    SummaryByRiskCategory = $summaryByRisk
    SummaryByArea = $summaryByArea
    Findings = $findingsArray
}

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    Write-Utf8NoBomFile -Path $OutputJson -Content (($result | ConvertTo-Json -Depth 8) + "`r`n")
}

if (-not [string]::IsNullOrWhiteSpace($OutputMarkdown)) {
    $markdown = New-Object 'System.Collections.Generic.List[string]'
    $markdown.Add('# Portal TODO / Deferred Comment Debt Inventory') | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add(("Generated UTC: {0}" -f $result.GeneratedAtUtc)) | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add(("Total findings: {0}" -f $result.TotalFindings)) | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add('## Classification Summary') | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add('| Classification | Count |') | Out-Null
    $markdown.Add('| --- | ---: |') | Out-Null
    foreach ($item in $summaryByClassification) {
        $markdown.Add(("| {0} | {1} |" -f $item.Classification, $item.Count)) | Out-Null
    }

    $markdown.Add('') | Out-Null
    $markdown.Add('## Top Findings') | Out-Null
    $markdown.Add('') | Out-Null
    $markdown.Add('| Classification | Risk | Severity | Location | Marker | Text |') | Out-Null
    $markdown.Add('| --- | --- | --- | --- | --- | --- |') | Out-Null
    foreach ($finding in @($findingsArray | Sort-Object Severity, Classification, Path, Line | Select-Object -First 80)) {
        $safeText = ([string]$finding.Text).Replace('|', '\|')
        if ($safeText.Length -gt 120) {
            $safeText = $safeText.Substring(0, 120) + '...'
        }

        $markdown.Add(("| {0} | {1} | {2} | `{3}:{4}` | `{5}` | {6} |" -f $finding.Classification, $finding.RiskCategory, $finding.Severity, $finding.Path, $finding.Line, $finding.Marker, $safeText)) | Out-Null
    }

    Write-Utf8NoBomFile -Path $OutputMarkdown -Content (($markdown -join "`r`n") + "`r`n")
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 8
}
else {
    $result
}
