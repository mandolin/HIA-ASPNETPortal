<#
.SYNOPSIS
    Generates a read-only TODO/deferred-comment debt inventory for W-anp-P16.3.

.DESCRIPTION
<lang>
  <zh-CN>本脚本只读取 Git 已追踪的 C#、Web Forms、JavaScript、PowerShell 和主要 Markdown，排除 designer/generated/temp/历史生成目录，并用启发式规则分类 TODO、FIXME、临时、延期、后续和待确认标记；不改写源码、不构建项目、不生成 API 文档、不访问数据库或网络。</zh-CN>
  <en>This script reads only Git-tracked C#, Web Forms, JavaScript, PowerShell, and primary Markdown, excludes designer/generated/temp/historical directories, and heuristically classifies TODO, FIXME, temporary, deferred, follow-up, and confirmation-needed markers without rewriting source, building the project, generating API docs, or accessing databases or the network.</en>
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

# <lang>
#   <zh-CN>以 UTF-8 无 BOM 写入可选 TODO 债务产物，并按需创建父目录。</zh-CN>
#   <en>Writes optional TODO-debt artifacts as UTF-8 without BOM and creates the parent directory when needed.</en>
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
#   <zh-CN>将仓库路径统一为正斜杠形式，供过滤、区域和输出比较复用。</zh-CN>
#   <en>Normalizes repository paths to slash-separated values reused by filtering, areas, and output comparisons.</en>
# </lang>
function ConvertTo-RepoPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    return ($Path -replace '\\', '/')
}

# <lang>
#   <zh-CN>按固定生成、临时和工具依赖目录前缀排除不应进入 TODO 盘点的路径。</zh-CN>
#   <en>Excludes generated, temporary, and tool-dependency prefixes from the TODO inventory.</en>
# </lang>
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

# <lang>
#   <zh-CN>按允许扩展名和排除前缀决定文件是否进入 TODO/延期标记盘点。</zh-CN>
#   <en>Decides TODO/deferred inventory inclusion from allowed extensions and excluded prefixes.</en>
# </lang>
function Test-IsIncludedFile {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    if (Test-IsExcludedPath -RelativePath $RelativePath) {
        return $false
    }

    return $includedExtensions.Contains([System.IO.Path]::GetExtension($RelativePath))
}

# <lang>
#   <zh-CN>把规范化相对路径解析为仓库内绝对路径，保持盘点根边界固定。</zh-CN>
#   <en>Resolves a normalized relative path inside the repository while keeping the inventory root fixed.</en>
# </lang>
function Get-AbsolutePath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    return Join-Path $repoRoot ((ConvertTo-RepoPath -Path $RelativePath) -replace '/', '\')
}

# <lang>
#   <zh-CN>按路径前缀把文件归入稳定区域，供 TODO 债务按域汇总。</zh-CN>
#   <en>Assigns files to stable path-based areas for domain-level TODO debt summaries.</en>
# </lang>
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

# <lang>
#   <zh-CN>根据路径和正文关键词归类配置、安全、身份、路径、异常、审计、数据和发布风险。</zh-CN>
#   <en>Classifies configuration, security, identity, path, diagnostics, audit, data, and release risks from path and text keywords.</en>
# </lang>
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

# <lang>
#   <zh-CN>把匹配到的 TODO/延期文本映射为稳定分类；扫描器自身的规则描述单独标记，避免把检测逻辑误报为业务债务。</zh-CN>
#   <en>Maps matched TODO/deferred text to a stable classification and separates scanner rule descriptions so detection logic is not reported as business debt.</en>
# </lang>
function Get-Classification {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Text
    )

    $path = ConvertTo-RepoPath -Path $RelativePath
    $sample = $Text.Trim()

    # <lang>
    #   <zh-CN>两个注释债务扫描器自身会包含待办、修复和延期等匹配规则文本；命中扫描器术语时标记为 scanner-rule-text，保留可追踪性但不抬高业务债务严重度。</zh-CN>
    #   <en>The two comment-debt scanners contain matching-rule text for debt, fix, and deferred markers; scanner terminology is classified as scanner-rule-text so it remains traceable without inflating business-debt severity.</en>
    # </lang>
    if ($path -in @('dev/scripts/Get-PortalTodoDebtInventory.ps1', 'dev/scripts/Get-PortalCommentDebtInventory.ps1') -and
        $sample -match '(?i)(<lang>|markerPattern|Get-Classification|TODO|FIXME|HACK|XXX|待办|待确认|后续|债务|需要确认|owner|TBD)') {
        return 'scanner-rule-text'
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

# <lang>
#   <zh-CN>根据标记分类和风险域计算排序用严重度。</zh-CN>
#   <en>Computes a sorting severity from marker classification and risk domain.</en>
# </lang>
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

    if ($Classification -in @('resolved-stale', 'scanner-rule-text')) {
        return 'Low'
    }

    return 'Normal'
}

# <lang>
#   <zh-CN>从指定行提取低敏上下文摘要，限制长度并保持盘点输出可读。</zh-CN>
#   <en>Extracts a bounded low-sensitivity line context for readable inventory output.</en>
# </lang>
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
