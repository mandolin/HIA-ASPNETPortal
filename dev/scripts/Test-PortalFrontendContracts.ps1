[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# 中文：本检查只读取 Git 已追踪的 Web Forms 呈现与前端构建契约，不调用 npm、Gulp 或 IIS Express，
# 因而不会创建 js/css 输出、修改 Visual Studio Task Runner 状态或访问运行数据库。
# English: This check reads only Git-tracked Web Forms presentation and front-end build contracts. It never invokes npm,
# Gulp, or IIS Express, so it cannot create js/css outputs, change Visual Studio Task Runner state, or access the runtime database.
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$checks = New-Object 'System.Collections.Generic.List[object]'

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

function Test-TrackedPortalFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    # 中文：文件必须同时存在于工作树和 Git 索引历史中，避免把临时生成物误当成正式契约输入。
    # English: A file must exist in both the work tree and Git history so temporary generated output is not treated as a formal contract input.
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

# 中文：js/css 当前的所有权尚待资产治理专题确认，因此只报告其边界状态，不能将其是否存在或是否已跟踪作为失败条件。
# English: Ownership of js/css remains for a dedicated asset-governance effort, so report their boundary state without making existence or tracking a failure condition.
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
$checks
[pscustomobject][ordered]@{
    TotalChecks = $checks.Count
    FailedChecks = $failedChecks.Count
    AssetBoundary = $assetBoundary -join '; '
}

if ($failedChecks.Count -gt 0) {
    throw ('Portal front-end contract check failed: ' + (($failedChecks | ForEach-Object { $_.Name }) -join ', '))
}
