<#
.SYNOPSIS
.LANG en
Runs the static legacy-browser CSS compatibility gate.

.LANG zh-CN
运行旧浏览器 CSS 兼容性静态门禁。

.DESCRIPTION
<lang>
  <en>Scans tracked theme and module CSS for constructs that cannot be baseline requirements for IE9/IE8 compatibility. The script is static and read-only: it does not run Gulp, IIS Express, or any database-backed page.</en>
  <zh-CN>扫描已追踪主题和模块 CSS，找出不能作为 IE9/IE8 基础兼容要求的现代 CSS 构造。本脚本是静态只读门禁，不运行 Gulp、IIS Express 或任何数据库页面。</zh-CN>
</lang>

.PARAMETER FailOnWarning
.LANG en
Treats warning-level findings, such as IE8 visual degradation markers, as failures.

.LANG zh-CN
将 Warning 级发现也视为失败，例如 IE8 视觉降级标记。

.PARAMETER MaxSamplesPerRule
.LANG en
Maximum number of sample findings to print for each rule.

.LANG zh-CN
每条规则最多输出的样例发现数量。
#>
[CmdletBinding()]
param(
    [switch]$FailOnWarning,

    [ValidateRange(1, 200)]
    [int]$MaxSamplesPerRule = 12
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>本脚本是旧浏览器兼容的本地静态门禁，只读取 Git 已追踪 CSS，不运行 Gulp、IIS Express 或数据库。</zh-CN>
#   <en>This script is a local static gate for legacy-browser compatibility. It reads only Git-tracked CSS and never invokes Gulp, IIS Express, or the runtime database.</en>
# </lang>
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$findings = New-Object 'System.Collections.Generic.List[object]'

# <lang>
#   <zh-CN>此 helper 将规则发现归一为固定低敏字段，供摘要和样例共享而不携带原始文件内容。</zh-CN>
#   <en>This helper normalizes each finding into fixed low-sensitivity fields shared by summaries and samples without carrying raw file content.</en>
# </lang>
function Add-PortalFinding {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Severity,

        [Parameter(Mandatory = $true)]
        [string]$Rule,

        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [int]$Line,

        [Parameter(Mandatory = $true)]
        [string]$Excerpt
    )

# <lang>
#   <zh-CN>保留严重级别、规则、相对路径、行号和修剪后的片段，确保输出可定位但不扩大读取范围。</zh-CN>
#   <en>The severity, rule, relative path, line number, and trimmed excerpt keep output locatable without widening the read scope.</en>
# </lang>
    $findings.Add([pscustomobject][ordered]@{
            Severity = $Severity
            Rule     = $Rule
            Path     = $Path
            Line     = $Line
            Excerpt  = $Excerpt.Trim()
        })
}

# <lang>
#   <zh-CN>Git 只读提供受版本控制的主题/模块 CSS 清单；失败立即停止，避免对未知文件集给出兼容结论。</zh-CN>
#   <en>Git read-only supplies the tracked theme/module CSS set; failure stops immediately so compatibility is never concluded from an unknown file set.</en>
# </lang>
$trackedFiles = @(& git -C $repositoryRoot ls-files -- 'src/Portal/App_Themes' 'src/Portal/DesktopModules')
if ($LASTEXITCODE -ne 0) {
# <lang>
#   <zh-CN>保留 Git 的原始退出码，调用方可区分清单读取失败与 CSS 规则命中。</zh-CN>
#   <en>The Git exit code is preserved so callers can distinguish file-list failure from CSS rule findings.</en>
# </lang>
    throw "Git 无法读取已追踪 CSS 文件列表，退出代码：$LASTEXITCODE"
}

# <lang>
#   <zh-CN>过滤只保留 CSS 文件并排除文档目录，规则扫描不触达非前端资产。</zh-CN>
#   <en>Filtering keeps CSS files and excludes documentation directories so the rules do not scan unrelated assets.</en>
# </lang>
$cssFiles = @($trackedFiles | Where-Object {
        $_ -match '\.css$' -and
        $_ -notmatch '(^|/)(Documentation|DoxyGen)(/|$)'
    } | Sort-Object -Unique)

# <lang>
#   <zh-CN>规则集合固定表达 IE9/IE8 基础能力限制；Warning 仅表示视觉降级，不自动等同 Blocker。</zh-CN>
#   <en>The fixed rule set expresses IE9/IE8 baseline limits; Warning marks visual degradation and is not automatically a Blocker.</en>
# </lang>
$rules = @(
    [pscustomobject]@{
        Severity = 'Blocker'
        Rule = 'NoFlexOrGrid'
        Pattern = '(?i)\bdisplay\s*:\s*(?:inline-)?(?:flex|grid)\b|grid-template|grid-column|grid-row'
        Reason = 'IE9/IE8 不支持现代 Flex/Grid 布局作为基础布局。'
    },
    [pscustomobject]@{
        Severity = 'Blocker'
        Rule = 'NoCssVariablesOrModernFunctions'
        Pattern = '(?i)var\s*\(|clamp\s*\(|minmax\s*\(|@supports'
        Reason = 'IE9/IE8 不支持 CSS 变量、clamp/minmax 或 @supports。'
    },
    [pscustomobject]@{
        Severity = 'Blocker'
        Rule = 'NoModernPositionOrMediaEffects'
        Pattern = '(?i)\bposition\s*:\s*sticky\b|object-fit\s*:|backdrop-filter\s*:|filter\s*:'
        Reason = 'IE9/IE8 不支持 sticky、object-fit 或现代滤镜作为基础能力。'
    },
    [pscustomobject]@{
        Severity = 'Blocker'
        Rule = 'NoModernUnitsOrTransforms'
        Pattern = '(?i)calc\s*\(|(?<![a-z])\d+(?:\.\d+)?(?:vw|vh|vmin|vmax|rem)\b|transform\s*:|transition\s*:'
        Reason = 'IE9/IE8 对现代单位、calc、transform/transition 的支持不足，不能作为基础路径。'
    },
    [pscustomobject]@{
        Severity = 'Blocker'
        Rule = 'NoGradientDependency'
        Pattern = '(?i)linear-gradient|radial-gradient|repeating-linear-gradient'
        Reason = '旧 IE 不应依赖 CSS 渐变表达关键边界或内容。'
    },
    [pscustomobject]@{
        Severity = 'Warning'
        Rule = 'IE8VisualDegradation'
        Pattern = '(?i)rgba\s*\(|box-shadow\s*:|border-radius\s*:|text-shadow\s*:|opacity\s*:|background-size\s*:|:nth-|:not\s*\('
        Reason = '这些样式在 IE9 多数可接受，但 IE8 或更低版本需要允许视觉降级或补实机证据。'
    }
)

# <lang>
#   <zh-CN>逐文件逐行应用固定规则，命中时只记录相对路径和行片段，不修改 CSS。</zh-CN>
#   <en>Each fixed rule is applied line by line to each file; matches record only relative path and excerpt, never modify CSS.</en>
# </lang>
foreach ($relativePath in $cssFiles) {
    $absolutePath = Join-Path $repositoryRoot ($relativePath -replace '/', '\')
    $lines = [System.IO.File]::ReadAllLines($absolutePath)
# <lang>
#   <zh-CN>保持原始行号以便人工复核，行读取只服务于当前静态门禁。</zh-CN>
#   <en>Original line positions are preserved for manual review; line reads serve only this static gate.</en>
# </lang>
    for ($lineIndex = 0; $lineIndex -lt $lines.Length; $lineIndex++) {
        $line = $lines[$lineIndex]
# <lang>
#   <zh-CN>每条规则独立匹配，允许同一行产生多个可解释发现而不短路其它规则。</zh-CN>
#   <en>Rules match independently so one line may produce multiple explainable findings without short-circuiting other rules.</en>
# </lang>
        foreach ($rule in $rules) {
            if ([regex]::IsMatch($line, $rule.Pattern)) {
                Add-PortalFinding -Severity $rule.Severity -Rule $rule.Rule -Path $relativePath -Line ($lineIndex + 1) -Excerpt $line
            }
        }
    }
}

# <lang>
#   <zh-CN>浏览器目标只读取 package.json 的 browserslist，缺失或不匹配保持失败事实。</zh-CN>
#   <en>The browser target reads only package.json browserslist; missing or mismatched targets remain a failure fact.</en>
# </lang>
$packagePath = Join-Path $repositoryRoot 'src/Portal/package.json'
$browsersListValid = $false
if (Test-Path -LiteralPath $packagePath -PathType Leaf) {
    $package = Get-Content -LiteralPath $packagePath -Raw -Encoding utf8 | ConvertFrom-Json
    $browsersListValid = @($package.browserslist | ForEach-Object { [string]$_ }) -contains 'ie >= 9'
}

# <lang>
#   <zh-CN>doctype 只验证门户 Master 的 Transitional 标记，不自动修复模板或推断浏览器实机结果。</zh-CN>
#   <en>The doctype check verifies only the portal Master's Transitional marker; it never repairs the template or infers browser hardware results.</en>
# </lang>
$masterPath = Join-Path $repositoryRoot 'src/Portal/Default.master'
$masterText = if (Test-Path -LiteralPath $masterPath -PathType Leaf) {
    [System.IO.File]::ReadAllText($masterPath)
}
else {
    [string]::Empty
}
$doctypeValid = $masterText.Contains('XHTML 1.0 Transitional')

# <lang>
#   <zh-CN>摘要按严重级别和规则聚合，样例遵守每规则上限，避免输出失控。</zh-CN>
#   <en>The summary groups by severity and rule, while samples obey the per-rule cap to keep output bounded.</en>
# </lang>
$summary = $findings |
    Group-Object Severity, Rule |
    Sort-Object Name |
    ForEach-Object {
        [pscustomobject][ordered]@{
            Severity = ($_.Name -split ', ')[0]
            Rule = ($_.Name -split ', ')[1]
            Count = $_.Count
        }
    }

$samples = $findings |
    Group-Object Severity, Rule |
    ForEach-Object {
        $_.Group | Select-Object -First $MaxSamplesPerRule
    } |
    Sort-Object Severity, Rule, Path, Line

$blockerCount = @($findings | Where-Object { $_.Severity -eq 'Blocker' }).Count
$warningCount = @($findings | Where-Object { $_.Severity -eq 'Warning' }).Count
# <lang>
#   <zh-CN>失败条件组合 Blocker、显式 FailOnWarning、browserslist 和 doctype；默认 Warning 不改变旧门禁语义。</zh-CN>
#   <en>Failure combines Blockers, explicit FailOnWarning, browserslist, and doctype; warnings remain non-failing by default.</en>
# </lang>
$failed = $blockerCount -gt 0 -or ($FailOnWarning -and $warningCount -gt 0) -or -not $browsersListValid -or -not $doctypeValid

$summary
if ($samples.Count -gt 0) {
    $samples | Format-Table -AutoSize
}

[pscustomobject][ordered]@{
    TotalCssFiles      = $cssFiles.Count
    BlockerFindings    = $blockerCount
    WarningFindings    = $warningCount
    BrowserslistIE9    = $browsersListValid
    TransitionalDoctype = $doctypeValid
    Failed             = $failed
}

# <lang>
#   <zh-CN>仅在组合门禁失败时抛出异常；成功路径保留摘要和样例作为静态证据。</zh-CN>
#   <en>An exception is thrown only when the combined gate fails; successful runs retain summary and samples as static evidence.</en>
# </lang>
if ($failed) {
    throw 'Portal legacy CSS compatibility check failed.'
}
