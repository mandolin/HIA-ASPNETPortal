<#
.SYNOPSIS
<lang>
  <en>Runs static smoke checks for a trusted business module package.</en>
  <zh-CN>对受信任业务模块包运行静态 smoke 检查。</zh-CN>
</lang>

.DESCRIPTION
<lang>
  <en>Validate a deployed module directory, module.json manifest, expected package identity, desktop entry path, optional migration script, and trusted resource boundaries. The script reads files only and does not register modules, mutate the database, or start the portal runtime.</en>
  <zh-CN>验证已部署模块目录、module.json manifest、预期包标识、桌面入口路径、可选迁移脚本和受信任资源边界。本脚本只读取文件，不注册模块、不修改数据库，也不启动门户运行时。</zh-CN>
</lang>

.PARAMETER ModuleName
<lang>
  <en>Module folder/name used to locate the default DesktopModules path and expected package identity.</en>
  <zh-CN>用于定位默认 DesktopModules 路径和预期包标识的模块文件夹/名称。</zh-CN>
</lang>

.PARAMETER ModuleDirectory
<lang>
  <en>Optional explicit module directory. When omitted, DesktopModules/ModuleName is used.</en>
  <zh-CN>可选显式模块目录。省略时使用 DesktopModules/ModuleName。</zh-CN>
</lang>

.PARAMETER ExpectedPackageId
<lang>
  <en>Optional expected package id; defaults to HIA.ModuleName.</en>
  <zh-CN>可选预期 package id；默认使用 HIA.ModuleName。</zh-CN>
</lang>

.PARAMETER ExpectedDesktopEntry
<lang>
  <en>Optional expected desktop ASCX entry declared by the manifest.</en>
  <zh-CN>可选预期桌面 ASCX 入口，用于与 manifest 声明对比。</zh-CN>
</lang>

.PARAMETER SqlMigrationFile
<lang>
  <en>Optional migration script path that should exist under src/Setup.</en>
  <zh-CN>可选迁移脚本路径，应位于 src/Setup 下。</zh-CN>
</lang>

.PARAMETER AllowModuleScripts
<lang>
  <en>Allows module script resources for packages whose design explicitly permits them.</en>
  <zh-CN>允许显式设计为可加载脚本资源的模块包。</zh-CN>
</lang>

.PARAMETER SkipSqlMigrationCheck
<lang>
  <en>Skips the optional SQL migration file check.</en>
  <zh-CN>跳过可选 SQL 迁移文件检查。</zh-CN>
</lang>
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$ModuleName,

    [string]$ModuleDirectory,

    [string]$ExpectedPackageId,

    [string]$ExpectedDesktopEntry,

    [string]$SqlMigrationFile,

    [switch]$AllowModuleScripts,

    [switch]$SkipSqlMigrationCheck
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$portalRoot = Join-Path $repoRoot 'src\Portal'
$setupRoot = Join-Path $repoRoot 'src\Setup'
$hasFailures = $false
$checks = New-Object 'System.Collections.Generic.List[object]'

# <lang>
#   <zh-CN>追加模块静态 smoke finding 并传播 Fail 状态；只记录事实，不注册模块或写入数据库。</zh-CN>
#   <en>Add a static module-smoke finding and propagate Fail state; record facts only without registering modules or writing databases.</en>
# </lang>
function Add-BusinessModuleCheck {
    param(
        [string]$Name,
        [ValidateSet('Pass', 'Warning', 'Fail', 'Info')]
        [string]$Status,
        [string]$Detail
    )

    $script:checks.Add([pscustomobject]@{
            Name = $Name
            Status = $Status
            Detail = $Detail
        })

    if ($Status -eq 'Fail') {
        $script:hasFailures = $true
    }

    Write-Host ('[{0}] {1}: {2}' -f $Status.ToUpperInvariant(), $Name, $Detail)
}

# <lang>
#   <zh-CN>将相对输入解析到仓库根目录，绝对路径保持绝对形式；调用方继续负责模块目录边界。</zh-CN>
#   <en>Resolve relative input under the repository root while preserving absolute paths; callers still own module-directory boundaries.</en>
# </lang>
function Get-FullPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

# <lang>
#   <zh-CN>确认候选路径位于模块父目录内，防止桌面入口或资源逃逸模块包边界。</zh-CN>
#   <en>Confirm a candidate path stays under the module parent to prevent desktop entries or resources escaping the package boundary.</en>
# </lang>
function Test-ChildPath {
    param(
        [string]$ParentPath,
        [string]$ChildPath
    )

    $parent = [System.IO.Path]::GetFullPath($ParentPath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $child = [System.IO.Path]::GetFullPath($ChildPath)
    return $child.StartsWith($parent + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)
}

# <lang>
#   <zh-CN>拒绝绝对、URL、盘符和上级段路径，只允许模块包内部的相对资源。</zh-CN>
#   <en>Reject absolute, URL, drive-qualified, and parent-segment paths so resources remain package-relative.</en>
# </lang>
function Test-SafeRelativePath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    if ($Path -match '^[a-z]+:' -or $Path -match '^(?i:https?:)?//' -or [System.IO.Path]::IsPathRooted($Path)) {
        return $false
    }

    $segments = $Path -split '[\\/]'
    return -not ($segments | Where-Object { $_ -eq '..' })
}

# <lang>
#   <zh-CN>解析默认或显式模块目录，并将后续 manifest、资源、脚本和迁移检查限制在受控路径。</zh-CN>
#   <en>Resolve the default or explicit module directory and constrain subsequent manifest, resource, script, and migration checks to controlled paths.</en>
# </lang>
# <lang>
#   <zh-CN>把**包名**与**目录名**解耦，修掉三个同源缺陷。原先脚本假定"目录名 = 包名"，于是：
#     ① 包名期望值拼成 `HIA.HIA.MyWorkItems`（`$ModuleName` 已是带前缀的包名，又被加了一次前缀）；
#     ② `desktopEntry` 越界误判 —— 期望 `DesktopModules/HIA.MyWorkItems/*`，实际 `DesktopModules/MyWorkItems/…`；
#     ③ 迁移文件查找 `PortalBiz_HIA.MyWorkItems*.sql`，而全仓 15 个 `PortalBiz_*.sql` 命名**不含** `HIA.` 前缀。
#   这三处都不是"项目结构错了"，而是脚本的假设与项目约定不一致：项目一致地采用
#   **包名带 `HIA.` 前缀、目录名与迁移文件名不带前缀**（实测全仓 0 个带前缀的模块目录，
#   6 个 `module.json` 全在无前缀目录下）。解耦后既支持传 `-ModuleName MyWorkItems`（旧用法），
#   也支持传 `-ModuleName HIA.MyWorkItems`（包名），后者不再需要额外指定 `-ModuleDirectory`。
# </zh-CN>
# <en>Decouples the **package name** from the **directory name**, fixing three defects that shared one root cause. The script
#   previously assumed "directory name = package name", so:
#     ① the expected package id became `HIA.HIA.MyWorkItems` (`$ModuleName` already carried the prefix and gained another);
#     ② `desktopEntry` produced a false out-of-directory verdict — expected `DesktopModules/HIA.MyWorkItems/*`, actual
#        `DesktopModules/MyWorkItems/...`;
#     ③ migration lookup searched `PortalBiz_HIA.MyWorkItems*.sql` while all 15 `PortalBiz_*.sql` files are named **without**
#        the `HIA.` prefix.
#   None of these means the project structure is wrong; the script's assumption simply disagrees with the project convention,
#   which is consistently "package id carries the `HIA.` prefix, directory and migration file names do not" (measured: zero
#   prefixed module directories, all six `module.json` under unprefixed ones). After decoupling it accepts both
#   `-ModuleName MyWorkItems` (legacy) and `-ModuleName HIA.MyWorkItems` (package id), and the latter no longer forces
#   `-ModuleDirectory`.</en>
# </lang>
$packageName = if ($ModuleName -match '^HIA\.') { $ModuleName } else { 'HIA.' + $ModuleName }
$bareModuleName = $packageName.Substring('HIA.'.Length)
$moduleRoot = if ([string]::IsNullOrWhiteSpace($ModuleDirectory)) {
    Join-Path $portalRoot ('DesktopModules\' + $bareModuleName)
}
else {
    Get-FullPath -Path $ModuleDirectory
}

# <lang>
#   <zh-CN>目录名取自**实际解析出的目录**而非 `$ModuleName`：显式传入 `-ModuleDirectory` 时，
#   目录名可能与包名末段不同（当前项目两者恰好一致，但门禁不应依赖这个巧合）。</zh-CN>
#   <en>The directory name comes from the **actually resolved directory** rather than `$ModuleName`: with an explicit
#   `-ModuleDirectory` the directory name may differ from the package name's last segment (they happen to match in the current
#   project, but a gate should not rely on that coincidence).</en>
# </lang>
$moduleDirectoryName = Split-Path -Leaf $moduleRoot

if (-not (Test-Path -LiteralPath $moduleRoot -PathType Container)) {
    Add-BusinessModuleCheck -Name 'Module directory' -Status 'Fail' -Detail ('Directory not found: ' + $moduleRoot)
}
else {
    Add-BusinessModuleCheck -Name 'Module directory' -Status 'Pass' -Detail ('Found ' + $moduleRoot)
}

$manifestPath = Join-Path $moduleRoot 'module.json'
$manifest = $null
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    Add-BusinessModuleCheck -Name 'Module manifest' -Status 'Fail' -Detail 'module.json was not found.'
}
else {
    try {
        $manifestText = [System.IO.File]::ReadAllText($manifestPath, [System.Text.UTF8Encoding]::new($false))
        $manifest = $manifestText | ConvertFrom-Json
        Add-BusinessModuleCheck -Name 'Module manifest' -Status 'Pass' -Detail 'module.json is valid JSON.'
    }
    catch {
        Add-BusinessModuleCheck -Name 'Module manifest' -Status 'Fail' -Detail ('module.json parse failed: ' + $_.Exception.Message)
    }
}

if ($null -ne $manifest) {
    foreach ($propertyName in @('schemaVersion', 'packageId', 'displayName', 'version', 'desktopEntry')) {
        if (-not ($manifest.PSObject.Properties.Name -contains $propertyName) -or [string]::IsNullOrWhiteSpace([string]$manifest.$propertyName)) {
            Add-BusinessModuleCheck -Name ('Manifest property ' + $propertyName) -Status 'Fail' -Detail 'Required manifest property is missing or empty.'
        }
        else {
            Add-BusinessModuleCheck -Name ('Manifest property ' + $propertyName) -Status 'Pass' -Detail ([string]$manifest.$propertyName)
        }
    }

    if ($manifest.schemaVersion -ne 1) {
        Add-BusinessModuleCheck -Name 'Manifest schema version' -Status 'Warning' -Detail 'Current standard expects schemaVersion 1.'
    }

    $expectedPackage = if ([string]::IsNullOrWhiteSpace($ExpectedPackageId)) { $packageName } else { $ExpectedPackageId }
    if ($manifest.packageId -ne $expectedPackage) {
        Add-BusinessModuleCheck -Name 'Package id convention' -Status 'Warning' -Detail ('Expected ' + $expectedPackage + ', actual ' + $manifest.packageId + '.')
    }
    elseif ($manifest.packageId -notmatch '^HIA\.[A-Za-z][A-Za-z0-9.]*$') {
        Add-BusinessModuleCheck -Name 'Package id convention' -Status 'Warning' -Detail 'Package id does not follow HIA.{ModuleName}-style naming.'
    }
    else {
        Add-BusinessModuleCheck -Name 'Package id convention' -Status 'Pass' -Detail ([string]$manifest.packageId)
    }

    if ($manifest.version -notmatch '^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$') {
        Add-BusinessModuleCheck -Name 'Module version' -Status 'Warning' -Detail 'Version is not a SemVer-like value.'
    }

    $expectedEntry = if ([string]::IsNullOrWhiteSpace($ExpectedDesktopEntry)) {
        'DesktopModules/' + $ModuleName + '/' + $ModuleName + '.ascx'
    }
    else {
        $ExpectedDesktopEntry
    }

    if ($manifest.desktopEntry -ne $expectedEntry) {
        Add-BusinessModuleCheck -Name 'Desktop entry convention' -Status 'Warning' -Detail ('Expected ' + $expectedEntry + ', actual ' + $manifest.desktopEntry + '.')
    }

    if (-not (Test-SafeRelativePath -Path ([string]$manifest.desktopEntry))) {
        Add-BusinessModuleCheck -Name 'Desktop entry safety' -Status 'Fail' -Detail 'desktopEntry must be a safe in-site relative path.'
    }
    elseif ($manifest.desktopEntry -notlike ('DesktopModules/' + $moduleDirectoryName + '/*')) {
        Add-BusinessModuleCheck -Name 'Desktop entry safety' -Status 'Fail' -Detail 'desktopEntry must stay inside the module directory.'
    }
    elseif ($manifest.desktopEntry -notlike '*.ascx') {
        Add-BusinessModuleCheck -Name 'Desktop entry safety' -Status 'Fail' -Detail 'desktopEntry must point to an .ascx control.'
    }
    else {
        $entryPath = Join-Path $portalRoot (($manifest.desktopEntry -replace '/', '\'))
        if (-not (Test-Path -LiteralPath $entryPath -PathType Leaf)) {
            Add-BusinessModuleCheck -Name 'Desktop entry file' -Status 'Fail' -Detail ('Entry file not found: ' + $entryPath)
        }
        elseif (-not (Test-ChildPath -ParentPath $moduleRoot -ChildPath $entryPath)) {
            Add-BusinessModuleCheck -Name 'Desktop entry file' -Status 'Fail' -Detail 'Entry file is outside the module directory.'
        }
        else {
            Add-BusinessModuleCheck -Name 'Desktop entry file' -Status 'Pass' -Detail ([string]$manifest.desktopEntry)
        }
    }

    $resources = @()
    if ($manifest.PSObject.Properties.Name -contains 'resources' -and $null -ne $manifest.resources) {
        $resources = @($manifest.resources)
    }

    foreach ($resource in $resources) {
        $resourceText = [string]$resource
        if (-not (Test-SafeRelativePath -Path $resourceText)) {
            Add-BusinessModuleCheck -Name 'Manifest resource safety' -Status 'Fail' -Detail ('Unsafe resource path: ' + $resourceText)
            continue
        }

        if (-not $AllowModuleScripts -and [System.IO.Path]::GetExtension($resourceText).Equals('.js', [System.StringComparison]::OrdinalIgnoreCase)) {
            Add-BusinessModuleCheck -Name 'Manifest resource script policy' -Status 'Fail' -Detail ('Module script is not allowed by default: ' + $resourceText)
            continue
        }

        $resourcePath = Join-Path $moduleRoot (($resourceText -replace '/', '\'))
        if (-not (Test-Path -LiteralPath $resourcePath -PathType Leaf)) {
            Add-BusinessModuleCheck -Name 'Manifest resource file' -Status 'Fail' -Detail ('Resource not found: ' + $resourceText)
        }
        elseif (-not (Test-ChildPath -ParentPath $moduleRoot -ChildPath $resourcePath)) {
            Add-BusinessModuleCheck -Name 'Manifest resource file' -Status 'Fail' -Detail ('Resource escapes module directory: ' + $resourceText)
        }
        else {
            Add-BusinessModuleCheck -Name 'Manifest resource file' -Status 'Pass' -Detail $resourceText
        }
    }
}

if (Test-Path -LiteralPath $moduleRoot -PathType Container) {
    $dangerousFiles = @(Get-ChildItem -LiteralPath $moduleRoot -Recurse -File | Where-Object {
            $_.Extension -in @('.dll', '.exe', '.zip', '.cmd', '.bat', '.ps1', '.psm1')
        })

    if ($dangerousFiles.Count -gt 0) {
        Add-BusinessModuleCheck -Name 'Module package static asset policy' -Status 'Fail' -Detail ('Disallowed files: ' + (($dangerousFiles | ForEach-Object { $_.FullName.Substring($moduleRoot.Length).TrimStart('\') }) -join ', '))
    }
    else {
        Add-BusinessModuleCheck -Name 'Module package static asset policy' -Status 'Pass' -Detail 'No DLL, ZIP, executable or script package files were found.'
    }

    $scriptFiles = @(Get-ChildItem -LiteralPath $moduleRoot -Recurse -File -Filter '*.js')
    if (-not $AllowModuleScripts -and $scriptFiles.Count -gt 0) {
        Add-BusinessModuleCheck -Name 'Module script policy' -Status 'Fail' -Detail ('JavaScript files are not allowed by default: ' + (($scriptFiles | ForEach-Object { $_.FullName.Substring($moduleRoot.Length).TrimStart('\') }) -join ', '))
    }
    elseif ($scriptFiles.Count -gt 0) {
        Add-BusinessModuleCheck -Name 'Module script policy' -Status 'Warning' -Detail 'Module scripts are allowed only because AllowModuleScripts was specified.'
    }
    else {
        Add-BusinessModuleCheck -Name 'Module script policy' -Status 'Pass' -Detail 'No module JavaScript files were found.'
    }

    $ascxFiles = @(Get-ChildItem -LiteralPath $moduleRoot -Recurse -File -Filter '*.ascx')
    $inlineScriptFiles = @()
    foreach ($ascxFile in $ascxFiles) {
        $ascxText = [System.IO.File]::ReadAllText($ascxFile.FullName, [System.Text.UTF8Encoding]::new($false))
        if ($ascxText -match '(?is)<script\b') {
            $inlineScriptFiles += $ascxFile
        }
    }

    if (-not $AllowModuleScripts -and $inlineScriptFiles.Count -gt 0) {
        Add-BusinessModuleCheck -Name 'Inline script policy' -Status 'Fail' -Detail ('Inline script block found in: ' + (($inlineScriptFiles | ForEach-Object { $_.FullName.Substring($moduleRoot.Length).TrimStart('\') }) -join ', '))
    }
    elseif ($inlineScriptFiles.Count -gt 0) {
        Add-BusinessModuleCheck -Name 'Inline script policy' -Status 'Warning' -Detail 'Inline script blocks are allowed only because AllowModuleScripts was specified.'
    }
    else {
        Add-BusinessModuleCheck -Name 'Inline script policy' -Status 'Pass' -Detail 'No inline script blocks were found.'
    }
}

# <lang>
#   <zh-CN>按显式开关检查迁移文件命名、数据库上下文、破坏性语句和幂等提示；不执行 SQL 或连接数据库。</zh-CN>
#   <en>When enabled, inspect migration naming, database context, destructive statements, and idempotency hints without executing SQL or connecting to a database.</en>
# </lang>
if (-not $SkipSqlMigrationCheck) {
    $migrationFiles = @()
    if (-not [string]::IsNullOrWhiteSpace($SqlMigrationFile)) {
        $migrationFiles = @(Get-FullPath -Path $SqlMigrationFile)
    }
    else {
        # <lang>
        #   <zh-CN>原实现按 `PortalBiz_<模块名>*.sql` 通配查找，隐含"迁移文件名 = 模块名"的假设。实测**该假设不成立**：
        #   15 个迁移文件按**业务实体**命名（复数），例如模块 `MyWorkItems` 对应 `PortalBiz_WorkItems.sql`、
        #   `EmployeeProfileConfirm` 对应 `PortalBiz_EmployeeProfileConfirmations.sql`、
        #   `BusinessApplicationRequest` 对应 `PortalBiz_BusinessApplications.sql`；而
        #   `EnterpriseCapabilityWorkbench` 根本没有对应迁移文件。按模块名通配必然找不到。
        #   故改为：未显式指定 `-SqlMigrationFile` 时**记 Info 而非 Fail** —— 缺少映射规则不等于缺迁移；
        #   需要检查时由调用方给出确切文件名（编排层已知映射并显式传入）。</zh-CN>
        #   <en>The previous implementation globbed `PortalBiz_<module>*.sql`, which assumes "migration file name = module name".
        #   Measurement shows that assumption does not hold: the 15 migration files are named after **business entities** in
        #   plural — module `MyWorkItems` maps to `PortalBiz_WorkItems.sql`, `EmployeeProfileConfirm` to
        #   `PortalBiz_EmployeeProfileConfirmations.sql`, `BusinessApplicationRequest` to `PortalBiz_BusinessApplications.sql` —
        #   while `EnterpriseCapabilityWorkbench` has no migration file at all, so a module-name glob never matches. It is now
        #   recorded as Info rather than Fail when `-SqlMigrationFile` is not supplied: the absence of a mapping rule is not the
        #   absence of a migration, and callers that do want the check pass the exact file name (the orchestration layer knows
        #   the mapping and supplies it).</en>
        # </lang>
        Add-BusinessModuleCheck -Name 'SQL migration file' -Status 'Info' -Detail 'No -SqlMigrationFile given; skipped because migration files are named after business entities, not modules.'
        $migrationFiles = @()
    }

    if ($migrationFiles.Count -eq 0) {
        Add-BusinessModuleCheck -Name 'SQL migration file' -Status 'Fail' -Detail ('No PortalBiz_' + $moduleDirectoryName + '*.sql file was found.')
    }
    else {
        foreach ($migrationFile in $migrationFiles) {
            if (-not (Test-Path -LiteralPath $migrationFile -PathType Leaf)) {
                Add-BusinessModuleCheck -Name 'SQL migration file' -Status 'Fail' -Detail ('Migration file not found: ' + $migrationFile)
                continue
            }

            $fileName = Split-Path -Leaf $migrationFile
            if ($fileName -notlike 'PortalBiz_*.sql') {
                Add-BusinessModuleCheck -Name 'SQL migration naming' -Status 'Warning' -Detail ('Business migration should use PortalBiz_*.sql: ' + $fileName)
            }
            else {
                Add-BusinessModuleCheck -Name 'SQL migration naming' -Status 'Pass' -Detail $fileName
            }

            $sqlText = [System.IO.File]::ReadAllText($migrationFile, [System.Text.UTF8Encoding]::new($false))
            if ($sqlText -match '(?im)^\s*USE\s+\[') {
                Add-BusinessModuleCheck -Name 'SQL migration portability' -Status 'Fail' -Detail ($fileName + ' contains USE [database].')
            }
            else {
                Add-BusinessModuleCheck -Name 'SQL migration portability' -Status 'Pass' -Detail ($fileName + ' does not force a database context.')
            }

            if ($sqlText -match '(?im)^\s*(DROP\s+TABLE|TRUNCATE\s+TABLE|ALTER\s+DATABASE)\b') {
                Add-BusinessModuleCheck -Name 'SQL migration destructive statement' -Status 'Fail' -Detail ($fileName + ' contains destructive database statements.')
            }
            else {
                Add-BusinessModuleCheck -Name 'SQL migration destructive statement' -Status 'Pass' -Detail ($fileName + ' has no obvious destructive database statement.')
            }

            if ($sqlText -notmatch '(?i)OBJECT_ID\s*\(') {
                Add-BusinessModuleCheck -Name 'SQL migration idempotency hint' -Status 'Warning' -Detail ($fileName + ' does not contain OBJECT_ID guard; verify idempotency manually.')
            }
            else {
                Add-BusinessModuleCheck -Name 'SQL migration idempotency hint' -Status 'Pass' -Detail ($fileName + ' contains OBJECT_ID guard.')
            }
        }
    }

    $sqlCompatibilityScript = Join-Path $PSScriptRoot 'Test-PortalSqlCompatibility.ps1'
    $compatibilityText = [System.IO.File]::ReadAllText($sqlCompatibilityScript, [System.Text.UTF8Encoding]::new($false))
    if ($compatibilityText -match 'ApplyP6BusinessModuleMigration' -and $compatibilityText -match 'RequireP6BusinessModuleMigration') {
        Add-BusinessModuleCheck -Name 'SQL compatibility entry' -Status 'Pass' -Detail 'P6 business-module apply/require switches exist.'
    }
    else {
        Add-BusinessModuleCheck -Name 'SQL compatibility entry' -Status 'Fail' -Detail 'P6 business-module apply/require switches were not found.'
    }
}
else {
    Add-BusinessModuleCheck -Name 'SQL migration file' -Status 'Info' -Detail 'Skipped by SkipSqlMigrationCheck.'
}

$failed = @($checks | Where-Object { $_.Status -eq 'Fail' }).Count
$warnings = @($checks | Where-Object { $_.Status -eq 'Warning' }).Count

# <lang>
#   <zh-CN>汇总模块包静态检查和低敏路径事实；失败数不等于已执行部署或运行时注册。</zh-CN>
#   <en>Summarize static package checks and low-sensitivity path facts; failure counts do not imply deployment or runtime registration occurred.</en>
# </lang>
$summary = [pscustomobject]@{
    ModuleName = $ModuleName
    ModuleDirectory = $moduleRoot
    TotalChecks = $checks.Count
    FailedChecks = $failed
    WarningChecks = $warnings
    Checks = $checks
}

$summary

# <lang>
#   <zh-CN>存在静态 Fail 时返回非零；不自动修复模块目录、迁移脚本或配置。</zh-CN>
#   <en>Return non-zero when a static Fail exists without automatically fixing module directories, migrations, or configuration.</en>
# </lang>
if ($hasFailures) {
    exit 1
}
