<#
.SYNOPSIS
.LANG en
Runs an isolated theme-resolution proof against a test database configuration.

.LANG zh-CN
针对测试数据库配置运行隔离主题解析 proof。

.DESCRIPTION
<lang>
  <en>Uses an external Portal connection string, mutates temporary system/tab theme state, starts an isolated IIS Express instance, verifies effective theme behavior, and restores previous state afterward. The script is intended for disposable development databases and must not be pointed at production data.</en>
  <zh-CN>使用外置 Portal 连接串临时变更系统/Tab 主题状态，启动隔离 IIS Express 实例，验证有效主题行为，并在结束后恢复原状态。本脚本面向可丢弃的开发数据库，不应指向生产数据。</zh-CN>
</lang>

.PARAMETER ConnectionStringsConfigPath
.LANG en
External connectionStrings.config file containing the Portal SQL Server connection string.

.LANG zh-CN
包含 Portal SQL Server 连接串的外置 connectionStrings.config 文件。

.PARAMETER Port
.LANG en
IIS Express port used by the isolated theme proof site.

.LANG zh-CN
隔离主题 proof 站点使用的 IIS Express 端口。
#>
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

# <lang>
#   <zh-CN>下面的状态只保存隔离 proof 的仓库根、固定设置键、测试 actor、连接和快照生命周期；不承载生产配置。</zh-CN>
#   <en>The state below holds only the isolated proof root, fixed setting key, test actor, connection, and snapshot lifetimes; it does not carry production configuration.</en>
# </lang>
$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$settingKey = 'Portal.Theme.Name'
$testActor = 'P3.5-theme-smoke'
$startedThemeSite = $false
$connection = $null
$settingSnapshot = $null
$tabSnapshot = $null
$tabId = 0

# <lang>
#   <zh-CN>只从外置 connectionStrings.config 读取 Portal 节点，拒绝缺失或空连接串，且不把配置正文写入输出。</zh-CN>
#   <en>Reads only the Portal node from the external connectionStrings.config, rejects a missing or empty connection string, and never writes configuration text to output.</en>
# </lang>
function Get-ExternalPortalConnectionString {
    param([string]$Path)

# <lang>
#   <zh-CN>使用无 BOM UTF-8 读取外置 XML，并兼容 connectionStrings 根节点或 configuration 包裹形式。</zh-CN>
#   <en>Reads the external XML as UTF-8 without a BOM and accepts either a connectionStrings root or a configuration wrapper.</en>
# </lang>
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

# <lang>
#   <zh-CN>只允许命名为 Portal 且具有非空连接串的节点通过；异常文本不回显连接串。</zh-CN>
#   <en>Only a non-empty node named Portal is accepted; failure text never echoes the connection string.</en>
# </lang>
    if ($null -eq $portalNode -or [string]::IsNullOrWhiteSpace($portalNode.connectionString)) {
        throw 'The external connectionStrings.config file does not contain a Portal connection string.'
    }

    return $portalNode.connectionString
}

# <lang>
#   <zh-CN>将可空文本安全映射为 NVarChar 参数，保持 NULL 语义并避免字符串拼接 SQL。</zh-CN>
#   <en>Maps nullable text to an NVarChar parameter, preserving NULL semantics and avoiding string-concatenated SQL.</en>
# </lang>
function Add-TextParameter {
    param(
        [System.Data.SqlClient.SqlCommand]$Command,
        [string]$Name,
        [int]$Size,
        [AllowNull()][string]$Value
    )

# <lang>
#   <zh-CN>参数大小由调用方显式提供，数据库驱动负责值编码与边界检查。</zh-CN>
#   <en>The caller supplies the explicit size while the provider handles value encoding and bounds.</en>
# </lang>
    $parameter = $Command.Parameters.Add($Name, [System.Data.SqlDbType]::NVarChar, $Size)
    $parameter.Value = if ($null -eq $Value) { [DBNull]::Value } else { $Value }
}

# <lang>
#   <zh-CN>为整型键创建参数，供 TabId 等数据库键保持类型化查询。</zh-CN>
#   <en>Creates a typed integer parameter so database keys such as TabId remain parameterized.</en>
# </lang>
function Add-IntParameter {
    param(
        [System.Data.SqlClient.SqlCommand]$Command,
        [string]$Name,
        [int]$Value
    )

    $parameter = $Command.Parameters.Add($Name, [System.Data.SqlDbType]::Int)
    $parameter.Value = $Value
}

# <lang>
#   <zh-CN>为布尔配置创建 Bit 参数，恢复 CanDelete 等状态时不依赖文本转换。</zh-CN>
#   <en>Creates a Bit parameter so flags such as CanDelete are restored without text conversion.</en>
# </lang>
function Add-BitParameter {
    param(
        [System.Data.SqlClient.SqlCommand]$Command,
        [string]$Name,
        [bool]$Value
    )

    $parameter = $Command.Parameters.Add($Name, [System.Data.SqlDbType]::Bit)
    $parameter.Value = $Value
}

# <lang>
#   <zh-CN>为快照时间创建 DateTime2 参数，恢复原始 UTC 时间精度。</zh-CN>
#   <en>Creates a DateTime2 parameter so snapshot restoration retains the original UTC precision.</en>
# </lang>
function Add-DateTime2Parameter {
    param(
        [System.Data.SqlClient.SqlCommand]$Command,
        [string]$Name,
        [DateTime]$Value
    )

    $parameter = $Command.Parameters.Add($Name, [System.Data.SqlDbType]::DateTime2)
    $parameter.Value = $Value
}

# <lang>
#   <zh-CN>执行带参数配置的非查询并在 finally 释放命令，统一覆盖写入、删除和恢复操作。</zh-CN>
#   <en>Executes a configured parameterized non-query and disposes the command in finally for writes, deletes, and restores.</en>
# </lang>
function Invoke-NonQuery {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Sql,
        [scriptblock]$Configure
    )

# <lang>
#   <zh-CN>每次调用创建独立命令，避免复用残留参数或命令文本。</zh-CN>
#   <en>Each call creates an independent command so parameters and command text cannot leak across operations.</en>
# </lang>
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

# <lang>
#   <zh-CN>读取首个公共 Tab 的整数标识；空值或 DBNull 表示 proof 无可用目标，不能继续修改状态。</zh-CN>
#   <en>Reads the first public Tab identifier as an integer; null or DBNull means no proof target exists and state mutation must not continue.</en>
# </lang>
function Invoke-ScalarInt {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Sql
    )

# <lang>
#   <zh-CN>标量查询独立创建命令并在 finally 释放，保持连接可继续执行后续快照操作。</zh-CN>
#   <en>The scalar query uses a dedicated command and finally disposal so the connection remains usable for later snapshots.</en>
# </lang>
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

# <lang>
#   <zh-CN>读取系统主题设置的完整可恢复字段，保留不存在状态以区分删除与更新恢复。</zh-CN>
#   <en>Reads the complete restorable system-theme fields and preserves existence so restoration can distinguish delete from update.</en>
# </lang>
function Get-SystemSettingSnapshot {
    param([System.Data.SqlClient.SqlConnection]$Connection)

# <lang>
#   <zh-CN>快照命令使用参数化 SettingKey，reader 和 command 分层释放，避免锁住配置表。</zh-CN>
#   <en>The snapshot command parameterizes SettingKey and disposes reader and command in layers so the configuration table is not left locked.</en>
# </lang>
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

# <lang>
#   <zh-CN>读取指定 Tab 的主题覆盖及审计时间，保留不存在状态供 finally 精确恢复。</zh-CN>
#   <en>Reads a Tab's theme override and audit timestamps, preserving absence so finally can restore precisely.</en>
# </lang>
function Get-TabOverrideSnapshot {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [int]$TabId
    )

# <lang>
#   <zh-CN>TabId 通过整型参数绑定，reader 在 command 释放前关闭，保持查询边界。</zh-CN>
#   <en>TabId is bound as an integer and the reader closes before command disposal, keeping the query boundary explicit.</en>
# </lang>
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

# <lang>
#   <zh-CN>写入全局主题设置并用固定测试 actor 标记来源；所有值仍通过参数传入。</zh-CN>
#   <en>Writes the global theme setting with a fixed test actor marker; every value remains parameter-bound.</en>
# </lang>
function Set-GlobalTheme {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$ThemeName
    )

# <lang>
#   <zh-CN>SQL 保持既有 upsert 语义，仅更新 proof 需要的主题字段和时间，不改变生产契约。</zh-CN>
#   <en>The SQL keeps its existing upsert semantics and changes only proof theme fields and timestamps without altering the production contract.</en>
# </lang>
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

# <lang>
#   <zh-CN>写入指定 Tab 的主题覆盖，覆盖优先级由门户运行时解释而非脚本自行模拟。</zh-CN>
#   <en>Writes a theme override for one Tab; precedence is interpreted by the portal runtime rather than simulated by this script.</en>
# </lang>
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

# <lang>
#   <zh-CN>删除指定 Tab 覆盖以验证全局主题回退路径，删除仍使用参数化 TabId。</zh-CN>
#   <en>Deletes one Tab override to exercise global-theme fallback, keeping TabId parameterized.</en>
# </lang>
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

# <lang>
#   <zh-CN>按原始快照恢复系统设置和 Tab 覆盖；不存在时删除 proof 创建的临时记录。</zh-CN>
#   <en>Restores system settings and Tab overrides from their snapshots, deleting temporary proof records when they did not previously exist.</en>
# </lang>
function Restore-ThemeSnapshots {
    param([System.Data.SqlClient.SqlConnection]$Connection)

# <lang>
#   <zh-CN>只有已取得系统快照才执行恢复，避免连接提前失败时误删未知配置。</zh-CN>
#   <en>System restoration runs only after a snapshot was captured, preventing an early connection failure from deleting unknown configuration.</en>
# </lang>
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

# <lang>
#   <zh-CN>Tab 覆盖恢复与系统设置独立判断，确保任一快照缺失不会覆盖另一层状态。</zh-CN>
#   <en>Tab restoration is evaluated independently from system settings so one missing snapshot cannot overwrite the other layer.</en>
# </lang>
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

# <lang>
#   <zh-CN>短暂重试隔离站点的 HTTP 200，允许首次编译完成但最终超时仍抛出失败。</zh-CN>
#   <en>Retries the isolated site's HTTP 200 briefly to allow first compilation, while a final timeout still fails.</en>
# </lang>
function Invoke-PortalPage {
    param([string]$Uri)

# <lang>
#   <zh-CN>固定 20 次、每次 1 秒的等待边界，避免 proof 无限等待。</zh-CN>
#   <en>A fixed 20-attempt, one-second boundary prevents the proof from waiting indefinitely.</en>
# </lang>
    for ($attempt = 1; $attempt -le 20; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri $Uri -SkipHttpErrorCheck -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                return $response.Content
            }
        }
        catch {
            # <lang>
            #   <zh-CN>IIS Express 首次编译前可能暂不可访问；短暂重试不会掩盖最终失败。</zh-CN>
            #   <en>IIS Express may be unavailable during first compilation; a short retry does not mask final failure.</en>
            # </lang>
        }

        Start-Sleep -Seconds 1
    }

    throw 'The isolated theme-proof site did not return HTTP 200 before the timeout.'
}

# <lang>
#   <zh-CN>以不区分大小写的序数匹配验证主题 class 和 CSS 资源信号，失败只抛出调用方消息。</zh-CN>
#   <en>Validates theme class and CSS resource signals with an ordinal case-insensitive match, throwing only the caller-provided message on failure.</en>
# </lang>
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

# <lang>
#   <zh-CN>主 proof 先阻止端口冲突，再捕获目标 Tab 与两层主题快照，随后按全局、Tab、非法主题回退顺序验证。</zh-CN>
#   <en>The proof first rejects port conflicts, captures the target Tab and both snapshots, then verifies global, Tab, and invalid-theme fallback in order.</en>
# </lang>
try {
# <lang>
#   <zh-CN>端口探测只读本机监听状态，不抢占或终止已有进程。</zh-CN>
#   <en>Port detection reads local listeners only and never claims or terminates an existing process.</en>
# </lang>
    $listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($listener) {
        throw "The isolated theme-proof port $Port is already in use."
    }

# <lang>
#   <zh-CN>使用外置连接串建立一次隔离连接，所有 proof SQL 都在同一显式生命周期内执行。</zh-CN>
#   <en>Creates one isolated connection from the external string so all proof SQL runs within an explicit connection lifetime.</en>
# </lang>
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

# <lang>
#   <zh-CN>先清除 Tab 覆盖再设置全局 ThemeProbe，明确验证全局主题路径。</zh-CN>
#   <en>Clears the Tab override before setting global ThemeProbe so the global-theme path is tested explicitly.</en>
# </lang>
    Clear-TabTheme -Connection $connection -TabId $tabId
    Set-GlobalTheme -Connection $connection -ThemeName 'ThemeProbe'
    $connection.Dispose()
    $connection = $null

# <lang>
#   <zh-CN>只启动指定端口的隔离 IIS Express，并记录本脚本拥有的启动事实供 finally 清理。</zh-CN>
#   <en>Starts only the isolated IIS Express port and records ownership so finally can clean up what this script started.</en>
# </lang>
    & (Join-Path $PSScriptRoot 'Start-IISExpress.ps1') -Port $Port
    $startedThemeSite = $true
    $probeUri = 'http://localhost:' + $Port + '/DesktopDefault.aspx?tabindex=0&tabid=' + $tabId

# <lang>
#   <zh-CN>断言全局主题 class 与 CSS 资源均来自数据库覆盖，避免只验证单一标记。</zh-CN>
#   <en>Asserts both the global theme class and CSS resource so database override behavior is not reduced to one marker.</en>
# </lang>
    $globalHtml = Invoke-PortalPage -Uri $probeUri
    Assert-Contains -Html $globalHtml -Expected 'portal-theme-themeprobe' -Message 'The database global ThemeProbe override was not applied to the public portal Tab.'
    Assert-Contains -Html $globalHtml -Expected 'App_Themes/ThemeProbe/Default.css' -Message 'The ThemeProbe CSS resource was not emitted for the global override.'
    Write-Host '[PASS] Database global ThemeProbe override applied.'

    $connection = [System.Data.SqlClient.SqlConnection]::new((Get-ExternalPortalConnectionString -Path $ConnectionStringsConfigPath))
    $connection.Open()
    Set-TabTheme -Connection $connection -TabId $tabId -ThemeName 'Default'
    $connection.Dispose()
    $connection = $null

# <lang>
#   <zh-CN>再次请求同一 Tab，验证 Tab 覆盖优先于全局设置且资源链同步切换。</zh-CN>
#   <en>Requests the same Tab again to verify Tab override precedence and synchronized resource switching.</en>
# </lang>
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

# <lang>
#   <zh-CN>写入非法全局主题后验证运行时安全回退到 Default，而不是接受未受信主题名。</zh-CN>
#   <en>After writing an invalid global theme, verifies runtime safely falls back to Default instead of accepting an untrusted name.</en>
# </lang>
    $fallbackHtml = Invoke-PortalPage -Uri $probeUri
    Assert-Contains -Html $fallbackHtml -Expected 'portal-theme-default' -Message 'An invalid global theme did not fall back to Default.'
    Assert-Contains -Html $fallbackHtml -Expected 'App_Themes/Default/Default.css' -Message 'The Default CSS resource was not emitted after invalid-theme fallback.'
    Write-Host '[PASS] Invalid global theme fell back to Default.'
}
finally {
# <lang>
#   <zh-CN>finally 无论 proof 成功或失败都释放连接、停止本脚本启动的 IIS 并恢复已捕获快照。</zh-CN>
#   <en>Finally disposes connections, stops IIS started by this script, and restores captured snapshots on both success and failure.</en>
# </lang>
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
