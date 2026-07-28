<#
.SYNOPSIS
.LANG en
Checks or applies Portal SQL compatibility migrations.

.LANG zh-CN
检查或执行 Portal SQL 兼容性迁移。

.LANG en
Connects to the configured SQL Server database and verifies selected schema
milestones. Apply switches execute migration scripts and therefore change the
target database; require switches are read-only checks. Use only against a
prepared development, test, or explicitly approved target database. The script
does not print connection-string secrets and should be run with a template or
external config file that is excluded from Git when it contains credentials.

.LANG zh-CN
连接到配置的 SQL Server 数据库，并验证指定的 schema 里程碑。Apply 开关会执行
迁移脚本，因此会修改目标数据库；Require 开关仅做只读检查。请只在已准备好的
开发库、测试库或明确批准的目标库上运行。本脚本不应打印连接串敏感值；当配置
包含凭据时，应使用已排除入库的模板或外置配置文件。

.PARAMETER ConnectionStringsConfigPath
.LANG en
Connection strings config file used to locate the target database.

.LANG zh-CN
用于定位目标数据库的连接串配置文件。

.PARAMETER ConnectionStringName
.LANG en
Logical connection-string name, defaults to Portal.

.LANG zh-CN
逻辑连接串名称，默认为 Portal。

.PARAMETER ApplyP2Migrations
.LANG en
Applies P2 migration scripts and changes the target database.

.LANG zh-CN
执行 P2 迁移脚本，并会修改目标数据库。

.PARAMETER RequireP2Migrations
.LANG en
Checks P2 schema without applying changes.

.LANG zh-CN
仅检查 P2 schema，不执行变更。

.PARAMETER ApplyP21CollaborationItemMigration
.LANG en
Applies P21 collaboration-item migration scripts and changes the target database.

.LANG zh-CN
执行 P21 企业协同事项迁移脚本，并会修改目标数据库。

.PARAMETER RequireP21CollaborationItemMigration
.LANG en
Checks P21 collaboration-item schema without applying changes.

.LANG zh-CN
仅检查 P21 企业协同事项 schema，不执行变更。

.PARAMETER ApplyP23ReferenceDataMigration
.LANG en
Applies the P23 governed business reference-data migration and changes the target database.

.LANG zh-CN
执行 P23 受治理业务参考数据迁移，并会修改目标数据库。

.PARAMETER RequireP23ReferenceDataMigration
.LANG en
Checks P23 governed business reference-data schema without applying changes.

.LANG zh-CN
仅检查 P23 受治理业务参考数据 schema，不执行变更。
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$ConnectionStringsConfigPath,

    [string]$ConnectionStringName = 'Portal',

    [switch]$ApplyP2Migrations,

    [switch]$RequireP2Migrations,

    [switch]$ApplyP3Migrations,

    [switch]$RequireP3Migrations,

    [switch]$ApplyP5Migrations,

    [switch]$RequireP5Migrations,

    [switch]$ApplyP6UserProfileMigration,

    [switch]$RequireP6UserProfileMigration,

    [switch]$ApplyP6EmployeeOrganizationMigration,

    [switch]$RequireP6EmployeeOrganizationMigration,

    [switch]$ApplyP6BusinessModuleMigration,

    [switch]$RequireP6BusinessModuleMigration,

    [switch]$ApplyP12WorkItemMigration,

    [switch]$RequireP12WorkItemMigration,

    [switch]$ApplyP19BusinessApplicationMigration,

    [switch]$RequireP19BusinessApplicationMigration,

    [switch]$ApplyP21CollaborationItemMigration,

    [switch]$RequireP21CollaborationItemMigration,

    [switch]$ApplyP23ReferenceDataMigration,

    [switch]$RequireP23ReferenceDataMigration
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

    if ($ApplyP5Migrations) {
        if ($PSCmdlet.ShouldProcess('the selected external test database', 'Apply idempotent P5 security migration scripts')) {
            $migrationFiles = @(
                (Join-Path $repoRoot 'src/Setup/Portal_UserCredentials.sql'),
                (Join-Path $repoRoot 'src/Setup/PortalCfg_RolePermissions.sql')
            )
            foreach ($migrationFile in $migrationFiles) {
                Invoke-MigrationFile -Connection $connection -Path $migrationFile
            }
            Add-DatabaseCheck -Name 'P5 migration application' -Status 'Pass' -Detail 'The idempotent P5 credential, security-version, and role-permission migration batches completed.'
        }
        else {
            Add-DatabaseCheck -Name 'P5 migration application' -Status 'Info' -Detail 'Skipped by WhatIf or confirmation response.'
        }
    }

    if ($ApplyP6UserProfileMigration) {
        $duplicateNameGroups = [int](Invoke-SqlScalar -Connection $connection -CommandText @'
SELECT COUNT(*)
FROM
(
    SELECT [Name]
    FROM [dbo].[Portal_Users]
    GROUP BY [Name]
    HAVING COUNT(*) > 1
) AS [DuplicateNames];
'@)
        $invalidNameRows = [int](Invoke-SqlScalar -Connection $connection -CommandText @'
SELECT COUNT(*)
FROM [dbo].[Portal_Users]
WHERE NULLIF(LTRIM(RTRIM([Name])), N'') IS NULL
    OR [Name] <> LTRIM(RTRIM([Name]));
'@)
        $duplicateEmailGroups = [int](Invoke-SqlScalar -Connection $connection -CommandText @'
SELECT COUNT(*)
FROM
(
    SELECT NULLIF(LTRIM(RTRIM([Email])), N'') AS [PreferredEmail]
    FROM [dbo].[Portal_Users]
    WHERE NULLIF(LTRIM(RTRIM([Email])), N'') IS NOT NULL
    GROUP BY NULLIF(LTRIM(RTRIM([Email])), N'')
    HAVING COUNT(*) > 1
) AS [DuplicateEmails];
'@)

        Add-DatabaseCheck -Name 'P6 user-profile legacy name preflight' -Status $(if ($duplicateNameGroups -eq 0 -and $invalidNameRows -eq 0) { 'Pass' } else { 'Fail' }) -Detail ('Duplicate name groups: ' + $duplicateNameGroups + '; invalid name rows: ' + $invalidNameRows + '.')
        Add-DatabaseCheck -Name 'P6 user-profile legacy email preflight' -Status $(if ($duplicateEmailGroups -eq 0) { 'Pass' } else { 'Fail' }) -Detail ('Duplicate normalized non-empty email groups: ' + $duplicateEmailGroups + '.')

        if ($duplicateNameGroups -eq 0 -and $invalidNameRows -eq 0 -and $duplicateEmailGroups -eq 0) {
            if ($PSCmdlet.ShouldProcess('the selected external test database', 'Apply idempotent P6 user-profile migration script')) {
                Invoke-MigrationFile -Connection $connection -Path (Join-Path $repoRoot 'src/Setup/PortalBiz_UserProfiles.sql')
                Add-DatabaseCheck -Name 'P6 user-profile migration application' -Status 'Pass' -Detail 'The idempotent P6 user-profile migration batches completed.'
            }
            else {
                Add-DatabaseCheck -Name 'P6 user-profile migration application' -Status 'Info' -Detail 'Skipped by WhatIf or confirmation response.'
            }
        }
        else {
            Add-DatabaseCheck -Name 'P6 user-profile migration application' -Status 'Fail' -Detail 'Skipped because legacy user preflight failed.'
        }
    }

    if ($ApplyP6EmployeeOrganizationMigration) {
        if ($PSCmdlet.ShouldProcess('the selected external test database', 'Apply idempotent P6.3 employee and organization migration scripts')) {
            $migrationFiles = @(
                (Join-Path $repoRoot 'src/Setup/PortalBiz_OrganizationUnits.sql'),
                (Join-Path $repoRoot 'src/Setup/PortalBiz_Employees.sql'),
                (Join-Path $repoRoot 'src/Setup/PortalBiz_UserEmployeeBindings.sql')
            )

            foreach ($migrationFile in $migrationFiles) {
                Invoke-MigrationFile -Connection $connection -Path $migrationFile
            }

            Add-DatabaseCheck -Name 'P6.3 employee organization migration application' -Status 'Pass' -Detail 'The idempotent P6.3 organization, employee, and binding migration batches completed.'
        }
        else {
            Add-DatabaseCheck -Name 'P6.3 employee organization migration application' -Status 'Info' -Detail 'Skipped by WhatIf or confirmation response.'
        }
    }

    if ($ApplyP6BusinessModuleMigration) {
        if ($PSCmdlet.ShouldProcess('the selected external test database', 'Apply idempotent P6.4 business-module migration scripts')) {
            $migrationFiles = @(
                (Join-Path $repoRoot 'src/Setup/PortalBiz_EmployeeProfileConfirmations.sql'),
                (Join-Path $repoRoot 'src/Setup/PortalBiz_EmployeeProfileCorrectionRequests.sql')
            )

            foreach ($migrationFile in $migrationFiles) {
                Invoke-MigrationFile -Connection $connection -Path $migrationFile
            }

            Add-DatabaseCheck -Name 'P6.4 business module migration application' -Status 'Pass' -Detail 'The idempotent P6.4 business-module migration batches completed.'
        }
        else {
            Add-DatabaseCheck -Name 'P6.4 business module migration application' -Status 'Info' -Detail 'Skipped by WhatIf or confirmation response.'
        }
    }

    if ($ApplyP12WorkItemMigration) {
        if ($PSCmdlet.ShouldProcess('the selected external test database', 'Apply idempotent P12.3 work-item migration scripts')) {
            $migrationFiles = @(
                (Join-Path $repoRoot 'src/Setup/PortalBiz_WorkItems.sql'),
                (Join-Path $repoRoot 'src/Setup/PortalBiz_WorkItemEvents.sql')
            )

            foreach ($migrationFile in $migrationFiles) {
                Invoke-MigrationFile -Connection $connection -Path $migrationFile
            }

            Add-DatabaseCheck -Name 'P12.3 work-item migration application' -Status 'Pass' -Detail 'The idempotent P12.3 work-item migration batches completed.'
        }
        else {
            Add-DatabaseCheck -Name 'P12.3 work-item migration application' -Status 'Info' -Detail 'Skipped by WhatIf or confirmation response.'
        }
    }

    if ($ApplyP19BusinessApplicationMigration) {
        if ($PSCmdlet.ShouldProcess('the selected external test database', 'Apply idempotent P19.4 business-application migration scripts')) {
            $migrationFiles = @(
                (Join-Path $repoRoot 'src/Setup/PortalBiz_BusinessApplications.sql'),
                (Join-Path $repoRoot 'src/Setup/PortalBiz_WorkflowEvents.sql')
            )

            foreach ($migrationFile in $migrationFiles) {
                Invoke-MigrationFile -Connection $connection -Path $migrationFile
            }

            Add-DatabaseCheck -Name 'P19.4 business application migration application' -Status 'Pass' -Detail 'The idempotent P19.4 business-application migration batches completed.'
        }
        else {
            Add-DatabaseCheck -Name 'P19.4 business application migration application' -Status 'Info' -Detail 'Skipped by WhatIf or confirmation response.'
        }
    }

    if ($ApplyP21CollaborationItemMigration) {
        if ($PSCmdlet.ShouldProcess('the selected external test database', 'Apply idempotent P21.3 collaboration-item migration scripts')) {
            $migrationFiles = @(
                (Join-Path $repoRoot 'src/Setup/PortalBiz_CollaborationItems.sql'),
                (Join-Path $repoRoot 'src/Setup/PortalBiz_CollaborationItemEvents.sql')
            )

            foreach ($migrationFile in $migrationFiles) {
                Invoke-MigrationFile -Connection $connection -Path $migrationFile
            }

            Add-DatabaseCheck -Name 'P21.3 collaboration item migration application' -Status 'Pass' -Detail 'The idempotent P21.3 collaboration-item migration batches completed.'
        }
        else {
            Add-DatabaseCheck -Name 'P21.3 collaboration item migration application' -Status 'Info' -Detail 'Skipped by WhatIf or confirmation response.'
        }
    }

    if ($ApplyP23ReferenceDataMigration) {
        if ($PSCmdlet.ShouldProcess('the selected external test database', 'Apply idempotent P23.2 business reference-data migration script')) {
            Invoke-MigrationFile -Connection $connection -Path (Join-Path $repoRoot 'src/Setup/PortalBiz_ReferenceData.sql')
            Add-DatabaseCheck -Name 'P23.2 reference-data migration application' -Status 'Pass' -Detail 'The idempotent P23.2 governed business reference-data migration batches completed.'
        }
        else {
            Add-DatabaseCheck -Name 'P23.2 reference-data migration application' -Status 'Info' -Detail 'Skipped by WhatIf or confirmation response.'
        }
    }

    $baseTables = @('Portal_Users', 'PortalCfg_Globals', 'PortalCfg_Tabs', 'PortalCfg_Modules')
    $p2Tables = @('PortalCfg_SystemSettings', 'PortalCfg_SystemSettingAudits', 'PortalCfg_RegistrationInvites', 'PortalCfg_UserRegistrations', 'PortalCfg_OperationAudits')
    $p3Tables = @('PortalCfg_TabThemeOverrides', 'PortalCfg_ModulePackageStates')
    $p5Tables = @('Portal_UserCredentials', 'Portal_UserSecurityStates', 'PortalCfg_RolePermissions')
    $p6UserProfileTables = @('PortalBiz_UserProfiles')
    $p6EmployeeOrganizationTables = @('PortalBiz_OrganizationUnits', 'PortalBiz_Employees', 'PortalBiz_UserEmployeeBindings')
    $p6BusinessModuleTables = @('PortalBiz_EmployeeProfileConfirmations', 'PortalBiz_EmployeeProfileCorrectionRequests')
    $p12WorkItemTables = @('PortalBiz_WorkItems', 'PortalBiz_WorkItemEvents')
    $p19BusinessApplicationTables = @('PortalBiz_BusinessApplications', 'PortalBiz_WorkflowEvents')
    $p21CollaborationItemTables = @('PortalBiz_CollaborationItems', 'PortalBiz_CollaborationItemEvents')
    $p23ReferenceDataTables = @('PortalBiz_ReferenceData')
    $existingTables = Get-ExistingTableNames -Connection $connection -TableNames ($baseTables + $p2Tables + $p3Tables + $p5Tables + $p6UserProfileTables + $p6EmployeeOrganizationTables + $p6BusinessModuleTables + $p12WorkItemTables + $p19BusinessApplicationTables + $p21CollaborationItemTables + $p23ReferenceDataTables)

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

    $missingP5Tables = @($p5Tables | Where-Object { -not $existingTables.Contains($_) })
    if ($missingP5Tables.Count -eq 0) {
        Add-DatabaseCheck -Name 'P5 security schema' -Status 'Pass' -Detail 'All P5 credential, security-version, and role-permission tables are present.'
    }
    elseif ($RequireP5Migrations) {
        Add-DatabaseCheck -Name 'P5 security schema' -Status 'Fail' -Detail ('Missing: ' + ($missingP5Tables -join ', '))
    }
    else {
        Add-DatabaseCheck -Name 'P5 security schema' -Status 'Warning' -Detail ('Not required for this run; missing: ' + ($missingP5Tables -join ', '))
    }

    $missingP6UserProfileTables = @($p6UserProfileTables | Where-Object { -not $existingTables.Contains($_) })
    if ($missingP6UserProfileTables.Count -eq 0) {
        Add-DatabaseCheck -Name 'P6 user-profile schema' -Status 'Pass' -Detail 'The P6 user-profile extension table is present.'

        $userCount = [int](Invoke-SqlScalar -Connection $connection -CommandText 'SELECT COUNT(*) FROM [dbo].[Portal_Users];')
        $profileCount = [int](Invoke-SqlScalar -Connection $connection -CommandText 'SELECT COUNT(*) FROM [dbo].[PortalBiz_UserProfiles];')
        $missingProfileCount = [int](Invoke-SqlScalar -Connection $connection -CommandText @'
SELECT COUNT(*)
FROM [dbo].[Portal_Users] AS [Users]
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[PortalBiz_UserProfiles] AS [Profiles]
    WHERE [Profiles].[UserId] = [Users].[UserID]
);
'@)
        $orphanProfileCount = [int](Invoke-SqlScalar -Connection $connection -CommandText @'
SELECT COUNT(*)
FROM [dbo].[PortalBiz_UserProfiles] AS [Profiles]
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[Portal_Users] AS [Users]
    WHERE [Users].[UserID] = [Profiles].[UserId]
);
'@)

        $profileCoverageOk = $profileCount -eq $userCount -and $missingProfileCount -eq 0 -and $orphanProfileCount -eq 0
        Add-DatabaseCheck -Name 'P6 user-profile seed coverage' -Status $(if ($profileCoverageOk) { 'Pass' } elseif ($RequireP6UserProfileMigration) { 'Fail' } else { 'Warning' }) -Detail ('Users: ' + $userCount + '; profiles: ' + $profileCount + '; missing profiles: ' + $missingProfileCount + '; orphan profiles: ' + $orphanProfileCount + '.')
    }
    elseif ($RequireP6UserProfileMigration) {
        Add-DatabaseCheck -Name 'P6 user-profile schema' -Status 'Fail' -Detail ('Missing: ' + ($missingP6UserProfileTables -join ', '))
    }
    else {
        Add-DatabaseCheck -Name 'P6 user-profile schema' -Status 'Warning' -Detail ('Not required for this run; missing: ' + ($missingP6UserProfileTables -join ', '))
    }

    $missingP6EmployeeOrganizationTables = @($p6EmployeeOrganizationTables | Where-Object { -not $existingTables.Contains($_) })
    if ($missingP6EmployeeOrganizationTables.Count -eq 0) {
        Add-DatabaseCheck -Name 'P6.3 employee organization schema' -Status 'Pass' -Detail 'The P6.3 organization, employee, and binding tables are present.'

        $organizationCount = [int](Invoke-SqlScalar -Connection $connection -CommandText 'SELECT COUNT(*) FROM [dbo].[PortalBiz_OrganizationUnits];')
        $employeeCount = [int](Invoke-SqlScalar -Connection $connection -CommandText 'SELECT COUNT(*) FROM [dbo].[PortalBiz_Employees];')
        $bindingCount = [int](Invoke-SqlScalar -Connection $connection -CommandText 'SELECT COUNT(*) FROM [dbo].[PortalBiz_UserEmployeeBindings];')
        Add-DatabaseCheck -Name 'P6.3 employee organization row counts' -Status 'Info' -Detail ('Organizations: ' + $organizationCount + '; employees: ' + $employeeCount + '; bindings: ' + $bindingCount + '.')

        $duplicateActiveUserBindings = [int](Invoke-SqlScalar -Connection $connection -CommandText @'
SELECT COUNT(*)
FROM
(
    SELECT [UserId]
    FROM [dbo].[PortalBiz_UserEmployeeBindings]
    WHERE [BindingStatus] = N'Active'
    GROUP BY [UserId]
    HAVING COUNT(*) > 1
) AS [DuplicateActiveUsers];
'@)
        $duplicateActiveEmployeeBindings = [int](Invoke-SqlScalar -Connection $connection -CommandText @'
SELECT COUNT(*)
FROM
(
    SELECT [EmployeeId]
    FROM [dbo].[PortalBiz_UserEmployeeBindings]
    WHERE [BindingStatus] = N'Active'
    GROUP BY [EmployeeId]
    HAVING COUNT(*) > 1
) AS [DuplicateActiveEmployees];
'@)
        $activeBindingUniquenessOk = $duplicateActiveUserBindings -eq 0 -and $duplicateActiveEmployeeBindings -eq 0
        Add-DatabaseCheck -Name 'P6.3 active binding uniqueness' -Status $(if ($activeBindingUniquenessOk) { 'Pass' } elseif ($RequireP6EmployeeOrganizationMigration) { 'Fail' } else { 'Warning' }) -Detail ('Duplicate active user bindings: ' + $duplicateActiveUserBindings + '; duplicate active employee bindings: ' + $duplicateActiveEmployeeBindings + '.')
    }
    elseif ($RequireP6EmployeeOrganizationMigration) {
        Add-DatabaseCheck -Name 'P6.3 employee organization schema' -Status 'Fail' -Detail ('Missing: ' + ($missingP6EmployeeOrganizationTables -join ', '))
    }
    else {
        Add-DatabaseCheck -Name 'P6.3 employee organization schema' -Status 'Warning' -Detail ('Not required for this run; missing: ' + ($missingP6EmployeeOrganizationTables -join ', '))
    }

    $missingP6BusinessModuleTables = @($p6BusinessModuleTables | Where-Object { -not $existingTables.Contains($_) })
    if ($missingP6BusinessModuleTables.Count -eq 0) {
        Add-DatabaseCheck -Name 'P6.4 business module schema' -Status 'Pass' -Detail 'The P6.4 employee-profile confirmation and correction-request tables are present.'

        $confirmationCount = [int](Invoke-SqlScalar -Connection $connection -CommandText 'SELECT COUNT(*) FROM [dbo].[PortalBiz_EmployeeProfileConfirmations];')
        Add-DatabaseCheck -Name 'P6.4 employee profile confirmation row count' -Status 'Info' -Detail ('Confirmations: ' + $confirmationCount + '.')

        $correctionRequestCount = [int](Invoke-SqlScalar -Connection $connection -CommandText 'SELECT COUNT(*) FROM [dbo].[PortalBiz_EmployeeProfileCorrectionRequests];')
        Add-DatabaseCheck -Name 'P6.4 employee profile correction request row count' -Status 'Info' -Detail ('Correction requests: ' + $correctionRequestCount + '.')
    }
    elseif ($RequireP6BusinessModuleMigration) {
        Add-DatabaseCheck -Name 'P6.4 business module schema' -Status 'Fail' -Detail ('Missing: ' + ($missingP6BusinessModuleTables -join ', '))
    }
    else {
        Add-DatabaseCheck -Name 'P6.4 business module schema' -Status 'Warning' -Detail ('Not required for this run; missing: ' + ($missingP6BusinessModuleTables -join ', '))
    }

    $missingP12WorkItemTables = @($p12WorkItemTables | Where-Object { -not $existingTables.Contains($_) })
    if ($missingP12WorkItemTables.Count -eq 0) {
        Add-DatabaseCheck -Name 'P12.3 work-item schema' -Status 'Pass' -Detail 'The P12.3 work-item and work-item-event tables are present.'

        $workItemCount = [int](Invoke-SqlScalar -Connection $connection -CommandText 'SELECT COUNT(*) FROM [dbo].[PortalBiz_WorkItems];')
        $workItemEventCount = [int](Invoke-SqlScalar -Connection $connection -CommandText 'SELECT COUNT(*) FROM [dbo].[PortalBiz_WorkItemEvents];')
        Add-DatabaseCheck -Name 'P12.3 work-item row counts' -Status 'Info' -Detail ('Work items: ' + $workItemCount + '; events: ' + $workItemEventCount + '.')
    }
    elseif ($RequireP12WorkItemMigration) {
        Add-DatabaseCheck -Name 'P12.3 work-item schema' -Status 'Fail' -Detail ('Missing: ' + ($missingP12WorkItemTables -join ', '))
    }
    else {
        Add-DatabaseCheck -Name 'P12.3 work-item schema' -Status 'Warning' -Detail ('Not required for this run; missing: ' + ($missingP12WorkItemTables -join ', '))
    }

    $missingP19BusinessApplicationTables = @($p19BusinessApplicationTables | Where-Object { -not $existingTables.Contains($_) })
    if ($missingP19BusinessApplicationTables.Count -eq 0) {
        Add-DatabaseCheck -Name 'P19.4 business application schema' -Status 'Pass' -Detail 'The P19.4 business application and workflow-event tables are present.'

        $businessApplicationCount = [int](Invoke-SqlScalar -Connection $connection -CommandText 'SELECT COUNT(*) FROM [dbo].[PortalBiz_BusinessApplications];')
        $workflowEventCount = [int](Invoke-SqlScalar -Connection $connection -CommandText 'SELECT COUNT(*) FROM [dbo].[PortalBiz_WorkflowEvents];')
        Add-DatabaseCheck -Name 'P19.4 business application row counts' -Status 'Info' -Detail ('Applications: ' + $businessApplicationCount + '; workflow events: ' + $workflowEventCount + '.')
    }
    elseif ($RequireP19BusinessApplicationMigration) {
        Add-DatabaseCheck -Name 'P19.4 business application schema' -Status 'Fail' -Detail ('Missing: ' + ($missingP19BusinessApplicationTables -join ', '))
    }
    else {
        Add-DatabaseCheck -Name 'P19.4 business application schema' -Status 'Warning' -Detail ('Not required for this run; missing: ' + ($missingP19BusinessApplicationTables -join ', '))
    }

    $missingP21CollaborationItemTables = @($p21CollaborationItemTables | Where-Object { -not $existingTables.Contains($_) })
    if ($missingP21CollaborationItemTables.Count -eq 0) {
        Add-DatabaseCheck -Name 'P21.3 collaboration item schema' -Status 'Pass' -Detail 'The P21.3 collaboration item and collaboration-item-event tables are present.'

        $collaborationItemCount = [int](Invoke-SqlScalar -Connection $connection -CommandText 'SELECT COUNT(*) FROM [dbo].[PortalBiz_CollaborationItems];')
        $collaborationItemEventCount = [int](Invoke-SqlScalar -Connection $connection -CommandText 'SELECT COUNT(*) FROM [dbo].[PortalBiz_CollaborationItemEvents];')
        Add-DatabaseCheck -Name 'P21.3 collaboration item row counts' -Status 'Info' -Detail ('Collaboration items: ' + $collaborationItemCount + '; collaboration item events: ' + $collaborationItemEventCount + '.')
    }
    elseif ($RequireP21CollaborationItemMigration) {
        Add-DatabaseCheck -Name 'P21.3 collaboration item schema' -Status 'Fail' -Detail ('Missing: ' + ($missingP21CollaborationItemTables -join ', '))
    }
    else {
        Add-DatabaseCheck -Name 'P21.3 collaboration item schema' -Status 'Warning' -Detail ('Not required for this run; missing: ' + ($missingP21CollaborationItemTables -join ', '))
    }

    $missingP23ReferenceDataTables = @($p23ReferenceDataTables | Where-Object { -not $existingTables.Contains($_) })
    if ($missingP23ReferenceDataTables.Count -eq 0) {
        Add-DatabaseCheck -Name 'P23.2 reference-data schema' -Status 'Pass' -Detail 'The P23.2 governed business reference-data table is present.'

        $requiredReferenceDataCount = [int](Invoke-SqlScalar -Connection $connection -CommandText @'
SELECT COUNT(*)
FROM [dbo].[PortalBiz_ReferenceData]
WHERE ([ReferenceSetKey] = N'CollaborationItemType' AND [ValueKey] IN (N'General', N'Content', N'Operations', N'Workflow'))
   OR ([ReferenceSetKey] = N'CollaborationPriority' AND [ValueKey] IN (N'Normal', N'Important'));
'@)
        $referenceDataSeedOk = $requiredReferenceDataCount -eq 6
        Add-DatabaseCheck -Name 'P23.2 reference-data seed coverage' -Status $(if ($referenceDataSeedOk) { 'Pass' } elseif ($RequireP23ReferenceDataMigration) { 'Fail' } else { 'Warning' }) -Detail ('Required active-or-historical reference values found: ' + $requiredReferenceDataCount + ' of 6.')
    }
    elseif ($RequireP23ReferenceDataMigration) {
        Add-DatabaseCheck -Name 'P23.2 reference-data schema' -Status 'Fail' -Detail ('Missing: ' + ($missingP23ReferenceDataTables -join ', '))
    }
    else {
        Add-DatabaseCheck -Name 'P23.2 reference-data schema' -Status 'Warning' -Detail ('Not required for this run; missing: ' + ($missingP23ReferenceDataTables -join ', '))
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
