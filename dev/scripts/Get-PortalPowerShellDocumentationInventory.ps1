<#
.SYNOPSIS
<lang>
  <zh-CN>生成只读 PowerShell 文档化 inventory。</zh-CN>
  <en>Generates a read-only PowerShell documentation inventory.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>只读取 Git 已追踪 PowerShell 脚本，按运行风险分类并报告 comment-based help、HIA 双语标记、旧 `.LANG` help 表面和标准 `<lang>` 表面；不执行被扫描脚本、不连接服务、不改写被扫描文件、不读取密钥。新增的 `EvidenceSummary` 只解释本 inventory 的证明范围和未证明范围，不能作为历史注释语义充分的自动证明。</zh-CN>
  <en>Reads only Git-tracked PowerShell scripts, classifies operational risk, and reports comment-based help, HIA bilingual markers, legacy `.LANG` help surfaces, and standard `<lang>` surfaces without executing scanned scripts, connecting to services, rewriting scanned files, or reading secrets. The added `EvidenceSummary` explains only what this inventory proves and does not prove; it is not an automatic proof of historical comment semantic sufficiency.</en>
</lang>

.PARAMETER OutputJson
<lang>
  <zh-CN>可选 JSON 输出路径；指定后只写入调用方提供的文件。</zh-CN>
  <en>Optional JSON output path; when supplied, writes only the caller-provided file.</en>
</lang>

.PARAMETER OutputMarkdown
<lang>
  <zh-CN>可选 Markdown 输出路径；指定后只写入调用方提供的文件。</zh-CN>
  <en>Optional Markdown output path; when supplied, writes only the caller-provided file.</en>
</lang>

.PARAMETER AsJson
<lang>
  <zh-CN>将 inventory 写到 stdout 的 JSON 形式；不改变只读扫描范围。</zh-CN>
  <en>Writes the inventory to stdout as JSON without changing the read-only scan scope.</en>
</lang>
#>
[CmdletBinding()]
param(
    [string]$OutputJson,

    [string]$OutputMarkdown,

    [switch]$AsJson
)

# <lang>
#   <zh-CN>严格模式与 fail-fast 策略保证 Git 输入缺失或文本读取异常时不会产出误导性 inventory。</zh-CN>
#   <en>Strict mode and fail-fast handling ensure missing Git inputs or text-read failures cannot produce a misleading inventory.</en>
# </lang>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>仓库根由脚本位置解析，保证扫描范围固定为当前 checkout，而不是调用者工作目录。</zh-CN>
#   <en>Resolve the repository root from the script location so the scan scope is fixed to this checkout rather than the caller's working directory.</en>
# </lang>
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# <lang>
#   <zh-CN>脚本路径列表只来自 Git 已追踪的 `dev/scripts/*.ps1`，避免把本机临时文件或未审查脚本混入证据。</zh-CN>
#   <en>The script path list comes only from Git-tracked `dev/scripts/*.ps1` files, preventing local temporary or unreviewed scripts from entering the evidence.</en>
# </lang>
$scriptPaths = @(& git -C $repoRoot ls-files 'dev/scripts/*.ps1')
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to read Git-tracked PowerShell scripts.'
}

# <lang>
#   <zh-CN>以 UTF-8 无 BOM 写入可选 PowerShell inventory 产物，并按需创建父目录。</zh-CN>
#   <en>Writes optional PowerShell inventory artifacts as UTF-8 without BOM and creates the parent directory when needed.</en>
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
#   <zh-CN>根据脚本路径和正文关键词计算风险域，保持分类规则只读且可复核。</zh-CN>
#   <en>Computes a read-only, reviewable risk category from script paths and text keywords.</en>
# </lang>
function Get-RiskCategory {
    param([Parameter(Mandatory = $true)][string]$Name)

    if ($Name -match '(?i)(Sql|Database|Migration)') {
        return 'DataMigration'
    }

    if ($Name -match '(?i)(Compliance|Credential|Hardening|Security|EnterpriseScan)') {
        return 'SecurityCompliance'
    }

    if ($Name -match '(?i)(Publish|Release|TargetEnvironment|NearTarget)') {
        return 'ReleaseEnvironment'
    }

    if ($Name -match '(?i)(IIS|Smoke|IeMode|LegacyIe|VmAgent|VmTask)') {
        return 'RuntimeAutomation'
    }

    if ($Name -match ('(?i)(Documentation|DotNetDoc|Jsdoc|Comment|SourceDocumentation|PortalTo' + 'doDebt)')) {
        return 'Documentation'
    }

    if ($Name -match '(?i)(Operations|Log|Manifest|Evidence|Summary)') {
        return 'OperationsEvidence'
    }

    return 'General'
}

# <lang>
#   <zh-CN>把风险域映射为稳定等级，供清单排序和摘要使用。</zh-CN>
#   <en>Maps risk categories to stable levels for inventory ordering and summaries.</en>
# </lang>
function Get-RiskLevel {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Category,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $highNamePattern = '(?i)(Publish-PortalFileSystem|Start-IISExpress|Stop-IISExpress|Initialize-PortalTestDatabase|Test-PortalSqlCompatibility|Test-PortalComplianceBaseline|Test-PortalDefaultCredentialRisk|Test-PortalProductionHardening|New-PortalNearTargetReleaseRehearsal|New-PortalLegacyIeTestPackage|New-PortalVmAgentTask|New-PortalVmTaskAgentPackage)'
    if ($Name -match $highNamePattern) {
        return 'High'
    }

    if ($Content -match '(?i)(Start-Process|Stop-Process|Invoke-WebRequest|Invoke-RestMethod|SqlConnection|ExecuteNonQuery|MSBuild|WebPublish|Set-Cookie|Password|SecureString)') {
        return 'Medium'
    }

    if ($Category -in @('Documentation', 'General')) {
        return 'Low'
    }

    return 'Medium'
}

# <lang>
#   <zh-CN>从 PowerShell AST 提取参数名称，不读取参数值或任何秘密内容。</zh-CN>
#   <en>Extracts parameter names from the PowerShell AST without reading values or secrets.</en>
# </lang>
function Get-ParameterNames {
    param([Parameter(Mandatory = $true)][string]$Content)

    $matches = [regex]::Matches($Content, '(?m)^\s*(?:\[[^\]]+\]\s*)*\$(?<name>[A-Za-z_][A-Za-z0-9_]*)')
    $names = New-Object 'System.Collections.Generic.List[string]'
    foreach ($match in $matches) {
        $name = $match.Groups['name'].Value
        if (-not $names.Contains($name)) {
            [void]$names.Add($name)
        }
    }

    return @($names)
}

# <lang>
#   <zh-CN>识别脚本文本中的双语标记表面，特别把旧 `.LANG` help 行与标准 `<lang>` block 分开统计。</zh-CN>
#   <en>Identifies bilingual marker surfaces in script text, especially separating legacy `.LANG` help lines from standard `<lang>` blocks.</en>
# </lang>
function Get-LanguageMarkerSurface {
    param([Parameter(Mandatory = $true)][string]$Content)

    # <lang>
    #   <zh-CN>旧 help 表面沿用 P33.0 的严格行首 `^\.LANG en|zh-CN` 口径，避免因缩进文本或说明文字扩大债务范围。</zh-CN>
    #   <en>Legacy help surfaces reuse P33.0's strict line-start `^\.LANG en|zh-CN` rule so indented text or explanatory prose cannot widen the debt scope.</en>
    # </lang>
    $legacyLangMatches = [regex]::Matches($Content, '(?m)^\.LANG\s+(?:en|zh-CN)\b')

    # <lang>
    #   <zh-CN>标准 block 统计 `<lang>` 开始标签；这覆盖 help 与普通注释中的当前批准表面。</zh-CN>
    #   <en>Standard block counting uses opening `<lang>` tags, covering the currently approved surface in both help and ordinary comments.</en>
    # </lang>
    $standardLangBlockMatches = [regex]::Matches($Content, '(?i)<lang>')

    # <lang>
    #   <zh-CN>locale 标签计数用于兼容已有 `<en>`/`<zh-CN>` 检测，不单独作为 help 表面迁移结论。</zh-CN>
    #   <en>Locale tag counting preserves existing `<en>`/`<zh-CN>` detection without using it alone as a help-surface migration conclusion.</en>
    # </lang>
    $standardLocaleTagMatches = [regex]::Matches($Content, '(?i)<(?:en|zh-CN)>')

    # <lang>
    #   <zh-CN>布尔状态用于保持旧 `HasHiaLanguageMarkers` 字段兼容，同时支持新的表面分类字段。</zh-CN>
    #   <en>Boolean states preserve compatibility for the existing `HasHiaLanguageMarkers` field while supporting the new surface classification fields.</en>
    # </lang>
    $hasLegacySurface = $legacyLangMatches.Count -gt 0
    $hasStandardBlocks = $standardLangBlockMatches.Count -gt 0
    $hasLocaleTags = $standardLocaleTagMatches.Count -gt 0

    # <lang>
    #   <zh-CN>表面 profile 是人类审查入口：Mixed 代表旧 help 与标准 block 共存，不代表语义已完成迁移。</zh-CN>
    #   <en>The surface profile is a human-review entry point: Mixed means legacy help and standard blocks coexist, not that semantic migration is complete.</en>
    # </lang>
    $profile = if ($hasLegacySurface -and $hasStandardBlocks) {
        'Mixed'
    }
    elseif ($hasLegacySurface) {
        'LegacyLangOnly'
    }
    elseif ($hasStandardBlocks) {
        'StandardLangOnly'
    }
    elseif ($hasLocaleTags) {
        'LocaleTagsOnly'
    }
    else {
        'NoHiaMarkers'
    }

    # <lang>
    #   <zh-CN>返回对象只包含计数和分类，不包含脚本正文或任何参数值。</zh-CN>
    #   <en>Return only counts and classification, never script bodies or parameter values.</en>
    # </lang>
    return [pscustomobject]@{
        HasLegacyLangHelpSurface = $hasLegacySurface
        LegacyLangHelpSurfaceCount = $legacyLangMatches.Count
        HasStandardLangBlocks = $hasStandardBlocks
        StandardLangBlockCount = $standardLangBlockMatches.Count
        StandardLocaleTagCount = $standardLocaleTagMatches.Count
        HasAnyHiaLanguageMarkers = ($hasLegacySurface -or $hasStandardBlocks -or $hasLocaleTags)
        LanguageHelpSurface = $profile
    }
}

# <lang>
#   <zh-CN>items 是本次扫描的内存结果集；只保存路径、分类和计数，不缓存脚本全文。</zh-CN>
#   <en>`items` is the in-memory scan result set; it stores paths, classifications, and counts without caching script bodies.</en>
# </lang>
$items = New-Object 'System.Collections.Generic.List[object]'
foreach ($relativePath in $scriptPaths) {
    # <lang>
    #   <zh-CN>fullPath 是当前 Git 跟踪脚本的仓库内绝对路径，仅用于本次只读文本读取。</zh-CN>
    #   <en>`fullPath` is the repository-local absolute path for the current Git-tracked script, used only for this read-only text read.</en>
    # </lang>
    $fullPath = Join-Path $repoRoot $relativePath

    # <lang>
    #   <zh-CN>content 是单个脚本的文本快照；后续只提取分类/计数，不执行内容。</zh-CN>
    #   <en>`content` is a text snapshot of one script; later logic extracts only classifications and counts and never executes it.</en>
    # </lang>
    $content = Get-Content -LiteralPath $fullPath -Encoding UTF8 -Raw

    # <lang>
    #   <zh-CN>name 只用于风险规则和输出展示，避免在摘要中重复长路径。</zh-CN>
    #   <en>`name` is used for risk rules and display output so summaries do not repeat long paths.</en>
    # </lang>
    $name = Split-Path -Leaf $relativePath

    # <lang>
    #   <zh-CN>category 是启发式风险域，不是人工签收结论。</zh-CN>
    #   <en>`category` is a heuristic risk domain, not a human sign-off conclusion.</en>
    # </lang>
    $category = Get-RiskCategory -Name $name

    # <lang>
    #   <zh-CN>hasHelp 只检测 comment-based help 结构存在性，不判断 help 内容是否语义充分。</zh-CN>
    #   <en>`hasHelp` detects only the existence of comment-based help structure, not whether the help text is semantically sufficient.</en>
    # </lang>
    $hasHelp = $content -match '(?s)^\s*<#.*?\.(SYNOPSIS|DESCRIPTION|PARAMETER)'

    # <lang>
    #   <zh-CN>languageSurface 单列旧 `.LANG`、标准 `<lang>` 和混合表面，补足 P33.0 发现的证据误读缺口。</zh-CN>
    #   <en>`languageSurface` separates legacy `.LANG`, standard `<lang>`, and mixed surfaces, addressing the evidence ambiguity found in P33.0.</en>
    # </lang>
    $languageSurface = Get-LanguageMarkerSurface -Content $content

    # <lang>
    #   <zh-CN>hasHiaLang 保持既有字段语义：只说明脚本中存在某种 HIA 双语 marker，不说明表面已完成迁移。</zh-CN>
    #   <en>`hasHiaLang` preserves the old field meaning: some HIA bilingual marker exists, but the surface may still need migration.</en>
    # </lang>
    $hasHiaLang = $languageSurface.HasAnyHiaLanguageMarkers

    # <lang>
    #   <zh-CN>敏感参数检测只看参数名，作为人工风险提示；不读取参数值或配置内容。</zh-CN>
    #   <en>Sensitive-parameter detection reads only parameter names as a human risk hint and never reads parameter values or configuration contents.</en>
    # </lang>
    $hasSensitiveParameter = $content -match '(?i)\$(AdminPassword|Password|Token|Secret|ConnectionString|Cookie|Credential)'

    # <lang>
    #   <zh-CN>riskLevel 是按名称、风险域和文本关键词计算的排序信号，不是安全认证等级。</zh-CN>
    #   <en>`riskLevel` is a sorting signal computed from name, category, and text keywords, not a security certification level.</en>
    # </lang>
    $riskLevel = Get-RiskLevel -Name $name -Category $category -Content $content

    # <lang>
    #   <zh-CN>parameters 只保存参数名数量，用于判断脚本文档复杂度和敏感面。</zh-CN>
    #   <en>`parameters` stores only parameter names/counts to indicate documentation complexity and sensitive surface area.</en>
    # </lang>
    $parameters = @(Get-ParameterNames -Content $content)

    # <lang>
    #   <zh-CN>单项结果保留既有字段，并新增语言表面字段以便后续轻量摘要和人工复核复用。</zh-CN>
    #   <en>Each item preserves existing fields and adds language-surface fields for later lightweight summaries and human review.</en>
    # </lang>
    $items.Add([pscustomobject]@{
            Path = $relativePath
            Name = $name
            RiskCategory = $category
            RiskLevel = $riskLevel
            HasCommentHelp = [bool]$hasHelp
            HasHiaLanguageMarkers = [bool]$hasHiaLang
            HasLegacyLangHelpSurface = [bool]$languageSurface.HasLegacyLangHelpSurface
            LegacyLangHelpSurfaceCount = $languageSurface.LegacyLangHelpSurfaceCount
            HasStandardLangBlocks = [bool]$languageSurface.HasStandardLangBlocks
            StandardLangBlockCount = $languageSurface.StandardLangBlockCount
            StandardLocaleTagCount = $languageSurface.StandardLocaleTagCount
            LanguageHelpSurface = $languageSurface.LanguageHelpSurface
            HasSensitiveParameter = [bool]$hasSensitiveParameter
            ParameterCount = $parameters.Count
            LineCount = ($content -split "`r?`n").Count
        })
}

# <lang>
#   <zh-CN>按风险等级聚合只用于概览排序，不改变任何脚本的检查结果。</zh-CN>
#   <en>Risk-level aggregation is for overview ordering only and does not change any script check result.</en>
# </lang>
$summaryByRiskLevel = @($items | Group-Object RiskLevel | Sort-Object Name | ForEach-Object {
        [pscustomobject]@{ RiskLevel = $_.Name; Count = $_.Count }
    })

# <lang>
#   <zh-CN>风险域聚合帮助维护者选择后续样本，不表示某个领域已经完成语义治理。</zh-CN>
#   <en>Risk-category aggregation helps maintainers choose later samples and does not mean a domain has completed semantic governance.</en>
# </lang>
$summaryByCategory = @($items | Group-Object RiskCategory | Sort-Object Name | ForEach-Object {
        [pscustomobject]@{ RiskCategory = $_.Name; Count = $_.Count }
    })

# <lang>
#   <zh-CN>高风险缺 HIA marker 仍沿用旧门禁口径；它不覆盖旧 `.LANG` 表面迁移债。</zh-CN>
#   <en>High-risk missing-HIA markers retain the old gate meaning and do not cover legacy `.LANG` surface migration debt.</en>
# </lang>
$highRiskMissingHia = @($items | Where-Object { $_.RiskLevel -eq 'High' -and -not $_.HasHiaLanguageMarkers } | Sort-Object Name)

# <lang>
#   <zh-CN>missingHelp 只表示 comment-based help 缺失；help 存在时仍可能需要语义或表面迁移。</zh-CN>
#   <en>`missingHelp` means only that comment-based help is absent; existing help may still need semantic or surface migration.</en>
# </lang>
$missingHelp = @($items | Where-Object { -not $_.HasCommentHelp } | Sort-Object RiskLevel, Name)

# <lang>
#   <zh-CN>legacyLangHelpSurface 直接承接 P33.0 发现，用于显式暴露旧 `.LANG` help 表面文件。</zh-CN>
#   <en>`legacyLangHelpSurface` directly carries the P33.0 finding by explicitly exposing files with legacy `.LANG` help surfaces.</en>
# </lang>
$legacyLangHelpSurface = @($items | Where-Object { $_.HasLegacyLangHelpSurface } | Sort-Object RiskLevel, RiskCategory, Name)

# <lang>
#   <zh-CN>standardLangBlockItems 统计已含标准 `<lang>` block 的脚本；它不保证这些 block 的语义质量。</zh-CN>
#   <en>`standardLangBlockItems` counts scripts containing standard `<lang>` blocks without claiming the blocks are semantically sufficient.</en>
# </lang>
$standardLangBlockItems = @($items | Where-Object { $_.HasStandardLangBlocks } | Sort-Object RiskLevel, RiskCategory, Name)

# <lang>
#   <zh-CN>mixedLanguageSurfaceItems 表示旧 help 与标准 block 共存，是后续迁移时最容易被“有 marker”掩盖的集合。</zh-CN>
#   <en>`mixedLanguageSurfaceItems` marks coexistence of legacy help and standard blocks, the set most easily hidden by a coarse “has marker” metric.</en>
# </lang>
$mixedLanguageSurfaceItems = @($items | Where-Object { $_.LanguageHelpSurface -eq 'Mixed' } | Sort-Object RiskLevel, RiskCategory, Name)

# <lang>
#   <zh-CN>旧 `.LANG` 计数按文件项求和，用于衡量 help 表面迁移规模。</zh-CN>
#   <en>The legacy `.LANG` count is summed across item records to size the help-surface migration work.</en>
# </lang>
$legacyLangHelpSurfaceCount = [int](($items | Measure-Object -Property LegacyLangHelpSurfaceCount -Sum).Sum)

# <lang>
#   <zh-CN>标准 `<lang>` block 计数按文件项求和，用于观察标准表面的覆盖规模，但不作为语义完成证明。</zh-CN>
#   <en>The standard `<lang>` block count is summed across item records to observe standard-surface coverage without proving semantic completion.</en>
# </lang>
$standardLangBlockCount = [int](($items | Measure-Object -Property StandardLangBlockCount -Sum).Sum)

# <lang>
#   <zh-CN>本次生成时间供 inventory 与 EvidenceSummary 复用，避免同一输出内时间戳漂移。</zh-CN>
#   <en>The generated timestamp is reused by both the inventory and EvidenceSummary so one output does not contain drifting timestamps.</en>
# </lang>
$generatedAtUtc = (Get-Date).ToUniversalTime().ToString('o')

# <lang>
#   <zh-CN>writes 描述本次调用会写入的显式输出文件；stdout 输出不记录为长期文件写入。</zh-CN>
#   <en>`writes` describes explicit output files for this invocation; stdout output is not recorded as a durable file write.</en>
# </lang>
$writes = @()
if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    $writes += (Resolve-Path -LiteralPath (Split-Path -Parent $OutputJson) -ErrorAction SilentlyContinue) ? $OutputJson : $OutputJson
}

if (-not [string]::IsNullOrWhiteSpace($OutputMarkdown)) {
    $writes += (Resolve-Path -LiteralPath (Split-Path -Parent $OutputMarkdown) -ErrorAction SilentlyContinue) ? $OutputMarkdown : $OutputMarkdown
}

# <lang>
#   <zh-CN>counts 是轻量摘要的稳定数字口径，附带说明以防止把覆盖数量误解成语义充分。</zh-CN>
#   <en>`counts` is the stable numeric surface for the lightweight summary and carries explanations to prevent coverage counts from being misread as semantic sufficiency.</en>
# </lang>
$counts = @(
    [pscustomobject]@{ Name = 'TotalScripts'; Value = $items.Count; Meaning = 'Git-tracked dev/scripts/*.ps1 files scanned as text only.' },
    [pscustomobject]@{ Name = 'ScriptsWithCommentHelp'; Value = @($items | Where-Object HasCommentHelp).Count; Meaning = 'Scripts with comment-based help structure; this does not prove help semantics are sufficient.' },
    [pscustomobject]@{ Name = 'ScriptsWithHiaLanguageMarkers'; Value = @($items | Where-Object HasHiaLanguageMarkers).Count; Meaning = 'Scripts with some HIA bilingual marker; this includes legacy and standard surfaces.' },
    [pscustomobject]@{ Name = 'ScriptsWithLegacyLangHelpSurface'; Value = $legacyLangHelpSurface.Count; Meaning = 'Scripts with legacy line-start .LANG help markers that still need explicit migration or acceptance.' },
    [pscustomobject]@{ Name = 'ScriptsWithStandardLangBlocks'; Value = $standardLangBlockItems.Count; Meaning = 'Scripts containing standard <lang> blocks; this does not prove semantic sufficiency.' },
    [pscustomobject]@{ Name = 'ScriptsWithMixedLanguageSurfaces'; Value = $mixedLanguageSurfaceItems.Count; Meaning = 'Scripts where legacy .LANG help and standard <lang> blocks coexist.' },
    [pscustomobject]@{ Name = 'LegacyLangHelpSurfaceCount'; Value = $legacyLangHelpSurfaceCount; Meaning = 'Total line-start .LANG markers across scanned scripts.' },
    [pscustomobject]@{ Name = 'HighRiskMissingHiaLanguageMarkers'; Value = $highRiskMissingHia.Count; Meaning = 'High-risk scripts with no HIA marker at all under the compatibility scan.' },
    [pscustomobject]@{ Name = 'MissingCommentHelp'; Value = $missingHelp.Count; Meaning = 'Scripts lacking comment-based help structure.' }
)

# <lang>
#   <zh-CN>findings 用候选/提示语义表达，不把旧表面或缺失项直接宣称为构建失败。</zh-CN>
#   <en>`findings` uses candidate/hint semantics and does not automatically turn legacy surfaces or missing items into build failures.</en>
# </lang>
$findings = @(
    [pscustomobject]@{ Category = 'LegacyLangHelpSurface'; Count = $legacyLangHelpSurface.Count; Severity = 'Warning'; Meaning = 'Legacy .LANG help surfaces remain visible and should not be hidden by a coarse HIA-marker count.' },
    [pscustomobject]@{ Category = 'MixedLanguageSurface'; Count = $mixedLanguageSurfaceItems.Count; Severity = 'Info'; Meaning = 'Legacy and standard marker surfaces coexist in the same script.' },
    [pscustomobject]@{ Category = 'HighRiskMissingHia'; Count = $highRiskMissingHia.Count; Severity = 'Warning'; Meaning = 'High-risk scripts with no HIA marker under the compatibility scan.' },
    [pscustomobject]@{ Category = 'MissingCommentHelp'; Count = $missingHelp.Count; Severity = 'Warning'; Meaning = 'Scripts lacking comment-based help structure.' }
)

# <lang>
#   <zh-CN>pendingGaps 明确自动化 inventory 无法证明的语义事项，承接 W33 的“不要冒充语义证明”原则。</zh-CN>
#   <en>`pendingGaps` states what the inventory cannot prove, carrying W33's principle that automation must not impersonate semantic review.</en>
# </lang>
$pendingGaps = @(
    [pscustomobject]@{ Code = 'SEMANTIC_REVIEW_REQUIRED'; Reason = 'Marker and help counts do not prove comments accurately describe behavior, risks, or side effects.'; OwnerHint = 'W33/P34 sampling and governance manual.' },
    [pscustomobject]@{ Code = 'LEGACY_LANG_SURFACE_MIGRATION'; Reason = 'Legacy .LANG surfaces are now counted but not automatically migrated by this inventory.'; OwnerHint = 'Future touch-improve or dedicated script help migration slice.' },
    [pscustomobject]@{ Code = 'SCRIPT_RUNTIME_NOT_EXECUTED'; Reason = 'Scanned scripts are read as text only and are not executed.'; OwnerHint = 'Relevant release, DB, browser, or target-environment gates when explicitly authorized.' }
)

# <lang>
#   <zh-CN>EvidenceSummary 是 P33 轻量证据摘要，不替代完整 inventory，也不改变退出码。</zh-CN>
#   <en>`EvidenceSummary` is the P33 lightweight evidence summary; it does not replace the full inventory and does not change exit-code behavior.</en>
# </lang>
$evidenceSummary = [pscustomobject]@{
    SchemaVersion = 'p33.lightweight-evidence-summary.v1'
    GeneratedAtUtc = $generatedAtUtc
    Tool = 'Get-PortalPowerShellDocumentationInventory.ps1'
    Command = 'mise exec -- pwsh -File dev/scripts/Get-PortalPowerShellDocumentationInventory.ps1 [-AsJson|-OutputJson <path>|-OutputMarkdown <path>]'
    Scope = 'Git-tracked dev/scripts/*.ps1; scanned as text only.'
    ExecutionMode = 'read-only-inventory'
    ExitCodePolicy = 'Fails on Git/text read errors; marker findings are reported as inventory data, not as failing gates.'
    Writes = @($writes)
    Proves = @(
        'Which Git-tracked PowerShell scripts have comment-based help structure.',
        'Which scanned scripts have some HIA bilingual marker under the compatibility scan.',
        'Which scanned scripts still contain legacy line-start .LANG help surfaces.',
        'Heuristic risk category, risk level, parameter count, and sensitive-parameter-name signals.'
    )
    DoesNotProve = @(
        'That comment text is semantically sufficient or current.',
        'That standard <lang> blocks fully cover every ROP node or local variable.',
        'That scanned scripts execute successfully or are safe to run in a target environment.',
        'That database, IIS, browser, release, credential, or production evidence has been collected.'
    )
    Counts = $counts
    Findings = $findings
    PendingGaps = $pendingGaps
    RecommendedNextAction = 'Use legacy-surface counts to plan a focused help-surface migration or acceptance slice; do not treat marker coverage as semantic completion.'
}

$inventory = [pscustomobject]@{
    GeneratedAtUtc = $generatedAtUtc
    Scope = 'Git-tracked dev/scripts/*.ps1; scanned as text only.'
    TotalScripts = $items.Count
    ScriptsWithCommentHelp = @($items | Where-Object HasCommentHelp).Count
    ScriptsWithHiaLanguageMarkers = @($items | Where-Object HasHiaLanguageMarkers).Count
    ScriptsWithLegacyLangHelpSurface = $legacyLangHelpSurface.Count
    ScriptsWithStandardLangBlocks = $standardLangBlockItems.Count
    ScriptsWithMixedLanguageSurfaces = $mixedLanguageSurfaceItems.Count
    LegacyLangHelpSurfaceCount = $legacyLangHelpSurfaceCount
    StandardLangBlockCount = $standardLangBlockCount
    HighRiskScripts = @($items | Where-Object { $_.RiskLevel -eq 'High' }).Count
    HighRiskMissingHiaLanguageMarkers = $highRiskMissingHia.Count
    MissingCommentHelp = $missingHelp.Count
    SummaryByRiskLevel = $summaryByRiskLevel
    SummaryByRiskCategory = $summaryByCategory
    HighRiskMissingHia = $highRiskMissingHia
    MissingHelp = $missingHelp
    LegacyLangHelpSurface = $legacyLangHelpSurface
    EvidenceSummary = $evidenceSummary
    Items = @($items | Sort-Object RiskLevel, RiskCategory, Name)
}

if ($OutputJson) {
    $json = $inventory | ConvertTo-Json -Depth 8
    Write-Utf8NoBomFile -Path $OutputJson -Content ($json + "`r`n")
}

if ($OutputMarkdown) {
    $lines = New-Object 'System.Collections.Generic.List[string]'
    # <lang><zh-CN>Markdown 标题使用通用 inventory 名称，避免 P33 证据包继续携带历史 P16.4 阶段标签而误导交接读者。</zh-CN><en>The Markdown title uses a generic inventory name so P33 evidence packages do not continue carrying the historical P16.4 stage label and mislead handoff readers.</en></lang>
    $lines.Add('# PowerShell Documentation Inventory')
    $lines.Add('')
    $lines.Add("Generated UTC: $($inventory.GeneratedAtUtc)")
    $lines.Add('')
    $lines.Add('| Metric | Value |')
    $lines.Add('| --- | ---: |')
    $lines.Add("| Total scripts | $($inventory.TotalScripts) |")
    $lines.Add("| Scripts with comment help | $($inventory.ScriptsWithCommentHelp) |")
    $lines.Add("| Scripts with HIA language markers | $($inventory.ScriptsWithHiaLanguageMarkers) |")
    $lines.Add("| Scripts with legacy `.LANG` help surface | $($inventory.ScriptsWithLegacyLangHelpSurface) |")
    $lines.Add("| Scripts with standard `<lang>` blocks | $($inventory.ScriptsWithStandardLangBlocks) |")
    $lines.Add("| Scripts with mixed language surfaces | $($inventory.ScriptsWithMixedLanguageSurfaces) |")
    $lines.Add("| Legacy `.LANG` help marker count | $($inventory.LegacyLangHelpSurfaceCount) |")
    $lines.Add("| High-risk scripts | $($inventory.HighRiskScripts) |")
    $lines.Add("| High-risk scripts missing HIA markers | $($inventory.HighRiskMissingHiaLanguageMarkers) |")
    $lines.Add("| Scripts missing comment help | $($inventory.MissingCommentHelp) |")
    $lines.Add('')
    $lines.Add('## High-Risk Scripts Missing HIA Markers')
    $lines.Add('')
    $lines.Add('| Script | Category | Has Help | Sensitive Parameter |')
    $lines.Add('| --- | --- | --- | --- |')
    foreach ($item in $highRiskMissingHia) {
        # <lang><zh-CN>脚本名单元格直接展开 item.Name；不要用 PowerShell 反引号模拟 Markdown code span，否则会把对象表达式文本写入证据表。</zh-CN><en>The script-name cell expands item.Name directly; do not emulate Markdown code spans with PowerShell backticks because that writes object-expression text into the evidence table.</en></lang>
        $lines.Add("| $($item.Name) | $($item.RiskCategory) | $($item.HasCommentHelp) | $($item.HasSensitiveParameter) |")
    }
    $lines.Add('')
    $lines.Add('## Missing Comment Help')
    $lines.Add('')
    $lines.Add('| Script | Risk | Category |')
    $lines.Add('| --- | --- | --- |')
    foreach ($item in $missingHelp) {
        # <lang><zh-CN>缺 help 表格也使用同一直接展开策略，保持 Markdown 证据的可读性和机器输出一致性。</zh-CN><en>The missing-help table uses the same direct expansion strategy to keep Markdown evidence readable and aligned with machine output.</en></lang>
        $lines.Add("| $($item.Name) | $($item.RiskLevel) | $($item.RiskCategory) |")
    }
    $lines.Add('')
    $lines.Add('## Legacy `.LANG` Help Surface')
    $lines.Add('')
    $lines.Add('| Script | Risk | Category | Legacy markers | Surface |')
    $lines.Add('| --- | --- | --- | ---: | --- |')
    foreach ($item in $legacyLangHelpSurface) {
        # <lang><zh-CN>旧 `.LANG` 债务表是本轮主要人工交接入口，脚本名必须显示为实际文件名而不是 PowerShell 对象表达式。</zh-CN><en>The legacy `.LANG` debt table is this round's main human handoff entry, so script names must render as real file names rather than PowerShell object expressions.</en></lang>
        $lines.Add("| $($item.Name) | $($item.RiskLevel) | $($item.RiskCategory) | $($item.LegacyLangHelpSurfaceCount) | $($item.LanguageHelpSurface) |")
    }
    $lines.Add('')
    $lines.Add('## Evidence Summary')
    $lines.Add('')
    $lines.Add('### Proves')
    foreach ($item in $evidenceSummary.Proves) {
        $lines.Add("- $item")
    }
    $lines.Add('')
    $lines.Add('### Does Not Prove')
    foreach ($item in $evidenceSummary.DoesNotProve) {
        $lines.Add("- $item")
    }
    $lines.Add('')
    $lines.Add("Recommended next action: $($evidenceSummary.RecommendedNextAction)")

    Write-Utf8NoBomFile -Path $OutputMarkdown -Content (($lines -join "`r`n") + "`r`n")
}

if ($AsJson) {
    $inventory | ConvertTo-Json -Depth 8
}
else {
    $inventory
}
