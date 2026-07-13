[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$ConnectionStringsConfigPath,

    [string]$ConnectionStringName = 'Portal',

    [switch]$ApplyP2Migrations,

    [switch]$RequireP2Migrations,

    [switch]$ApplyP3Migrations,

    [switch]$RequireP3Migrations
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$checks = New-Object 'System.Collections.Generic.List[object]'

function Add-DatabaseCheck {
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

function Get-ExternalConnectionString {
    param(
        [string]$Path,
        [string]$Name
    )

    [xml]$document = [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))

    # 应用正式契约是 <connectionStrings> 根节点；同时兼容早期人工包装的 <configuration> 形态。
    # The production contract uses a <connectionStrings> root; also accept the legacy <configuration> wrapper.
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

    $matches = @($connectionStringsNode.add | Where-Object { $_.name -eq $Name })
    if ($matches.Count -ne 1 -or [string]::IsNullOrWhiteSpace($matches[0].connectionString)) {
        throw "The external connection-string file does not contain one non-empty '$Name' entry."
    }

    return $matches[0].connectionString
}

function Invoke-SqlScalar {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$CommandText
    )

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = $CommandText
        $command.CommandTimeout = 30
        return $command.ExecuteScalar()
    }
    finally {
        $command.Dispose()
    }
}

function Get-SqlServerInfo {
    param([System.Data.SqlClient.SqlConnection]$Connection)

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = @'
SELECT
    CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion')) AS ProductVersion,
    CONVERT(nvarchar(256), SERVERPROPERTY('Edition')) AS Edition,
    DB_NAME() AS DatabaseName,
    (SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME()) AS CompatibilityLevel;
'@
        $reader = $command.ExecuteReader()
        try {
            if (-not $reader.Read()) {
                throw 'SQL Server did not return version information.'
            }

            return [pscustomobject]@{
                ProductVersion = $reader.GetString(0)
                Edition = $reader.GetString(1)
                DatabaseName = $reader.GetString(2)
                CompatibilityLevel = [System.Convert]::ToInt32($reader.GetValue(3), [System.Globalization.CultureInfo]::InvariantCulture)
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

function Get-ExistingTableNames {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string[]]$TableNames
    )

    $command = $Connection.CreateCommand()
    try {
        $parameterNames = New-Object 'System.Collections.Generic.List[string]'
        for ($index = 0; $index -lt $TableNames.Count; $index++) {
            $parameterName = '@Table' + $index
            $parameterNames.Add($parameterName)
            [void]$command.Parameters.Add($parameterName, [System.Data.SqlDbType]::NVarChar, 128)
            $command.Parameters[$parameterName].Value = $TableNames[$index]
        }

        $command.CommandText = 'SELECT [name] FROM sys.tables WHERE [name] IN (' + ($parameterNames -join ', ') + ');'
        $reader = $command.ExecuteReader()
        try {
            $names = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
            while ($reader.Read()) {
                [void]$names.Add($reader.GetString(0))
            }

            return $names
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $command.Dispose()
    }
}

function Get-SqlBatches {
    param([string]$SqlText)

    # 扩展迁移使用独立 GO 批次；只接受裸 GO，避免把未知 sqlcmd 指令静默当作 SQL 执行。
    # Extension migrations use standalone GO batches; only bare GO is accepted to avoid treating unknown sqlcmd directives as SQL.
    if ($SqlText -match '(?im)^\s*GO\s+\d+') {
        throw 'SQL batch repeat counts are not supported by this compatibility script.'
    }

    return [regex]::Split($SqlText, '(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?\n') |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
}

function Invoke-MigrationFile {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Path
    )

    $sqlText = [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))
    foreach ($batch in Get-SqlBatches -SqlText $sqlText) {
        $command = $Connection.CreateCommand()
        try {
            $command.CommandText = $batch
            $command.CommandTimeout = 60
            [void]$command.ExecuteNonQuery()
        }
        finally {
            $command.Dispose()
        }
    }
}

$connectionString = Get-ExternalConnectionString -Path $ConnectionStringsConfigPath -Name $ConnectionStringName
$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString

try {
    $connection.Open()
    $server = Get-SqlServerInfo -Connection $connection
    $serverMajorText = ($server.ProductVersion -split '\.')[0]
    $serverMajor = 0
    [void][int]::TryParse($serverMajorText, [ref]$serverMajor)

    Add-DatabaseCheck -Name 'SQL Server engine baseline' -Status $(if ($serverMajor -ge 13) { 'Pass' } else { 'Fail' }) -Detail ('Major version ' + $serverMajor + '; SQL Server 2016 baseline is 13.')
    Add-DatabaseCheck -Name 'Database compatibility level' -Status $(if ($server.CompatibilityLevel -ge 130) { 'Pass' } else { 'Warning' }) -Detail ('Reported level ' + $server.CompatibilityLevel + '; recorded without automatic upgrade.')
    Add-DatabaseCheck -Name 'Target database' -Status 'Pass' -Detail ('Database ' + $server.DatabaseName + '; selected by the external connection string.')

    if ($ApplyP2Migrations) {
        if ($PSCmdlet.ShouldProcess('the selected external test database', 'Apply idempotent P2 migration scripts')) {
            $migrationFiles = @(
                (Join-Path $repoRoot 'src/Setup/PortalCfg_SystemSettings.sql'),
                (Join-Path $repoRoot 'src/Setup/PortalCfg_UserRegistration.sql'),
                (Join-Path $repoRoot 'src/Setup/PortalCfg_OperationAudits.sql')
            )

            foreach ($migrationFile in $migrationFiles) {
                Invoke-MigrationFile -Connection $connection -Path $migrationFile
            }

            Add-DatabaseCheck -Name 'P2 migration application' -Status 'Pass' -Detail 'All idempotent P2 migration batches completed.'
        }
        else {
            Add-DatabaseCheck -Name 'P2 migration application' -Status 'Info' -Detail 'Skipped by WhatIf or confirmation response.'
        }
    }

    if ($ApplyP3Migrations) {
        if ($PSCmdlet.ShouldProcess('the selected external test database', 'Apply idempotent P3 extension migration scripts')) {
            $migrationFiles = @(
                (Join-Path $repoRoot 'src/Setup/PortalCfg_TabThemeOverrides.sql'),
                (Join-Path $repoRoot 'src/Setup/PortalCfg_ModulePackageStates.sql')
            )
            foreach ($migrationFile in $migrationFiles) {
                Invoke-MigrationFile -Connection $connection -Path $migrationFile
            }
            Add-DatabaseCheck -Name 'P3 migration application' -Status 'Pass' -Detail 'The idempotent P3 theme and module-package migration batches completed.'
        }
        else {
            Add-DatabaseCheck -Name 'P3 migration application' -Status 'Info' -Detail 'Skipped by WhatIf or confirmation response.'
        }
    }

    $baseTables = @('Portal_Users', 'PortalCfg_Globals', 'PortalCfg_Tabs', 'PortalCfg_Modules')
    $p2Tables = @('PortalCfg_SystemSettings', 'PortalCfg_SystemSettingAudits', 'PortalCfg_RegistrationInvites', 'PortalCfg_UserRegistrations', 'PortalCfg_OperationAudits')
    $p3Tables = @('PortalCfg_TabThemeOverrides', 'PortalCfg_ModulePackageStates')
    $existingTables = Get-ExistingTableNames -Connection $connection -TableNames ($baseTables + $p2Tables + $p3Tables)

    $missingBaseTables = @($baseTables | Where-Object { -not $existingTables.Contains($_) })
    Add-DatabaseCheck -Name 'Base Portal schema' -Status $(if ($missingBaseTables.Count -eq 0) { 'Pass' } else { 'Fail' }) -Detail $(if ($missingBaseTables.Count -eq 0) { 'Required base tables are present.' } else { 'Missing: ' + ($missingBaseTables -join ', ') })

    $missingP2Tables = @($p2Tables | Where-Object { -not $existingTables.Contains($_) })
    if ($missingP2Tables.Count -eq 0) {
        Add-DatabaseCheck -Name 'P2 migration schema' -Status 'Pass' -Detail 'All P2 extension tables are present.'
    }
    elseif ($RequireP2Migrations) {
        Add-DatabaseCheck -Name 'P2 migration schema' -Status 'Fail' -Detail ('Missing: ' + ($missingP2Tables -join ', '))
    }
    else {
        Add-DatabaseCheck -Name 'P2 migration schema' -Status 'Warning' -Detail ('Not required for this run; missing: ' + ($missingP2Tables -join ', '))
    }

    $missingP3Tables = @($p3Tables | Where-Object { -not $existingTables.Contains($_) })
    if ($missingP3Tables.Count -eq 0) {
        Add-DatabaseCheck -Name 'P3 extension schema' -Status 'Pass' -Detail 'All P3 theme and module-package extension tables are present.'
    }
    elseif ($RequireP3Migrations) {
        Add-DatabaseCheck -Name 'P3 extension schema' -Status 'Fail' -Detail ('Missing: ' + ($missingP3Tables -join ', '))
    }
    else {
        Add-DatabaseCheck -Name 'P3 extension schema' -Status 'Warning' -Detail ('Not required for this run; missing: ' + ($missingP3Tables -join ', '))
    }

    $failedChecks = @($checks | Where-Object { $_.Status -eq 'Fail' })
    [pscustomobject]@{
        ProductVersion = $server.ProductVersion
        Edition = $server.Edition
        DatabaseName = $server.DatabaseName
        CompatibilityLevel = $server.CompatibilityLevel
        TotalChecks = $checks.Count
        FailedChecks = $failedChecks.Count
    }

    if ($failedChecks.Count -gt 0) {
        throw ('Portal SQL compatibility test failed: ' + (($failedChecks | ForEach-Object { $_.Name }) -join ', '))
    }
}
finally {
    $connection.Dispose()
}
