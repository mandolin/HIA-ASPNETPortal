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

function readJson(filePath) {
  return JSON.parse(fs.readFileSync(filePath, 'utf8'));
}

function joinUrl(relativeUrl) {
  return new URL(relativeUrl, baseUrl).toString();
}

async function signIn(page, data, userName) {
  await page.goto(baseUrl, { waitUntil: 'domcontentloaded' });
  await page.locator('input[id$="EmailOrName"]').fill(userName);
  await page.locator('input[id$="password"]').fill(data.password);
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('input[id$="SigninBtn"]').click()
  ]);
  await page.waitForTimeout(600);
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

async function capture(page, target) {
  await page.goto(target.url, { waitUntil: 'domcontentloaded', timeout: 45000 });
  await page.waitForTimeout(900);
  const fileName = `${theme}-${target.id}.png`;
  const filePath = path.join(outputDir, fileName);
  await page.screenshot({ path: filePath, fullPage: false });

  const bodyText = await page.locator('body').innerText().catch(() => '');
  const html = await page.content().catch(() => '');
  if (bodyText.includes('应用程序暂时无法完成请求') || page.url().includes('GenericErrorPage.aspx')) {
    throw new Error('Generic error page detected.');
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
fs.mkdirSync(outputDir, { recursive: true });

const anonymousTargets = [
  { id: 'home-anonymous', title: '匿名首页', role: 'anonymous', url: joinUrl('DesktopDefault.aspx') },
  { id: 'signin', title: '登录模块', role: 'anonymous', url: joinUrl('DesktopDefault.aspx?tabindex=0&tabid=0') }
];

if (p64?.tabUrl) {
  anonymousTargets.push({ id: 'p64-confirm-anonymous', title: '员工资料确认匿名态', role: 'anonymous', url: p64.tabUrl });
}

if (p65?.tabUrl) {
  anonymousTargets.push({ id: 'p65-correction-anonymous', title: '员工资料更正匿名态', role: 'anonymous', url: p65.tabUrl });
}

const adminTargets = [
  { id: 'admin-employee-directory', title: '员工目录后台', role: 'admin', url: joinUrl('Admin/EmployeeDirectory.aspx') },
  { id: 'admin-operation-audits', title: '运营审计后台', role: 'admin', url: joinUrl('Admin/OperationAudits.aspx') },
  { id: 'admin-system-health', title: '系统健康后台', role: 'admin', url: joinUrl('Admin/SystemHealth.aspx') },
  { id: 'admin-theme-settings', title: '主题设置后台', role: 'admin', url: joinUrl('Admin/ThemeSettings.aspx') },
  { id: 'admin-module-catalog', title: '模块目录后台', role: 'admin', url: joinUrl('Admin/ModuleCatalog.aspx') },
  { id: 'admin-correction-requests', title: '员工更正请求后台', role: 'admin', url: joinUrl('Admin/EmployeeProfileCorrectionRequests.aspx') }
];

const boundTargets = [];
if (p64?.tabUrl) {
  boundTargets.push({ id: 'p64-confirm-bound', title: '员工资料确认绑定用户态', role: 'bound-user', url: p64.tabUrl, data: p64, userName: p64.boundUserName });
}
if (p65?.tabUrl) {
  boundTargets.push({ id: 'p65-correction-bound', title: '员工资料更正绑定用户态', role: 'bound-user', url: p65.tabUrl, data: p65, userName: p65.boundUserName });
}

const browser = await chromium.launch({ headless: true });
const results = [];

async function runCaptureGroup(targets, signedIn) {
  const { context, page } = await openPage(browser);
  try {
    if (signedIn) {
      await signIn(page, signedIn.data, signedIn.userName);
    }

    for (const target of targets) {
      try {
        const fileName = await capture(page, target);
        results.push({ theme, ...target, fileName, status: 'Pass', detail: '' });
      } catch (error) {
        results.push({ theme, ...target, status: 'Fail', detail: error instanceof Error ? error.message : String(error) });
      }
    }
  } finally {
    await context.close();
  }
}

try {
  await runCaptureGroup(anonymousTargets, null);
  if (p65?.adminUserName) {
    await runCaptureGroup(adminTargets, { data: p65, userName: p65.adminUserName });
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

    foreach ($theme in $Themes) {
        Write-Host ("[INFO] Capturing theme {0}" -f $theme)
        Set-GlobalTheme -Connection $connection -ThemeName $theme

        $env:P7_THEME_NAME = $theme
        $env:P7_THEME_BASE_URL = $BaseUrl
        $env:P7_THEME_OUTPUT_DIR = (Resolve-Path -LiteralPath $OutputDirectory).Path
        $env:P7_THEME_P64_CONTEXT = (Resolve-Path -LiteralPath $P64ContextPath).Path
        $env:P7_THEME_P65_CONTEXT = (Resolve-Path -LiteralPath $P65ContextPath).Path

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
