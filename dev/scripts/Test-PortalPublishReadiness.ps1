[CmdletBinding()]
param(
    [string]$PortalProjectPath = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'src/Portal/Portal.csproj'),

    [string]$PublishedPath,

    [switch]$TreatWarningsAsErrors
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$portalProject = Resolve-Path -LiteralPath $PortalProjectPath
$portalRoot = Split-Path -Parent $portalProject.Path
$checks = New-Object 'System.Collections.Generic.List[object]'

function Add-PublishCheck {
    param(
        [string]$Name,
        [ValidateSet('Pass', 'Warning', 'Fail', 'Info')]
        [string]$Status,
        [string]$Detail
    )

    $checks.Add([pscustomobject]@{
            Name = $Name
            Status = $Status
            Detail = $Detail
        })

    Write-Host ('[{0}] {1}: {2}' -f $Status.ToUpperInvariant(), $Name, $Detail)
}

function ConvertTo-RepoPath {
    param([string]$Path)

    $relative = [System.IO.Path]::GetRelativePath($repoRoot, $Path)
    return ($relative -replace '\\', '/')
}

function Test-GitTrackedPath {
    param([string]$Path)

    $repoPath = ConvertTo-RepoPath -Path $Path
    $output = & git -C $repoRoot ls-files -- $repoPath
    return -not [string]::IsNullOrWhiteSpace(($output -join ''))
}

function Get-MsBuildItems {
    param(
        [xml]$Project,
        [string]$ItemName
    )

    $namespaceManager = New-Object System.Xml.XmlNamespaceManager($Project.NameTable)
    $namespaceManager.AddNamespace('msb', 'http://schemas.microsoft.com/developer/msbuild/2003')
    return @($Project.SelectNodes("//msb:$ItemName[@Include]", $namespaceManager))
}

function Test-ContentPathPattern {
    param(
        [string]$Include,
        [string[]]$Patterns
    )

    foreach ($pattern in $Patterns) {
        if ($Include -imatch $pattern) {
            return $true
        }
    }

    return $false
}

function Test-RelativeLeafPath {
    param(
        [string]$RootPath,
        [string]$RelativePath
    )

    $candidatePath = Join-Path $RootPath $RelativePath
    return Test-Path -LiteralPath $candidatePath -PathType Leaf
}

function Get-RelativePublishedFiles {
    param([string]$RootPath)

    if (-not (Test-Path -LiteralPath $RootPath -PathType Container)) {
        return @()
    }

    return @(Get-ChildItem -LiteralPath $RootPath -File -Recurse |
        ForEach-Object { [System.IO.Path]::GetRelativePath($RootPath, $_.FullName) })
}

# 中文：P9.4 发布门禁只做仓库和项目文件的只读核查，不替代真实 IIS 发布验证。
# English: The P9.4 publish gate performs read-only repository/project checks and does not replace real IIS deployment verification.
[xml]$project = [System.IO.File]::ReadAllText($portalProject.Path, [System.Text.UTF8Encoding]::new($false))
$contentItems = Get-MsBuildItems -Project $project -ItemName 'Content'
$compileItems = Get-MsBuildItems -Project $project -ItemName 'Compile'

Add-PublishCheck -Name 'Portal project exists' -Status 'Pass' -Detail $portalProject.Path
Add-PublishCheck -Name 'Content item count' -Status 'Info' -Detail ($contentItems.Count.ToString() + ' Content items declared in Portal.csproj.')
Add-PublishCheck -Name 'Compile item count' -Status 'Info' -Detail ($compileItems.Count.ToString() + ' Compile items declared in Portal.csproj.')

$missingContent = New-Object 'System.Collections.Generic.List[string]'
$untrackedContent = New-Object 'System.Collections.Generic.List[string]'
foreach ($item in $contentItems) {
    $include = [string]$item.Include
    if ([string]::IsNullOrWhiteSpace($include) -or $include.Contains('*')) {
        continue
    }

    $path = Join-Path $portalRoot $include
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $missingContent.Add($include)
        continue
    }

    if (-not (Test-GitTrackedPath -Path $path)) {
        $untrackedContent.Add($include)
    }
}

if ($missingContent.Count -eq 0) {
    Add-PublishCheck -Name 'Content files exist' -Status 'Pass' -Detail 'All non-wildcard Content files exist on disk.'
}
else {
    Add-PublishCheck -Name 'Content files exist' -Status 'Fail' -Detail (($missingContent | Select-Object -First 12) -join '; ')
}

if ($untrackedContent.Count -eq 0) {
    Add-PublishCheck -Name 'Content files tracked by Git' -Status 'Pass' -Detail 'All existing Content files are tracked by Git.'
}
else {
    Add-PublishCheck -Name 'Content files tracked by Git' -Status 'Fail' -Detail (($untrackedContent | Select-Object -First 12) -join '; ')
}

$requiredContent = @(
    'Web.config',
    'Config\Web.config',
    'Config\Templates\connectionStrings.config',
    'Global.asax',
    'Default.aspx',
    'DesktopDefault.aspx',
    'Admin\SystemHealth.aspx',
    'Admin\ThemeSettings.aspx',
    'Admin\ModuleCatalog.aspx',
    'DesktopModules\Signin.ascx'
)

$contentSet = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($item in $contentItems) {
    [void]$contentSet.Add([string]$item.Include)
}

$missingRequired = @($requiredContent | Where-Object { -not $contentSet.Contains($_) })
if ($missingRequired.Count -eq 0) {
    Add-PublishCheck -Name 'Required runtime content declared' -Status 'Pass' -Detail 'Core Web.config, pages, templates and Admin/module entry files are Content.'
}
else {
    Add-PublishCheck -Name 'Required runtime content declared' -Status 'Fail' -Detail ($missingRequired -join '; ')
}

$forbiddenPatterns = @(
    '^Documentation\\',
    '^DoxyGen\\',
    '^node_modules\\',
    '^bin\\',
    '^obj\\',
    '^Uploads\\sample-',
    '^Demo\\'
)

$forbiddenContent = @($contentItems |
    ForEach-Object { [string]$_.Include } |
    Where-Object { Test-ContentPathPattern -Include $_ -Patterns $forbiddenPatterns })

if ($forbiddenContent.Count -eq 0) {
    Add-PublishCheck -Name 'Generated/demo content exclusion' -Status 'Pass' -Detail 'No generated documentation, bin/obj, node_modules, sample uploads or Demo paths are declared as Content.'
}
else {
    Add-PublishCheck -Name 'Generated/demo content exclusion' -Status 'Fail' -Detail (($forbiddenContent | Select-Object -First 12) -join '; ')
}

$themeContent = @($contentItems | ForEach-Object { [string]$_.Include } | Where-Object { $_ -ilike 'App_Themes\*' })
$themeNames = @($themeContent |
    ForEach-Object { ($_ -split '\\')[1] } |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    Sort-Object -Unique)

$themeIssues = New-Object 'System.Collections.Generic.List[string]'
foreach ($themeName in $themeNames) {
    if (-not $contentSet.Contains("App_Themes\$themeName\Default.css")) {
        $themeIssues.Add("$themeName missing Default.css")
    }

    if (-not $contentSet.Contains("App_Themes\$themeName\theme.json")) {
        $themeIssues.Add("$themeName missing theme.json")
    }
}

if ($themeIssues.Count -eq 0) {
    Add-PublishCheck -Name 'Theme package publish contract' -Status 'Pass' -Detail ($themeNames.Count.ToString() + ' themes declare Default.css and theme.json.')
}
else {
    Add-PublishCheck -Name 'Theme package publish contract' -Status 'Fail' -Detail (($themeIssues | Select-Object -First 12) -join '; ')
}

$modulePackageContent = @($contentItems |
    ForEach-Object { [string]$_.Include } |
    Where-Object { $_ -imatch '^DesktopModules\\[^\\]+\\' })

$modulePackageNames = @($modulePackageContent |
    ForEach-Object { ($_ -split '\\')[1] } |
    Sort-Object -Unique)

$modulePackageIssues = New-Object 'System.Collections.Generic.List[string]'
foreach ($moduleName in $modulePackageNames) {
    if (-not $contentSet.Contains("DesktopModules\$moduleName\module.json")) {
        $modulePackageIssues.Add("$moduleName missing module.json")
    }

    $badAssets = @($modulePackageContent |
        Where-Object { $_ -ilike "DesktopModules\$moduleName\*" } |
        Where-Object { $_ -imatch '\.(dll|exe|ps1|cmd|bat|zip|js)$' })
    foreach ($badAsset in $badAssets) {
        $modulePackageIssues.Add("$moduleName has blocked asset $badAsset")
    }
}

if ($modulePackageIssues.Count -eq 0) {
    Add-PublishCheck -Name 'Trusted module package publish contract' -Status 'Pass' -Detail ($modulePackageNames.Count.ToString() + ' trusted module packages declare module.json and no blocked runtime assets.')
}
else {
    Add-PublishCheck -Name 'Trusted module package publish contract' -Status 'Fail' -Detail (($modulePackageIssues | Select-Object -First 12) -join '; ')
}

$actualEnvironmentConfig = @($contentItems |
    ForEach-Object { [string]$_.Include } |
    Where-Object { $_ -imatch '^Config\\UnityCfg\.(dev|test|prod)\.xml$' })

if ($actualEnvironmentConfig.Count -eq 0) {
    Add-PublishCheck -Name 'Environment Unity config publish source' -Status 'Pass' -Detail 'No environment-specific UnityCfg files are declared as Content.'
}
else {
    Add-PublishCheck -Name 'Environment Unity config publish source' -Status 'Warning' -Detail ('Environment-specific UnityCfg files are declared as Content: ' + ($actualEnvironmentConfig -join '; ') + '. Confirm they contain no secrets and match deployment policy.')
}

if (-not [string]::IsNullOrWhiteSpace($PublishedPath)) {
    $publishRoot = Resolve-Path -LiteralPath $PublishedPath -ErrorAction SilentlyContinue
    if (-not $publishRoot) {
        Add-PublishCheck -Name 'Published output exists' -Status 'Fail' -Detail "Published output folder not found: $PublishedPath"
    }
    else {
        $publishedRootPath = $publishRoot.Path
        Add-PublishCheck -Name 'Published output exists' -Status 'Pass' -Detail $publishedRootPath

        # 中文：发布目录检查针对 IIS 文件系统包，不直接连接真实 IIS 或读取外置敏感配置。
        # English: Published-output checks target a filesystem IIS package and do not connect to real IIS or read external secrets.
        $requiredPublishedFiles = @(
            'Web.config',
            'Default.aspx',
            'DesktopDefault.aspx',
            'GenericErrorPage.aspx',
            'Global.asax',
            'Config\Web.config',
            'Config\Templates\connectionStrings.config',
            'Admin\SystemHealth.aspx',
            'Admin\ThemeSettings.aspx',
            'Admin\ModuleCatalog.aspx',
            'DesktopModules\Signin.ascx',
            'App_Themes\EnterpriseLight\Default.css',
            'App_Themes\StateClassicLight\Default.css',
            'App_Themes\OaDark\theme.json',
            'bin\Portal.dll'
        )

        $missingPublishedFiles = @($requiredPublishedFiles |
            Where-Object { -not (Test-RelativeLeafPath -RootPath $publishedRootPath -RelativePath $_) })

        if ($missingPublishedFiles.Count -eq 0) {
            Add-PublishCheck -Name 'Published required files' -Status 'Pass' -Detail 'Core pages, config templates, themes, module entry and Portal.dll are present.'
        }
        else {
            Add-PublishCheck -Name 'Published required files' -Status 'Fail' -Detail (($missingPublishedFiles | Select-Object -First 12) -join '; ')
        }

        $publishedFiles = Get-RelativePublishedFiles -RootPath $publishedRootPath
        $forbiddenPublishedPatterns = @(
            '^Documentation\\',
            '^DoxyGen\\',
            '^node_modules\\',
            '^Demo\\',
            '^Uploads\\sample-',
            '^Config\\appSettings\.(dev|test|prod)\.json$',
            '^Config\\UnityCfg\.(dev|test|prod)\.xml$',
            '^css\\sample',
            '^js\\(grunt-sample|sample)'
        )

        $forbiddenPublishedFiles = @($publishedFiles |
            Where-Object { Test-ContentPathPattern -Include $_ -Patterns $forbiddenPublishedPatterns })

        if ($forbiddenPublishedFiles.Count -eq 0) {
            Add-PublishCheck -Name 'Published forbidden files exclusion' -Status 'Pass' -Detail 'No Demo, generated docs, local env config or sample frontend files are present.'
        }
        else {
            Add-PublishCheck -Name 'Published forbidden files exclusion' -Status 'Fail' -Detail (($forbiddenPublishedFiles | Select-Object -First 12) -join '; ')
        }
    }
}

$summary = [pscustomobject][ordered]@{
    PortalProject = $portalProject.Path
    ContentItems = $contentItems.Count
    Themes = $themeNames.Count
    TrustedModulePackages = $modulePackageNames.Count
    TotalChecks = $checks.Count
    FailedChecks = @($checks | Where-Object { $_.Status -eq 'Fail' }).Count
    WarningChecks = @($checks | Where-Object { $_.Status -eq 'Warning' }).Count
}

$summary

if ($summary.FailedChecks -gt 0 -or ($TreatWarningsAsErrors -and $summary.WarningChecks -gt 0)) {
    exit 1
}
