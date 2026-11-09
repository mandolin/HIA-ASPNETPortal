<#
.SYNOPSIS
<lang>
  <zh-CN>独立模块浏览器级 L2 运行期证据的编排入口：按库内事实推导页签 ID，逐主题/语言切换环境并采集证据，最后恢复全局主题快照。</zh-CN>
  <en>Orchestration entry for independent-module browser-level L2 runtime evidence: derives the tab id from database facts, iterates themes/locales, and restores the global-theme snapshot.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>本编排只负责「环境准备 + 事实推导 + 恢复」，断言与截图由 Test-PortalIndependentModuleRuntimeEvidence.mjs 承载。
  关键设计（遵循「门禁对易变数据态不敏感」）：
    - 页签 ID 由 PortalCfg_Tabs 按页签名查询得到，不硬编码任何具体行号；模块路径由 manifest desktopEntry 推导。
    - 全局主题经参数化 upsert 临时切换，finally 按运行前快照逐字段还原（含原本不存在时删除），零残留。
    - 期望主题类从主题名按 PortalThemeResolver 的规范化规则推导，与被测脚本同一口径。
    - node 可执行文件优先由 mise 提供（缺失时回退 PATH 中的 node）。
  证明边界：只证明采集时刻当前部署配置下独立模块在指定主题/语言组合下渲染出预期结构与文案；
  不证明跨机器像素一致、不证明提交落库链路、不证明可访问性与性能、不证明第三方权限边界的运行期判定。</zh-CN>
  <en>This orchestrator owns only "environment preparation + fact derivation + restoration"; assertions and screenshots live in
  Test-PortalIndependentModuleRuntimeEvidence.mjs. Key design (keeping gates insensitive to volatile data state):
    - The tab id is queried from PortalCfg_Tabs by tab name, never hard-coded to a particular row; the module path is derived from the manifest desktopEntry.
    - The global theme is switched by parameterized upsert and restored field-by-field from a pre-run snapshot in finally (deleting it when it did not exist), leaving zero residue.
    - The expected theme class is derived from the theme name by the same normalization rule as PortalThemeResolver.
    - The node executable is preferred from mise, falling back to node on PATH.
  Proof boundary: proves only that, at collection time under the current deployment configuration, the independent module renders the
  expected structure and copy for each theme/locale; it does not prove cross-machine pixel equality, post-submit persistence,
  accessibility or performance, nor runtime authorization decisions for third-party modules.</en>
</lang>
#>
[CmdletBinding()]
param(
    [string]$ConnectionString = 'Server=(localdb)\MSSQLLocalDB;Database=Portal;Trusted_Connection=True;',
    [string]$BaseUrl = 'http://localhost:40001/',
    [string]$TabName = 'Form Builder',
    [string[]]$Themes = @('EnterpriseLight', 'EnterpriseDark'),
    [string[]]$Locales = @('zh-CN', 'en-US'),
    [string]$OutputDirectory,
    [string]$RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>统一无 BOM UTF-8 读取，避免中文配置与页签名在 PowerShell 默认编码下失真。</zh-CN>
#   <en>Use UTF-8 without BOM throughout so Chinese configuration and tab names are not corrupted by PowerShell's default encoding.</en>
# </lang>
$utf8 = [System.Text.UTF8Encoding]::new($false)
$settingKey = 'Portal.Theme.Name'
$testActor = 'independent-module-l2-evidence'
$repoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot 'work-zone/dev/evidence/p50-independent-module-l2/screenshots'
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

# <lang>
#   <zh-CN>node 优先取自 mise（项目约定的运行时管理入口），缺失时回退 PATH，runspace 不因单机差异写死绝对路径。</zh-CN>
#   <en>Resolve node from mise (the project's agreed runtime manager) and fall back to PATH, so no machine-specific absolute path is hard-coded.</en>
# </lang>
$node = $null
$mise = Get-Command 'mise' -ErrorAction SilentlyContinue
if ($mise) {
    $node = (& $mise.Source 'which' 'node' 2>$null | Select-Object -First 1)
}
if (-not $node) {
    $nodeOnPath = Get-Command 'node' -ErrorAction SilentlyContinue
    if ($nodeOnPath) { $node = $nodeOnPath.Source }
}
if (-not $node -or -not (Test-Path -LiteralPath $node)) {
    throw 'node was not found. Install it via mise (for example: mise use node@22) or put it on PATH.'
}

# <lang>
#   <zh-CN>playwright 为开发期依赖且不入库；优先按环境变量定位，否则按仓库内约定路径解析。</zh-CN>
#   <en>playwright is a development-only dependency that is not committed; prefer the environment variable, else the repository convention.</en>
# </lang>
if (-not $env:PORTAL_PLAYWRIGHT_MODULE) {
    $candidate = Join-Path $repoRoot 'temp/node_modules/playwright'
    if (Test-Path -LiteralPath $candidate) { $env:PORTAL_PLAYWRIGHT_MODULE = (Resolve-Path -LiteralPath $candidate).Path }
}
if (-not $env:PORTAL_PLAYWRIGHT_MODULE) {
    throw 'PORTAL_PLAYWRIGHT_MODULE is not set and temp/node_modules/playwright was not found.'
}

$connection = $null
$snapshot = $null
$results = New-Object 'System.Collections.Generic.List[object]'
try {
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    $connection.Open()

    # <lang>
    #   <zh-CN>按页签名查询模块实例所在页签；同时以 manifest desktopEntry 反查模块定义，确认页签与模块路径确实由安装机制写入。</zh-CN>
    #   <en>Query the tab hosting the module instance by tab name, and cross-check its module definition against the manifest desktopEntry to confirm provisioning really wrote both.</en>
    # </lang>
    $manifestPath = Join-Path $repoRoot 'src/Portal/DesktopModules/FormBuilder/module.json'
    $manifest = [System.IO.File]::ReadAllText($manifestPath, $utf8) | ConvertFrom-Json
    $desktopEntry = $manifest.desktopEntry

    $tabCommand = $connection.CreateCommand()
    $tabCommand.CommandText = @'
SELECT t.TabId
FROM PortalCfg_Tabs t
JOIN PortalCfg_Modules m ON m.TabId = t.TabId
JOIN PortalCfg_ModuleDefinitions d ON d.ModuleDefId = m.ModuleDefId
WHERE t.TabName = @TabName AND d.DesktopSourceFile = @DesktopEntry
'@
    [void]$tabCommand.Parameters.AddWithValue('@TabName', $TabName)
    [void]$tabCommand.Parameters.AddWithValue('@DesktopEntry', $desktopEntry)
    $tabId = $tabCommand.ExecuteScalar()
    if ($null -eq $tabId) {
        throw "No module instance was found for tab '$TabName' with desktop entry '$desktopEntry'. Run the provision script first."
    }
    $tabIdText = [Convert]::ToString($tabId, [System.Globalization.CultureInfo]::InvariantCulture)

    # <lang>
    #   <zh-CN>保存全局主题设置的完整快照，供 finally 逐字段还原；记录行是否存在，避免把临时写入变成永久配置变更。</zh-CN>
    #   <en>Capture a full snapshot of the global-theme setting for field-by-field restoration in finally; record whether the row existed so a temporary write never becomes a permanent configuration change.</en>
    # </lang>
    $snapshotCommand = $connection.CreateCommand()
    $snapshotCommand.CommandText = @'
SELECT SettingValue, ValueType, SourceLevel, CanDelete, UpdatedBy, UpdatedUtc
FROM PortalCfg_SystemSettings WHERE SettingKey = @SettingKey
'@
    [void]$snapshotCommand.Parameters.AddWithValue('@SettingKey', $settingKey)
    $reader = $snapshotCommand.ExecuteReader()
    if ($reader.Read()) {
        $snapshot = [ordered]@{
            Exists = $true
            SettingValue = if ($reader.IsDBNull(0)) { $null } else { $reader.GetString(0) }
            ValueType = $reader.GetString(1)
            SourceLevel = $reader.GetString(2)
            CanDelete = $reader.GetBoolean(3)
            UpdatedBy = if ($reader.IsDBNull(4)) { $null } else { $reader.GetString(4) }
            UpdatedUtc = $reader.GetDateTime(5)
        }
    }
    else { $snapshot = [ordered]@{ Exists = $false } }
    $reader.Close()

    foreach ($theme in $Themes) {
        # <lang>
        #   <zh-CN>每次切换只改全局主题一行，使用参数化 upsert 与固定测试 actor，便于审计与还原。</zh-CN>
        #   <en>Each switch changes only the global-theme row via parameterized upsert with a fixed test actor, for auditability and restoration.</en>
        # </lang>
        $setCommand = $connection.CreateCommand()
        $setCommand.CommandText = @'
IF EXISTS (SELECT 1 FROM PortalCfg_SystemSettings WHERE SettingKey = @SettingKey)
    UPDATE PortalCfg_SystemSettings
       SET SettingValue = @SettingValue, ValueType = N'Enum', SourceLevel = N'Database',
           CanDelete = 1, UpdatedBy = @UpdatedBy, UpdatedUtc = SYSUTCDATETIME()
     WHERE SettingKey = @SettingKey
ELSE
    INSERT INTO PortalCfg_SystemSettings (SettingKey, SettingValue, ValueType, SourceLevel, CanDelete, UpdatedBy, UpdatedUtc)
    VALUES (@SettingKey, @SettingValue, N'Enum', N'Database', 1, @UpdatedBy, SYSUTCDATETIME())
'@
        [void]$setCommand.Parameters.AddWithValue('@SettingKey', $settingKey)
        [void]$setCommand.Parameters.AddWithValue('@SettingValue', $theme)
        [void]$setCommand.Parameters.AddWithValue('@UpdatedBy', $testActor)
        [void]$setCommand.ExecuteNonQuery()

        # <lang>
        #   <zh-CN>主题类按 PortalThemeResolver 的规范化规则（小写去分隔符）推导，与被测脚本的期望同源。</zh-CN>
        #   <en>Derive the theme class with the same normalization rule as PortalThemeResolver (lower-case, separators removed), sharing one source of truth with the evidence script.</en>
        # </lang>
        $themeClass = 'portal-theme-' + $theme.ToLowerInvariant()

        foreach ($locale in $Locales) {
            $env:PORTAL_L2_BASE_URL = $BaseUrl
            $env:PORTAL_L2_CONTEXT = (Join-Path $repoRoot 'temp/p65/p65-acceptance-context.json')
            $env:PORTAL_L2_TAB_ID = $tabIdText
            $env:PORTAL_L2_THEME = $theme
            $env:PORTAL_L2_LOCALE = $locale
            $env:PORTAL_L2_OUTPUT_DIR = $OutputDirectory

            $raw = & $node (Join-Path $PSScriptRoot 'Test-PortalIndependentModuleRuntimeEvidence.mjs') 2>&1
            $exit = $LASTEXITCODE
            $text = ($raw | Out-String).Trim()
            try { $parsed = $text | ConvertFrom-Json } catch { $parsed = $null }
            if ($parsed) {
                $results.Add([ordered]@{
                    theme = $theme; locale = $locale; themeClass = $themeClass
                    status = $parsed.status; checks = $parsed.checks
                    screenshot = $parsed.screenshot; url = $parsed.url
                })
                Write-Output ("[{0}] {1}/{2}: {3}" -f $(if ($exit -eq 0) { 'PASS' } else { 'FAIL' }), $theme, $locale, $parsed.status)
            }
            else {
                $results.Add([ordered]@{ theme = $theme; locale = $locale; status = 'Fail'; error = $text })
                Write-Output ("[FAIL] {0}/{1}: script produced no JSON :: {2}" -f $theme, $locale, $text)
            }
        }
    }
}
finally {
    # <lang>
    #   <zh-CN>无论采集成功与否都按快照还原全局主题：原本存在则逐字段 UPDATE，原本不存在则 DELETE；随后清理本轮环境变量。</zh-CN>
    #   <en>Always restore the global theme from the snapshot whether collection succeeded or not: field-by-field UPDATE when it existed, DELETE when it did not; then clear run-scoped environment variables.</en>
    # </lang>
    if ($connection -and $connection.State -eq [System.Data.ConnectionState]::Open -and $snapshot) {
        if ($snapshot.Exists) {
            $restore = $connection.CreateCommand()
            $restore.CommandText = @'
UPDATE PortalCfg_SystemSettings
   SET SettingValue = @SettingValue, ValueType = @ValueType, SourceLevel = @SourceLevel,
       CanDelete = @CanDelete, UpdatedBy = @UpdatedBy, UpdatedUtc = @UpdatedUtc
 WHERE SettingKey = @SettingKey
'@
            [void]$restore.Parameters.AddWithValue('@SettingKey', $settingKey)
            [void]$restore.Parameters.AddWithValue('@SettingValue', $snapshot.SettingValue)
            [void]$restore.Parameters.AddWithValue('@ValueType', $snapshot.ValueType)
            [void]$restore.Parameters.AddWithValue('@SourceLevel', $snapshot.SourceLevel)
            [void]$restore.Parameters.AddWithValue('@CanDelete', $snapshot.CanDelete)
            [void]$restore.Parameters.AddWithValue('@UpdatedBy', $snapshot.UpdatedBy)
            [void]$restore.Parameters.AddWithValue('@UpdatedUtc', $snapshot.UpdatedUtc)
            [void]$restore.ExecuteNonQuery()
        }
        else {
            $restore = $connection.CreateCommand()
            $restore.CommandText = 'DELETE FROM PortalCfg_SystemSettings WHERE SettingKey = @SettingKey'
            [void]$restore.Parameters.AddWithValue('@SettingKey', $settingKey)
            [void]$restore.ExecuteNonQuery()
        }
        Write-Output '[PASS] Global theme setting restored to its pre-run snapshot.'
        $connection.Dispose()
    }

    foreach ($name in @('PORTAL_L2_BASE_URL','PORTAL_L2_CONTEXT','PORTAL_L2_TAB_ID','PORTAL_L2_THEME','PORTAL_L2_LOCALE','PORTAL_L2_OUTPUT_DIR')) {
        Remove-Item -Path "Env:$name" -ErrorAction SilentlyContinue
    }
}

$failed = @($results | Where-Object { $_.status -ne 'Pass' })
$summary = [ordered]@{
    collectedUtc = [DateTime]::UtcNow.ToString('o')
    baseUrl = $BaseUrl
    themes = $Themes
    locales = $Locales
    total = $results.Count
    passed = $results.Count - $failed.Count
    failed = $failed.Count
    results = $results
}
# <lang>
#   <zh-CN>截图目录按项目口径不入库（work-zone/.gitignore 的 dev/evidence/**/screenshots/），
#   故把可复现的 JSON 摘要写到截图目录的**父目录**（入库资产），截图本身留在忽略目录按需生成。</zh-CN>
#   <en>Screenshot directories are not committed (dev/evidence/**/screenshots/ in work-zone/.gitignore), so the
#   reproducible JSON summary is written to the **parent** of the screenshot directory (a committed asset) while
#   screenshots themselves stay in the ignored directory and are generated on demand.</en>
# </lang>
$summaryPath = Join-Path (Split-Path -Parent $OutputDirectory.TrimEnd('\', '/')) 'runtime-evidence-summary.json'
[System.IO.File]::WriteAllText($summaryPath, ($summary | ConvertTo-Json -Depth 8) + "`n", $utf8)
Write-Output ("SUMMARY total={0} passed={1} failed={2}" -f $summary.total, $summary.passed, $summary.failed)
if ($failed.Count -gt 0) { exit 1 }
exit 0
