<#
.SYNOPSIS
<lang>
  <en>Validates tracked Web Forms presentation and front-end build contracts.</en>
  <zh-CN>验证已追踪 Web Forms 呈现层和前端构建契约。</zh-CN>
</lang>

.DESCRIPTION
<lang>
  <en>Checks the master page, native theme manifests, module CSS boundaries, Gulp task bindings, package scripts, browser targets, and public front-end guide text. The gate is read-only and does not run npm, Gulp, IIS Express, or the runtime database.</en>
  <zh-CN>检查 master 页、原生主题 manifest、模块 CSS 边界、Gulp 任务绑定、package scripts、浏览器目标和公开前端指南文本。本门禁为只读行为，不运行 npm、Gulp、IIS Express 或运行时数据库。</zh-CN>
</lang>
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>本检查只读取 Git 已追踪的 Web Forms 呈现与前端构建契约，不调用 npm、Gulp 或 IIS Express，因此不会创建 js/css 输出、修改 Visual Studio Task Runner 状态或访问运行数据库。</zh-CN>
#   <en>This check reads only Git-tracked Web Forms presentation and front-end build contracts. It never invokes npm, Gulp, or IIS Express, so it cannot create js/css outputs, change Visual Studio Task Runner state, or access the runtime database.</en>
# </lang>
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$checks = New-Object 'System.Collections.Generic.List[object]'

# <lang>
#   <zh-CN>将静态事实压缩为可报告的名称、布尔结果和低敏细节；该列表不触发修复或外部副作用。</zh-CN>
#   <en>Reduce static facts to reportable names, Boolean results, and low-sensitivity details; the list triggers no repair or external side effect.</en>
# </lang>
function Add-PortalCheck {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [bool]$Passed,

        [Parameter(Mandatory = $true)]
        [string]$Detail
    )

    $checks.Add([pscustomobject][ordered]@{
            Name = $Name
            Passed = $Passed
            Detail = $Detail
        })
}

# <lang>
#   <zh-CN>同时核对工作树文件与 Git 索引追踪状态，防止临时生成物伪装成前端契约输入。</zh-CN>
#   <en>Check both work-tree presence and Git tracking so temporary generated output cannot masquerade as a front-end contract input.</en>
# </lang>
function Test-TrackedPortalFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    # <lang>
    #   <zh-CN>文件必须同时存在于工作树和 Git 索引历史中，避免把临时生成物误当成正式契约输入。</zh-CN>
    #   <en>A file must exist in both the work tree and Git history so temporary generated output is not treated as a formal contract input.</en>
    # </lang>
    $absolutePath = Join-Path $repositoryRoot ($RelativePath -replace '/', '\\')
    $trackedPaths = @(& git -C $repositoryRoot ls-files -- $RelativePath)
    if ($LASTEXITCODE -ne 0) {
        throw "Git 无法检查已追踪文件 '$RelativePath'，退出代码：$LASTEXITCODE"
    }

    $isTracked = $trackedPaths | Where-Object {
        [string]::Equals($_, $RelativePath, [System.StringComparison]::OrdinalIgnoreCase)
    }

    Add-PortalCheck -Name ('Tracked input: ' + $RelativePath) -Passed ((Test-Path -LiteralPath $absolutePath -PathType Leaf) -and $null -ne $isTracked) -Detail $RelativePath
}

# <lang>
#   <zh-CN>对单个文本契约执行精确包含检查并聚合缺失项；不改写输入文件，也不将缺失转成隐式默认值。</zh-CN>
#   <en>Perform exact containment checks for one text contract and aggregate missing items without rewriting inputs or masking absence with defaults.</en>
# </lang>
function Test-TextContract {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [string]$RelativePath,

        [Parameter(Mandatory = $true)]
        [string[]]$ExpectedText
    )

    $absolutePath = Join-Path $repositoryRoot ($RelativePath -replace '/', '\\')
    $content = if (Test-Path -LiteralPath $absolutePath -PathType Leaf) {
        [System.IO.File]::ReadAllText($absolutePath)
    }
    else {
        [string]::Empty
    }

    $missing = @($ExpectedText | Where-Object { -not $content.Contains($_) })
    $detail = if ($missing.Count -eq 0) { $RelativePath } else { 'Missing: ' + ($missing -join ', ') }
    Add-PortalCheck -Name $Name -Passed ($missing.Count -eq 0) -Detail $detail
}

$trackedInputs = @(
    'src/Portal/Default.master',
    'src/Portal/Default.master.cs',
    'src/Portal/App_Themes/Default/Default.css',
    'src/Portal/App_Themes/Default/theme.json',
    'src/Portal/App_Themes/ThemeProbe/Default.css',
    'src/Portal/App_Themes/ThemeProbe/theme.json',
    'src/Portal/DesktopModules/ModuleProbe/Styles/ModuleProbe.css',
    'src/Portal/gulpfile.js',
    'src/Portal/package.json'
)

foreach ($trackedInput in $trackedInputs) {
    Test-TrackedPortalFile -RelativePath $trackedInput
}

# <lang>
#   <zh-CN>先确认呈现层、主题和构建绑定的固定文本契约，再检查 JSON 中的脚本与浏览器目标；这些事实不等于真实构建通过。</zh-CN>
#   <en>Check fixed presentation, theme, and build-binding text before JSON scripts and browser targets; these facts do not prove a real build passed.</en>
# </lang>
Test-TextContract -Name 'Default Master presentation contract' -RelativePath 'src/Portal/Default.master' -ExpectedText @(
    'DesktopPortalBanner.ascx',
    'id="PortalBody"',
    'ID="MainContent"'
)

Test-TextContract -Name 'Master theme and module CSS contract' -RelativePath 'src/Portal/Default.master.cs' -ExpectedText @(
    'PortalThemeResolver.GetCurrentCssClass(Context)',
    'PortalModuleCatalog.GetActiveStyleResources(Context)',
    'link.Attributes["rel"] = "stylesheet"'
)

Test-TextContract -Name 'Gulp Visual Studio and VSCode task contract' -RelativePath 'src/Portal/gulpfile.js' -ExpectedText @(
    "<binding ProjectOpened='startWatch' />",
    "gulp.task('assets:build', assetsBuild)",
    "gulp.watch('js/**/*.src.js'",
    "gulp.watch(['css/**/*.scss', 'css/**/*.sass']"
)

try {
    $package = Get-Content -LiteralPath (Join-Path $repositoryRoot 'src/Portal/package.json') -Raw -Encoding utf8 | ConvertFrom-Json
    $requiredScripts = @{
        'assets:build' = 'gulp assets:build'
        'assets:watch' = 'gulp startWatch'
        'assets:stop-watch' = 'gulp stopWatch'
    }

    $scriptFailures = @()
    foreach ($scriptName in $requiredScripts.Keys) {
        $scriptValue = [string]$package.scripts.$scriptName
        if (-not [string]::Equals($scriptValue, $requiredScripts[$scriptName], [System.StringComparison]::Ordinal)) {
            $scriptFailures += $scriptName
        }
    }

    $assetScriptDetail = if ($scriptFailures.Count -eq 0) {
        'assets:build, assets:watch, assets:stop-watch'
    }
    else {
        'Invalid: ' + ($scriptFailures -join ', ')
    }
    Add-PortalCheck -Name 'Package asset scripts' -Passed ($scriptFailures.Count -eq 0) -Detail $assetScriptDetail

    $browserTargets = @($package.browserslist | ForEach-Object { [string]$_ })
    Add-PortalCheck -Name 'IE9 compatibility target' -Passed ($browserTargets -contains 'ie >= 9') -Detail ($browserTargets -join '; ')
}
catch {
    Add-PortalCheck -Name 'Package JSON contract' -Passed $false -Detail $_.Exception.Message
}

# <lang>
#   <zh-CN>主题 manifest 只验证名称与资源声明的静态一致性，不加载 Theme、控件或 IIS 运行时。</zh-CN>
#   <en>Theme manifests are checked only for static name/resource consistency; no Theme, control, or IIS runtime is loaded.</en>
# </lang>
try {
    $defaultTheme = Get-Content -LiteralPath (Join-Path $repositoryRoot 'src/Portal/App_Themes/Default/theme.json') -Raw -Encoding utf8 | ConvertFrom-Json
    $probeTheme = Get-Content -LiteralPath (Join-Path $repositoryRoot 'src/Portal/App_Themes/ThemeProbe/theme.json') -Raw -Encoding utf8 | ConvertFrom-Json
    $themeValid = $defaultTheme.name -eq 'Default' -and $probeTheme.name -eq 'ThemeProbe' -and
        @($defaultTheme.resources) -contains 'Default.css' -and @($probeTheme.resources) -contains 'Default.css'
    Add-PortalCheck -Name 'Native theme manifest contract' -Passed $themeValid -Detail 'Default and ThemeProbe declare Default.css'
}
catch {
    Add-PortalCheck -Name 'Native theme manifest contract' -Passed $false -Detail $_.Exception.Message
}

$defaultCss = [System.IO.File]::ReadAllText((Join-Path $repositoryRoot 'src/Portal/App_Themes/Default/Default.css'))
$prohibitedFonts = @('Verdana', 'Arial', 'Helvetica', 'Times New Roman', 'Courier New', 'Consolas', 'Segoe UI', 'Microsoft YaHei')
$foundProhibitedFonts = @($prohibitedFonts | Where-Object {
        $defaultCss.IndexOf($_, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
    })
$fontBoundaryDetail = if ($foundProhibitedFonts.Count -eq 0) {
    'Open-font stacks with generic fallbacks only'
}
else {
    'Found: ' + ($foundProhibitedFonts -join ', ')
}
Add-PortalCheck -Name 'Default theme font boundary' -Passed ($foundProhibitedFonts.Count -eq 0) -Detail $fontBoundaryDetail

Test-TextContract -Name 'Public front-end guide contract' -RelativePath 'docs/frontend-asset-guide.md' -ExpectedText @(
    'Visual Studio Task Runner',
    'assets:build',
    '不得读取、不移动、不提交',
    '模块 JavaScript'
)

# <lang>
#   <zh-CN>js/css 目录的资产归属仍由专门治理确认；此处只报告 Git 边界，不把是否存在或是否追踪升级为失败。</zh-CN>
#   <en>Asset ownership for js/css remains a dedicated governance topic; report its Git boundary without turning existence or tracking into a failure.</en>
# </lang>
# <lang>
#   <zh-CN>js/css 当前的所有权尚待资产治理专题确认，因此只报告其边界状态，不能将其是否存在或是否已跟踪作为失败条件。</zh-CN>
#   <en>Ownership of js/css remains for a dedicated asset-governance effort, so report their boundary state without making existence or tracking a failure condition.</en>
# </lang>
$assetBoundary = foreach ($relativeDirectory in @('src/Portal/js', 'src/Portal/css')) {
    $trackedFiles = @(& git -C $repositoryRoot ls-files -- ($relativeDirectory + '/**'))
    if ($LASTEXITCODE -ne 0) {
        throw "Git 无法检查资产目录 '$relativeDirectory'，退出代码：$LASTEXITCODE"
    }

    if ($trackedFiles.Count -eq 0) {
        $relativeDirectory + ' (currently no tracked files)'
    }
    else {
        $relativeDirectory + ' (tracked files: ' + $trackedFiles.Count + ')'
    }
}

$failedChecks = @($checks | Where-Object { -not $_.Passed })
# <lang>
#   <zh-CN>最终仅汇总已记录的静态失败并以非零退出；脚本不自动修复、不运行构建，也不写入默认证据文件。</zh-CN>
#   <en>Summarize recorded static failures and exit non-zero only for those facts; the script never auto-fixes, builds, or writes default evidence files.</en>
# </lang>
$checks
[pscustomobject][ordered]@{
    TotalChecks = $checks.Count
    FailedChecks = $failedChecks.Count
    AssetBoundary = $assetBoundary -join '; '
}

if ($failedChecks.Count -gt 0) {
    throw ('Portal front-end contract check failed: ' + (($failedChecks | ForEach-Object { $_.Name }) -join ', '))
}
