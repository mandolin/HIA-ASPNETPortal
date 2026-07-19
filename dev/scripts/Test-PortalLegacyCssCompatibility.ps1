[CmdletBinding()]
param(
    [switch]$FailOnWarning,

    [ValidateRange(1, 200)]
    [int]$MaxSamplesPerRule = 12
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# 中文：本脚本是旧浏览器兼容的本地静态门禁，只读取 Git 已追踪 CSS，不运行 Gulp、IIS Express 或数据库。
# English: This script is a local static gate for legacy-browser compatibility. It reads only Git-tracked CSS and never
# invokes Gulp, IIS Express, or the runtime database.
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$findings = New-Object 'System.Collections.Generic.List[object]'

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

    $findings.Add([pscustomobject][ordered]@{
            Severity = $Severity
            Rule     = $Rule
            Path     = $Path
            Line     = $Line
            Excerpt  = $Excerpt.Trim()
        })
}

$trackedFiles = @(& git -C $repositoryRoot ls-files -- 'src/Portal/App_Themes' 'src/Portal/DesktopModules')
if ($LASTEXITCODE -ne 0) {
    throw "Git 无法读取已追踪 CSS 文件列表，退出代码：$LASTEXITCODE"
}

$cssFiles = @($trackedFiles | Where-Object {
        $_ -match '\.css$' -and
        $_ -notmatch '(^|/)(Documentation|DoxyGen)(/|$)'
    } | Sort-Object -Unique)

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

foreach ($relativePath in $cssFiles) {
    $absolutePath = Join-Path $repositoryRoot ($relativePath -replace '/', '\')
    $lines = [System.IO.File]::ReadAllLines($absolutePath)
    for ($lineIndex = 0; $lineIndex -lt $lines.Length; $lineIndex++) {
        $line = $lines[$lineIndex]
        foreach ($rule in $rules) {
            if ([regex]::IsMatch($line, $rule.Pattern)) {
                Add-PortalFinding -Severity $rule.Severity -Rule $rule.Rule -Path $relativePath -Line ($lineIndex + 1) -Excerpt $line
            }
        }
    }
}

$packagePath = Join-Path $repositoryRoot 'src/Portal/package.json'
$browsersListValid = $false
if (Test-Path -LiteralPath $packagePath -PathType Leaf) {
    $package = Get-Content -LiteralPath $packagePath -Raw -Encoding utf8 | ConvertFrom-Json
    $browsersListValid = @($package.browserslist | ForEach-Object { [string]$_ }) -contains 'ie >= 9'
}

$masterPath = Join-Path $repositoryRoot 'src/Portal/Default.master'
$masterText = if (Test-Path -LiteralPath $masterPath -PathType Leaf) {
    [System.IO.File]::ReadAllText($masterPath)
}
else {
    [string]::Empty
}
$doctypeValid = $masterText.Contains('XHTML 1.0 Transitional')

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

if ($failed) {
    throw 'Portal legacy CSS compatibility check failed.'
}
