<#
.SYNOPSIS
初始化外置连接串指定的隔离 Portal 测试数据库。
Initializes an isolated Portal test database selected by an external connection string.

.DESCRIPTION
仅当目标数据库不存在且调用方显式确认时，按历史基础脚本和当前 P2 迁移建立测试库。
Only when the target database is absent and the caller explicitly confirms, runs the legacy base scripts and current P2 migrations.

真实连接串只从仓库外 XML 文件读取，脚本不会输出或保存密码、服务器地址或完整连接串。
The real connection string is read only from an external XML file; passwords, server addresses, and full connection strings are never output or persisted.
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$ConnectionStringsConfigPath,

    [string]$ConnectionStringName = 'Portal',

    [ValidateRange(30, 3600)]
    [int]$CommandTimeoutSeconds = 300
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

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

function Get-QuotedSqlIdentifier {
    param([string]$Identifier)

    if ([string]::IsNullOrWhiteSpace($Identifier)) {
        throw 'The connection string must define a non-empty Initial Catalog value.'
    }

    # 只以 SQL Server 方括号形式转义数据库标识符，避免把配置值直接拼入 DDL。
    # Escape the database identifier with SQL Server brackets before using it in DDL.
    return '[' + $Identifier.Replace(']', ']]') + ']'
}

function Get-SqlBatches {
    param([string]$SqlText)

    # 历史脚本只能包含裸 GO；拒绝 SQLCMD 指令与重复次数，避免悄然改变执行语义。
    # Legacy scripts may contain only bare GO separators; reject SQLCMD directives and repeat counts.
    if ($SqlText -match '(?im)^\s*:') {
        throw 'SQLCMD directives are not supported by this initialization script.'
    }

    if ($SqlText -match '(?im)^\s*GO\s+\d+') {
        throw 'SQL batch repeat counts are not supported by this initialization script.'
    }

    return [regex]::Split($SqlText, '(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?\n') |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
}

function Set-LegacyDatabaseContext {
    param(
        [string]$SqlText,
        [string]$QuotedDatabaseName,
        [int]$ExpectedCreateDatabaseCount,
        [int]$ExpectedUseDatabaseCount,
        [string]$ScriptName
    )

    $createPattern = '(?im)^(\s*)CREATE\s+DATABASE\s+\[Portal\]\s*;?\s*$'
    $usePattern = '(?im)^(\s*)USE\s+\[Portal\]\s*;?\s*$'
    $createCount = [regex]::Matches($SqlText, $createPattern).Count
    $useCount = [regex]::Matches($SqlText, $usePattern).Count

    if ($createCount -ne $ExpectedCreateDatabaseCount -or $useCount -ne $ExpectedUseDatabaseCount) {
        throw ("Unexpected legacy database context in {0}: expected CREATE={1}, USE={2}; found CREATE={3}, USE={4}." -f $ScriptName, $ExpectedCreateDatabaseCount, $ExpectedUseDatabaseCount, $createCount, $useCount)
    }

    # 仅替换已经计数验证的整行上下文；表、存储过程和约束名称保持原样。
    # Replace only verified context lines; table, stored procedure, and constraint names remain unchanged.
    $sqlText = [regex]::Replace($SqlText, $createPattern, ('$1CREATE DATABASE ' + $QuotedDatabaseName))
    return [regex]::Replace($sqlText, $usePattern, ('$1USE ' + $QuotedDatabaseName))
}

function Invoke-SqlScript {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Path,
        [string]$QuotedDatabaseName,
        [int]$ExpectedCreateDatabaseCount,
        [int]$ExpectedUseDatabaseCount,
        [int]$TimeoutSeconds
    )

    $sqlText = [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))
    $sqlText = Set-LegacyDatabaseContext -SqlText $sqlText -QuotedDatabaseName $QuotedDatabaseName -ExpectedCreateDatabaseCount $ExpectedCreateDatabaseCount -ExpectedUseDatabaseCount $ExpectedUseDatabaseCount -ScriptName ([System.IO.Path]::GetFileName($Path))

    foreach ($batch in Get-SqlBatches -SqlText $sqlText) {
        $command = $Connection.CreateCommand()
        try {
            $command.CommandText = $batch
            $command.CommandTimeout = $TimeoutSeconds
            [void]$command.ExecuteNonQuery()
        }
        finally {
            $command.Dispose()
        }
    }
}

function Test-TargetDatabaseExists {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$DatabaseName
    )

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = 'SELECT CASE WHEN DB_ID(@DatabaseName) IS NULL THEN 0 ELSE 1 END;'
        [void]$command.Parameters.Add('@DatabaseName', [System.Data.SqlDbType]::NVarChar, 128)
        $command.Parameters['@DatabaseName'].Value = $DatabaseName
        return ([System.Convert]::ToInt32($command.ExecuteScalar()) -eq 1)
    }
    finally {
        $command.Dispose()
    }
}

function Get-SqlServerMajorVersion {
    param([System.Data.SqlClient.SqlConnection]$Connection)

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = @'
SELECT ISNULL(CONVERT(int, SERVERPROPERTY('ProductMajorVersion')), 0);
'@
        return [System.Convert]::ToInt32($command.ExecuteScalar())
    }
    finally {
        $command.Dispose()
    }
}

$connectionString = Get-ExternalConnectionString -Path $ConnectionStringsConfigPath -Name $ConnectionStringName
$targetBuilder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
$targetDatabaseName = [string]$targetBuilder['Initial Catalog']
$quotedTargetDatabaseName = Get-QuotedSqlIdentifier -Identifier $targetDatabaseName

if ($targetDatabaseName -in @('master', 'model', 'msdb', 'tempdb')) {
    throw 'A system database cannot be used as the initialization target.'
}

$masterBuilder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
$masterBuilder['Initial Catalog'] = 'master'
$masterConnection = [System.Data.SqlClient.SqlConnection]::new($masterBuilder.ConnectionString)
$targetConnection = $null
$initializationStarted = $false

try {
    $masterConnection.Open()
    $serverMajorVersion = Get-SqlServerMajorVersion -Connection $masterConnection
    if ($serverMajorVersion -lt 13) {
        throw ("SQL Server 2016+ is required; detected major version {0}." -f $serverMajorVersion)
    }

    if (Test-TargetDatabaseExists -Connection $masterConnection -DatabaseName $targetDatabaseName) {
        throw ("The configured target database '{0}' already exists. This script never replaces an existing database." -f $targetDatabaseName)
    }

    $action = 'Create the database, load legacy base data, and apply P2/P3 migrations'
    if (-not $PSCmdlet.ShouldProcess(("database '{0}'" -f $targetDatabaseName), $action)) {
        Write-Host 'Initialization was skipped by WhatIf or confirmation response.'
        return
    }

    Write-Host ('[1/8] Creating isolated database {0}.' -f $targetDatabaseName)
    $initializationStarted = $true
    Invoke-SqlScript -Connection $masterConnection -Path (Join-Path $repoRoot 'src/Setup/Portal_CreateDB.sql') -QuotedDatabaseName $quotedTargetDatabaseName -ExpectedCreateDatabaseCount 1 -ExpectedUseDatabaseCount 1 -TimeoutSeconds $CommandTimeoutSeconds

    $targetConnection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
    $targetConnection.Open()

    $steps = @(
        [pscustomobject]@{ Number = 2; Path = (Join-Path $repoRoot 'src/Setup/Portal_LoadConfig.sql'); CreateCount = 0; UseCount = 1; Description = 'Loading legacy configuration data' },
        [pscustomobject]@{ Number = 3; Path = (Join-Path $repoRoot 'src/Setup/Portal_LoadData.sql'); CreateCount = 0; UseCount = 1; Description = 'Loading legacy sample data' },
        [pscustomobject]@{ Number = 4; Path = (Join-Path $repoRoot 'src/Setup/PortalCfg_SystemSettings.sql'); CreateCount = 0; UseCount = 0; Description = 'Applying system-settings migration' },
        [pscustomobject]@{ Number = 5; Path = (Join-Path $repoRoot 'src/Setup/PortalCfg_UserRegistration.sql'); CreateCount = 0; UseCount = 0; Description = 'Applying registration migration' },
        [pscustomobject]@{ Number = 6; Path = (Join-Path $repoRoot 'src/Setup/PortalCfg_OperationAudits.sql'); CreateCount = 0; UseCount = 0; Description = 'Applying operation-audit migration' },
        [pscustomobject]@{ Number = 7; Path = (Join-Path $repoRoot 'src/Setup/PortalCfg_TabThemeOverrides.sql'); CreateCount = 0; UseCount = 0; Description = 'Applying tab-theme migration' },
        [pscustomobject]@{ Number = 8; Path = (Join-Path $repoRoot 'src/Setup/PortalCfg_ModulePackageStates.sql'); CreateCount = 0; UseCount = 0; Description = 'Applying module-package-state migration' }
    )

    foreach ($step in $steps) {
        Write-Host ('[{0}/8] {1}.' -f $step.Number, $step.Description)
        Invoke-SqlScript -Connection $targetConnection -Path $step.Path -QuotedDatabaseName $quotedTargetDatabaseName -ExpectedCreateDatabaseCount $step.CreateCount -ExpectedUseDatabaseCount $step.UseCount -TimeoutSeconds $CommandTimeoutSeconds
    }

    [pscustomobject]@{
        DatabaseName = $targetDatabaseName
        ServerMajorVersion = $serverMajorVersion
        CompletedSteps = 8
        Status = 'Initialized'
    }
}
catch {
    # 保留失败现场以便诊断；绝不在异常路径中猜测性删库。
    # Preserve the failed state for diagnosis; never guessfully drop a database during error handling.
    if ($initializationStarted) {
        Write-Warning ('Initialization did not complete for database {0}. The script did not remove the database automatically.' -f $targetDatabaseName)
    }

    throw
}
finally {
    if ($null -ne $targetConnection) {
        $targetConnection.Dispose()
    }

    $masterConnection.Dispose()
}
