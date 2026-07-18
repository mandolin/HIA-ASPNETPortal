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

function Get-ExternalPortalConnectionString {
    param([string]$Path)

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

function Add-BitParameter {
    param(
        [System.Data.SqlClient.SqlCommand]$Command,
        [string]$Name,
        [bool]$Value
    )

    $parameter = $Command.Parameters.Add($Name, [System.Data.SqlDbType]::Bit)
    $parameter.Value = $Value
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

function Add-DateTime2Parameter {
    param(
        [System.Data.SqlClient.SqlCommand]$Command,
        [string]$Name,
        [DateTime]$Value
    )

    $parameter = $Command.Parameters.Add($Name, [System.Data.SqlDbType]::DateTime2)
    $parameter.Value = $Value
}

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

    return $targets
}

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

function Write-Utf8NoBomJson {
    param(
        [string]$Path,
        [object]$Value
    )

    $json = $Value | ConvertTo-Json -Depth 8
    [System.IO.File]::WriteAllText($Path, $json, [System.Text.UTF8Encoding]::new($false))
}

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

function readJson(filePath) {
  return JSON.parse(fs.readFileSync(filePath, 'utf8'));
}

function readEnvJson(value, fallback) {
  try {
    return JSON.parse(value);
  } catch {
    return fallback;
  }
}

function joinUrl(relativeUrl) {
  return new URL(relativeUrl, baseUrl).toString();
}

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

    // 中文：后台截图必须基于真实登录态；偶发登录未完成时重试一次，不把拒绝访问页当作目标页。
    // English: Admin screenshots require a verified signed-in state; retry once for transient incomplete sign-in.
    await page.context().clearCookies().catch(() => {});
    await page.waitForTimeout(800);
  }

  throw new Error(`Sign-in did not complete for ${userName}.`);
}

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

async function gotoTarget(page, target) {
  for (let attempt = 1; attempt <= 2; attempt++) {
    try {
      await page.goto(target.url, { waitUntil: 'domcontentloaded', timeout: 45000 });
      return;
    } catch (error) {
      if (attempt === 2) {
        throw error;
      }

      // 中文：IIS Express 偶发冷启动或页面阻塞时重试一次，但不吞掉持续性错误。
      // English: Retry once for transient IIS Express/page stalls without hiding persistent failures.
      await page.goto('about:blank', { waitUntil: 'domcontentloaded', timeout: 10000 }).catch(() => {});
      await page.waitForTimeout(1200);
    }
  }
}

async function capture(page, target) {
  await gotoTarget(page, target);
  await page.waitForTimeout(900);
  const fileName = `${theme}-${target.id}.png`;
  const filePath = path.join(outputDir, fileName);
  await page.screenshot({ path: filePath, fullPage: false });

  const bodyText = await page.locator('body').innerText().catch(() => '');
  const html = await page.content().catch(() => '');
  if (bodyText.includes('应用程序暂时无法完成请求') || page.url().includes('GenericErrorPage.aspx')) {
    throw new Error('Generic error page detected.');
  }
  // 中文：截图回归不能把拒绝访问页误判为目标页正常渲染。
  // English: The screenshot smoke must not treat access-denied fallbacks as successful target renders.
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

const p64 = fs.existsSync(p64Path) ? readJson(p64Path) : null;
const p65 = fs.existsSync(p65Path) ? readJson(p65Path) : null;
const contentTabs = readEnvJson(contentTabsJson, []);
const discussionDetail = readEnvJson(discussionDetailJson, null);
const editPageTargets = readEnvJson(editPageTargetsJson, []);
fs.mkdirSync(outputDir, { recursive: true });

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

const boundTargets = [];
if (p64?.tabUrl) {
  boundTargets.push({ id: 'p64-confirm-bound', title: '员工资料确认绑定用户态', role: 'bound-user', url: p64.tabUrl, data: p64, userName: p64.boundUserName });
}
if (p65?.tabUrl) {
  boundTargets.push({ id: 'p65-correction-bound', title: '员工资料更正绑定用户态', role: 'bound-user', url: p65.tabUrl, data: p65, userName: p65.boundUserName });
}

const browser = await chromium.launch({ headless: true });
const results = [];

// 中文：摘要文件只记录截图索引字段，避免把登录密码等上下文写入 WorkZone。
// English: Summary output keeps only screenshot index fields and never serializes sign-in context.
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
  await browser.close();
}

console.log(JSON.stringify(results, null, 2));
if (results.some(item => item.status !== 'Pass')) {
  process.exitCode = 1;
}
'@

    [System.IO.File]::WriteAllText($Path, $script, [System.Text.UTF8Encoding]::new($false))
}

if (-not (Test-Path -LiteralPath (Join-Path $repoRoot 'temp\node_modules\playwright') -PathType Container)) {
    throw 'Playwright is not available under temp\node_modules. Run an existing Playwright setup or create the local junction before capturing screenshots.'
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$runtimeDir = Join-Path $repoRoot 'temp\p7'
New-Item -ItemType Directory -Path $runtimeDir -Force | Out-Null
$runtimeScript = Join-Path $runtimeDir 'Capture-PortalThemeScreenshots.runtime.mjs'
Write-NodeCaptureScript -Path $runtimeScript

$summary = New-Object 'System.Collections.Generic.List[object]'
$connectionString = Get-ExternalPortalConnectionString -Path $ConnectionStringsConfigPath

try {
    $connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
    $connection.Open()
    $settingSnapshot = Get-SystemSettingSnapshot -Connection $connection
    $contentTabTargets = Get-ContentTabTargets -Connection $connection
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
    $moduleDefinitionId = Invoke-ScalarQuery -Connection $connection -Sql @'
SELECT TOP (1) [ModuleDefId]
FROM [dbo].[PortalCfg_ModuleDefinitions]
ORDER BY [ModuleDefId]
'@ -Configure {
        param($command)
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

Write-Utf8NoBomJson -Path (Join-Path $OutputDirectory 'screenshot-summary.json') -Value $summary
New-ContactSheet -Directory $OutputDirectory

$failed = @($summary | Where-Object { $_.status -ne 'Pass' })
if ($failed.Count -gt 0) {
    $failed | Format-Table theme, id, status, detail -AutoSize
    throw ("{0} theme screenshot checks failed." -f $failed.Count)
}

Write-Host ("[PASS] Captured {0} screenshots for {1} themes." -f $summary.Count, $Themes.Count)
