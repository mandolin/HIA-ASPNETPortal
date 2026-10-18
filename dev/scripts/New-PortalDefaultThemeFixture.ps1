#requires -Version 7.0

<#
.SYNOPSIS
<lang>
  <zh-CN>默认主题视觉基线门禁的临时主题 fixture：把门户主题临时切到 `Default`（回退主题）并在结束后恢复原值。</zh-CN>
  <en>Temporary theme fixture for the default-theme visual baseline gate: switches the portal theme to `Default` (the fallback theme) and restores the previous value afterwards.</en>
</lang>
.DESCRIPTION
<lang>
  <zh-CN>`C-anp-P27`（`W-anp-P149`）交付：主题**只能**由 Tab 覆盖 → 数据库运行设置 → appSettings → Default 决定
  （`PortalThemeResolver` 源码明确"不接受 URL 覆盖"），因此要拿到 `Default` 回退主题的运行期证据，
  唯一可靠途径是**真实切换主题设置**。本脚本即为该临时状态变更，遵循与 `Test-PortalThemeResolution.ps1` 相同的口径：
  ① 只从外置 `connectionStrings.config` 读取 Portal 连接串（不回显正文）；② 变更前快照
  `[dbo].[PortalCfg_SystemSettings]` 中 `Portal.Theme.Name` 行的全部字段（含审计字段），
  快照落 `temp\p149\theme-setting-snapshot.json`（gitignore）；③ 参数化 SQL upsert，不做字符串拼接；
  ④ `-Action Remove` 按快照恢复（原行不存在则删除，避免留下人工痕迹）。

  只应针对**可丢弃的开发数据库**运行（与既有主题 proof / 截图工具同一约束）。

  用法（仓库根目录）：
    & 'dev\scripts\New-PortalDefaultThemeFixture.ps1' -Action Apply
    & 'dev\scripts\New-PortalDefaultThemeFixture.ps1' -Action Remove
  退出码：0 成功；1 失败（连接串缺失、SQL 失败、快照缺失等）。
  </zh-CN>
  <en>`C-anp-P27` (`W-anp-P149`) deliverable: a theme can be selected **only** by tab override → database runtime setting →
  appSettings → Default (`PortalThemeResolver` states explicitly that it "does not accept theme overrides from a URL"), so the only
  reliable way to obtain runtime evidence for the `Default` fallback theme is to **actually switch the theme setting**. This script
  performs that temporary state change following the same conventions as `Test-PortalThemeResolution.ps1`: ① read the Portal
  connection string only from the external `connectionStrings.config` (never echoing its text); ② snapshot every column of the
  `Portal.Theme.Name` row in `[dbo].[PortalCfg_SystemSettings]` (audit columns included) into
  `temp\p149\theme-setting-snapshot.json` (gitignored); ③ parameterized SQL upsert, no string concatenation; ④ `-Action Remove`
  restores from the snapshot (deleting the row when it did not exist originally, so no manual trace is left).

  Run it only against **disposable development databases** (the same constraint as the existing theme proof and screenshot tools).

  Usage (repository root):
    & 'dev\scripts\New-PortalDefaultThemeFixture.ps1' -Action Apply
    & 'dev\scripts\New-PortalDefaultThemeFixture.ps1' -Action Remove
  Exit codes: 0 success; 1 failure (missing connection string, SQL failure, missing snapshot, ...).
  </en>
</lang>
.EXAMPLE
<lang>
  <zh-CN>& 'dev\scripts\New-PortalDefaultThemeFixture.ps1' -Action Apply   # 切换为 Default 并记录快照</zh-CN>
  <en>& 'dev\scripts\New-PortalDefaultThemeFixture.ps1' -Action Apply   # switch to Default and record the snapshot</en>
</lang>
.NOTES
<lang>
  <zh-CN>演进：`W-anp-P149` 交付；仅供 L2 门禁 `Test-PortalDefaultThemeVisualBaseline.mjs` 使用（由编排以 Fixture 调用）。</zh-CN>
  <en>Provenance: delivered by `W-anp-P149`; used only by the L2 gate `Test-PortalDefaultThemeVisualBaseline.mjs` (invoked as a suite Fixture).</en>
</lang>
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Apply', 'Remove')]
    [string] $Action,

    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string] $ConnectionStringsConfigPath = (Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\dev\connectionStrings.config'),

    [string] $ThemeName = 'Default'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang><zh-CN>只保存隔离 fixture 的仓库根、固定设置键、测试 actor 与快照位置；不承载生产配置。</zh-CN>
# <en>Holds only the fixture's repository root, fixed setting key, test actor and snapshot location; carries no production configuration.</en></lang>
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$settingKey = 'Portal.Theme.Name'
$testActor = 'P149-default-theme-baseline'
$snapshotDir = Join-Path (Join-Path $repoRoot 'temp') 'p149'
$snapshotPath = Join-Path $snapshotDir 'theme-setting-snapshot.json'

function Get-ExternalPortalConnectionString {
    param([string] $Path)

    # <lang><zh-CN>无 BOM UTF-8 读取外置 XML，兼容 connectionStrings 根节点或 configuration 包裹形式。</zh-CN>
    # <en>Reads the external XML as UTF-8 without BOM, accepting either a connectionStrings root or a configuration wrapper.</en></lang>
    [xml]$document = [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))
    $connectionStringsNode = if ($document.DocumentElement -and $document.DocumentElement.Name -eq 'connectionStrings') {
        $document.DocumentElement
    }
    else {
        $document.SelectSingleNode('/configuration/connectionStrings')
    }
    $portalNode = if ($connectionStringsNode) { $connectionStringsNode.SelectSingleNode("add[@name='Portal']") } else { $null }
    if ($null -eq $portalNode -or [string]::IsNullOrWhiteSpace($portalNode.connectionString)) {
        throw 'The external connectionStrings.config file does not contain a Portal connection string.'
    }
    return $portalNode.connectionString
}

function Add-TextParameter {
    param([System.Data.SqlClient.SqlCommand]$Command, [string]$Name, [int]$Size, [AllowNull()][string]$Value)

    $parameter = $Command.Parameters.Add($Name, [System.Data.SqlDbType]::NVarChar, $Size)
    $parameter.Value = if ($null -eq $Value) { [DBNull]::Value } else { $Value }
}

function Invoke-NonQuery {
    param([System.Data.SqlClient.SqlConnection]$Connection, [string]$Sql, [scriptblock]$Configure)

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = $Sql
        if ($Configure) { & $Configure $command }
        [void]$command.ExecuteNonQuery()
    }
    finally {
        $command.Dispose()
    }
}

function Get-SettingSnapshot {
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
            if (-not $reader.Read()) { return [pscustomobject]@{ Exists = $false } }
            return [pscustomobject]@{
                Exists = $true
                SettingValue = if ($reader.IsDBNull(0)) { $null } else { $reader.GetString(0) }
                ValueType = $reader.GetString(1)
                SourceLevel = $reader.GetString(2)
                CanDelete = $reader.GetBoolean(3)
                UpdatedBy = if ($reader.IsDBNull(4)) { $null } else { $reader.GetString(4) }
                UpdatedUtc = if ($reader.IsDBNull(5)) { $null } else { $reader.GetDateTime(5) }
            }
        }
        finally {
            $reader.Close()
        }
    }
    finally {
        $command.Dispose()
    }
}

$connectionString = Get-ExternalPortalConnectionString -Path $ConnectionStringsConfigPath
$connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
try {
    $connection.Open()

    if ($Action -eq 'Apply') {
        $snapshot = Get-SettingSnapshot -Connection $connection
        if (-not (Test-Path -LiteralPath $snapshotDir)) { New-Item -ItemType Directory -Force -Path $snapshotDir | Out-Null }
        [System.IO.File]::WriteAllText($snapshotPath, ($snapshot | ConvertTo-Json -Depth 4), [System.Text.UTF8Encoding]::new($false))

        # <lang><zh-CN>upsert 语义与既有主题 proof 一致：只改主题字段与审计字段，不改其它列语义。</zh-CN>
        # <en>Upsert semantics match the existing theme proof: only theme and audit columns change, other column semantics stay intact.</en></lang>
        Invoke-NonQuery -Connection $connection -Sql @'
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
        Write-Host ("主题已切换为 {0}（快照：{1}）" -f $ThemeName, $snapshotPath)
        exit 0
    }

    if (-not (Test-Path -LiteralPath $snapshotPath)) {
        Write-Host "[FAIL] 恢复失败：快照不存在 $snapshotPath（Apply 未执行或已被清理）。"
        exit 1
    }
    $snapshot = Get-Content -LiteralPath $snapshotPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($snapshot.Exists) {
        Invoke-NonQuery -Connection $connection -Sql @'
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
            Add-TextParameter -Command $command -Name '@SettingValue' -Size 4000 -Value $snapshot.SettingValue
            Add-TextParameter -Command $command -Name '@ValueType' -Size 50 -Value $snapshot.ValueType
            Add-TextParameter -Command $command -Name '@SourceLevel' -Size 50 -Value $snapshot.SourceLevel
            $bit = $command.Parameters.Add('@CanDelete', [System.Data.SqlDbType]::Bit)
            $bit.Value = [bool]$snapshot.CanDelete
            Add-TextParameter -Command $command -Name '@UpdatedBy' -Size 100 -Value $snapshot.UpdatedBy
            $stamp = $command.Parameters.Add('@UpdatedUtc', [System.Data.SqlDbType]::DateTime2)
            $stamp.Value = if ($null -eq $snapshot.UpdatedUtc) { [DBNull]::Value } else { [datetime]$snapshot.UpdatedUtc }
        }
        Write-Host ("主题已恢复为 {0}" -f $snapshot.SettingValue)
    }
    else {
        # <lang><zh-CN>原行不存在则删除本次插入，不留人工痕迹。</zh-CN>
        # <en>When no original row existed, delete the row inserted for this run so no manual trace remains.</en></lang>
        Invoke-NonQuery -Connection $connection -Sql 'DELETE FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = @SettingKey' -Configure {
            param($command)
            Add-TextParameter -Command $command -Name '@SettingKey' -Size 200 -Value $settingKey
        }
        Write-Host '主题设置已删除（恢复为「原本不存在」状态）。'
    }
    Remove-Item -LiteralPath $snapshotPath -Force -ErrorAction SilentlyContinue
    exit 0
}
finally {
    if ($connection.State -ne [System.Data.ConnectionState]::Closed) { $connection.Close() }
    $connection.Dispose()
}
