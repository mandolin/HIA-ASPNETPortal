<#
.SYNOPSIS
.LANG en
Runs the module cache isolation proof against a test database configuration.

.LANG zh-CN
针对测试数据库配置运行模块缓存隔离 proof。

.DESCRIPTION
<lang>
  <en>Uses an external Portal connection string, creates temporary ModuleProbe state, starts an isolated IIS Express instance, verifies cache behavior, and restores database state afterward. The script is intentionally restricted to the Portal SQL Server provider and should be run only against a disposable development database.</en>
  <zh-CN>使用外置 Portal 连接串创建临时 ModuleProbe 状态，启动隔离 IIS Express 实例，验证缓存行为，并在结束后恢复数据库状态。本脚本有意限制为 Portal SQL Server provider，只应针对可丢弃的开发数据库运行。</zh-CN>
</lang>

.PARAMETER ConnectionStringsConfigPath
.LANG en
External connectionStrings.config file containing the Portal SQL Server connection string.

.LANG zh-CN
包含 Portal SQL Server 连接串的外置 connectionStrings.config 文件。

.PARAMETER Port
.LANG en
IIS Express port used by the isolated cache proof site.

.LANG zh-CN
隔离缓存 proof 站点使用的 IIS Express 端口。

.PARAMETER CacheSeconds
.LANG en
Temporary cache duration used for the module instance under test.

.LANG zh-CN
测试模块实例使用的临时缓存秒数。
#>
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

# <lang>
#   <zh-CN>以下状态只属于本次隔离缓存 proof：测试 token、临时模块标识、包状态快照和 IIS 站点生命周期，不代表生产数据。</zh-CN>
#   <en>The state below belongs only to this isolated cache proof: test token, temporary module identifiers, package snapshot, and IIS site lifetime; it is not production data.</en>
# </lang>
$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
$packageId = 'HIA.ModuleProbe'
$testToken = 'P3CacheProbe-' + [Guid]::NewGuid().ToString('N')
$testActor = 'P3.5-cache-smoke'
$definitionId = 0
$moduleId = 0
$stateSnapshot = $null
$startedCacheSite = $false

# <lang>
#   <zh-CN>读取并校验唯一 Portal 外置连接串及 SQL Server provider，拒绝空值、重复项和非预期 provider。</zh-CN>
#   <en>Reads and validates the single external Portal connection string and SQL Server provider, rejecting empty, duplicate, or unexpected provider entries.</en>
# </lang>
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

# <lang>
#   <zh-CN>将可空文本绑定为参数化 NVarChar，保持 NULL 语义并阻断 SQL 拼接。</zh-CN>
#   <en>Binds nullable text as parameterized NVarChar, preserving NULL semantics and preventing SQL concatenation.</en>
# </lang>
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

# <lang>
#   <zh-CN>为 Tab、模块和模块定义键绑定类型化 Int 参数，避免字符串转换影响缓存 proof。</zh-CN>
#   <en>Binds typed Int parameters for tab, module, and module-definition keys so string conversion cannot affect the cache proof.</en>
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
#   <zh-CN>执行只读标量 SQL，并在 finally 释放命令；调用方负责参数化和连接生命周期。</zh-CN>
#   <en>Executes scalar SQL and disposes the command in finally; callers own parameter binding and connection lifetime.</en>
# </lang>
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

# <lang>
#   <zh-CN>执行临时状态写入/删除 SQL，并在 finally 释放命令；不吞掉数据库异常。</zh-CN>
#   <en>Executes temporary-state insert/update/delete SQL and disposes the command in finally without hiding database errors.</en>
# </lang>
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

# <lang>
#   <zh-CN>读取模块包状态完整快照，区分不存在、NULL Note 和可恢复审计字段，并保证 reader/command 释放。</zh-CN>
#   <en>Reads the complete package-state snapshot, distinguishing absence, NULL Note, and restorable audit fields while releasing reader and command resources.</en>
# </lang>
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

# <lang>
#   <zh-CN>以固定测试 actor 和当前 UTC 时间参数化写入/更新临时模块包状态，供缓存身份切换验证。</zh-CN>
#   <en>Parameterizes temporary package-state insert/update with a fixed test actor and current UTC time for cache-identity transitions.</en>
# </lang>
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

# <lang>
#   <zh-CN>依据初始快照恢复包状态；原本不存在时删除临时记录，保持 proof 的最小持久化副作用。</zh-CN>
#   <en>Restores package state from the initial snapshot; deletes the temporary row when none existed, minimizing persistent proof side effects.</en>
# </lang>
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

# <lang>
#   <zh-CN>在固定 HTTP 200 目标上进行有限轮询，为隔离站点首次编译留出时间但最终失败不降级。</zh-CN>
#   <en>Polls the fixed target for HTTP 200 with a bounded retry window for first compilation, then fails instead of degrading.</en>
# </lang>
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
            # <lang>
            #   <zh-CN>独立 IIS Express 首次编译期间允许短暂重试；不输出连接串或物理路径。</zh-CN>
            #   <en>Allow a short retry while the isolated IIS Express site compiles for the first time, without printing connection strings or physical paths.</en>
            # </lang>
        }

        Start-Sleep -Seconds 1
    }

    throw 'The isolated cache-proof site did not return HTTP 200 before the timeout.'
}

# <lang>
#   <zh-CN>从 ModuleProbe HTML 中提取可比较的 Rendered UTC marker，缺失时拒绝继续缓存身份断言。</zh-CN>
#   <en>Extracts a comparable Rendered UTC marker from ModuleProbe HTML and refuses to continue cache-identity assertions when absent.</en>
# </lang>
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

# <lang>
#   <zh-CN>断言页面包含预期低敏文本；失败时抛出调用方提供的诊断消息。</zh-CN>
#   <en>Asserts that the page contains expected low-sensitivity text and throws the caller-provided diagnostic on failure.</en>
# </lang>
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

# <lang>
#   <zh-CN>断言页面不包含禁用模块或样式标识，防止包停用后仍误判为成功。</zh-CN>
#   <en>Asserts that the page omits disabled-module or stylesheet markers so a disabled package cannot be misreported as successful.</en>
# </lang>
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

# <lang>
#   <zh-CN>主流程先校验 schema、发现 Tab、创建临时模块和包状态，再切换到隔离 IIS/HTTP 缓存断言。</zh-CN>
#   <en>The main flow validates the schema, discovers a tab, creates temporary module and package state, then switches to isolated IIS/HTTP cache assertions.</en>
# </lang>
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
# <lang>
#   <zh-CN>所有临时定义/实例写入均使用参数化 SQL，并以唯一测试 token 作为可清理关联键。</zh-CN>
#   <en>All temporary definition/instance writes use parameterized SQL and the unique test token as a cleanup correlation key.</en>
# </lang>
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

# <lang>
#   <zh-CN>启动前再次检查端口并调用独立 IIS 入口；后续 URI 和 marker 只指向本次临时 Tab/模块。</zh-CN>
#   <en>Checks the port again before invoking the independent IIS entry point; subsequent URI and markers target only this temporary tab/module.</en>
# </lang>
    $listening = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($listening) {
        throw "The isolated cache-proof port $Port is already in use."
    }

    & (Join-Path $PSScriptRoot 'Start-IISExpress.ps1') -Port $Port
    $startedCacheSite = $true
    $probeUri = 'http://localhost:' + $Port + '/DesktopDefault.aspx?tabindex=0&tabid=' + $tabId
    # <lang>
    #   <zh-CN>ModuleProbe 不显示旧模块标题；使用控件自身输出的标识确认动态实例实际被装载。</zh-CN>
    #   <en>ModuleProbe does not render the legacy module title, so use its own output marker to prove the dynamic instance was loaded.</en>
    # </lang>
    $moduleMarker = 'Id={0}; Source=DesktopModules/ModuleProbe/ModuleProbe.ascx' -f $moduleId

# <lang>
#   <zh-CN>依次验证首次渲染、缓存命中、包状态修订失效、停用隐藏和重新启用新身份，保持每步的观察值独立。</zh-CN>
#   <en>Verifies first render, cache hit, package-state invalidation, disabled suppression, and fresh identity after re-enable with independent observations.</en>
# </lang>
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
# <lang>
#   <zh-CN>无论 proof 成功或失败，都释放连接、停止本次站点、删除临时模块/定义并恢复包状态；清理异常继续传播。</zh-CN>
#   <en>Whether the proof passes or fails, releases connections, stops this site, deletes temporary module/definition rows, and restores package state while propagating cleanup errors.</en>
# </lang>
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
