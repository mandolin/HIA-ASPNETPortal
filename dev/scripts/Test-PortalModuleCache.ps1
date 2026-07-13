[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$ConnectionStringsConfigPath,

    [ValidateRange(1025, 65535)]
    [int]$Port = 40004,

    [ValidateRange(10, 300)]
    [int]$CacheSeconds = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$packageId = 'HIA.ModuleProbe'
$testToken = 'P3CacheProbe-' + [Guid]::NewGuid().ToString('N')
$testActor = 'P3.5-cache-smoke'
$definitionId = 0
$moduleId = 0
$stateSnapshot = $null
$startedCacheSite = $false

function Get-ExternalPortalConnectionString {
    param([string]$Path)

    [xml]$document = [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))
    $connectionStringsNode = if ($document.DocumentElement -and
        $document.DocumentElement.Name -eq 'connectionStrings') {
        $document.DocumentElement
    }
    elseif ($document.configuration -and $document.configuration.connectionStrings) {
        $document.configuration.connectionStrings
    }
    else {
        throw 'The external connection-string file must contain a <connectionStrings> section.'
    }

    $entries = @($connectionStringsNode.add | Where-Object { $_.name -eq 'Portal' })
    if ($entries.Count -ne 1 -or [string]::IsNullOrWhiteSpace($entries[0].connectionString)) {
        throw "The external connection-string file must contain one non-empty 'Portal' entry."
    }

    if ($entries[0].providerName -and $entries[0].providerName -ne 'System.Data.SqlClient') {
        throw 'The module-cache proof currently supports only the Portal SQL Server provider.'
    }

    return $entries[0].connectionString
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

function Invoke-SqlScalar {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$CommandText,
        [scriptblock]$Configure
    )

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = $CommandText
        $command.CommandTimeout = 30
        if ($Configure) {
            & $Configure $command
        }

        return $command.ExecuteScalar()
    }
    finally {
        $command.Dispose()
    }
}

function Invoke-SqlNonQuery {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$CommandText,
        [scriptblock]$Configure
    )

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = $CommandText
        $command.CommandTimeout = 30
        if ($Configure) {
            & $Configure $command
        }

        [void]$command.ExecuteNonQuery()
    }
    finally {
        $command.Dispose()
    }
}

function Get-PackageStateSnapshot {
    param([System.Data.SqlClient.SqlConnection]$Connection)

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = @'
SELECT [IsEnabled], [Note], [UpdatedBy], [UpdatedUtc]
FROM [dbo].[PortalCfg_ModulePackageStates]
WHERE [PackageId] = @PackageId;
'@
        Add-TextParameter -Command $command -Name '@PackageId' -Size 100 -Value $packageId
        $reader = $command.ExecuteReader()
        try {
            if (-not $reader.Read()) {
                return [pscustomobject]@{ Exists = $false }
            }

            return [pscustomobject]@{
                Exists = $true
                IsEnabled = $reader.GetBoolean(0)
                Note = if ($reader.IsDBNull(1)) { $null } else { $reader.GetString(1) }
                UpdatedBy = $reader.GetString(2)
                UpdatedUtc = $reader.GetDateTime(3)
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

function Set-PackageState {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [bool]$IsEnabled,
        [string]$Note
    )

    Invoke-SqlNonQuery -Connection $Connection -CommandText @'
IF EXISTS (SELECT 1 FROM [dbo].[PortalCfg_ModulePackageStates] WHERE [PackageId] = @PackageId)
BEGIN
    UPDATE [dbo].[PortalCfg_ModulePackageStates]
    SET [IsEnabled] = @IsEnabled,
        [Note] = @Note,
        [UpdatedBy] = @UpdatedBy,
        [UpdatedUtc] = @UpdatedUtc
    WHERE [PackageId] = @PackageId;
END
ELSE
BEGIN
    INSERT INTO [dbo].[PortalCfg_ModulePackageStates]
        ([PackageId], [IsEnabled], [Note], [UpdatedBy], [UpdatedUtc])
    VALUES
        (@PackageId, @IsEnabled, @Note, @UpdatedBy, @UpdatedUtc);
END
'@ -Configure {
        param($command)
        Add-TextParameter -Command $command -Name '@PackageId' -Size 100 -Value $packageId
        $enabled = $command.Parameters.Add('@IsEnabled', [System.Data.SqlDbType]::Bit)
        $enabled.Value = $IsEnabled
        Add-TextParameter -Command $command -Name '@Note' -Size 500 -Value $Note
        Add-TextParameter -Command $command -Name '@UpdatedBy' -Size 100 -Value $testActor
        $updatedUtc = $command.Parameters.Add('@UpdatedUtc', [System.Data.SqlDbType]::DateTime2)
        $updatedUtc.Value = [DateTime]::UtcNow
    }
}

function Restore-PackageState {
    param([System.Data.SqlClient.SqlConnection]$Connection)

    if ($null -eq $stateSnapshot -or -not $stateSnapshot.Exists) {
        Invoke-SqlNonQuery -Connection $Connection -CommandText @'
DELETE FROM [dbo].[PortalCfg_ModulePackageStates]
WHERE [PackageId] = @PackageId;
'@ -Configure {
            param($command)
            Add-TextParameter -Command $command -Name '@PackageId' -Size 100 -Value $packageId
        }
        return
    }

    Invoke-SqlNonQuery -Connection $Connection -CommandText @'
UPDATE [dbo].[PortalCfg_ModulePackageStates]
SET [IsEnabled] = @IsEnabled,
    [Note] = @Note,
    [UpdatedBy] = @UpdatedBy,
    [UpdatedUtc] = @UpdatedUtc
WHERE [PackageId] = @PackageId;
'@ -Configure {
        param($command)
        Add-TextParameter -Command $command -Name '@PackageId' -Size 100 -Value $packageId
        $enabled = $command.Parameters.Add('@IsEnabled', [System.Data.SqlDbType]::Bit)
        $enabled.Value = $stateSnapshot.IsEnabled
        Add-TextParameter -Command $command -Name '@Note' -Size 500 -Value $stateSnapshot.Note
        Add-TextParameter -Command $command -Name '@UpdatedBy' -Size 100 -Value $stateSnapshot.UpdatedBy
        $updatedUtc = $command.Parameters.Add('@UpdatedUtc', [System.Data.SqlDbType]::DateTime2)
        $updatedUtc.Value = $stateSnapshot.UpdatedUtc
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
            # 独立 IIS Express 首次编译期间允许短暂重试；不输出连接串或物理路径。
        }

        Start-Sleep -Seconds 1
    }

    throw 'The isolated cache-proof site did not return HTTP 200 before the timeout.'
}

function Get-RenderedUtcMarker {
    param([string]$Html)

    $match = [regex]::Match(
        $Html,
        '<td[^>]*>\s*Rendered UTC:\s*</td>\s*<td[^>]*>\s*(?:<span[^>]*>)?(?<value>[^<]+)',
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $match.Success) {
        throw 'The temporary ModuleProbe instance did not expose its UTC render marker.'
    }

    return [System.Net.WebUtility]::HtmlDecode($match.Groups['value'].Value).Trim()
}

function Assert-Contains {
    param(
        [string]$Html,
        [string]$Expected,
        [string]$Message
    )

    if ($Html.IndexOf($Expected, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw $Message
    }
}

function Assert-DoesNotContain {
    param(
        [string]$Html,
        [string]$Unexpected,
        [string]$Message
    )

    if ($Html.IndexOf($Unexpected, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw $Message
    }
}

$connection = $null
try {
    $connection = [System.Data.SqlClient.SqlConnection]::new((Get-ExternalPortalConnectionString -Path $ConnectionStringsConfigPath))
    $connection.Open()

    $requiredTables = @('PortalCfg_ModuleDefinitions', 'PortalCfg_Modules', 'PortalCfg_ModulePackageStates', 'PortalCfg_Tabs')
    $existingTableCount = [int](Invoke-SqlScalar -Connection $connection -CommandText @'
SELECT COUNT(*)
FROM sys.tables
WHERE [name] IN (N'PortalCfg_ModuleDefinitions', N'PortalCfg_Modules', N'PortalCfg_ModulePackageStates', N'PortalCfg_Tabs');
'@)
    if ($existingTableCount -ne $requiredTables.Count) {
        throw 'The cache proof requires the P3 module-package schema in the selected development or test database.'
    }

    $tabId = [int](Invoke-SqlScalar -Connection $connection -CommandText @'
SELECT TOP (1) [TabId]
FROM [dbo].[PortalCfg_Tabs]
ORDER BY [TabOrder], [TabId];
'@)
    if ($tabId -le 0) {
        throw 'The selected database does not contain a usable portal tab for the cache proof.'
    }

    $stateSnapshot = Get-PackageStateSnapshot -Connection $connection
    $definitionId = [int](Invoke-SqlScalar -Connection $connection -CommandText @'
INSERT INTO [dbo].[PortalCfg_ModuleDefinitions]
    ([FriendlyName], [DesktopSourceFile], [MobileSourceFile])
OUTPUT INSERTED.[ModuleDefId]
VALUES
    (@FriendlyName, @DesktopSourceFile, NULL);
'@ -Configure {
        param($command)
        Add-TextParameter -Command $command -Name '@FriendlyName' -Size 128 -Value $testToken
        Add-TextParameter -Command $command -Name '@DesktopSourceFile' -Size 128 -Value 'DesktopModules/ModuleProbe/ModuleProbe.ascx'
    })

    $moduleId = [int](Invoke-SqlScalar -Connection $connection -CommandText @'
DECLARE @ModuleOrder INT = ISNULL(
    (SELECT MAX([ModuleOrder]) FROM [dbo].[PortalCfg_Modules] WHERE [TabId] = @TabId AND [PaneName] = @PaneName),
    0) + 1;

INSERT INTO [dbo].[PortalCfg_Modules]
    ([ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId])
OUTPUT INSERTED.[ModuleId]
VALUES
    (@ModuleTitle, @ModuleOrder, N'Admins;', @PaneName, 0, @CacheTimeout, @ModuleDefId, @TabId);
'@ -Configure {
        param($command)
        Add-IntParameter -Command $command -Name '@TabId' -Value $tabId
        Add-TextParameter -Command $command -Name '@PaneName' -Size 50 -Value 'ContentPane'
        Add-TextParameter -Command $command -Name '@ModuleTitle' -Size 100 -Value $testToken
        Add-IntParameter -Command $command -Name '@CacheTimeout' -Value $CacheSeconds
        Add-IntParameter -Command $command -Name '@ModuleDefId' -Value $definitionId
    })

    Set-PackageState -Connection $connection -IsEnabled $true -Note ($testToken + ': initial cache state')
    $connection.Dispose()
    $connection = $null

    $listening = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($listening) {
        throw "The isolated cache-proof port $Port is already in use."
    }

    & (Join-Path $PSScriptRoot 'Start-IISExpress.ps1') -Port $Port
    $startedCacheSite = $true
    $probeUri = 'http://localhost:' + $Port + '/DesktopDefault.aspx?tabindex=0&tabid=' + $tabId
    # ModuleProbe 不显示旧模块标题；使用控件自身输出的标识确认动态实例实际被装载。
    # ModuleProbe does not render the legacy module title, so use its own output marker to prove the dynamic instance was loaded.
    $moduleMarker = 'Id={0}; Source=DesktopModules/ModuleProbe/ModuleProbe.ascx' -f $moduleId

    $firstHtml = Invoke-PortalPage -Uri $probeUri
    Assert-Contains -Html $firstHtml -Expected $moduleMarker -Message 'The temporary ModuleProbe instance was not rendered on the first request.'
    Assert-Contains -Html $firstHtml -Expected 'DesktopModules/ModuleProbe/Styles/ModuleProbe.css' -Message 'The ModuleProbe CSS resource was not rendered while the package was enabled.'
    $firstMarker = Get-RenderedUtcMarker -Html $firstHtml

    Start-Sleep -Seconds 2
    $secondHtml = Invoke-PortalPage -Uri $probeUri
    $secondMarker = Get-RenderedUtcMarker -Html $secondHtml
    if ($secondMarker -ne $firstMarker) {
        throw 'The second request did not reuse the cached ModuleProbe output.'
    }
    Write-Host '[PASS] Module cache hit reused the first render marker.'

    $connection = [System.Data.SqlClient.SqlConnection]::new((Get-ExternalPortalConnectionString -Path $ConnectionStringsConfigPath))
    $connection.Open()
    Start-Sleep -Seconds 2
    Set-PackageState -Connection $connection -IsEnabled $true -Note ($testToken + ': cache identity revision')
    $connection.Dispose()
    $connection = $null

    $thirdHtml = Invoke-PortalPage -Uri $probeUri
    $thirdMarker = Get-RenderedUtcMarker -Html $thirdHtml
    if ($thirdMarker -eq $firstMarker) {
        throw 'The package-state revision did not invalidate the ModuleProbe cache identity.'
    }
    Write-Host '[PASS] Package-state revision invalidated the cached ModuleProbe output.'

    $connection = [System.Data.SqlClient.SqlConnection]::new((Get-ExternalPortalConnectionString -Path $ConnectionStringsConfigPath))
    $connection.Open()
    Start-Sleep -Seconds 2
    Set-PackageState -Connection $connection -IsEnabled $false -Note ($testToken + ': disabled state')
    $connection.Dispose()
    $connection = $null

    $disabledHtml = Invoke-PortalPage -Uri $probeUri
    Assert-DoesNotContain -Html $disabledHtml -Unexpected $moduleMarker -Message 'The disabled ModuleProbe package still rendered its temporary module instance.'
    Assert-DoesNotContain -Html $disabledHtml -Unexpected 'DesktopModules/ModuleProbe/Styles/ModuleProbe.css' -Message 'The disabled ModuleProbe package still rendered its CSS resource.'
    Write-Host '[PASS] Disabled package suppressed the temporary module and CSS resource.'

    $connection = [System.Data.SqlClient.SqlConnection]::new((Get-ExternalPortalConnectionString -Path $ConnectionStringsConfigPath))
    $connection.Open()
    Start-Sleep -Seconds 2
    Set-PackageState -Connection $connection -IsEnabled $true -Note ($testToken + ': re-enabled state')
    $connection.Dispose()
    $connection = $null

    $fourthHtml = Invoke-PortalPage -Uri $probeUri
    Assert-Contains -Html $fourthHtml -Expected $moduleMarker -Message 'The re-enabled ModuleProbe package did not render its temporary module instance.'
    $fourthMarker = Get-RenderedUtcMarker -Html $fourthHtml
    if ($fourthMarker -eq $thirdMarker) {
        throw 'Re-enabling the package did not create a fresh cache identity.'
    }
    Write-Host '[PASS] Re-enabled package rendered a fresh ModuleProbe cache entry.'
}
finally {
    if ($connection) {
        $connection.Dispose()
    }

    if ($startedCacheSite) {
        & (Join-Path $PSScriptRoot 'Stop-IISExpress.ps1') -Port $Port
    }

    $cleanupConnection = $null
    try {
        $cleanupConnection = [System.Data.SqlClient.SqlConnection]::new((Get-ExternalPortalConnectionString -Path $ConnectionStringsConfigPath))
        $cleanupConnection.Open()
        if ($moduleId -gt 0) {
            Invoke-SqlNonQuery -Connection $cleanupConnection -CommandText @'
DELETE FROM [dbo].[PortalCfg_Modules]
WHERE [ModuleId] = @ModuleId;
'@ -Configure {
                param($command)
                Add-IntParameter -Command $command -Name '@ModuleId' -Value $moduleId
            }
        }

        if ($definitionId -gt 0) {
            Invoke-SqlNonQuery -Connection $cleanupConnection -CommandText @'
DELETE FROM [dbo].[PortalCfg_ModuleDefinitions]
WHERE [ModuleDefId] = @ModuleDefId;
'@ -Configure {
                param($command)
                Add-IntParameter -Command $command -Name '@ModuleDefId' -Value $definitionId
            }
        }

        if ($null -ne $stateSnapshot) {
            Restore-PackageState -Connection $cleanupConnection
        }
        Write-Host '[PASS] Temporary module data and package state were restored.'
    }
    finally {
        if ($cleanupConnection) {
            $cleanupConnection.Dispose()
        }
    }
}
