<#
.SYNOPSIS
.LANG en
Generates a development/test SQL seed for the P22.4 enterprise workbench module.

.LANG zh-CN
生成 P22.4 企业能力工作台模块的开发/测试 SQL seed。

.DESCRIPTION
<lang>
  <en>Creates a reviewable SQL file that enables the trusted EnterpriseCapabilityWorkbench module package, registers its module definition, and creates or updates a P22-Test-EnterpriseWorkbench portal Tab with one module instance. The script does not connect to SQL Server unless Apply is explicitly specified.</en>
  <zh-CN>生成一份可审阅 SQL 文件，用于启用受信任 EnterpriseCapabilityWorkbench 模块包、注册模块定义，并创建或更新 P22-Test-EnterpriseWorkbench 门户 Tab 及一个模块实例。除非显式指定 Apply，否则本脚本不会连接 SQL Server。</zh-CN>
</lang>

.PARAMETER OutputPath
.LANG en
Target SQL file path. The default path is under temp/p22.4.

.LANG zh-CN
目标 SQL 文件路径。默认输出到 temp/p22.4 下。

.PARAMETER ConnectionStringsConfigPath
.LANG en
Optional external connectionStrings.config path. Required only when Apply is specified.

.LANG zh-CN
可选外置 connectionStrings.config 路径。只有指定 Apply 时才需要。
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string]$OutputPath = (Join-Path (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path 'temp/p22.4/PortalP22EnterpriseWorkbenchScenario.sql'),

    [ValidateNotNullOrEmpty()]
    [string]$TabName = 'P22-Test-EnterpriseWorkbench',

    [ValidateNotNullOrEmpty()]
    [string]$ModuleTitle = 'Enterprise Capability Workbench',

    [ValidateRange(1, 2147483647)]
    [int]$PortalId = 1,

    [ValidateNotNullOrEmpty()]
    [string]$AccessRoles = 'All Users;',

    [ValidateNotNullOrEmpty()]
    [string]$EditRoles = 'Admins;',

    [ValidateNotNullOrEmpty()]
    [string]$Actor = 'P22.4ScenarioSeed',

    [ValidateScript({ [string]::IsNullOrWhiteSpace($_) -or (Test-Path -LiteralPath $_ -PathType Leaf) })]
    [string]$ConnectionStringsConfigPath,

    [string]$ConnectionStringName = 'Portal',

    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>将输入值安全转换为 SQL Unicode 字面量；空值保持为 NULL，单引号加倍转义。本函数只生成文本。</zh-CN>
#   <en>Convert an input to a SQL Unicode literal; preserve null as NULL and double apostrophes for escaping. This function only generates text.</en>
# </lang>
function ConvertTo-SqlNVarCharLiteral {
    param([AllowNull()][string]$Value)

    if ($null -eq $Value) {
        return 'NULL'
    }

    return "N'" + ($Value -replace "'", "''") + "'"
}

# <lang>
#   <zh-CN>以 UTF-8 无 BOM 写出 SQL 模板并按需创建目录；文件写入本身不连接数据库。</zh-CN>
#   <en>Write the SQL template as UTF-8 without a BOM and create the directory when needed; file output itself does not connect to a database.</en>
# </lang>
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

# <lang>
#   <zh-CN>读取人工指定的外置 connectionStrings.config 并校验唯一命名项/提供程序；只在 Apply 且用户确认后调用，连接串不写日志。</zh-CN>
#   <en>Read a manually supplied connectionStrings.config and validate the unique named entry/provider; call only after Apply and user confirmation, and never log the connection string.</en>
# </lang>
function Get-ExternalPortalConnectionString {
    param(
        [string]$Path,
        [string]$Name
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

    $entries = @($connectionStringsNode.add | Where-Object { $_.name -eq $Name })
    if ($entries.Count -ne 1 -or [string]::IsNullOrWhiteSpace($entries[0].connectionString)) {
        throw "The external connection-string file must contain one non-empty '$Name' entry."
    }

    if ($entries[0].providerName -and $entries[0].providerName -ne 'System.Data.SqlClient') {
        throw 'The P22.4 scenario apply path currently supports only System.Data.SqlClient.'
    }

    return $entries[0].connectionString
}

$desktopSource = 'DesktopModules/EnterpriseCapabilityWorkbench/EnterpriseCapabilityWorkbench.ascx'
$packageId = 'HIA.EnterpriseCapabilityWorkbench'
$friendlyName = 'Enterprise Capability Workbench'

# <lang>
#   <zh-CN>以下 SQL 是供开发/测试库人工复核和执行的模板；不创建用户、不写密码，是否执行及事务环境由 Apply 调用方负责。</zh-CN>
#   <en>The following SQL is a template for manual review and execution in a development/test database; it creates no users or passwords, and Apply callers own execution and transaction context.</en>
# </lang>
$sql = @"
/*
    P22.4 企业能力工作台开发/测试挂载脚本。
    P22.4 development/test mount script for the enterprise-capability workbench.

    重要边界 / Important boundary:
    1. 本 SQL 只应在开发库或测试库手动执行，不应直接用于生产库。
       Run this SQL manually in development or test databases only; do not run it directly in production.
    2. 执行前应先完成 P21 企业协同事项迁移、P12 待办迁移、P5 权限和 P3 模块包状态表。
       Before running this SQL, apply the P21 collaboration-item, P12 work-item, P5 permission,
       and P3 module-package-state migrations.
    3. 本 SQL 只注册测试 Tab、模块定义、模块实例和模块包启用状态，不创建用户、不写密码。
       This SQL only registers the test Tab, module definition, module instance, and package enablement;
       it does not create users or write passwords.
*/

SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @PortalId INT = $PortalId;
DECLARE @TabName NVARCHAR(150) = $(ConvertTo-SqlNVarCharLiteral $TabName);
DECLARE @ModuleTitle NVARCHAR(150) = $(ConvertTo-SqlNVarCharLiteral $ModuleTitle);
DECLARE @AccessRoles NVARCHAR(250) = $(ConvertTo-SqlNVarCharLiteral $AccessRoles);
DECLARE @EditRoles NVARCHAR(250) = $(ConvertTo-SqlNVarCharLiteral $EditRoles);
DECLARE @FriendlyName NVARCHAR(150) = $(ConvertTo-SqlNVarCharLiteral $friendlyName);
DECLARE @DesktopSource NVARCHAR(250) = $(ConvertTo-SqlNVarCharLiteral $desktopSource);
DECLARE @PackageId NVARCHAR(100) = $(ConvertTo-SqlNVarCharLiteral $packageId);
DECLARE @Actor NVARCHAR(100) = $(ConvertTo-SqlNVarCharLiteral $Actor);

IF OBJECT_ID(N'[dbo].[PortalCfg_ModuleDefinitions]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalCfg_Tabs]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalCfg_Modules]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalCfg_ModulePackageStates]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItems]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItemEvents]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_WorkItemEvents]', N'U') IS NULL
BEGIN
    RAISERROR(N'P22.4 scenario requires P3/P12/P21 schema. Run the prerequisite migrations first.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

DECLARE @ModuleDefId INT;
SELECT TOP (1) @ModuleDefId = [ModuleDefId]
FROM [dbo].[PortalCfg_ModuleDefinitions]
WHERE [DesktopSourceFile] = @DesktopSource
ORDER BY [ModuleDefId];

IF @ModuleDefId IS NULL
BEGIN
    INSERT INTO [dbo].[PortalCfg_ModuleDefinitions]
        ([FriendlyName], [DesktopSourceFile], [MobileSourceFile])
    VALUES
        (@FriendlyName, @DesktopSource, N'');

    SET @ModuleDefId = CONVERT(INT, SCOPE_IDENTITY());
END
ELSE
BEGIN
    UPDATE [dbo].[PortalCfg_ModuleDefinitions]
    SET [FriendlyName] = @FriendlyName,
        [MobileSourceFile] = N''
    WHERE [ModuleDefId] = @ModuleDefId;
END

IF EXISTS (SELECT 1 FROM [dbo].[PortalCfg_ModulePackageStates] WHERE [PackageId] = @PackageId)
BEGIN
    UPDATE [dbo].[PortalCfg_ModulePackageStates]
    SET [IsEnabled] = 1,
        [Note] = N'Enabled by P22.4 scenario seed.',
        [UpdatedBy] = @Actor,
        [UpdatedUtc] = SYSUTCDATETIME()
    WHERE [PackageId] = @PackageId;
END
ELSE
BEGIN
    INSERT INTO [dbo].[PortalCfg_ModulePackageStates]
        ([PackageId], [IsEnabled], [Note], [UpdatedBy], [UpdatedUtc])
    VALUES
        (@PackageId, 1, N'Enabled by P22.4 scenario seed.', @Actor, SYSUTCDATETIME());
END

DECLARE @TabId INT;
SELECT TOP (1) @TabId = [TabId]
FROM [dbo].[PortalCfg_Tabs]
WHERE [PortalId] = @PortalId
  AND [TabName] = @TabName
ORDER BY [TabId];

IF @TabId IS NULL
BEGIN
    DECLARE @TabOrder INT;
    SELECT @TabOrder = ISNULL(MAX([TabOrder]), 0) + 2
    FROM [dbo].[PortalCfg_Tabs]
    WHERE [PortalId] = @PortalId;

    INSERT INTO [dbo].[PortalCfg_Tabs]
        ([TabName], [TabOrder], [AccessRoles], [ShowMobile], [MobileTabName], [PortalId])
    VALUES
        (@TabName, @TabOrder, @AccessRoles, 0, @TabName, @PortalId);

    SET @TabId = CONVERT(INT, SCOPE_IDENTITY());
END
ELSE
BEGIN
    UPDATE [dbo].[PortalCfg_Tabs]
    SET [AccessRoles] = @AccessRoles,
        [ShowMobile] = 0,
        [MobileTabName] = @TabName
    WHERE [TabId] = @TabId;
END

DECLARE @ModuleId INT;
SELECT TOP (1) @ModuleId = [ModuleId]
FROM [dbo].[PortalCfg_Modules]
WHERE [TabId] = @TabId
  AND [ModuleDefId] = @ModuleDefId
ORDER BY [ModuleId];

IF @ModuleId IS NULL
BEGIN
    DECLARE @ModuleOrder INT;
    SELECT @ModuleOrder = ISNULL(MAX([ModuleOrder]), 0) + 1
    FROM [dbo].[PortalCfg_Modules]
    WHERE [TabId] = @TabId
      AND [PaneName] = N'ContentPane';

    INSERT INTO [dbo].[PortalCfg_Modules]
        ([ModuleTitle], [ModuleOrder], [EditRoles], [PaneName], [ShowMobile], [CacheTimeout], [ModuleDefId], [TabId])
    VALUES
        (@ModuleTitle, @ModuleOrder, @EditRoles, N'ContentPane', 0, 0, @ModuleDefId, @TabId);

    SET @ModuleId = CONVERT(INT, SCOPE_IDENTITY());
END
ELSE
BEGIN
    UPDATE [dbo].[PortalCfg_Modules]
    SET [ModuleTitle] = @ModuleTitle,
        [EditRoles] = @EditRoles,
        [PaneName] = N'ContentPane',
        [ShowMobile] = 0,
        [CacheTimeout] = 0
    WHERE [ModuleId] = @ModuleId;
END

COMMIT TRANSACTION;

SELECT
    @TabId AS [P22TestTabId],
    @ModuleDefId AS [EnterpriseWorkbenchModuleDefId],
    @ModuleId AS [EnterpriseWorkbenchModuleId],
    @PackageId AS [PackageId],
    @TabName AS [TabName];
"@

# <lang>
#   <zh-CN>先将 SQL 模板写入指定文件；生成文件不代表数据库已变更。</zh-CN>
#   <en>Write the SQL template to the requested file first; generating the file does not mean that a database changed.</en>
# </lang>
Write-Utf8NoBomFile -Path $OutputPath -Content ($sql -replace "`n", "`r`n")

[bool]$applied = $false
# <lang>
#   <zh-CN>Apply 是显式高影响操作，必须提供外置连接串并通过 ShouldProcess；未启用时只生成文件。</zh-CN>
#   <en>Apply is an explicit high-impact operation requiring an external connection string and ShouldProcess approval; when disabled, only the file is generated.</en>
# </lang>
if ($Apply) {
    if ([string]::IsNullOrWhiteSpace($ConnectionStringsConfigPath)) {
        throw 'ConnectionStringsConfigPath is required when Apply is specified.'
    }

    if ($PSCmdlet.ShouldProcess('the selected external development/test database', 'Apply the P22.4 enterprise-workbench scenario SQL')) {
        $connection = [System.Data.SqlClient.SqlConnection]::new((Get-ExternalPortalConnectionString -Path $ConnectionStringsConfigPath -Name $ConnectionStringName))
        try {
            $connection.Open()
            $command = $connection.CreateCommand()
            try {
                # <lang>
                #   <zh-CN>生成的 SQL 不含 GO 分隔符，因此可作为单批次执行；真实连接串只保存在内存中，不输出到日志。</zh-CN>
                #   <en>The generated SQL contains no GO separators, so it can run as one batch; the real connection string stays in memory and is not logged.</en>
                # </lang>
                $command.CommandText = $sql
                $command.CommandTimeout = 120
                [void]$command.ExecuteNonQuery()
                $applied = $true
            }
            finally {
                $command.Dispose()
            }
        }
        finally {
            $connection.Dispose()
        }
    }
}

[pscustomobject]@{
    OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
    TabName = $TabName
    ModuleTitle = $ModuleTitle
    PortalId = $PortalId
    PackageId = $packageId
    DesktopSource = $desktopSource
    Applied = $applied
}
