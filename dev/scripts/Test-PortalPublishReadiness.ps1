<#
.SYNOPSIS
<lang>
  <en>Checks portal publish readiness without deploying to IIS.</en>
  <zh-CN>在不部署到 IIS 的情况下检查门户发布就绪状态。</zh-CN>
</lang>

.DESCRIPTION
<lang>
  <en>Inspect Portal.csproj content and compile items, required publish files, tracked-file boundaries, optional filesystem publish output, and known deployment risks. The script is read-only for repository/project files and does not connect to real IIS or read external sensitive configuration.</en>
  <zh-CN>检查 Portal.csproj 的 Content/Compile 项、必需发布文件、Git 追踪边界、可选文件系统发布输出和已知部署风险。本脚本对仓库和项目文件只读，不连接真实 IIS，也不读取外置敏感配置。</zh-CN>
</lang>

.PARAMETER PortalProjectPath
<lang>
  <en>Path to Portal.csproj. Defaults to the repository Web Forms project.</en>
  <zh-CN>Portal.csproj 路径。默认使用仓库中的 Web Forms 项目。</zh-CN>
</lang>

.PARAMETER PublishedPath
<lang>
  <en>Optional filesystem publish output to validate after WebPublish.</en>
  <zh-CN>可选的文件系统发布输出目录，用于 WebPublish 后验证。</zh-CN>
</lang>

.PARAMETER TreatWarningsAsErrors
<lang>
  <en>Returns a failing exit code when warning-level findings are present.</en>
  <zh-CN>存在 Warning 级发现时返回失败退出码。</zh-CN>
</lang>
#>
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

# <lang>
#   <zh-CN>先固定项目和仓库根目录，后续所有路径检查均相对于该边界，不扩大到外置配置或运行时服务。</zh-CN>
#   <en>Fix the project and repository roots first; all later path checks stay within that boundary and do not expand to external configuration or runtime services.</en>
# </lang>
# <lang>
#   <zh-CN>追加发布就绪 finding 并立即输出；Status 只表示静态检查分类，不执行发布或 IIS 操作。</zh-CN>
#   <en>Add and display a publish-readiness finding; Status is a static-check classification and does not deploy or operate IIS.</en>
# </lang>
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

# <lang>
#   <zh-CN>把仓库内路径转换为稳定相对路径，供 Git 追踪检查使用，不解析外置秘密路径。</zh-CN>
#   <en>Convert a repository path to a stable relative path for Git tracking checks without resolving external secret paths.</en>
# </lang>
function ConvertTo-RepoPath {
    param([string]$Path)

    $relative = [System.IO.Path]::GetRelativePath($repoRoot, $Path)
    return ($relative -replace '\\', '/')
}

# <lang>
#   <zh-CN>只读查询路径是否由当前仓库 Git 追踪，不添加、删除或修改索引。</zh-CN>
#   <en>Read whether a path is tracked by the current repository without adding, deleting, or modifying the index.</en>
# </lang>
function Test-GitTrackedPath {
    param([string]$Path)

    $repoPath = ConvertTo-RepoPath -Path $Path
    $output = & git -C $repoRoot ls-files -- $repoPath
    return -not [string]::IsNullOrWhiteSpace(($output -join ''))
}

# <lang>
#   <zh-CN>按 MSBuild 命名空间读取带 Include 的项目项，缺失项由调用方转换为 finding。</zh-CN>
#   <en>Read MSBuild items with Include attributes using the project namespace; callers turn missing items into findings.</en>
# </lang>
function Get-MsBuildItems {
    param(
        [xml]$Project,
        [string]$ItemName
    )

    $namespaceManager = New-Object System.Xml.XmlNamespaceManager($Project.NameTable)
    $namespaceManager.AddNamespace('msb', 'http://schemas.microsoft.com/developer/msbuild/2003')
    return @($Project.SelectNodes("//msb:$ItemName[@Include]", $namespaceManager))
}

# <lang>
#   <zh-CN>用大小写不敏感正则匹配 Content 路径，维持发布排除清单的静态语义。</zh-CN>
#   <en>Match Content paths with case-insensitive regexes while preserving the static publish-exclusion semantics.</en>
# </lang>
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

# <lang>
#   <zh-CN>在指定发布根目录下检查相对文件是否存在，不读取文件内容或连接运行时服务。</zh-CN>
#   <en>Check whether a relative file exists under the selected publish root without reading its contents or contacting runtime services.</en>
# </lang>
function Test-RelativeLeafPath {
    param(
        [string]$RootPath,
        [string]$RelativePath
    )

    $candidatePath = Join-Path $RootPath $RelativePath
    return Test-Path -LiteralPath $candidatePath -PathType Leaf
}

# <lang>
#   <zh-CN>枚举可选文件系统发布目录中的相对文件，用于静态禁入项检查，不执行发布。</zh-CN>
#   <en>Enumerate relative files in an optional filesystem publish directory for static forbidden-item checks without publishing.</en>
# </lang>
function Get-RelativePublishedFiles {
    param([string]$RootPath)

    if (-not (Test-Path -LiteralPath $RootPath -PathType Container)) {
        return @()
    }

    return @(Get-ChildItem -LiteralPath $RootPath -File -Recurse |
        ForEach-Object { [System.IO.Path]::GetRelativePath($RootPath, $_.FullName) })
}

# <lang>
#   <zh-CN>P9.4 发布门禁只做仓库和项目文件的只读核查，不替代真实 IIS 发布验证。</zh-CN>
#   <en>The P9.4 publish gate performs read-only repository/project checks and does not replace real IIS deployment verification.</en>
# </lang>
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

# <lang>
#   <zh-CN>Content 检查分别报告缺失与未追踪文件，通配项跳过逐文件断言；缺失不会被静默当作发布成功。</zh-CN>
#   <en>Report missing and untracked Content files separately while skipping wildcard per-file assertions; absence is never silently treated as publish success.</en>
# </lang>
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
# <lang>
#   <zh-CN>必需运行内容集合验证核心页面、配置模板和入口文件已声明为 Content；这仍是项目静态事实，不是 IIS 运行 proof。</zh-CN>
#   <en>Validate that core pages, configuration templates, and entry files are declared as Content; this remains a project-static fact, not IIS runtime proof.</en>
# </lang>
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
# <lang>
#   <zh-CN>受信任模块包只允许声明 module.json 和非运行时资源；阻断资产发现会形成明确 Fail，不尝试自动清理。</zh-CN>
#   <en>Trusted module packages may declare module.json and non-runtime assets only; blocked assets produce an explicit Fail without automatic cleanup.</en>
# </lang>
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
# <lang>
#   <zh-CN>提供 PublishedPath 时才检查实际文件系统产物；该分支仍只读，不创建、删除或修复发布目录。</zh-CN>
#   <en>Inspect a filesystem artifact only when PublishedPath is supplied; this branch remains read-only and never creates, deletes, or repairs the publish directory.</en>
# </lang>
    $publishRoot = Resolve-Path -LiteralPath $PublishedPath -ErrorAction SilentlyContinue
    if (-not $publishRoot) {
        Add-PublishCheck -Name 'Published output exists' -Status 'Fail' -Detail "Published output folder not found: $PublishedPath"
    }
    else {
        $publishedRootPath = $publishRoot.Path
        Add-PublishCheck -Name 'Published output exists' -Status 'Pass' -Detail $publishedRootPath

        # <lang>
        #   <zh-CN>发布目录检查针对 IIS 文件系统包，不直接连接真实 IIS 或读取外置敏感配置。</zh-CN>
        #   <en>Published-output checks target a filesystem IIS package and do not connect to real IIS or read external secrets.</en>
        # </lang>
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

# <lang>
#   <zh-CN>汇总项目、主题、模块包和发布目录检查；Warning/Fail 仍是静态证据，不等于真实 IIS proof。</zh-CN>
#   <en>Summarize project, theme, module-package, and published-directory checks; Warning/Fail are static evidence, not real IIS proof.</en>
# </lang>
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

# <lang>
#   <zh-CN>存在 Fail 或显式 TreatWarningsAsErrors 的 Warning 时返回非零；不会自动发布或修改配置。</zh-CN>
#   <en>Return non-zero for Fail findings or warnings when TreatWarningsAsErrors is explicit; do not deploy or modify configuration.</en>
# </lang>
if ($summary.FailedChecks -gt 0 -or ($TreatWarningsAsErrors -and $summary.WarningChecks -gt 0)) {
    exit 1
}
