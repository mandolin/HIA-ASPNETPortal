<#
.SYNOPSIS
.LANG en
Builds a read-only Tab and Module navigation inventory for the Portal project.

.LANG zh-CN
生成 Portal 项目的只读 Tab 与模块导航配置盘点。

.LANG en
Combines Portal Tab rows, module instances, module definitions, trusted
module-package manifests, and module Profile settings into one runtime-entry map.
When a connection-string config file is provided, the script reads the target
database in read-only mode; otherwise it falls back to the setup seed SQL files.
It does not modify databases, IIS, source files, or external configuration.

.LANG zh-CN
将 Portal Tab 行、模块实例、模块定义、受信任模块包 manifest 和模块 Profile 设置合并成
一份运行期入口地图。提供连接串配置文件时，脚本以只读方式读取目标数据库；否则回退
到 Setup 种子 SQL 文件。本脚本不修改数据库、IIS、源码或外置配置。

.PARAMETER ConnectionStringsConfigPath
.LANG en
Optional connectionStrings.config file used to read the live Portal database.

.LANG zh-CN
可选 connectionStrings.config 文件，用于读取当前 Portal 数据库。

.PARAMETER AppSettingsJson
.LANG en
AppSettings JSON file used to resolve the active module Profile.

.LANG zh-CN
用于解析当前模块 Profile 的 appSettings JSON 文件。

.PARAMETER OutputJson
.LANG en
Optional UTF-8 no-BOM JSON output path.

.LANG zh-CN
可选 UTF-8 无 BOM JSON 输出路径。

.PARAMETER OutputMarkdown
.LANG en
Optional UTF-8 no-BOM Markdown summary output path.

.LANG zh-CN
可选 UTF-8 无 BOM Markdown 摘要输出路径。

.PARAMETER AsJson
.LANG en
Writes the full inventory object to stdout as JSON.

.LANG zh-CN
将完整盘点对象以 JSON 写到标准输出。
#>
[CmdletBinding()]
param(
    [ValidateScript({ [string]::IsNullOrWhiteSpace($_) -or (Test-Path -LiteralPath $_ -PathType Leaf) })]
    [string]$ConnectionStringsConfigPath,

    [string]$ConnectionStringName = 'Portal',

    [ValidateScript({ [string]::IsNullOrWhiteSpace($_) -or (Test-Path -LiteralPath $_ -PathType Leaf) })]
    [string]$AppSettingsJson,

    [string]$OutputJson,

    [string]$OutputMarkdown,

    [switch]$AsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($AppSettingsJson)) {
    $AppSettingsJson = Join-Path $repoRoot 'src/Portal/Config/appSettings.json'
}

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

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootPrefix = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\') + '\'
    if ($fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ($fullPath.Substring($rootPrefix.Length) -replace '\\', '/')
    }

    return ($fullPath -replace '\\', '/')
}

function Get-ExternalConnectionString {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Name
    )

    [xml]$document = [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))
    $connectionStringsNode = if ($document.DocumentElement -and $document.DocumentElement.Name -eq 'connectionStrings') {
        $document.DocumentElement
    }
    elseif ($document.configuration -and $document.configuration.connectionStrings) {
        $document.configuration.connectionStrings
    }
    else {
        throw 'The external connection-string file must contain a <connectionStrings> section.'
    }

    $matches = @($connectionStringsNode.add | Where-Object { $_.name -eq $Name })
    if ($matches.Count -ne 1 -or [string]::IsNullOrWhiteSpace($matches[0].connectionString)) {
        throw "The external connection-string file does not contain one non-empty '$Name' entry."
    }

    return [string]$matches[0].connectionString
}

function Invoke-ReaderRows {
    param(
        [Parameter(Mandatory = $true)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory = $true)][string]$CommandText
    )

    $rows = New-Object 'System.Collections.Generic.List[object]'
    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = $CommandText
        $command.CommandTimeout = 30
        $reader = $command.ExecuteReader()
        try {
            while ($reader.Read()) {
                $row = [ordered]@{}
                for ($index = 0; $index -lt $reader.FieldCount; $index++) {
                    $value = $reader.GetValue($index)
                    $row[$reader.GetName($index)] = if ([System.DBNull]::Value.Equals($value)) { $null } else { $value }
                }

                $rows.Add([pscustomobject]$row)
            }
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $command.Dispose()
    }

    return $rows.ToArray()
}

function ConvertFrom-SqlQuotedString {
    param([AllowEmptyString()][string]$Value)

    if ($null -eq $Value) {
        return ''
    }

    $trimmed = $Value.Trim()
    if ($trimmed.StartsWith("N'", [System.StringComparison]::OrdinalIgnoreCase)) {
        $trimmed = $trimmed.Substring(1)
    }

    if ($trimmed.StartsWith("'") -and $trimmed.EndsWith("'")) {
        $trimmed = $trimmed.Substring(1, $trimmed.Length - 2)
    }

    return $trimmed -replace "''", "'"
}

function Read-StaticSeedRows {
    $loadDataPath = Join-Path $repoRoot 'src/Setup/Portal_LoadData.sql'
    $loadConfigPath = Join-Path $repoRoot 'src/Setup/Portal_LoadConfig.sql'
    $tabs = New-Object 'System.Collections.Generic.List[object]'
    $modules = New-Object 'System.Collections.Generic.List[object]'
    $definitions = New-Object 'System.Collections.Generic.List[object]'

    # <lang>
    #   <zh-CN>静态模式只解析项目自带种子脚本，作为“首次安装基线”，不代表当前开发库一定完全一致。</zh-CN>
    #   <en>Static mode parses only bundled seed scripts as the first-install baseline; it may differ from the current development database.</en>
    # </lang>
    foreach ($line in [System.IO.File]::ReadLines($loadDataPath, [System.Text.UTF8Encoding]::new($false))) {
        $tabMatch = [regex]::Match($line, "INSERT\s+\[PortalCfg_Tabs\].*VALUES\s*\((\d+),\s*(N'[^']*(?:''[^']*)*'),\s*(\d+),\s*(N'[^']*(?:''[^']*)*'),\s*(\d+),\s*(N'[^']*(?:''[^']*)*'),\s*(\d+)\)", 'IgnoreCase')
        if ($tabMatch.Success) {
            $tabs.Add([pscustomobject]@{
                    TabId = [int]$tabMatch.Groups[1].Value
                    TabName = ConvertFrom-SqlQuotedString -Value $tabMatch.Groups[2].Value
                    TabOrder = [int]$tabMatch.Groups[3].Value
                    AccessRoles = ConvertFrom-SqlQuotedString -Value $tabMatch.Groups[4].Value
                    ShowMobile = [bool]([int]$tabMatch.Groups[5].Value)
                    MobileTabName = ConvertFrom-SqlQuotedString -Value $tabMatch.Groups[6].Value
                    Source = 'StaticSeed'
                })
            continue
        }

        $moduleMatch = [regex]::Match($line, "INSERT\s+\[PortalCfg_Modules\].*VALUES\s*\((\d+),\s*(N'[^']*(?:''[^']*)*'),\s*(\d+),\s*(N'[^']*(?:''[^']*)*'),\s*(N'[^']*(?:''[^']*)*'),\s*(\d+),\s*(\d+),\s*(\d+),\s*(\d+)\)", 'IgnoreCase')
        if ($moduleMatch.Success) {
            $modules.Add([pscustomobject]@{
                    ModuleId = [int]$moduleMatch.Groups[1].Value
                    ModuleTitle = ConvertFrom-SqlQuotedString -Value $moduleMatch.Groups[2].Value
                    ModuleOrder = [int]$moduleMatch.Groups[3].Value
                    EditRoles = ConvertFrom-SqlQuotedString -Value $moduleMatch.Groups[4].Value
                    PaneName = ConvertFrom-SqlQuotedString -Value $moduleMatch.Groups[5].Value
                    ShowMobile = [bool]([int]$moduleMatch.Groups[6].Value)
                    CacheTimeout = [int]$moduleMatch.Groups[7].Value
                    ModuleDefId = [int]$moduleMatch.Groups[8].Value
                    TabId = [int]$moduleMatch.Groups[9].Value
                    Source = 'StaticSeed'
                })
        }
    }

    foreach ($line in [System.IO.File]::ReadLines($loadConfigPath, [System.Text.UTF8Encoding]::new($false))) {
        $definitionMatch = [regex]::Match($line, "INSERT\s+\[PortalCfg_ModuleDefinitions\].*VALUES\s*\((\d+),\s*(N'[^']*(?:''[^']*)*'),\s*(N'[^']*(?:''[^']*)*'),\s*(N?'[^']*(?:''[^']*)*')\)", 'IgnoreCase')
        if ($definitionMatch.Success) {
            $definitions.Add([pscustomobject]@{
                    ModuleDefId = [int]$definitionMatch.Groups[1].Value
                    FriendlyName = ConvertFrom-SqlQuotedString -Value $definitionMatch.Groups[2].Value
                    DesktopSourceFile = ConvertFrom-SqlQuotedString -Value $definitionMatch.Groups[3].Value
                    MobileSourceFile = ConvertFrom-SqlQuotedString -Value $definitionMatch.Groups[4].Value
                    Source = 'StaticSeed'
                })
        }
    }

    return [pscustomobject]@{
        DataSource = 'StaticSeed'
        Tabs = $tabs.ToArray()
        Modules = $modules.ToArray()
        ModuleDefinitions = $definitions.ToArray()
        PackageStates = @()
        Warnings = @('Using setup seed SQL because no live connection-string config was provided.')
    }
}

function Read-LiveDatabaseRows {
    param([Parameter(Mandatory = $true)][string]$ConnectionString)

    $connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
    try {
        $connection.Open()
        $tabs = Invoke-ReaderRows -Connection $connection -CommandText @'
SELECT TabId, TabName, TabOrder, AccessRoles, ShowMobile, MobileTabName
FROM dbo.PortalCfg_Tabs
ORDER BY TabOrder, TabId;
'@
        $modules = Invoke-ReaderRows -Connection $connection -CommandText @'
SELECT ModuleId, ModuleTitle, ModuleOrder, EditRoles, PaneName, ShowMobile, CacheTimeout, ModuleDefId, TabId
FROM dbo.PortalCfg_Modules
ORDER BY TabId, PaneName, ModuleOrder, ModuleId;
'@
        $definitions = Invoke-ReaderRows -Connection $connection -CommandText @'
SELECT ModuleDefId, FriendlyName, DesktopSourceFile, MobileSourceFile
FROM dbo.PortalCfg_ModuleDefinitions
ORDER BY ModuleDefId;
'@
        $stateTableCount = Invoke-ReaderRows -Connection $connection -CommandText @"
SELECT COUNT(1) AS TableCount
FROM sys.objects
WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_ModulePackageStates]') AND type IN (N'U');
"@
        $states = if ($stateTableCount.Count -eq 1 -and [int]$stateTableCount[0].TableCount -eq 1) {
            Invoke-ReaderRows -Connection $connection -CommandText @'
SELECT PackageId, IsEnabled, UpdatedUtc
FROM dbo.PortalCfg_ModulePackageStates
ORDER BY PackageId;
'@
        }
        else {
            @()
        }

        return [pscustomobject]@{
            DataSource = 'LiveDatabase'
            Tabs = @($tabs | ForEach-Object { $_ | Add-Member -NotePropertyName Source -NotePropertyValue 'LiveDatabase' -PassThru })
            Modules = @($modules | ForEach-Object { $_ | Add-Member -NotePropertyName Source -NotePropertyValue 'LiveDatabase' -PassThru })
            ModuleDefinitions = @($definitions | ForEach-Object { $_ | Add-Member -NotePropertyName Source -NotePropertyValue 'LiveDatabase' -PassThru })
            PackageStates = @($states)
            Warnings = @()
        }
    }
    finally {
        $connection.Dispose()
    }
}

function Read-ModulePackages {
    $packageRoot = Join-Path $repoRoot 'src/Portal/DesktopModules'
    $packages = New-Object 'System.Collections.Generic.List[object]'
    if (-not (Test-Path -LiteralPath $packageRoot -PathType Container)) {
        return $packages.ToArray()
    }

    foreach ($manifest in Get-ChildItem -LiteralPath $packageRoot -Recurse -Filter 'module.json' -File | Sort-Object FullName) {
        if ((ConvertTo-RepoPath -Path $manifest.FullName) -match '/obj/') {
            continue
        }

        try {
            $json = Get-Content -LiteralPath $manifest.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
            $packages.Add([pscustomobject]@{
                    PackageId = [string]$json.packageId
                    DisplayName = [string]$json.displayName
                    Version = [string]$json.version
                    DesktopEntry = Normalize-DesktopSource -Source ([string]$json.desktopEntry)
                    ManifestPath = ConvertTo-RepoPath -Path $manifest.FullName
                })
        }
        catch {
            $packages.Add([pscustomobject]@{
                    PackageId = ''
                    DisplayName = ''
                    Version = ''
                    DesktopEntry = ''
                    ManifestPath = ConvertTo-RepoPath -Path $manifest.FullName
                    Error = $_.Exception.Message
                })
        }
    }

    return $packages.ToArray()
}

function Normalize-DesktopSource {
    param([AllowEmptyString()][string]$Source)

    return (($Source ?? '').Trim().TrimStart('~', '/') -replace '\\', '/')
}

function Read-AppSettings {
    param([Parameter(Mandatory = $true)][string]$Path)

    $settings = @{}
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $settings
    }

    $json = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($json.appSettings) {
        foreach ($property in $json.appSettings.PSObject.Properties) {
            $settings[$property.Name] = [string]$property.Value
        }
    }

    return $settings
}

function Split-Csv {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return @()
    }

    return @($Value.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_.Length -gt 0 })
}

function Resolve-Profile {
    param([hashtable]$Settings)

    $defaultProfilePackages = @{
        EnterpriseBase = 'HIA.EmployeeProfileConfirm,HIA.EmployeeProfileCorrectionRequest'
        BusinessWorkflow = 'HIA.BusinessApplicationRequest'
        LegacyContent = 'Legacy.Announcements,Legacy.Contacts,Legacy.Discussion,Legacy.Document,Legacy.Events,Legacy.HtmlModule,Legacy.ImageModule,Legacy.Links,Legacy.QuickLinks,Legacy.XmlModule'
        DevProbe = 'HIA.ModuleProbe'
    }
    $defaultProfileIncludes = @{
        BusinessWorkflow = 'EnterpriseBase'
    }
    $activeProfile = if ($Settings.ContainsKey('Portal.ModuleProfiles.Active') -and -not [string]::IsNullOrWhiteSpace($Settings['Portal.ModuleProfiles.Active'])) {
        $Settings['Portal.ModuleProfiles.Active']
    }
    else {
        'CoreOnly'
    }

    $allowed = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    $visited = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

    function Add-ProfilePackages {
        param([string]$ProfileName)

        if ([string]::IsNullOrWhiteSpace($ProfileName) -or $visited.Contains($ProfileName)) {
            return
        }

        [void]$visited.Add($ProfileName)
        $includeKey = 'Portal.ModuleProfiles.' + $ProfileName + '.Includes'
        $includes = if ($Settings.ContainsKey($includeKey)) { $Settings[$includeKey] } elseif ($defaultProfileIncludes.ContainsKey($ProfileName)) { $defaultProfileIncludes[$ProfileName] } else { '' }
        foreach ($include in Split-Csv -Value $includes) {
            Add-ProfilePackages -ProfileName $include
        }

        $packageKey = 'Portal.ModuleProfiles.' + $ProfileName + '.Packages'
        $packages = if ($Settings.ContainsKey($packageKey)) { $Settings[$packageKey] } elseif ($defaultProfilePackages.ContainsKey($ProfileName)) { $defaultProfilePackages[$ProfileName] } else { '' }
        foreach ($package in Split-Csv -Value $packages) {
            [void]$allowed.Add($package)
        }
    }

    Add-ProfilePackages -ProfileName $activeProfile
    if ($Settings.ContainsKey('Portal.ModulePackages.Enabled')) {
        foreach ($package in Split-Csv -Value $Settings['Portal.ModulePackages.Enabled']) {
            [void]$allowed.Add($package)
        }
    }

    return [pscustomobject]@{
        ActiveProfile = $activeProfile
        AllowedPackageIds = @($allowed | Sort-Object)
        AppSettingsPath = ConvertTo-RepoPath -Path $AppSettingsJson
    }
}

function Get-LegacyPackageId {
    param([string]$DesktopSource)

    $legacyMap = @{
        'DesktopModules/Announcements.ascx' = 'Legacy.Announcements'
        'DesktopModules/Contacts.ascx' = 'Legacy.Contacts'
        'DesktopModules/Discussion.ascx' = 'Legacy.Discussion'
        'DesktopModules/Document.ascx' = 'Legacy.Document'
        'DesktopModules/Events.ascx' = 'Legacy.Events'
        'DesktopModules/HtmlModule.ascx' = 'Legacy.HtmlModule'
        'DesktopModules/ImageModule.ascx' = 'Legacy.ImageModule'
        'DesktopModules/Links.ascx' = 'Legacy.Links'
        'DesktopModules/QuickLinks.ascx' = 'Legacy.QuickLinks'
        'DesktopModules/XmlModule.ascx' = 'Legacy.XmlModule'
    }

    $normalized = Normalize-DesktopSource -Source $DesktopSource
    if ($legacyMap.ContainsKey($normalized)) {
        return $legacyMap[$normalized]
    }

    return ''
}

function Resolve-ModuleEntry {
    param(
        [string]$DesktopSource,
        [object[]]$Packages,
        [object]$Profile,
        [hashtable]$StateByPackage
    )

    $normalized = Normalize-DesktopSource -Source $DesktopSource
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return [pscustomobject]@{ EntryKind = 'Missing'; PackageId = ''; ProfileStatus = 'Blocked'; StateStatus = 'Unknown' }
    }

    if ($normalized.StartsWith('Admin/', [System.StringComparison]::OrdinalIgnoreCase) -or
        [string]::Equals($normalized, 'DesktopModules/SignIn.ascx', [System.StringComparison]::OrdinalIgnoreCase)) {
        return [pscustomobject]@{ EntryKind = 'Core'; PackageId = 'Core'; ProfileStatus = 'Allowed'; StateStatus = 'ImplicitEnabled' }
    }

    $package = @($Packages | Where-Object { [string]::Equals($_.DesktopEntry, $normalized, [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1)
    if ($package.Count -eq 1) {
        $state = if ($StateByPackage.ContainsKey($package[0].PackageId)) { $StateByPackage[$package[0].PackageId] } else { $null }
        $stateStatus = if ($null -eq $state) { 'DefaultEnabledOrUnknown' } elseif ([bool]$state.IsEnabled) { 'Enabled' } else { 'Disabled' }
        return [pscustomobject]@{
            EntryKind = 'TrustedPackage'
            PackageId = $package[0].PackageId
            ProfileStatus = if ($Profile.AllowedPackageIds -contains $package[0].PackageId) { 'Allowed' } else { 'Blocked' }
            StateStatus = $stateStatus
        }
    }

    $legacyPackageId = Get-LegacyPackageId -DesktopSource $normalized
    if (-not [string]::IsNullOrWhiteSpace($legacyPackageId)) {
        return [pscustomobject]@{
            EntryKind = 'LegacyMapped'
            PackageId = $legacyPackageId
            ProfileStatus = if ($Profile.AllowedPackageIds -contains $legacyPackageId) { 'Allowed' } else { 'Blocked' }
            StateStatus = 'ImplicitEnabled'
        }
    }

    return [pscustomobject]@{ EntryKind = 'Unmapped'; PackageId = ''; ProfileStatus = 'Blocked'; StateStatus = 'Unknown' }
}

$appSettings = Read-AppSettings -Path $AppSettingsJson
$profile = Resolve-Profile -Settings $appSettings
$packages = @(Read-ModulePackages)
$data = if ([string]::IsNullOrWhiteSpace($ConnectionStringsConfigPath)) {
    Read-StaticSeedRows
}
else {
    Read-LiveDatabaseRows -ConnectionString (Get-ExternalConnectionString -Path $ConnectionStringsConfigPath -Name $ConnectionStringName)
}

$definitionsById = @{}
foreach ($definition in $data.ModuleDefinitions) {
    $definitionsById[[int]$definition.ModuleDefId] = $definition
}

$tabsById = @{}
foreach ($tab in $data.Tabs) {
    $tabsById[[int]$tab.TabId] = $tab
}

$stateByPackage = @{}
foreach ($state in $data.PackageStates) {
    if (-not [string]::IsNullOrWhiteSpace([string]$state.PackageId)) {
        $stateByPackage[[string]$state.PackageId] = $state
    }
}

$entries = New-Object 'System.Collections.Generic.List[object]'
foreach ($module in $data.Modules) {
    $definition = if ($null -ne $module.ModuleDefId -and $definitionsById.ContainsKey([int]$module.ModuleDefId)) { $definitionsById[[int]$module.ModuleDefId] } else { $null }
    $tab = if ($null -ne $module.TabId -and $tabsById.ContainsKey([int]$module.TabId)) { $tabsById[[int]$module.TabId] } else { $null }
    $desktopSource = if ($definition) { [string]$definition.DesktopSourceFile } else { '' }
    $resolution = Resolve-ModuleEntry -DesktopSource $desktopSource -Packages $packages -Profile $profile -StateByPackage $stateByPackage
    $entries.Add([pscustomobject]([ordered]@{
                TabId = if ($tab) { $tab.TabId } else { $module.TabId }
                TabName = if ($tab) { $tab.TabName } else { '' }
                TabOrder = if ($tab) { $tab.TabOrder } else { $null }
                TabAccessRoles = if ($tab) { $tab.AccessRoles } else { '' }
                ModuleId = $module.ModuleId
                ModuleTitle = $module.ModuleTitle
                PaneName = $module.PaneName
                ModuleOrder = $module.ModuleOrder
                EditRoles = $module.EditRoles
                ModuleDefId = $module.ModuleDefId
                FriendlyName = if ($definition) { $definition.FriendlyName } else { '' }
                DesktopSourceFile = $desktopSource
                EntryKind = $resolution.EntryKind
                PackageId = $resolution.PackageId
                ProfileStatus = $resolution.ProfileStatus
                StateStatus = $resolution.StateStatus
            }))
}

$inventory = [pscustomobject]([ordered]@{
        GeneratedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
        DataSource = $data.DataSource
        Warnings = @($data.Warnings)
        ActiveProfile = $profile.ActiveProfile
        AllowedPackageIds = $profile.AllowedPackageIds
        Tabs = @($data.Tabs | Sort-Object TabOrder, TabId)
        ModuleDefinitions = @($data.ModuleDefinitions | Sort-Object ModuleDefId)
        TrustedPackages = @($packages | Sort-Object PackageId)
        RuntimeEntries = @($entries | Sort-Object TabOrder, TabId, PaneName, ModuleOrder, ModuleId)
        Summary = [pscustomobject]([ordered]@{
                TabCount = @($data.Tabs).Count
                ModuleInstanceCount = @($data.Modules).Count
                ModuleDefinitionCount = @($data.ModuleDefinitions).Count
                TrustedPackageCount = @($packages | Where-Object { -not [string]::IsNullOrWhiteSpace($_.PackageId) }).Count
                RuntimeEntryCount = $entries.Count
                ProfileBlockedCount = @($entries | Where-Object { $_.ProfileStatus -eq 'Blocked' }).Count
                UnmappedEntryCount = @($entries | Where-Object { $_.EntryKind -eq 'Unmapped' -or $_.EntryKind -eq 'Missing' }).Count
            })
    })

$markdownLines = New-Object 'System.Collections.Generic.List[string]'
$markdownLines.Add('# Portal Tab / Module 运行入口盘点')
$markdownLines.Add('')
$markdownLines.Add('生成时间 UTC：' + $inventory.GeneratedUtc)
$markdownLines.Add('')
$markdownLines.Add('## 摘要')
$markdownLines.Add('')
$markdownLines.Add('| 指标 | 数值 |')
$markdownLines.Add('| --- | --- |')
$markdownLines.Add('| 数据源 | ' + $inventory.DataSource + ' |')
$markdownLines.Add('| Active Profile | ' + $inventory.ActiveProfile + ' |')
$markdownLines.Add('| Tab 数 | ' + $inventory.Summary.TabCount + ' |')
$markdownLines.Add('| 模块实例数 | ' + $inventory.Summary.ModuleInstanceCount + ' |')
$markdownLines.Add('| 模块定义数 | ' + $inventory.Summary.ModuleDefinitionCount + ' |')
$markdownLines.Add('| 受信任部署包数 | ' + $inventory.Summary.TrustedPackageCount + ' |')
$markdownLines.Add('| Profile 阻断实例数 | ' + $inventory.Summary.ProfileBlockedCount + ' |')
$markdownLines.Add('| 未映射入口数 | ' + $inventory.Summary.UnmappedEntryCount + ' |')
$markdownLines.Add('')
if ($inventory.Warnings.Count -gt 0) {
    $markdownLines.Add('## Warnings')
    $markdownLines.Add('')
    foreach ($warning in $inventory.Warnings) {
        $markdownLines.Add('- ' + $warning)
    }
    $markdownLines.Add('')
}
$markdownLines.Add('## Runtime Entries')
$markdownLines.Add('')
$markdownLines.Add('| Tab | Module | Source | EntryKind | Package | Profile | State |')
$markdownLines.Add('| --- | --- | --- | --- | --- | --- | --- |')
foreach ($entry in $inventory.RuntimeEntries) {
    $markdownLines.Add('| ' + $entry.TabName + ' (' + $entry.TabId + ') | ' + $entry.ModuleTitle + ' (' + $entry.ModuleId + ') | `' + $entry.DesktopSourceFile + '` | ' + $entry.EntryKind + ' | ' + $entry.PackageId + ' | ' + $entry.ProfileStatus + ' | ' + $entry.StateStatus + ' |')
}

$json = $inventory | ConvertTo-Json -Depth 8
$markdown = ($markdownLines -join [Environment]::NewLine) + [Environment]::NewLine

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    Write-Utf8NoBomFile -Path $OutputJson -Content $json
}

if (-not [string]::IsNullOrWhiteSpace($OutputMarkdown)) {
    Write-Utf8NoBomFile -Path $OutputMarkdown -Content $markdown
}

if ($AsJson) {
    $json
}
else {
    Write-Host ('Data source: {0}' -f $inventory.DataSource)
    Write-Host ('Tabs: {0}' -f $inventory.Summary.TabCount)
    Write-Host ('Runtime entries: {0}' -f $inventory.Summary.RuntimeEntryCount)
    Write-Host ('Profile blocked entries: {0}' -f $inventory.Summary.ProfileBlockedCount)
    Write-Host ('Unmapped entries: {0}' -f $inventory.Summary.UnmappedEntryCount)
    if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
        Write-Host ('JSON: {0}' -f $OutputJson)
    }

    if (-not [string]::IsNullOrWhiteSpace($OutputMarkdown)) {
        Write-Host ('Markdown: {0}' -f $OutputMarkdown)
    }
}
