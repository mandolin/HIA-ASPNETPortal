<#
.SYNOPSIS
.LANG en
Generates a development/test SQL seed for the P19.5 business-application sample page.

.LANG zh-CN
生成 P19.5 业务申请样板页的开发/测试 SQL seed。

.DESCRIPTION
.LANG en
Creates a reviewable SQL file that enables the trusted BusinessApplicationRequest
module package, registers its module definition, and creates or updates a
P19-Test-BusinessApplication portal Tab with a module instance. The script does
not connect to SQL Server and does not mutate any database by itself.

.LANG zh-CN
生成一份可审阅 SQL 文件，用于启用受信任 BusinessApplicationRequest 模块包、注册模块定义，
并创建或更新 P19-Test-BusinessApplication 门户 Tab 及模块实例。本脚本本身不连接 SQL Server，
也不会直接修改任何数据库。

.PARAMETER OutputPath
.LANG en
Target SQL file path. The default path is under temp/p19.5.

.LANG zh-CN
目标 SQL 文件路径。默认输出到 temp/p19.5 下。

.PARAMETER TabName
.LANG en
Portal Tab name used for the test page.

.LANG zh-CN
测试页使用的门户 Tab 名称。

.PARAMETER ModuleTitle
.LANG en
Module instance title shown on the test page.

.LANG zh-CN
测试页上显示的模块实例标题。

.PARAMETER PortalId
.LANG en
Legacy Portal id that owns the test Tab.

.LANG zh-CN
拥有测试 Tab 的旧 Portal 编号。

.PARAMETER ConnectionStringsConfigPath
.LANG en
Optional external connectionStrings.config path. Required only when Apply is specified.

.LANG zh-CN
可选外置 connectionStrings.config 路径。只有指定 Apply 时才需要。

.PARAMETER Apply
.LANG en
Executes the generated SQL against the configured development/test database.

.LANG zh-CN
将生成的 SQL 执行到已配置的开发/测试数据库。
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string]$OutputPath = (Join-Path (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path 'temp/p19.5/PortalP19BusinessApplicationScenario.sql'),

    [ValidateNotNullOrEmpty()]
    [string]$TabName = 'P19-Test-BusinessApplication',

    [ValidateNotNullOrEmpty()]
    [string]$ModuleTitle = 'Business Application Request',

    [ValidateRange(1, 2147483647)]
    [int]$PortalId = 1,

    [ValidateNotNullOrEmpty()]
    [string]$AccessRoles = 'All Users;',

    [ValidateNotNullOrEmpty()]
    [string]$EditRoles = 'Admins;',

    [ValidateNotNullOrEmpty()]
    [string]$Actor = 'P19.5ScenarioSeed',

    [ValidateScript({ [string]::IsNullOrWhiteSpace($_) -or (Test-Path -LiteralPath $_ -PathType Leaf) })]
    [string]$ConnectionStringsConfigPath,

    [string]$ConnectionStringName = 'Portal',

    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-SqlNVarCharLiteral {
    param([AllowNull()][string]$Value)

    if ($null -eq $Value) {
        return 'NULL'
    }

    return "N'" + ($Value -replace "'", "''") + "'"
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

function Get-ExternalPortalConnectionString {
    param(
        [string]$Path,
        [string]$Name
    )

    [xml]$document = [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))

    # <lang>
    #   <zh-CN>应用正式契约是 <connectionStrings> 根节点；保留 configuration 包装兼容，便于人工临时文件复用。</zh-CN>
    #   <en>The production contract uses a <connectionStrings> root; keep configuration-wrapper compatibility for temporary manual files.</en>
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

    $entries = @($connectionStringsNode.add | Where-Object { $_.name -eq $Name })
    if ($entries.Count -ne 1 -or [string]::IsNullOrWhiteSpace($entries[0].connectionString)) {
        throw "The external connection-string file must contain one non-empty '$Name' entry."
    }

    if ($entries[0].providerName -and $entries[0].providerName -ne 'System.Data.SqlClient') {
        throw 'The P19.5 scenario apply path currently supports only System.Data.SqlClient.'
    }

    return $entries[0].connectionString
}

$desktopSource = 'DesktopModules/BusinessApplicationRequest/BusinessApplicationRequest.ascx'
$packageId = 'HIA.BusinessApplicationRequest'
$friendlyName = 'Business Application Request'

$sql = @"
/*
    P19.5 业务申请样板页开发/测试挂载脚本。
    P19.5 development/test mount script for the business-application sample page.

    重要边界 / Important boundary:
    1. 本 SQL 只应在开发库或测试库手动执行，不应直接用于生产库。
       Run this SQL manually in development or test databases only; do not run it directly in production.
    2. 执行前应先完成 P19.4 迁移：PortalBiz_BusinessApplications.sql、PortalBiz_WorkflowEvents.sql，
       并补齐 P12.3 待办、P5 权限和 P3 模块包状态表。
       Before running this SQL, apply the P19.4 migrations and ensure the P12.3 work-item, P5 permission,
       and P3 module-package-state tables exist.
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
    OR OBJECT_ID(N'[dbo].[PortalCfg_RolePermissions]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalCfg_OperationAudits]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_WorkItemEvents]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_BusinessApplications]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_WorkflowEvents]', N'U') IS NULL
BEGIN
    RAISERROR(N'P19.5 scenario requires P3/P5/P12/P19 schema. Run the prerequisite migrations first.', 16, 1);
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
        [Note] = N'Enabled by P19.5 scenario seed.',
        [UpdatedBy] = @Actor,
        [UpdatedUtc] = SYSUTCDATETIME()
    WHERE [PackageId] = @PackageId;
END
ELSE
BEGIN
    INSERT INTO [dbo].[PortalCfg_ModulePackageStates]
        ([PackageId], [IsEnabled], [Note], [UpdatedBy], [UpdatedUtc])
    VALUES
        (@PackageId, 1, N'Enabled by P19.5 scenario seed.', @Actor, SYSUTCDATETIME());
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
    @TabId AS [P19TestTabId],
    @ModuleDefId AS [BusinessApplicationModuleDefId],
    @ModuleId AS [BusinessApplicationModuleId],
    @PackageId AS [PackageId],
    @TabName AS [TabName];

SELECT [TabId], [TabName], [TabOrder], [AccessRoles], [PortalId]
FROM [dbo].[PortalCfg_Tabs]
WHERE [TabId] = @TabId;

SELECT [ModuleId], [ModuleTitle], [PaneName], [ModuleDefId], [TabId]
FROM [dbo].[PortalCfg_Modules]
WHERE [ModuleId] = @ModuleId;

SELECT [PackageId], [IsEnabled], [UpdatedBy], [UpdatedUtc], [Note]
FROM [dbo].[PortalCfg_ModulePackageStates]
WHERE [PackageId] = @PackageId;

SELECT
    (SELECT COUNT(*) FROM [dbo].[PortalBiz_BusinessApplications]) AS [BusinessApplicationCount],
    (SELECT COUNT(*) FROM [dbo].[PortalBiz_WorkflowEvents]) AS [WorkflowEventCount],
    (SELECT COUNT(*) FROM [dbo].[PortalBiz_WorkItems] WHERE [BusinessKind] = N'BusinessApplication') AS [BusinessApplicationWorkItemCount],
    (SELECT COUNT(*) FROM [dbo].[PortalCfg_OperationAudits] WHERE [TargetType] = N'BusinessApplication') AS [BusinessApplicationAuditCount];
"@

Write-Utf8NoBomFile -Path $OutputPath -Content ($sql -replace "`n", "`r`n")

[bool]$applied = $false
if ($Apply) {
    if ([string]::IsNullOrWhiteSpace($ConnectionStringsConfigPath)) {
        throw 'ConnectionStringsConfigPath is required when Apply is specified.'
    }

    if ($PSCmdlet.ShouldProcess('the selected external development/test database', 'Apply the P19.5 business-application scenario SQL')) {
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
