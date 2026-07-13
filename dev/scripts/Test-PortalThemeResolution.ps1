[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$ConnectionStringsConfigPath,

    [ValidateRange(1025, 65535)]
    [int]$Port = 40005
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$settingKey = 'Portal.Theme.Name'
$testActor = 'P3.5-theme-smoke'
$startedThemeSite = $false
$connection = $null
$settingSnapshot = $null
$tabSnapshot = $null
$tabId = 0

function Get-ExternalPortalConnectionString {
    param([string]$Path)

    [xml]$document = [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))
    $connectionStringsNode = if ($document.DocumentElement -and $document.DocumentElement.Name -eq 'connectionStrings') {
        $document.DocumentElement
    }
    else {
        $document.SelectSingleNode('/configuration/connectionStrings')
    }
    $portalNode = if ($connectionStringsNode) {
        $connectionStringsNode.SelectSingleNode("add[@name='Portal']")
    }
    else {
        $null
    }

    if ($null -eq $portalNode -or [string]::IsNullOrWhiteSpace($portalNode.connectionString)) {
        throw 'The external connectionStrings.config file does not contain a Portal connection string.'
    }

    return $portalNode.connectionString
}

function Add-TextParameter {
    param(
        [System.Data.SqlClient.SqlCommand]$Command,
        [string]$Name,
        [int]$Size,
        [AllowNull()][string]$Value
    )

    $parameter = $Command.Parameters.Add($Name, [System.Data.SqlDbType]::NVarChar, $Size)
    $parameter.Value = if ($null -eq $Value) { [DBNull]::Value } else { $Value }
}

function Add-IntParameter {
    param(
        [System.Data.SqlClient.SqlCommand]$Command,
        [string]$Name,
        [int]$Value
    )

    $parameter = $Command.Parameters.Add($Name, [System.Data.SqlDbType]::Int)
    $parameter.Value = $Value
}

function Add-BitParameter {
    param(
        [System.Data.SqlClient.SqlCommand]$Command,
        [string]$Name,
        [bool]$Value
    )

    $parameter = $Command.Parameters.Add($Name, [System.Data.SqlDbType]::Bit)
    $parameter.Value = $Value
}

function Add-DateTime2Parameter {
    param(
        [System.Data.SqlClient.SqlCommand]$Command,
        [string]$Name,
        [DateTime]$Value
    )

    $parameter = $Command.Parameters.Add($Name, [System.Data.SqlDbType]::DateTime2)
    $parameter.Value = $Value
}

function Invoke-NonQuery {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Sql,
        [scriptblock]$Configure
    )

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = $Sql
        & $Configure $command
        [void]$command.ExecuteNonQuery()
    }
    finally {
        $command.Dispose()
    }
}

function Invoke-ScalarInt {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Sql
    )

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = $Sql
        $value = $command.ExecuteScalar()
        if ($null -eq $value -or $value -is [DBNull]) {
            throw 'No eligible public portal Tab was found for the theme proof.'
        }

        return [Convert]::ToInt32($value, [Globalization.CultureInfo]::InvariantCulture)
    }
    finally {
        $command.Dispose()
    }
}

function Get-SystemSettingSnapshot {
    param([System.Data.SqlClient.SqlConnection]$Connection)

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = @'
SELECT [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc]
FROM [dbo].[PortalCfg_SystemSettings]
WHERE [SettingKey] = @SettingKey
'@
        Add-TextParameter -Command $command -Name '@SettingKey' -Size 200 -Value $settingKey
        $reader = $command.ExecuteReader()
        try {
            if (-not $reader.Read()) {
                return [pscustomobject]@{ Exists = $false }
            }

            return [pscustomobject]@{
                Exists = $true
                SettingValue = if ($reader.IsDBNull(0)) { $null } else { $reader.GetString(0) }
                ValueType = $reader.GetString(1)
                SourceLevel = $reader.GetString(2)
                CanDelete = $reader.GetBoolean(3)
                UpdatedBy = if ($reader.IsDBNull(4)) { $null } else { $reader.GetString(4) }
                UpdatedUtc = $reader.GetDateTime(5)
            }
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $command.Dispose()
    }
}

function Get-TabOverrideSnapshot {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [int]$TabId
    )

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = @'
SELECT [ThemeName], [UpdatedBy], [UpdatedUtc]
FROM [dbo].[PortalCfg_TabThemeOverrides]
WHERE [TabId] = @TabId
'@
        Add-IntParameter -Command $command -Name '@TabId' -Value $TabId
        $reader = $command.ExecuteReader()
        try {
            if (-not $reader.Read()) {
                return [pscustomobject]@{ Exists = $false }
            }

            return [pscustomobject]@{
                Exists = $true
                ThemeName = $reader.GetString(0)
                UpdatedBy = if ($reader.IsDBNull(1)) { $null } else { $reader.GetString(1) }
                UpdatedUtc = $reader.GetDateTime(2)
            }
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $command.Dispose()
    }
}

function Set-GlobalTheme {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$ThemeName
    )

    Invoke-NonQuery -Connection $Connection -Sql @'
IF EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = @SettingKey)
BEGIN
    UPDATE [dbo].[PortalCfg_SystemSettings]
    SET [SettingValue] = @SettingValue,
        [ValueType] = N'Enum',
        [SourceLevel] = N'Database',
        [CanDelete] = 1,
        [UpdatedBy] = @UpdatedBy,
        [UpdatedUtc] = SYSUTCDATETIME()
    WHERE [SettingKey] = @SettingKey
END
ELSE
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (@SettingKey, @SettingValue, N'Enum', N'Database', 1, @UpdatedBy, SYSUTCDATETIME())
END
'@ -Configure {
        param($command)
        Add-TextParameter -Command $command -Name '@SettingKey' -Size 200 -Value $settingKey
        Add-TextParameter -Command $command -Name '@SettingValue' -Size 128 -Value $ThemeName
        Add-TextParameter -Command $command -Name '@UpdatedBy' -Size 100 -Value $testActor
    }
}

function Set-TabTheme {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [int]$TabId,
        [string]$ThemeName
    )

    Invoke-NonQuery -Connection $Connection -Sql @'
IF EXISTS (SELECT 1 FROM [dbo].[PortalCfg_TabThemeOverrides] WHERE [TabId] = @TabId)
BEGIN
    UPDATE [dbo].[PortalCfg_TabThemeOverrides]
    SET [ThemeName] = @ThemeName, [UpdatedBy] = @UpdatedBy, [UpdatedUtc] = SYSUTCDATETIME()
    WHERE [TabId] = @TabId
END
ELSE
BEGIN
    INSERT INTO [dbo].[PortalCfg_TabThemeOverrides] ([TabId], [ThemeName], [UpdatedBy], [UpdatedUtc])
    VALUES (@TabId, @ThemeName, @UpdatedBy, SYSUTCDATETIME())
END
'@ -Configure {
        param($command)
        Add-IntParameter -Command $command -Name '@TabId' -Value $TabId
        Add-TextParameter -Command $command -Name '@ThemeName' -Size 64 -Value $ThemeName
        Add-TextParameter -Command $command -Name '@UpdatedBy' -Size 100 -Value $testActor
    }
}

function Clear-TabTheme {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [int]$TabId
    )

    Invoke-NonQuery -Connection $Connection -Sql 'DELETE FROM [dbo].[PortalCfg_TabThemeOverrides] WHERE [TabId] = @TabId' -Configure {
        param($command)
        Add-IntParameter -Command $command -Name '@TabId' -Value $TabId
    }
}

function Restore-ThemeSnapshots {
    param([System.Data.SqlClient.SqlConnection]$Connection)

    if ($null -ne $settingSnapshot) {
        if ($settingSnapshot.Exists) {
            Invoke-NonQuery -Connection $Connection -Sql @'
UPDATE [dbo].[PortalCfg_SystemSettings]
SET [SettingValue] = @SettingValue,
    [ValueType] = @ValueType,
    [SourceLevel] = @SourceLevel,
    [CanDelete] = @CanDelete,
    [UpdatedBy] = @UpdatedBy,
    [UpdatedUtc] = @UpdatedUtc
WHERE [SettingKey] = @SettingKey
'@ -Configure {
                param($command)
                Add-TextParameter -Command $command -Name '@SettingKey' -Size 200 -Value $settingKey
                Add-TextParameter -Command $command -Name '@SettingValue' -Size 4000 -Value $settingSnapshot.SettingValue
                Add-TextParameter -Command $command -Name '@ValueType' -Size 50 -Value $settingSnapshot.ValueType
                Add-TextParameter -Command $command -Name '@SourceLevel' -Size 50 -Value $settingSnapshot.SourceLevel
                Add-BitParameter -Command $command -Name '@CanDelete' -Value $settingSnapshot.CanDelete
                Add-TextParameter -Command $command -Name '@UpdatedBy' -Size 100 -Value $settingSnapshot.UpdatedBy
                Add-DateTime2Parameter -Command $command -Name '@UpdatedUtc' -Value $settingSnapshot.UpdatedUtc
            }
        }
        else {
            Invoke-NonQuery -Connection $Connection -Sql 'DELETE FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = @SettingKey' -Configure {
                param($command)
                Add-TextParameter -Command $command -Name '@SettingKey' -Size 200 -Value $settingKey
            }
        }
    }

    if ($null -ne $tabSnapshot) {
        if ($tabSnapshot.Exists) {
            Set-TabTheme -Connection $Connection -TabId $tabId -ThemeName $tabSnapshot.ThemeName
            Invoke-NonQuery -Connection $Connection -Sql @'
UPDATE [dbo].[PortalCfg_TabThemeOverrides]
SET [UpdatedBy] = @UpdatedBy, [UpdatedUtc] = @UpdatedUtc
WHERE [TabId] = @TabId
'@ -Configure {
                param($command)
                Add-IntParameter -Command $command -Name '@TabId' -Value $tabId
                Add-TextParameter -Command $command -Name '@UpdatedBy' -Size 100 -Value $tabSnapshot.UpdatedBy
                Add-DateTime2Parameter -Command $command -Name '@UpdatedUtc' -Value $tabSnapshot.UpdatedUtc
            }
        }
        else {
            Clear-TabTheme -Connection $Connection -TabId $tabId
        }
    }
}

function Invoke-PortalPage {
    param([string]$Uri)

    for ($attempt = 1; $attempt -le 20; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri $Uri -SkipHttpErrorCheck -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                return $response.Content
            }
        }
        catch {
            # IIS Express 首次编译前可能暂不可访问；短暂重试不会掩盖最终失败。
            # IIS Express may be unavailable during first compilation; a short retry does not mask final failure.
        }

        Start-Sleep -Seconds 1
    }

    throw 'The isolated theme-proof site did not return HTTP 200 before the timeout.'
}

function Assert-Contains {
    param(
        [string]$Html,
        [string]$Expected,
        [string]$Message
    )

    if ($Html.IndexOf($Expected, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw $Message
    }
}

try {
    $listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($listener) {
        throw "The isolated theme-proof port $Port is already in use."
    }

    $connection = [System.Data.SqlClient.SqlConnection]::new((Get-ExternalPortalConnectionString -Path $ConnectionStringsConfigPath))
    $connection.Open()
    $tabId = Invoke-ScalarInt -Connection $connection -Sql @'
SELECT TOP (1) [TabId]
FROM [dbo].[PortalCfg_Tabs]
WHERE [AccessRoles] LIKE N'%All Users%'
ORDER BY [TabId]
'@
    $settingSnapshot = Get-SystemSettingSnapshot -Connection $connection
    $tabSnapshot = Get-TabOverrideSnapshot -Connection $connection -TabId $tabId

    Clear-TabTheme -Connection $connection -TabId $tabId
    Set-GlobalTheme -Connection $connection -ThemeName 'ThemeProbe'
    $connection.Dispose()
    $connection = $null

    & (Join-Path $PSScriptRoot 'Start-IISExpress.ps1') -Port $Port
    $startedThemeSite = $true
    $probeUri = 'http://localhost:' + $Port + '/DesktopDefault.aspx?tabindex=0&tabid=' + $tabId

    $globalHtml = Invoke-PortalPage -Uri $probeUri
    Assert-Contains -Html $globalHtml -Expected 'portal-theme-themeprobe' -Message 'The database global ThemeProbe override was not applied to the public portal Tab.'
    Assert-Contains -Html $globalHtml -Expected 'App_Themes/ThemeProbe/Default.css' -Message 'The ThemeProbe CSS resource was not emitted for the global override.'
    Write-Host '[PASS] Database global ThemeProbe override applied.'

    $connection = [System.Data.SqlClient.SqlConnection]::new((Get-ExternalPortalConnectionString -Path $ConnectionStringsConfigPath))
    $connection.Open()
    Set-TabTheme -Connection $connection -TabId $tabId -ThemeName 'Default'
    $connection.Dispose()
    $connection = $null

    $tabHtml = Invoke-PortalPage -Uri $probeUri
    Assert-Contains -Html $tabHtml -Expected 'portal-theme-default' -Message 'The Tab Default override did not take precedence over the global ThemeProbe override.'
    Assert-Contains -Html $tabHtml -Expected 'App_Themes/Default/Default.css' -Message 'The Default CSS resource was not emitted for the Tab override.'
    Write-Host '[PASS] Tab theme override took precedence over the global setting.'

    $connection = [System.Data.SqlClient.SqlConnection]::new((Get-ExternalPortalConnectionString -Path $ConnectionStringsConfigPath))
    $connection.Open()
    Clear-TabTheme -Connection $connection -TabId $tabId
    Set-GlobalTheme -Connection $connection -ThemeName ('Invalid-P3-Theme-' + [Guid]::NewGuid().ToString('N'))
    $connection.Dispose()
    $connection = $null

    $fallbackHtml = Invoke-PortalPage -Uri $probeUri
    Assert-Contains -Html $fallbackHtml -Expected 'portal-theme-default' -Message 'An invalid global theme did not fall back to Default.'
    Assert-Contains -Html $fallbackHtml -Expected 'App_Themes/Default/Default.css' -Message 'The Default CSS resource was not emitted after invalid-theme fallback.'
    Write-Host '[PASS] Invalid global theme fell back to Default.'
}
finally {
    if ($connection) {
        $connection.Dispose()
    }

    if ($startedThemeSite) {
        & (Join-Path $PSScriptRoot 'Stop-IISExpress.ps1') -Port $Port
    }

    if ($null -ne $settingSnapshot -or $null -ne $tabSnapshot) {
        $restoreConnection = [System.Data.SqlClient.SqlConnection]::new((Get-ExternalPortalConnectionString -Path $ConnectionStringsConfigPath))
        try {
            $restoreConnection.Open()
            Restore-ThemeSnapshots -Connection $restoreConnection
            Write-Host '[PASS] Theme-setting and Tab-override data were restored.'
        }
        finally {
            $restoreConnection.Dispose()
        }
    }
}
