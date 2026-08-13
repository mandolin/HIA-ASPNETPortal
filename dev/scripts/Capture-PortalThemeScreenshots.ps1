<#
.SYNOPSIS
.LANG en
Captures reference screenshots for the deployed portal themes.

.LANG zh-CN
采集已部署门户主题的参考截图。

.DESCRIPTION
<lang>
  <en>Temporarily applies theme settings through the configured test database, drives browser-based capture flows, and writes screenshots to a WorkZone research directory. The script is evidence tooling for design review; it should run only against development data that can tolerate temporary theme changes.</en>
  <zh-CN>通过配置的测试数据库临时应用主题设置，驱动浏览器截图流程，并把截图写入 WorkZone research 目录。本脚本是设计复核证据工具，只应针对可承受临时主题变更的开发数据运行。</zh-CN>
</lang>

.PARAMETER BaseUrl
.LANG en
Portal base URL used by the screenshot capture flow.

.LANG zh-CN
截图采集流程使用的门户基础 URL。

.PARAMETER ConnectionStringsConfigPath
.LANG en
External connectionStrings.config file used to update temporary theme settings.

.LANG zh-CN
用于更新临时主题设置的外置 connectionStrings.config 文件。

.PARAMETER P64ContextPath
.LANG en
Regression context JSON for employee profile confirmation screenshots.

.LANG zh-CN
员工资料确认截图使用的回归上下文 JSON。

.PARAMETER P65ContextPath
.LANG en
Acceptance context JSON for employee profile correction screenshots.

.LANG zh-CN
员工资料更正截图使用的验收上下文 JSON。

.PARAMETER OutputDirectory
.LANG en
Target directory for captured screenshots and related review artifacts.

.LANG zh-CN
截图和相关复核产物的目标目录。

.PARAMETER Themes
.LANG en
Theme names to apply and capture.

.LANG zh-CN
需要应用并采集的主题名称列表。
#>
[CmdletBinding()]
param(
    [ValidatePattern('^https?://')]
    [string]$BaseUrl = 'http://localhost:40001/',

    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$ConnectionStringsConfigPath = (Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\dev\connectionStrings.config'),

    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$P64ContextPath = (Join-Path (Join-Path $PSScriptRoot '..\..') 'temp\p64\p64-regression-context.json'),

    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$P65ContextPath = (Join-Path (Join-Path $PSScriptRoot '..\..') 'temp\p65\p65-acceptance-context.json'),

    [string]$OutputDirectory = (Join-Path (Join-Path $PSScriptRoot '..\..') 'work-zone\dev\research\p7-theme-prototype-screenshots'),

    [string[]]$Themes = @(
        'EnterpriseLight',
        'EnterpriseDark',
        'OaLight',
        'OaDark',
        'StateClassicLight',
        'StateClassicDark'
    )
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$settingKey = 'Portal.Theme.Name'
$testActor = 'P7.3-theme-screenshot'
$settingSnapshot = $null
$connection = $null

# <lang>
#   <zh-CN>下面的状态仅服务本次截图编排：外置数据库快照、测试 actor、输出目录和 Node 子进程环境，不代表生产配置。</zh-CN>
#   <en>The state below serves only this capture run: external database snapshot, test actor, output directory, and Node child-process environment; it is not production configuration.</en>
# </lang>
# <lang>
#   <zh-CN>读取外置 XML 中命名为 Portal 的连接串；缺失时失败且不回显敏感值。</zh-CN>
#   <en>Reads the connection string named Portal from external XML; missing input fails without echoing sensitive values.</en>
# </lang>
function Get-ExternalPortalConnectionString {
    param([string]$Path)

# <lang>
#   <zh-CN>以无 BOM UTF-8 解析两种 connectionStrings XML 包装形式，保持配置文件边界。</zh-CN>
#   <en>Parses both connectionStrings XML wrapper forms as UTF-8 without a BOM, keeping the external-file boundary explicit.</en>
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

    if ($null -eq $portalNode -or [string]::IsNullOrWhiteSpace($portalNode.connectionString)) {
        throw 'The external connectionStrings.config file does not contain a Portal connection string.'
    }

    return $portalNode.connectionString
}

# <lang>
#   <zh-CN>将可空文本映射为参数化 NVarChar，保持 NULL 语义并避免 SQL 拼接。</zh-CN>
#   <en>Maps nullable text to a parameterized NVarChar, preserving NULL semantics and avoiding SQL concatenation.</en>
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
#   <zh-CN>为布尔列创建类型化参数，供临时主题设置恢复使用。</zh-CN>
#   <en>Creates a typed Boolean parameter for temporary theme-setting restoration.</en>
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
#   <zh-CN>为整数键创建类型化参数，避免页面/模块目标查询依赖文本转换。</zh-CN>
#   <en>Creates a typed integer parameter so page and module target queries do not depend on text conversion.</en>
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
#   <zh-CN>为时间字段创建 DateTime2 参数，恢复主题设置原始审计时间。</zh-CN>
#   <en>Creates a DateTime2 parameter so restored theme settings retain their original audit timestamps.</en>
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
#   <zh-CN>执行带配置回调的非查询并在 finally 释放命令，覆盖主题写入和恢复路径。</zh-CN>
#   <en>Executes a configured non-query and disposes the command in finally for both theme writes and restoration.</en>
# </lang>
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

# <lang>
#   <zh-CN>执行只读标量查询并由调用方绑定参数，统一目标 ID 发现边界。</zh-CN>
#   <en>Executes a scalar query with caller-bound parameters, centralizing target-ID discovery boundaries.</en>
# </lang>
function Invoke-ScalarQuery {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Sql,
        [scriptblock]$Configure
    )

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = $Sql
        & $Configure $command
        $value = $command.ExecuteScalar()
        if ($null -eq $value -or $value -is [DBNull]) {
            return $null
        }

        return $value
    }
    finally {
        $command.Dispose()
    }
}

# <lang>
#   <zh-CN>发现可用于内容截图的 Tab 目标，输出低敏 ID/路径事实而不创建页面。</zh-CN>
#   <en>Discovers Tab targets usable for content screenshots and emits low-sensitivity ID/path facts without creating pages.</en>
# </lang>
function Get-ContentTabTargets {
    param([System.Data.SqlClient.SqlConnection]$Connection)

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = @'
WITH OrderedTabs AS
(
    SELECT
        [TabId],
        [TabName],
        ROW_NUMBER() OVER (ORDER BY [TabOrder], [TabId]) - 1 AS [TabIndex]
    FROM [dbo].[PortalCfg_Tabs]
    WHERE [PortalId] = 1
)
SELECT [TabName], [TabId], [TabIndex]
FROM OrderedTabs
WHERE [TabName] IN (N'Employee Info', N'Product Info', N'Discussions', N'About the Portal')
ORDER BY [TabIndex], [TabId]
'@
        $reader = $command.ExecuteReader()
        try {
            $targets = New-Object 'System.Collections.Generic.List[object]'
            while ($reader.Read()) {
                $targets.Add([pscustomobject]@{
                    tabName = $reader.GetString(0)
                    tabId = $reader.GetInt32(1)
                    tabIndex = [Convert]::ToInt32($reader.GetValue(2), [System.Globalization.CultureInfo]::InvariantCulture)
                })
            }

            return $targets
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
#   <zh-CN>发现旧后台模块页面目标，先通过模块定义和受治理路径过滤，避免截图越出范围。</zh-CN>
#   <en>Discovers legacy-admin module page targets, filtering by module definitions and governed paths before capture leaves scope.</en>
# </lang>
function Get-LegacyAdminModuleTargets {
    param([System.Data.SqlClient.SqlConnection]$Connection)

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = @'
WITH OrderedTabs AS
(
    SELECT
        [TabId],
        [TabName],
        ROW_NUMBER() OVER (ORDER BY [TabOrder], [TabId]) - 1 AS [TabIndex]
    FROM [dbo].[PortalCfg_Tabs]
    WHERE [PortalId] = 1
),
LegacyModules AS
(
    SELECT
        t.[TabId],
        t.[TabIndex],
        m.[ModuleTitle],
        d.[DesktopSourceFile]
    FROM OrderedTabs t
    INNER JOIN [dbo].[PortalCfg_Modules] m
        ON m.[TabId] = t.[TabId]
    INNER JOIN [dbo].[PortalCfg_ModuleDefinitions] d
        ON d.[ModuleDefId] = m.[ModuleDefId]
    WHERE t.[TabName] = N'Admin'
      AND d.[DesktopSourceFile] IN
      (
          N'Admin/ModuleDefs.ascx',
          N'Admin/SiteSettings.ascx',
          N'Admin/Tabs.ascx',
          N'Admin/Roles.ascx',
          N'Admin/Users.ascx'
      )
)
SELECT [TabId], [TabIndex], [ModuleTitle], [DesktopSourceFile]
FROM LegacyModules
ORDER BY
    CASE [DesktopSourceFile]
        WHEN N'Admin/ModuleDefs.ascx' THEN 1
        WHEN N'Admin/SiteSettings.ascx' THEN 2
        WHEN N'Admin/Tabs.ascx' THEN 3
        WHEN N'Admin/Roles.ascx' THEN 4
        WHEN N'Admin/Users.ascx' THEN 5
        ELSE 99
    END
'@
        $reader = $command.ExecuteReader()
        try {
            $targets = New-Object 'System.Collections.Generic.List[object]'
            while ($reader.Read()) {
                $sourceFile = $reader.GetString(3)
                $targetMeta = switch ($sourceFile) {
                    'Admin/ModuleDefs.ascx' {
                        @{ id = 'admin-legacy-module-defs'; title = '旧模块定义 ASCX'; scrollText = 'Legacy Module Definitions' }
                    }
                    'Admin/SiteSettings.ascx' {
                        @{ id = 'admin-legacy-site-settings'; title = '旧站点设置 ASCX'; scrollText = 'Legacy Site Settings' }
                    }
                    'Admin/Tabs.ascx' {
                        @{ id = 'admin-legacy-tabs'; title = '旧 Tab 管理 ASCX'; scrollText = 'Legacy Tab Administration' }
                    }
                    'Admin/Roles.ascx' {
                        @{ id = 'admin-legacy-roles'; title = '旧角色管理 ASCX'; scrollText = 'Legacy Role Administration' }
                    }
                    'Admin/Users.ascx' {
                        @{ id = 'admin-legacy-users'; title = '旧用户入口 ASCX'; scrollText = 'Legacy User Entry' }
                    }
                    default {
                        $null
                    }
                }

                if ($null -eq $targetMeta) {
                    continue
                }

                $tabId = $reader.GetInt32(0)
                $tabIndex = [Convert]::ToInt32($reader.GetValue(1), [System.Globalization.CultureInfo]::InvariantCulture)
                $targets.Add([pscustomobject]@{
                    id = $targetMeta.id
                    title = $targetMeta.title
                    url = "DesktopDefault.aspx?tabindex=$tabIndex&tabid=$tabId"
                    scrollText = $targetMeta.scrollText
                })
            }

            return $targets
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
#   <zh-CN>查找讨论详情目标并保留可回退的空结果，供截图 Node 流程条件执行。</zh-CN>
#   <en>Finds a discussion-detail target and preserves an empty fallback so the Node capture flow can remain conditional.</en>
# </lang>
function Get-DiscussionDetailTarget {
    param([System.Data.SqlClient.SqlConnection]$Connection)

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = @'
SELECT TOP (1) [ItemID], [ModuleID]
FROM [dbo].[Portal_Discussion]
WHERE [ItemID] > 0
  AND [ModuleID] > 0
ORDER BY [ItemID]
'@
        $reader = $command.ExecuteReader()
        try {
            if (-not $reader.Read()) {
                return $null
            }

            return [pscustomobject]@{
                itemId = $reader.GetInt32(0)
                moduleId = $reader.GetInt32(1)
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
#   <zh-CN>按模块定义取得首个可编辑页面 ID，缺失时返回空值而不伪造权限目标。</zh-CN>
#   <en>Gets the first editable page ID for a module definition, returning null when absent instead of fabricating a permission target.</en>
# </lang>
function Get-FirstModuleIdForDefinition {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$FriendlyName
    )

    return Invoke-ScalarQuery -Connection $Connection -Sql @'
SELECT TOP (1) [m].[ModuleId]
FROM [dbo].[PortalCfg_Modules] AS [m]
INNER JOIN [dbo].[PortalCfg_ModuleDefinitions] AS [d]
    ON [d].[ModuleDefId] = [m].[ModuleDefId]
WHERE [d].[FriendlyName] = @FriendlyName
ORDER BY [m].[TabId], [m].[ModuleOrder], [m].[ModuleId]
'@ -Configure {
        param($command)
        Add-TextParameter -Command $command -Name '@FriendlyName' -Size 150 -Value $FriendlyName
    }
}

# <lang>
#   <zh-CN>只读判定指定用户是否拥有权限键，供后台截图目标条件化，不执行授权变更。</zh-CN>
#   <en>Read-only checks whether a user has a permission key for conditional admin targets; it never changes authorization.</en>
# </lang>
function Test-PortalUserPermission {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$UserName,
        [string]$PermissionKey
    )

    if ([string]::IsNullOrWhiteSpace($UserName) -or [string]::IsNullOrWhiteSpace($PermissionKey)) {
        return $false
    }

    $hasPermission = Invoke-ScalarQuery -Connection $Connection -Sql @'
DECLARE @HasPermission bit = 0;

IF EXISTS
(
    SELECT 1
    FROM [dbo].[Portal_Users] AS [Users]
    INNER JOIN [dbo].[Portal_UserRoles] AS [UserRoles]
        ON [UserRoles].[UserID] = [Users].[UserID]
    INNER JOIN [dbo].[Portal_Roles] AS [Roles]
        ON [Roles].[RoleID] = [UserRoles].[RoleID]
    WHERE ([Users].[Name] = @UserName OR [Users].[Email] = @UserName)
      AND [Roles].[RoleName] = N'Admins'
)
BEGIN
    SET @HasPermission = 1;
END
ELSE IF OBJECT_ID(N'[dbo].[PortalCfg_RolePermissions]', N'U') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM [dbo].[Portal_Users] AS [Users]
        INNER JOIN [dbo].[Portal_UserRoles] AS [UserRoles]
            ON [UserRoles].[UserID] = [Users].[UserID]
        INNER JOIN [dbo].[PortalCfg_RolePermissions] AS [RolePermissions]
            ON [RolePermissions].[RoleId] = [UserRoles].[RoleID]
        WHERE ([Users].[Name] = @UserName OR [Users].[Email] = @UserName)
          AND [RolePermissions].[PermissionKey] = @PermissionKey
          AND [RolePermissions].[IsEnabled] = 1
    )
    BEGIN
        SET @HasPermission = 1;
    END
END

SELECT @HasPermission;
'@ -Configure {
        param($command)
        Add-TextParameter -Command $command -Name '@UserName' -Size 100 -Value $UserName
        Add-TextParameter -Command $command -Name '@PermissionKey' -Size 120 -Value $PermissionKey
    }

    return [Convert]::ToBoolean($hasPermission)
}

# <lang>
#   <zh-CN>从 P64/P65 上下文和受治理页面规则构造编辑页目标，保留缺失项的显式回退。</zh-CN>
#   <en>Builds edit-page targets from P64/P65 contexts and governed page rules, preserving explicit fallbacks for missing items.</en>
# </lang>
function Get-OrCreateEditPageTargets {
    param([System.Data.SqlClient.SqlConnection]$Connection)

    $targets = New-Object 'System.Collections.Generic.List[object]'

    $announcementModuleId = Get-FirstModuleIdForDefinition -Connection $Connection -FriendlyName 'Announcements'
    if ($null -ne $announcementModuleId) {
        $announcementItemId = Invoke-ScalarQuery -Connection $Connection -Sql @'
DECLARE @ItemId int;

SELECT TOP (1) @ItemId = [ItemID]
FROM [dbo].[Portal_Announcements]
WHERE [ModuleID] = @ModuleID
  AND [Title] = N'P7-Test-Announcement-Edit';

IF @ItemId IS NULL
BEGIN
    INSERT INTO [dbo].[Portal_Announcements]
        ([ModuleID], [CreatedByUser], [CreatedDate], [Title], [MoreLink], [MobileMoreLink], [ExpireDate], [Description])
    VALUES
        (@ModuleID, N'P7-Screenshot', GETDATE(), N'P7-Test-Announcement-Edit', N'~/DesktopDefault.aspx', N'', DATEADD(day, 90, GETDATE()), N'P7 theme edit-page screenshot sample.');

    SET @ItemId = CONVERT(int, SCOPE_IDENTITY());
END
ELSE
BEGIN
    UPDATE [dbo].[Portal_Announcements]
    SET [MoreLink] = N'~/DesktopDefault.aspx',
        [MobileMoreLink] = N'',
        [ExpireDate] = DATEADD(day, 90, GETDATE()),
        [Description] = N'P7 theme edit-page screenshot sample.'
    WHERE [ItemID] = @ItemId;
END

SELECT @ItemId;
'@ -Configure {
            param($command)
            Add-IntParameter -Command $command -Name '@ModuleID' -Value ([int]$announcementModuleId)
        }

        $targets.Add([pscustomobject]@{
            id = 'edit-announcement'
            title = '公告编辑页'
            url = 'DesktopModules/EditAnnouncements.aspx?ItemID=' + [Convert]::ToString($announcementItemId, [System.Globalization.CultureInfo]::InvariantCulture) + '&mid=' + [Convert]::ToString($announcementModuleId, [System.Globalization.CultureInfo]::InvariantCulture)
        })
    }

    $contactModuleId = Get-FirstModuleIdForDefinition -Connection $Connection -FriendlyName 'Contacts'
    if ($null -ne $contactModuleId) {
        $contactItemId = Invoke-ScalarQuery -Connection $Connection -Sql @'
DECLARE @ItemId int;

SELECT TOP (1) @ItemId = [ItemID]
FROM [dbo].[Portal_Contacts]
WHERE [ModuleID] = @ModuleID
  AND [Name] = N'P7-Test-Contact-Edit';

IF @ItemId IS NULL
BEGIN
    INSERT INTO [dbo].[Portal_Contacts]
        ([ModuleID], [CreatedByUser], [CreatedDate], [Name], [Role], [Email], [Contact1], [Contact2])
    VALUES
        (@ModuleID, N'P7-Screenshot', GETDATE(), N'P7-Test-Contact-Edit', N'Theme Probe', N'p7-contact@example.invalid', N'office: 010-0000-0001', N'mobile: 138-0000-0001');

    SET @ItemId = CONVERT(int, SCOPE_IDENTITY());
END
ELSE
BEGIN
    UPDATE [dbo].[Portal_Contacts]
    SET [Role] = N'Theme Probe',
        [Email] = N'p7-contact@example.invalid',
        [Contact1] = N'office: 010-0000-0001',
        [Contact2] = N'mobile: 138-0000-0001'
    WHERE [ItemID] = @ItemId;
END

SELECT @ItemId;
'@ -Configure {
            param($command)
            Add-IntParameter -Command $command -Name '@ModuleID' -Value ([int]$contactModuleId)
        }

        $targets.Add([pscustomobject]@{
            id = 'edit-contact'
            title = '联系人编辑页'
            url = 'DesktopModules/EditContacts.aspx?ItemID=' + [Convert]::ToString($contactItemId, [System.Globalization.CultureInfo]::InvariantCulture) + '&mid=' + [Convert]::ToString($contactModuleId, [System.Globalization.CultureInfo]::InvariantCulture)
        })
    }

    $documentModuleId = Get-FirstModuleIdForDefinition -Connection $Connection -FriendlyName 'Documents'
    if ($null -ne $documentModuleId) {
        $documentItemId = Invoke-ScalarQuery -Connection $Connection -Sql @'
DECLARE @ItemId int;

SELECT TOP (1) @ItemId = [ItemID]
FROM [dbo].[Portal_Documents]
WHERE [ModuleID] = @ModuleID
  AND [FileFriendlyName] = N'P7-Test-Document-Edit';

IF @ItemId IS NULL
BEGIN
    INSERT INTO [dbo].[Portal_Documents]
        ([ModuleID], [CreatedByUser], [CreatedDate], [FileNameUrl], [FileFriendlyName], [Category], [Content], [ContentType], [ContentSize])
    VALUES
        (@ModuleID, N'P7-Screenshot', GETDATE(), N'~/uploads/sample-under-10mb.json', N'P7-Test-Document-Edit', N'P7 Theme Probe', NULL, NULL, NULL);

    SET @ItemId = CONVERT(int, SCOPE_IDENTITY());
END
ELSE
BEGIN
    UPDATE [dbo].[Portal_Documents]
    SET [FileNameUrl] = N'~/uploads/sample-under-10mb.json',
        [Category] = N'P7 Theme Probe',
        [Content] = NULL,
        [ContentType] = NULL,
        [ContentSize] = NULL
    WHERE [ItemID] = @ItemId;
END

SELECT @ItemId;
'@ -Configure {
            param($command)
            Add-IntParameter -Command $command -Name '@ModuleID' -Value ([int]$documentModuleId)
        }

        $targets.Add([pscustomobject]@{
            id = 'edit-document'
            title = '文档编辑页'
            url = 'DesktopModules/EditDocs.aspx?ItemID=' + [Convert]::ToString($documentItemId, [System.Globalization.CultureInfo]::InvariantCulture) + '&mid=' + [Convert]::ToString($documentModuleId, [System.Globalization.CultureInfo]::InvariantCulture)
        })
    }

    $eventModuleId = Get-FirstModuleIdForDefinition -Connection $Connection -FriendlyName 'Events'
    if ($null -ne $eventModuleId) {
        $eventItemId = Invoke-ScalarQuery -Connection $Connection -Sql @'
DECLARE @ItemId int;

SELECT TOP (1) @ItemId = [ItemID]
FROM [dbo].[Portal_Events]
WHERE [ModuleID] = @ModuleID
  AND [Title] = N'P7-Test-Event-Edit';

IF @ItemId IS NULL
BEGIN
    INSERT INTO [dbo].[Portal_Events]
        ([ModuleID], [CreatedByUser], [CreatedDate], [Title], [WhereWhen], [Description], [ExpireDate])
    VALUES
        (@ModuleID, N'P7-Screenshot', GETDATE(), N'P7-Test-Event-Edit', N'P7 Screenshot Matrix', N'P7 theme edit-page screenshot sample.', DATEADD(day, 90, GETDATE()));

    SET @ItemId = CONVERT(int, SCOPE_IDENTITY());
END
ELSE
BEGIN
    UPDATE [dbo].[Portal_Events]
    SET [WhereWhen] = N'P7 Screenshot Matrix',
        [Description] = N'P7 theme edit-page screenshot sample.',
        [ExpireDate] = DATEADD(day, 90, GETDATE())
    WHERE [ItemID] = @ItemId;
END

SELECT @ItemId;
'@ -Configure {
            param($command)
            Add-IntParameter -Command $command -Name '@ModuleID' -Value ([int]$eventModuleId)
        }

        $targets.Add([pscustomobject]@{
            id = 'edit-event'
            title = '事件编辑页'
            url = 'DesktopModules/EditEvents.aspx?ItemID=' + [Convert]::ToString($eventItemId, [System.Globalization.CultureInfo]::InvariantCulture) + '&mid=' + [Convert]::ToString($eventModuleId, [System.Globalization.CultureInfo]::InvariantCulture)
        })
    }

    $linkModuleId = Get-FirstModuleIdForDefinition -Connection $Connection -FriendlyName 'Links'
    if ($null -ne $linkModuleId) {
        $linkItemId = Invoke-ScalarQuery -Connection $Connection -Sql @'
DECLARE @ItemId int;

SELECT TOP (1) @ItemId = [ItemID]
FROM [dbo].[Portal_Links]
WHERE [ModuleID] = @ModuleID
  AND [Title] = N'P7-Test-Link-Edit';

IF @ItemId IS NULL
BEGIN
    INSERT INTO [dbo].[Portal_Links]
        ([ModuleID], [CreatedByUser], [CreatedDate], [Title], [Url], [MobileUrl], [ViewOrder], [Description])
    VALUES
        (@ModuleID, N'P7-Screenshot', GETDATE(), N'P7-Test-Link-Edit', N'~/DesktopDefault.aspx', N'', 99, N'P7 theme edit-page screenshot sample.');

    SET @ItemId = CONVERT(int, SCOPE_IDENTITY());
END
ELSE
BEGIN
    UPDATE [dbo].[Portal_Links]
    SET [Url] = N'~/DesktopDefault.aspx',
        [MobileUrl] = N'',
        [ViewOrder] = 99,
        [Description] = N'P7 theme edit-page screenshot sample.'
    WHERE [ItemID] = @ItemId;
END

SELECT @ItemId;
'@ -Configure {
            param($command)
            Add-IntParameter -Command $command -Name '@ModuleID' -Value ([int]$linkModuleId)
        }

        $targets.Add([pscustomobject]@{
            id = 'edit-link'
            title = '链接编辑页'
            url = 'DesktopModules/EditLinks.aspx?ItemID=' + [Convert]::ToString($linkItemId, [System.Globalization.CultureInfo]::InvariantCulture) + '&mid=' + [Convert]::ToString($linkModuleId, [System.Globalization.CultureInfo]::InvariantCulture)
        })
    }

    $htmlModuleId = Get-FirstModuleIdForDefinition -Connection $Connection -FriendlyName 'Html Document'
    if ($null -ne $htmlModuleId) {
        $targets.Add([pscustomobject]@{
            id = 'edit-html'
            title = 'HTML 配置页'
            url = 'DesktopModules/EditHtml.aspx?Mid=' + [Convert]::ToString($htmlModuleId, [System.Globalization.CultureInfo]::InvariantCulture)
        })
    }

    $imageModuleId = Get-FirstModuleIdForDefinition -Connection $Connection -FriendlyName 'Image'
    if ($null -ne $imageModuleId) {
        $targets.Add([pscustomobject]@{
            id = 'edit-image'
            title = '图片配置页'
            url = 'DesktopModules/EditImage.aspx?Mid=' + [Convert]::ToString($imageModuleId, [System.Globalization.CultureInfo]::InvariantCulture)
        })
    }

    $xmlModuleId = Get-FirstModuleIdForDefinition -Connection $Connection -FriendlyName 'XML/XSL'
    if ($null -ne $xmlModuleId) {
        $targets.Add([pscustomobject]@{
            id = 'edit-xml'
            title = 'XML 配置页'
            url = 'DesktopModules/EditXml.aspx?Mid=' + [Convert]::ToString($xmlModuleId, [System.Globalization.CultureInfo]::InvariantCulture)
        })
    }

    return $targets
}

# <lang>
#   <zh-CN>读取系统主题设置的可恢复字段，区分不存在和已存在记录供 finally 恢复。</zh-CN>
#   <en>Reads restorable system-theme fields and distinguishes absence from an existing record for finally restoration.</en>
# </lang>
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

# <lang>
#   <zh-CN>使用参数化 upsert 临时设置全局主题，写入固定测试 actor 以便审计。</zh-CN>
#   <en>Temporarily upserts the global theme with parameterized values and a fixed test actor for auditability.</en>
# </lang>
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

# <lang>
#   <zh-CN>根据系统快照恢复或删除临时主题记录，保持失败路径的最小副作用。</zh-CN>
#   <en>Restores or deletes the temporary theme record from its snapshot, keeping failure-path side effects minimal.</en>
# </lang>
function Restore-SystemSettingSnapshot {
    param([System.Data.SqlClient.SqlConnection]$Connection)

    if ($null -eq $settingSnapshot) {
        return
    }

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
#   <zh-CN>以 UTF-8 无 BOM 写入 JSON 证据，确保输出目录产物可被后续工具稳定读取。</zh-CN>
#   <en>Writes JSON evidence as UTF-8 without a BOM so downstream tools read the output directory consistently.</en>
# </lang>
function Write-Utf8NoBomJson {
    param(
        [string]$Path,
        [object]$Value
    )

    $json = $Value | ConvertTo-Json -Depth 8
    [System.IO.File]::WriteAllText($Path, $json, [System.Text.UTF8Encoding]::new($false))
}

# <lang>
#   <zh-CN>从截图目录生成 Contact Sheet，保持视觉复核产物与单张截图同一输出边界。</zh-CN>
#   <en>Generates a Contact Sheet from the screenshot directory, keeping the visual-review artifact within the same output boundary.</en>
# </lang>
function New-ContactSheet {
    param([string]$Directory)

    Add-Type -AssemblyName System.Drawing
    $files = Get-ChildItem -LiteralPath $Directory -Filter '*.png' |
        Where-Object { $_.Name -ne 'contact-sheet.png' } |
        Sort-Object Name

    if ($files.Count -eq 0) {
        return
    }

    $thumbW = 420
    $thumbH = 292
    $captionH = 38
    $cols = 3
    $rows = [Math]::Ceiling($files.Count / $cols)
    $sheetW = $cols * $thumbW
    $sheetH = $rows * ($thumbH + $captionH)
    $bitmap = [System.Drawing.Bitmap]::new($sheetW, $sheetH)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $font = [System.Drawing.Font]::new([System.Drawing.FontFamily]::GenericSansSerif, 9)
    try {
        $graphics.Clear([System.Drawing.Color]::FromArgb(245, 247, 250))
        for ($i = 0; $i -lt $files.Count; $i++) {
            $image = [System.Drawing.Image]::FromFile($files[$i].FullName)
            try {
                $x = ($i % $cols) * $thumbW
                $y = [Math]::Floor($i / $cols) * ($thumbH + $captionH)
                $graphics.FillRectangle([System.Drawing.Brushes]::White, $x, $y, $thumbW, $thumbH + $captionH)
                $graphics.DrawImage($image, $x + 8, $y + 8, $thumbW - 16, $thumbH - 16)
                $graphics.DrawString($files[$i].Name, $font, [System.Drawing.Brushes]::Black, $x + 10, $y + $thumbH + 8)
            }
            finally {
                $image.Dispose()
            }
        }

        $bitmap.Save((Join-Path $Directory 'contact-sheet.png'), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $font.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

# <lang>
#   <zh-CN>生成隔离 Node 截图脚本并注入固定环境变量契约，不在 PowerShell 中执行浏览器逻辑。</zh-CN>
#   <en>Generates the isolated Node capture script and injects a fixed environment contract without executing browser logic in PowerShell.</en>
# </lang>
function Write-NodeCaptureScript {
    param([string]$Path)

    $script = @'
import fs from 'node:fs';
import path from 'node:path';
import { chromium } from 'playwright';

const theme = process.env.P7_THEME_NAME;
const baseUrl = process.env.P7_THEME_BASE_URL;
const outputDir = process.env.P7_THEME_OUTPUT_DIR;
const p64Path = process.env.P7_THEME_P64_CONTEXT;
const p65Path = process.env.P7_THEME_P65_CONTEXT;
const adminUserId = process.env.P7_THEME_ADMIN_USER_ID;
const roleId = process.env.P7_THEME_ROLE_ID;
const moduleDefinitionId = process.env.P7_THEME_MODULE_DEFINITION_ID;
const moduleSettingsModuleId = process.env.P7_THEME_MODULE_SETTINGS_MODULE_ID;
const moduleSettingsTabId = process.env.P7_THEME_MODULE_SETTINGS_TAB_ID;
const tabLayoutTabId = process.env.P7_THEME_TAB_LAYOUT_TAB_ID;
const contentTabsJson = process.env.P7_THEME_CONTENT_TABS || '[]';
const discussionDetailJson = process.env.P7_THEME_DISCUSSION_DETAIL || 'null';
const editPageTargetsJson = process.env.P7_THEME_EDIT_PAGE_TARGETS || '[]';
const legacyAdminTargetsJson = process.env.P7_THEME_LEGACY_ADMIN_TARGETS || '[]';

// <lang>
//   <zh-CN>读取必需的回归 JSON 文件；文件缺失或格式错误直接失败，避免生成无上下文的截图。</zh-CN>
//   <en>Reads required regression JSON files; missing or malformed files fail fast so context-free screenshots are not produced.</en>
// </lang>
function readJson(filePath) {
  return JSON.parse(fs.readFileSync(filePath, 'utf8'));
}

// <lang>
//   <zh-CN>解析可选的环境变量 JSON；异常时使用调用方指定的安全回退值。</zh-CN>
//   <en>Parses optional environment JSON and uses the caller-provided safe fallback when parsing fails.</en>
// </lang>
function readEnvJson(value, fallback) {
  try {
    return JSON.parse(value);
  } catch {
    return fallback;
  }
}

// <lang>
//   <zh-CN>将相对目标解析到固定门户基础 URL，保持截图目标不越出本次运行的站点边界。</zh-CN>
//   <en>Resolves relative targets against the fixed portal base URL so capture navigation stays within this run's site boundary.</en>
// </lang>
function joinUrl(relativeUrl) {
  return new URL(relativeUrl, baseUrl).toString();
}

// <lang>
//   <zh-CN>建立并验证后台登录态；仅对偶发登录未完成重试一次，并清理失败尝试的 Cookie。</zh-CN>
//   <en>Establishes and verifies the admin sign-in state; retries one transient incomplete attempt and clears its cookies.</en>
// </lang>
async function signIn(page, data, userName) {
  for (let attempt = 1; attempt <= 2; attempt++) {
    await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 45000 });
    await page.locator('input[id$="EmailOrName"]').fill(userName);
    await page.locator('input[id$="password"]').fill(data.password);
    await Promise.all([
      page.waitForLoadState('domcontentloaded').catch(() => {}),
      page.locator('input[id$="SigninBtn"]').click()
    ]);
    await page.waitForTimeout(900);

    const bodyText = await page.locator('body').innerText().catch(() => '');
    if (bodyText.includes(`欢迎 ${userName}`) || bodyText.includes('Logoff') || bodyText.includes('注销')) {
      return;
    }

    // <lang>
    //   <zh-CN>后台截图必须基于真实登录态；偶发登录未完成时重试一次，不把拒绝访问页当作目标页。</zh-CN>
    //   <en>Admin screenshots require a verified signed-in state; retry once for transient incomplete sign-in.</en>
    // </lang>
    await page.context().clearCookies().catch(() => {});
    await page.waitForTimeout(800);
  }

  throw new Error(`Sign-in did not complete for ${userName}.`);
}

// <lang>
//   <zh-CN>为一组截图创建隔离浏览器上下文，固定视口、语言和超时，结束时由调用方关闭上下文。</zh-CN>
//   <en>Creates an isolated browser context for a capture group with fixed viewport, locale, and timeout; the caller closes it.</en>
// </lang>
async function openPage(browser) {
  const context = await browser.newContext({
    viewport: { width: 1440, height: 1000 },
    deviceScaleFactor: 1,
    locale: 'zh-CN'
  });
  const page = await context.newPage();
  page.setDefaultTimeout(20000);
  return { context, page };
}

// <lang>
//   <zh-CN>导航到截图目标并仅对瞬时冷启动/阻塞重试一次，持续性错误继续向上传播。</zh-CN>
//   <en>Navigates to a screenshot target and retries only transient cold-start or stall failures once.</en>
// </lang>
async function gotoTarget(page, target) {
  for (let attempt = 1; attempt <= 2; attempt++) {
    try {
      await page.goto(target.url, { waitUntil: 'domcontentloaded', timeout: 45000 });
      return;
    } catch (error) {
      if (attempt === 2) {
        throw error;
      }

      // <lang>
      //   <zh-CN>IIS Express 偶发冷启动或页面阻塞时重试一次，但不吞掉持续性错误。</zh-CN>
      //   <en>Retry once for transient IIS Express/page stalls without hiding persistent failures.</en>
      // </lang>
      await page.goto('about:blank', { waitUntil: 'domcontentloaded', timeout: 10000 }).catch(() => {});
      await page.waitForTimeout(1200);
    }
  }
}

// <lang>
//   <zh-CN>采集单个目标、执行主题/CSS/错误页断言，并把截图写入当前主题目录。</zh-CN>
//   <en>Captures one target, asserts theme/CSS/error-page conditions, and writes the screenshot under the current theme output.</en>
// </lang>
async function capture(page, target) {
  await gotoTarget(page, target);
  if (target.scrollText) {
    const scrollTarget = page.getByText(target.scrollText, { exact: false }).first();
    await scrollTarget.scrollIntoViewIfNeeded({ timeout: 8000 });
    await page.waitForTimeout(350);
  }

  await page.waitForTimeout(900);
  const fileName = `${theme}-${target.id}.png`;
  const filePath = path.join(outputDir, fileName);
  await page.screenshot({ path: filePath, fullPage: false });

  const bodyText = await page.locator('body').innerText().catch(() => '');
  const html = await page.content().catch(() => '');
  if (bodyText.includes('应用程序暂时无法完成请求') || page.url().includes('GenericErrorPage.aspx')) {
    throw new Error('Generic error page detected.');
  }
  // <lang>
  //   <zh-CN>截图回归不能把拒绝访问页误判为目标页正常渲染。</zh-CN>
  //   <en>The screenshot smoke must not treat access-denied fallbacks as successful target renders.</en>
  // </lang>
  if (!target.allowAccessDenied && (bodyText.includes('拒绝编辑') || bodyText.includes('访问被拒绝') ||
      page.url().includes('AccessDenied.aspx') || page.url().includes('EditAccessDenied.aspx'))) {
    throw new Error('Access denied page detected.');
  }

  const expectedThemeClass = `portal-theme-${theme.toLowerCase()}`;
  if (!html.toLowerCase().includes(expectedThemeClass)) {
    throw new Error(`Expected ${expectedThemeClass} in body class.`);
  }

  if (!html.includes(`App_Themes/${theme}/Default.css`)) {
    throw new Error(`Expected App_Themes/${theme}/Default.css in page output.`);
  }

  return fileName;
}

// <lang>
//   <zh-CN>读取上下文、解析环境目标并创建输出目录；缺少可选上下文时保持空目标集合。</zh-CN>
//   <en>Loads contexts, parses target JSON, and creates the output directory; missing optional contexts yield empty target groups.</en>
// </lang>
const p64 = fs.existsSync(p64Path) ? readJson(p64Path) : null;
const p65 = fs.existsSync(p65Path) ? readJson(p65Path) : null;
const contentTabs = readEnvJson(contentTabsJson, []);
const discussionDetail = readEnvJson(discussionDetailJson, null);
const editPageTargets = readEnvJson(editPageTargetsJson, []);
const legacyAdminTargets = readEnvJson(legacyAdminTargetsJson, []);
fs.mkdirSync(outputDir, { recursive: true });

// <lang>
//   <zh-CN>按匿名、后台和绑定用户三类权限边界构造稳定的截图目标索引。</zh-CN>
//   <en>Builds a stable screenshot index across anonymous, admin, and bound-user permission boundaries.</en>
// </lang>
const anonymousTargets = [
  { id: 'home-anonymous', title: '匿名首页', role: 'anonymous', url: joinUrl('DesktopDefault.aspx') },
  { id: 'signin', title: '登录模块', role: 'anonymous', url: joinUrl('DesktopDefault.aspx?tabindex=0&tabid=0') }
];

for (const tab of contentTabs) {
  const id = `tab-${String(tab.tabName || '').toLowerCase().replace(/[^a-z0-9]+/g, '-')}`.replace(/-$/g, '');
  anonymousTargets.push({
    id,
    title: `${tab.tabName} 内容页`,
    role: 'anonymous',
    url: joinUrl(`DesktopDefault.aspx?tabindex=${encodeURIComponent(tab.tabIndex)}&tabid=${encodeURIComponent(tab.tabId)}`)
  });
}

anonymousTargets.push(
  { id: 'access-denied', title: '访问拒绝页', role: 'anonymous', url: joinUrl('Admin/AccessDenied.aspx'), allowAccessDenied: true },
  { id: 'edit-access-denied', title: '编辑拒绝页', role: 'anonymous', url: joinUrl('Admin/EditAccessDenied.aspx'), allowAccessDenied: true },
  { id: 'not-implemented', title: '未实现提示页', role: 'anonymous', url: joinUrl('Admin/NotImplemented.aspx?title=P7%20Theme%20Probe') }
);

if (discussionDetail?.itemId && discussionDetail?.moduleId) {
  anonymousTargets.push({
    id: 'discussion-detail',
    title: '讨论详情页',
    role: 'anonymous',
    url: joinUrl(`DesktopModules/DiscussDetails.aspx?ItemID=${encodeURIComponent(discussionDetail.itemId)}&mid=${encodeURIComponent(discussionDetail.moduleId)}`)
  });
}

if (p64?.tabUrl) {
  anonymousTargets.push({ id: 'p64-confirm-anonymous', title: '员工资料确认匿名态', role: 'anonymous', url: p64.tabUrl });
}

if (p65?.tabUrl) {
  anonymousTargets.push({ id: 'p65-correction-anonymous', title: '员工资料更正匿名态', role: 'anonymous', url: p65.tabUrl });
}

const adminTargets = [
  { id: 'admin-employee-directory', title: '员工目录后台', role: 'admin', url: joinUrl('Admin/EmployeeDirectory.aspx') },
  { id: 'admin-employee-edit-new', title: '新增员工后台', role: 'admin', url: joinUrl('Admin/EmployeeEdit.aspx') },
  { id: 'admin-organization-edit-new', title: '新增组织后台', role: 'admin', url: joinUrl('Admin/OrganizationUnitEdit.aspx') },
  { id: 'admin-user-employee-binding-new', title: '账号员工绑定后台', role: 'admin', url: joinUrl('Admin/UserEmployeeBindingEdit.aspx') },
  { id: 'admin-operation-audits', title: '运营审计后台', role: 'admin', url: joinUrl('Admin/OperationAudits.aspx') },
  { id: 'admin-system-health', title: '系统健康后台', role: 'admin', url: joinUrl('Admin/SystemHealth.aspx') },
  { id: 'admin-diagnostics-logs', title: '诊断日志后台', role: 'admin', url: joinUrl('Admin/DiagnosticsLogs.aspx') },
  { id: 'admin-diagnostic-log-detail', title: '诊断日志详情', role: 'admin', url: joinUrl('Admin/DiagnosticLogDetail.aspx?id=P7-Screenshot-Probe') },
  { id: 'admin-theme-settings', title: '主题设置后台', role: 'admin', url: joinUrl('Admin/ThemeSettings.aspx') },
  { id: 'admin-module-catalog', title: '模块目录后台', role: 'admin', url: joinUrl('Admin/ModuleCatalog.aspx') },
  { id: 'admin-correction-requests', title: '员工更正请求后台', role: 'admin', url: joinUrl('Admin/EmployeeProfileCorrectionRequests.aspx') }
];

if (roleId) {
  adminTargets.push({ id: 'admin-security-roles', title: '安全角色后台', role: 'admin', url: joinUrl(`Admin/SecurityRoles.aspx?roleid=${encodeURIComponent(roleId)}`) });
}

if (adminUserId) {
  adminTargets.push({ id: 'admin-manage-users', title: '管理用户后台', role: 'admin', url: joinUrl(`Admin/ManageUsers.aspx?userId=${encodeURIComponent(adminUserId)}`) });
}

if (moduleDefinitionId) {
  adminTargets.push({ id: 'admin-module-definitions', title: '模块定义编辑后台', role: 'admin', url: joinUrl(`Admin/ModuleDefinitions.aspx?defid=${encodeURIComponent(moduleDefinitionId)}`) });
}

if (moduleSettingsModuleId && moduleSettingsTabId) {
  adminTargets.push({ id: 'admin-module-settings', title: '模块实例设置后台', role: 'admin', url: joinUrl(`Admin/ModuleSettings.aspx?mid=${encodeURIComponent(moduleSettingsModuleId)}&tabid=${encodeURIComponent(moduleSettingsTabId)}`) });
}

if (tabLayoutTabId) {
  adminTargets.push({ id: 'admin-tab-layout', title: 'Tab 布局后台', role: 'admin', url: joinUrl(`Admin/TabLayout.aspx?tabid=${encodeURIComponent(tabLayoutTabId)}`) });
}

for (const target of editPageTargets) {
  if (target?.id && target?.url) {
    adminTargets.push({
      id: target.id,
      title: target.title || target.id,
      role: 'admin',
      url: joinUrl(target.url)
    });
  }
}

for (const target of legacyAdminTargets) {
  if (target?.id && target?.url) {
    adminTargets.push({
      id: target.id,
      title: target.title || target.id,
      role: 'admin',
      url: joinUrl(target.url),
      scrollText: target.scrollText || ''
    });
  }
}

const boundTargets = [];
if (p64?.tabUrl) {
  boundTargets.push({ id: 'p64-confirm-bound', title: '员工资料确认绑定用户态', role: 'bound-user', url: p64.tabUrl, data: p64, userName: p64.boundUserName });
}
if (p65?.tabUrl) {
  boundTargets.push({ id: 'p65-correction-bound', title: '员工资料更正绑定用户态', role: 'bound-user', url: p65.tabUrl, data: p65, userName: p65.boundUserName });
}

// <lang>
//   <zh-CN>由本脚本统一持有浏览器生命周期，确保所有截图组完成后再关闭实例。</zh-CN>
//   <en>The script owns the browser lifetime and closes it only after all capture groups finish.</en>
// </lang>
const browser = await chromium.launch({ headless: true });
const results = [];

// <lang>
//   <zh-CN>摘要文件只记录截图索引字段，避免把登录密码等上下文写入 WorkZone。</zh-CN>
//   <en>Summary output keeps only screenshot index fields and never serializes sign-in context.</en>
// </lang>
function createCaptureResult(target, fileName, status, detail) {
  return {
    theme,
    id: target.id,
    title: target.title,
    role: target.role,
    url: target.url,
    fileName: fileName || '',
    status,
    detail: detail || ''
  };
}

// <lang>
//   <zh-CN>在隔离上下文中按组执行登录和目标采集，并在 finally 中释放上下文。</zh-CN>
//   <en>Runs sign-in and target capture within an isolated context and releases that context in finally.</en>
// </lang>
async function runCaptureGroup(targets, signedIn) {
  const { context, page } = await openPage(browser);
  try {
    if (signedIn) {
      await signIn(page, signedIn.data, signedIn.userName);
    }

    for (const target of targets) {
      try {
        const fileName = await capture(page, target);
        results.push(createCaptureResult(target, fileName, 'Pass', ''));
      } catch (error) {
        results.push(createCaptureResult(target, '', 'Fail', error instanceof Error ? error.message : String(error)));
      }
    }
  } finally {
    await context.close();
  }
}

// <lang>
//   <zh-CN>按匿名、后台和绑定用户顺序编排目标组；每个失败目标进入摘要而不阻断同组后续目标。</zh-CN>
//   <en>Runs anonymous, admin, and bound-user groups in order; a failed target is recorded without stopping later targets in its group.</en>
// </lang>
try {
  await runCaptureGroup(anonymousTargets, null);
  if (p65?.adminUserName) {
    for (const target of adminTargets) {
      await runCaptureGroup([target], { data: p65, userName: p65.adminUserName });
    }
  }
  for (const target of boundTargets) {
    await runCaptureGroup([target], { data: target.data, userName: target.userName });
  }
} finally {
  // <lang>
  //   <zh-CN>浏览器无论采集成功与否都必须关闭，避免残留进程影响后续复核。</zh-CN>
  //   <en>Closes the browser regardless of capture success so no process remains for later review runs.</en>
  // </lang>
  await browser.close();
}

// <lang>
//   <zh-CN>仅输出低敏感度截图索引，并通过退出码向 PowerShell 汇报失败。</zh-CN>
//   <en>Outputs only the low-sensitivity screenshot index and reports failures through the process exit code.</en>
// </lang>
console.log(JSON.stringify(results, null, 2));
if (results.some(item => item.status !== 'Pass')) {
  process.exitCode = 1;
}
'@

    [System.IO.File]::WriteAllText($Path, $script, [System.Text.UTF8Encoding]::new($false))
}

# <lang>
#   <zh-CN>执行前只检查本地 Playwright 依赖是否存在；本检查本身不启动 Node 或浏览器。</zh-CN>
#   <en>Before execution, checks only for the local Playwright dependency; this check does not start Node or a browser.</en>
# </lang>
if (-not (Test-Path -LiteralPath (Join-Path $repoRoot 'temp\node_modules\playwright') -PathType Container)) {
    throw 'Playwright is not available under temp\node_modules. Run an existing Playwright setup or create the local junction before capturing screenshots.'
}

# <lang>
#   <zh-CN>准备截图输出目录和临时运行时脚本路径；运行时脚本由同一版本的编排函数生成。</zh-CN>
#   <en>Prepares the screenshot output directory and temporary runtime script path generated by this orchestration version.</en>
# </lang>
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$runtimeDir = Join-Path $repoRoot 'temp\p7'
New-Item -ItemType Directory -Path $runtimeDir -Force | Out-Null
$runtimeScript = Join-Path $runtimeDir 'Capture-PortalThemeScreenshots.runtime.mjs'
Write-NodeCaptureScript -Path $runtimeScript

# <lang>
#   <zh-CN>建立低敏感度结果集合，并读取外置连接串；连接串只进入内存，不写入日志。</zh-CN>
#   <en>Creates the low-sensitivity result collection and reads the external connection string into memory only.</en>
# </lang>
$summary = New-Object 'System.Collections.Generic.List[object]'
$connectionString = Get-ExternalPortalConnectionString -Path $ConnectionStringsConfigPath

# <lang>
#   <zh-CN>打开数据库后一次性发现主题快照、权限目标和页面 ID，随后按主题循环驱动独立 Node 子进程。</zh-CN>
#   <en>After opening the database, discovers the theme snapshot, permission targets, and page IDs once, then drives one Node child process per theme.</en>
# </lang>
try {
    $connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
    $connection.Open()
    $settingSnapshot = Get-SystemSettingSnapshot -Connection $connection
    $contentTabTargets = Get-ContentTabTargets -Connection $connection
    $legacyAdminTargets = Get-LegacyAdminModuleTargets -Connection $connection
    $discussionDetailTarget = Get-DiscussionDetailTarget -Connection $connection
    $editPageTargets = Get-OrCreateEditPageTargets -Connection $connection
    $p65Context = Get-Content -LiteralPath $P65ContextPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $adminUserId = if ($p65Context.adminUserName) {
        Invoke-ScalarQuery -Connection $connection -Sql @'
SELECT TOP (1) [UserID]
FROM [dbo].[Portal_Users]
WHERE [Name] = @UserName
'@ -Configure {
            param($command)
            Add-TextParameter -Command $command -Name '@UserName' -Size 100 -Value $p65Context.adminUserName
        }
    }
    else {
        $null
    }
    $roleId = Invoke-ScalarQuery -Connection $connection -Sql @'
SELECT TOP (1) [RoleID]
FROM [dbo].[Portal_Roles]
WHERE [RoleID] > 0
ORDER BY CASE WHEN [RoleName] = N'TestRole' THEN 0 ELSE 1 END, [RoleID]
'@ -Configure {
        param($command)
    }
    $canCaptureModuleDefinitionPage = Test-PortalUserPermission -Connection $connection -UserName $p65Context.adminUserName -PermissionKey 'Module.Definition.Edit'
    $moduleDefinitionId = if ($canCaptureModuleDefinitionPage) {
        Invoke-ScalarQuery -Connection $connection -Sql @'
SELECT TOP (1) [ModuleDefId]
FROM [dbo].[PortalCfg_ModuleDefinitions]
ORDER BY [ModuleDefId]
'@ -Configure {
            param($command)
        }
    }
    else {
        $null
    }
    $moduleSettingsProbe = Invoke-ScalarQuery -Connection $connection -Sql @'
SELECT TOP (1)
    CONVERT(nvarchar(20), [ModuleId]) + N'|' + CONVERT(nvarchar(20), [TabId])
FROM [dbo].[PortalCfg_Modules]
WHERE [ModuleId] > 0
  AND [TabId] IS NOT NULL
  AND [TabId] > 0
ORDER BY [TabId], [ModuleOrder], [ModuleId]
'@ -Configure {
        param($command)
    }
    $moduleSettingsModuleId = $null
    $moduleSettingsTabId = $null
    if (-not [string]::IsNullOrWhiteSpace($moduleSettingsProbe)) {
        $parts = ([string]$moduleSettingsProbe).Split('|')
        if ($parts.Length -eq 2) {
            $moduleSettingsModuleId = $parts[0]
            $moduleSettingsTabId = $parts[1]
        }
    }
    $tabLayoutTabId = Invoke-ScalarQuery -Connection $connection -Sql @'
SELECT TOP (1) [TabId]
FROM [dbo].[PortalCfg_Tabs]
WHERE [TabId] > 0
ORDER BY CASE WHEN [TabName] = N'Home' THEN 0 ELSE 1 END, [TabOrder], [TabId]
'@ -Configure {
        param($command)
    }

# <lang>
#   <zh-CN>每个主题先写入临时设置，再通过固定环境变量传递已发现目标；子进程结果只解析为摘要对象。</zh-CN>
#   <en>For each theme, applies the temporary setting, passes discovered targets through fixed environment variables, and parses child output only as summary objects.</en>
# </lang>
    foreach ($theme in $Themes) {
        Write-Host ("[INFO] Capturing theme {0}" -f $theme)
        Set-GlobalTheme -Connection $connection -ThemeName $theme

        $env:P7_THEME_NAME = $theme
        $env:P7_THEME_BASE_URL = $BaseUrl
        $env:P7_THEME_OUTPUT_DIR = (Resolve-Path -LiteralPath $OutputDirectory).Path
        $env:P7_THEME_P64_CONTEXT = (Resolve-Path -LiteralPath $P64ContextPath).Path
        $env:P7_THEME_P65_CONTEXT = (Resolve-Path -LiteralPath $P65ContextPath).Path
        $env:P7_THEME_ADMIN_USER_ID = if ($null -eq $adminUserId) { '' } else { [Convert]::ToString($adminUserId, [System.Globalization.CultureInfo]::InvariantCulture) }
        $env:P7_THEME_ROLE_ID = if ($null -eq $roleId) { '' } else { [Convert]::ToString($roleId, [System.Globalization.CultureInfo]::InvariantCulture) }
        $env:P7_THEME_MODULE_DEFINITION_ID = if ($null -eq $moduleDefinitionId) { '' } else { [Convert]::ToString($moduleDefinitionId, [System.Globalization.CultureInfo]::InvariantCulture) }
        $env:P7_THEME_MODULE_SETTINGS_MODULE_ID = if ($null -eq $moduleSettingsModuleId) { '' } else { [string]$moduleSettingsModuleId }
        $env:P7_THEME_MODULE_SETTINGS_TAB_ID = if ($null -eq $moduleSettingsTabId) { '' } else { [string]$moduleSettingsTabId }
        $env:P7_THEME_TAB_LAYOUT_TAB_ID = if ($null -eq $tabLayoutTabId) { '' } else { [Convert]::ToString($tabLayoutTabId, [System.Globalization.CultureInfo]::InvariantCulture) }
        $env:P7_THEME_CONTENT_TABS = if ($contentTabTargets.Count -eq 0) { '[]' } else { $contentTabTargets | ConvertTo-Json -Compress }
        $env:P7_THEME_LEGACY_ADMIN_TARGETS = if ($legacyAdminTargets.Count -eq 0) { '[]' } else { $legacyAdminTargets | ConvertTo-Json -Compress }
        $env:P7_THEME_DISCUSSION_DETAIL = if ($null -eq $discussionDetailTarget) { 'null' } else { $discussionDetailTarget | ConvertTo-Json -Compress }
        $env:P7_THEME_EDIT_PAGE_TARGETS = if ($editPageTargets.Count -eq 0) { '[]' } else { $editPageTargets | ConvertTo-Json -Compress }

        $nodeOutput = & node $runtimeScript
        if ($LASTEXITCODE -ne 0) {
            $nodeOutput | Write-Host
            throw "Screenshot capture failed for theme $theme."
        }

        $jsonText = $nodeOutput -join [Environment]::NewLine
        ($jsonText | ConvertFrom-Json) | ForEach-Object { $summary.Add($_) }
    }
}
finally {
# <lang>
#   <zh-CN>无论 Node、解析或后续断言是否失败，都清理本轮环境变量并恢复主题设置、释放连接。</zh-CN>
#   <en>Regardless of Node, parsing, or assertion failures, clears run-scoped environment variables, restores the theme, and releases the connection.</en>
# </lang>
    Remove-Item Env:P7_THEME_NAME -ErrorAction SilentlyContinue
    Remove-Item Env:P7_THEME_BASE_URL -ErrorAction SilentlyContinue
    Remove-Item Env:P7_THEME_OUTPUT_DIR -ErrorAction SilentlyContinue
    Remove-Item Env:P7_THEME_P64_CONTEXT -ErrorAction SilentlyContinue
    Remove-Item Env:P7_THEME_P65_CONTEXT -ErrorAction SilentlyContinue
    Remove-Item Env:P7_THEME_ADMIN_USER_ID -ErrorAction SilentlyContinue
    Remove-Item Env:P7_THEME_ROLE_ID -ErrorAction SilentlyContinue
    Remove-Item Env:P7_THEME_MODULE_DEFINITION_ID -ErrorAction SilentlyContinue
    Remove-Item Env:P7_THEME_MODULE_SETTINGS_MODULE_ID -ErrorAction SilentlyContinue
    Remove-Item Env:P7_THEME_MODULE_SETTINGS_TAB_ID -ErrorAction SilentlyContinue
    Remove-Item Env:P7_THEME_TAB_LAYOUT_TAB_ID -ErrorAction SilentlyContinue
    Remove-Item Env:P7_THEME_CONTENT_TABS -ErrorAction SilentlyContinue
    Remove-Item Env:P7_THEME_LEGACY_ADMIN_TARGETS -ErrorAction SilentlyContinue
    Remove-Item Env:P7_THEME_DISCUSSION_DETAIL -ErrorAction SilentlyContinue
    Remove-Item Env:P7_THEME_EDIT_PAGE_TARGETS -ErrorAction SilentlyContinue

    if ($connection) {
        if ($connection.State -eq [System.Data.ConnectionState]::Open) {
            Restore-SystemSettingSnapshot -Connection $connection
            Write-Host '[PASS] Theme setting restored.'
        }

        $connection.Dispose()
    }
}

# <lang>
#   <zh-CN>成功完成数据库清理后写入摘要和 Contact Sheet；失败目标在最终阶段统一报告。</zh-CN>
#   <en>After database cleanup succeeds, writes the summary and contact sheet; failed targets are reported together at the end.</en>
# </lang>
Write-Utf8NoBomJson -Path (Join-Path $OutputDirectory 'screenshot-summary.json') -Value $summary
New-ContactSheet -Directory $OutputDirectory

$failed = @($summary | Where-Object { $_.status -ne 'Pass' })
if ($failed.Count -gt 0) {
    $failed | Format-Table theme, id, status, detail -AutoSize
    throw ("{0} theme screenshot checks failed." -f $failed.Count)
}

Write-Host ("[PASS] Captured {0} screenshots for {1} themes." -f $summary.Count, $Themes.Count)
