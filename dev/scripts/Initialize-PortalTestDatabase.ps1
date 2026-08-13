<#
.SYNOPSIS
.LANG en
Initializes an isolated Portal test database selected by an external connection string.

.LANG zh-CN
初始化外置连接串指定的隔离 Portal 测试数据库。

.LANG en
Initializes a development or test Portal database only when the target database
does not already exist and the caller explicitly confirms the operation. The
script reads the connection string from an external config file, executes the
known setup/migration scripts, and must not print passwords, server names, or
full connection-string values.

.LANG zh-CN
仅在目标数据库尚不存在且调用方明确确认时，初始化开发或测试用 Portal 数据库。
脚本从外置配置文件读取连接串，执行已知的初始化和迁移脚本，并且不得输出密码、
服务器名称或完整连接串。

.DESCRIPTION
<lang>
  <zh-CN>仅当目标数据库不存在且调用方显式确认时，按历史基础脚本和当前 P2/P3/P5 迁移建立测试库。真实连接串只从仓库外 XML 文件读取，脚本不会输出或保存密码、服务器地址或完整连接串。</zh-CN>
  <en>Only when the target database is absent and the caller explicitly confirms, runs the legacy base scripts and current P2/P3/P5 migrations. The real connection string is read only from an external XML file; passwords, server addresses, and full connection strings are never output or persisted.</en>
</lang>
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

# <lang>
#   <zh-CN>从外置 XML 中读取唯一命名连接串；只接受预期节点形态、单一非空条目，不回显连接串正文。</zh-CN>
#   <en>Reads the uniquely named connection string from external XML, accepting only the expected shapes and one non-empty entry without echoing its contents.</en>
# </lang>
function Get-ExternalConnectionString {
    param(
        [string]$Path,
        [string]$Name
    )

    [xml]$document = [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))

    # <lang>
    #   <zh-CN>应用正式契约是 connectionStrings 根节点，同时兼容早期 configuration 包装；仅改变节点定位，不改变连接串值。</zh-CN>
    #   <en>The production contract uses a connectionStrings root while accepting the legacy configuration wrapper; this changes only node discovery, not the connection-string value.</en>
    # </lang>
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

# <lang>
#   <zh-CN>把 Initial Catalog 转为 SQL Server 方括号标识符并转义右方括号，阻断配置值直接进入 DDL。</zh-CN>
#   <en>Converts Initial Catalog into a SQL Server bracketed identifier and escapes closing brackets before the value enters DDL.</en>
# </lang>
function Get-QuotedSqlIdentifier {
    param([string]$Identifier)

    if ([string]::IsNullOrWhiteSpace($Identifier)) {
        throw 'The connection string must define a non-empty Initial Catalog value.'
    }

    # <lang>
    #   <zh-CN>仅以 SQL Server 方括号形式转义数据库标识符，避免把配置值直接拼入 DDL。</zh-CN>
    #   <en>Escapes the database identifier only with SQL Server brackets so the configuration value is not inserted into DDL as raw text.</en>
    # </lang>
    return '[' + $Identifier.Replace(']', ']]') + ']'
}

# <lang>
#   <zh-CN>按裸 GO 分隔历史 SQL batch，拒绝 SQLCMD 指令和重复次数，保持迁移脚本执行语义可审计。</zh-CN>
#   <en>Splits legacy SQL into bare-GO batches while rejecting SQLCMD directives and repeat counts so migration execution remains auditable.</en>
# </lang>
function Get-SqlBatches {
    param([string]$SqlText)

    # <lang>
    #   <zh-CN>历史脚本只能包含裸 GO；拒绝 SQLCMD 指令与重复次数，避免悄然改变执行语义。</zh-CN>
    #   <en>Legacy scripts may contain only bare GO separators; reject SQLCMD directives and repeat counts to prevent silent semantic changes.</en>
    # </lang>
    if ($SqlText -match '(?im)^\s*:') {
        throw 'SQLCMD directives are not supported by this initialization script.'
    }

    if ($SqlText -match '(?im)^\s*GO\s+\d+') {
        throw 'SQL batch repeat counts are not supported by this initialization script.'
    }

    return [regex]::Split($SqlText, '(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?\n') |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
}

# <lang>
#   <zh-CN>在已验证 CREATE/USE 计数后替换历史 Portal 上下文，只改整行数据库上下文，不改表、过程或约束名称。</zh-CN>
#   <en>Rewrites legacy Portal context only after validating CREATE/USE counts, changing whole context lines without touching table, procedure, or constraint names.</en>
# </lang>
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

    # <lang>
    #   <zh-CN>仅替换已经计数验证的整行上下文；表、存储过程和约束名称保持原样。</zh-CN>
    #   <en>Replaces only context lines whose counts were verified; table, stored procedure, and constraint names remain unchanged.</en>
    # </lang>
    $sqlText = [regex]::Replace($SqlText, $createPattern, ('$1CREATE DATABASE ' + $QuotedDatabaseName))
    return [regex]::Replace($sqlText, $usePattern, ('$1USE ' + $QuotedDatabaseName))
}

# <lang>
#   <zh-CN>读取、改写并逐 batch 执行历史 SQL；每个命令独立设置超时并在 finally 释放。</zh-CN>
#   <en>Reads, rewrites, and executes legacy SQL batch by batch, applying the timeout to each command and disposing it in finally.</en>
# </lang>
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

    # <lang>
    #   <zh-CN>按受控 batch 顺序执行，不合并或重排迁移语句。</zh-CN>
    #   <en>Executes controlled batches in order without merging or reordering migration statements.</en>
    # </lang>
    foreach ($batch in Get-SqlBatches -SqlText $sqlText) {
        # <lang>
        #   <zh-CN>每个 batch 使用独立命令并在 finally 释放，避免迁移异常留下未释放连接命令。</zh-CN>
        #   <en>Uses a distinct command per batch and disposes it in finally so migration failures do not leave command resources unreleased.</en>
        # </lang>
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

# <lang>
#   <zh-CN>以参数化 DB_ID 查询确认目标库是否存在；调用方据此拒绝覆盖既有数据库。</zh-CN>
#   <en>Checks target existence with a parameterized DB_ID query so the caller can refuse replacement of an existing database.</en>
# </lang>
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

# <lang>
#   <zh-CN>读取 SQL Server ProductMajorVersion，作为迁移前最低版本门禁，不输出服务器连接细节。</zh-CN>
#   <en>Reads SQL Server ProductMajorVersion as a pre-migration minimum-version gate without exposing server connection details.</en>
# </lang>
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

# <lang>
#   <zh-CN>以下顶层状态仅保存非敏感初始化事实：连接对象、目标库名、版本、步骤和是否已开始创建。</zh-CN>
#   <en>The top-level state below stores only non-sensitive initialization facts: connections, target name, version, steps, and whether creation started.</en>
# </lang>
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
    # <lang>
    #   <zh-CN>先连接 master 做版本/存在性门禁，再由 ShouldProcess 决定是否真正创建和迁移。</zh-CN>
    #   <en>Connects to master for version/existence gates before ShouldProcess decides whether creation and migration may proceed.</en>
    # </lang>
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

    Write-Host ('[1/9] Creating isolated database {0}.' -f $targetDatabaseName)
    $initializationStarted = $true
    Invoke-SqlScript -Connection $masterConnection -Path (Join-Path $repoRoot 'src/Setup/Portal_CreateDB.sql') -QuotedDatabaseName $quotedTargetDatabaseName -ExpectedCreateDatabaseCount 1 -ExpectedUseDatabaseCount 1 -TimeoutSeconds $CommandTimeoutSeconds

    $targetConnection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
    $targetConnection.Open()

    # <lang>
    #   <zh-CN>步骤数组固定 2/3/5 迁移顺序、期望上下文计数和低敏描述；不从外部输入扩展迁移范围。</zh-CN>
    #   <en>The steps array fixes the P2/P3/P5 order, expected context counts, and low-sensitivity descriptions without expanding scope from external input.</en>
    # </lang>
    $steps = @(
        [pscustomobject]@{ Number = 2; Path = (Join-Path $repoRoot 'src/Setup/Portal_LoadConfig.sql'); CreateCount = 0; UseCount = 1; Description = 'Loading legacy configuration data' },
        [pscustomobject]@{ Number = 3; Path = (Join-Path $repoRoot 'src/Setup/Portal_LoadData.sql'); CreateCount = 0; UseCount = 1; Description = 'Loading legacy sample data' },
        [pscustomobject]@{ Number = 4; Path = (Join-Path $repoRoot 'src/Setup/PortalCfg_SystemSettings.sql'); CreateCount = 0; UseCount = 0; Description = 'Applying system-settings migration' },
        [pscustomobject]@{ Number = 5; Path = (Join-Path $repoRoot 'src/Setup/PortalCfg_UserRegistration.sql'); CreateCount = 0; UseCount = 0; Description = 'Applying registration migration' },
        [pscustomobject]@{ Number = 6; Path = (Join-Path $repoRoot 'src/Setup/PortalCfg_OperationAudits.sql'); CreateCount = 0; UseCount = 0; Description = 'Applying operation-audit migration' },
        [pscustomobject]@{ Number = 7; Path = (Join-Path $repoRoot 'src/Setup/PortalCfg_TabThemeOverrides.sql'); CreateCount = 0; UseCount = 0; Description = 'Applying tab-theme migration' },
        [pscustomobject]@{ Number = 8; Path = (Join-Path $repoRoot 'src/Setup/PortalCfg_ModulePackageStates.sql'); CreateCount = 0; UseCount = 0; Description = 'Applying module-package-state migration' },
        [pscustomobject]@{ Number = 9; Path = (Join-Path $repoRoot 'src/Setup/Portal_UserCredentials.sql'); CreateCount = 0; UseCount = 0; Description = 'Applying user-credential and security-version migration' }
    )

    foreach ($step in $steps) {
        # <lang>
        #   <zh-CN>按固定编号输出并调用受控 SQL helper；异常立即停止后续步骤。</zh-CN>
        #   <en>Reports the fixed step number and invokes the controlled SQL helper; an exception stops later steps immediately.</en>
        # </lang>
        Write-Host ('[{0}/9] {1}.' -f $step.Number, $step.Description)
        Invoke-SqlScript -Connection $targetConnection -Path $step.Path -QuotedDatabaseName $quotedTargetDatabaseName -ExpectedCreateDatabaseCount $step.CreateCount -ExpectedUseDatabaseCount $step.UseCount -TimeoutSeconds $CommandTimeoutSeconds
    }

    [pscustomobject]@{
        DatabaseName = $targetDatabaseName
        ServerMajorVersion = $serverMajorVersion
        CompletedSteps = 9
        Status = 'Initialized'
    }
}
catch {
    # <lang>
    #   <zh-CN>保留失败现场以便诊断；绝不在异常路径中猜测性删库，避免掩盖迁移失败原因。</zh-CN>
    #   <en>Preserves the failed state for diagnosis and never guessfully drops a database, keeping the migration failure cause visible.</en>
    # </lang>
    if ($initializationStarted) {
        Write-Warning ('Initialization did not complete for database {0}. The script did not remove the database automatically.' -f $targetDatabaseName)
    }

    throw
}
finally {
    # <lang>
    #   <zh-CN>无论成功、WhatIf 或异常都释放目标和 master 连接；不修改数据库保留策略。</zh-CN>
    #   <en>Disposes target and master connections after success, WhatIf, or failure without changing database-retention policy.</en>
    # </lang>
    if ($null -ne $targetConnection) {
        $targetConnection.Dispose()
    }

    $masterConnection.Dispose()
}
