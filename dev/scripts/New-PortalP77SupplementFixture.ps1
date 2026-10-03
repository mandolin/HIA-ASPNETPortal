<#
.SYNOPSIS
<lang>
  <en>Applies or removes the P77 supplementary verification fixtures (dark-skin tab theme and multi-instance tab placement).</en>
  <zh-CN>应用或移除 P77 补充验证夹具（深色皮肤页签主题覆盖、多实例页签挂载）。</zh-CN>
</lang>

.DESCRIPTION
<lang>
  <en>
  Two registered P77 leftovers can only be closed at runtime: the dark-skin verification (previously covered by
  prototypes only) and the D1 multi-instance selection replay (previously covered by unit tests only). This script
  creates and removes only the temporary registration rows those two proofs need, and nothing else:

    DarkTheme      - inserts a tab-theme override sending the MyWorkItems tab to a dark skin.
    DarkThemeOff   - deletes exactly that override row.
    MultiInstance  - inserts two extra hosting tabs and one extra workbench instance on each, so the resolver must
                     choose among three candidates: the lowest order that the user cannot access, a lower order the
                     user can access, and the previously chosen tab with the largest order.
    MultiInstanceOff - deletes the temporary instances and tabs.

  Every fixture row carries a deterministic identifier so removal is exact. The script never touches work items,
  users, roles, or permissions.
  </en>
  <zh-CN>
  P77 有两项遗留只能在运行期闭合：深色皮肤验证（此前只有原型图）与 `D1` 多实例取舍复演（此前只有单测）。
  本脚本只创建/移除这两项验证所需的临时注册行，不做其它任何改动：

    DarkTheme      —— 插入页签主题覆盖，把我的待办页签切到深色皮肤。
    DarkThemeOff   —— 精确删除该覆盖行。
    MultiInstance  —— 插入两个额外承载页签并各挂一个协同工作台实例，使解析器必须在三个候选间取舍：
                      顺序最小但用户无权、顺序较小且用户有权、以及此前选中的顺序最大的页签。
    MultiInstanceOff —— 删除临时实例与页签。

  所有夹具行都使用确定性标识，因此移除精确无误。脚本不触碰待办、用户、角色与权限。
  </zh-CN>
</lang>

.PARAMETER Action
<lang>
  <en>Which fixture action to perform.</en>
  <zh-CN>要执行的夹具动作。</zh-CN>
</lang>

.PARAMETER WorkItemsTabId
<lang>
  <en>Tab hosting the My To-Do Items module, used as the theme-override target.</en>
  <zh-CN>承载「我的待办」的页签，作为主题覆盖的目标。</zh-CN>
</lang>

.PARAMETER DarkThemeName
<lang>
  <en>Name of the dark skin to force for the verification.</en>
  <zh-CN>验证时强制使用的深色皮肤名称。</zh-CN>
</lang>

.PARAMETER WorkbenchDefinitionId
<lang>
  <en>Module-definition identifier of the collaboration workbench, whose instances are duplicated.</en>
  <zh-CN>协同工作台的模块定义标识，其实例会被复制挂载。</zh-CN>
</lang>

.NOTES
<lang>
  <en>Companion evidence script: dev/scripts/Test-PortalP77SupplementEvidence.mjs. Remember the business-module profile must be active for the modules to render, and restore it afterwards. Because the data layer reads tabs and module instances into an in-memory snapshot when it is constructed, rows inserted by MultiInstance stay invisible until the application domain recycles; re-save src/Portal/web.config with identical content to trigger a recycle before running the multi-instance assertions.</en>
  <zh-CN>配套证据脚本：dev/scripts/Test-PortalP77SupplementEvidence.mjs。业务模块档位必须处于启用状态才会渲染，验证后请还原。由于数据层在**构造时**把页签与模块实例读入内存快照，`MultiInstance` 插入的行在应用域回收前不可见；跑多实例断言前，请以相同内容重写 `src/Portal/web.config` 触发回收。</zh-CN>
</lang>
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('DarkTheme', 'DarkThemeOff', 'MultiInstance', 'MultiInstanceOff', 'List')]
    [string]$Action,

    [int]$WorkItemsTabId = 1011,

    [string]$DarkThemeName = 'EnterpriseDark',

    [int]$WorkbenchDefinitionId = 1013
)

$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>临时页签与实例使用确定性标识：前一个比既有承载页签顺序更小但**无权**，后一个顺序较小且**有权**。</zh-CN>
#   <en>The temporary tabs and instances use deterministic identifiers: the first has a smaller order than the existing hosting tab but is **inaccessible**, the second has a smaller order and is **accessible**.</en>
# </lang>
$blockedTabId = 1098
$blockedTabOrder = 2
$allowedTabId = 1099
$allowedTabOrder = 5
$blockedModuleId = 1098
$allowedModuleId = 1099
$fixtureMarker = 'P77SupplementFixture'

$connection = New-Object System.Data.SqlClient.SqlConnection 'Server=(localdb)\MSSQLLocalDB;Database=Portal;Integrated Security=true;Connect Timeout=10'
$connection.Open()

function Invoke-PortalStatement {
    param([string]$Sql)

    $command = $connection.CreateCommand()
    $command.CommandText = $Sql
    return $command.ExecuteNonQuery()
}

function Invoke-PortalQuery {
    param([string]$Sql)

    $command = $connection.CreateCommand()
    $command.CommandText = $Sql
    $reader = $command.ExecuteReader()
    $rows = New-Object System.Collections.Generic.List[string]
    while ($reader.Read()) {
        $values = New-Object System.Collections.Generic.List[string]
        for ($i = 0; $i -lt $reader.FieldCount; $i++) { $values.Add('' + $reader.GetValue($i)) }
        $rows.Add(($values -join ' | '))
    }
    $reader.Close()
    return $rows
}

try {
    switch ($Action) {
        'DarkTheme' {
            Invoke-PortalStatement ("DELETE FROM PortalCfg_TabThemeOverrides WHERE TabId = " + $WorkItemsTabId + " AND UpdatedBy = N'" + $fixtureMarker + "'") | Out-Null
            Invoke-PortalStatement ("INSERT INTO PortalCfg_TabThemeOverrides (TabId, ThemeName, UpdatedBy, UpdatedUtc) VALUES (" + $WorkItemsTabId + ", N'" + $DarkThemeName + "', N'" + $fixtureMarker + "', SYSUTCDATETIME())") | Out-Null
            Write-Output ("DARK-THEME-APPLIED tab={0} theme={1}" -f $WorkItemsTabId, $DarkThemeName)
        }

        'DarkThemeOff' {
            $removed = Invoke-PortalStatement ("DELETE FROM PortalCfg_TabThemeOverrides WHERE TabId = " + $WorkItemsTabId + " AND UpdatedBy = N'" + $fixtureMarker + "'")
            Write-Output ("DARK-THEME-REMOVED rows={0}" -f $removed)
        }

        'MultiInstance' {
            # <lang>
            #   <zh-CN>先清同标识的历史行，保证重复执行不因主键/唯一约束失败。</zh-CN>
            #   <en>Clear same-marker rows first so repeated runs cannot fail on key or uniqueness constraints.</en>
            # </lang>
            Invoke-PortalStatement ("DELETE FROM PortalCfg_Modules WHERE ModuleId IN (" + $blockedModuleId + ", " + $allowedModuleId + ")") | Out-Null
            Invoke-PortalStatement ("DELETE FROM PortalCfg_Tabs WHERE TabId IN (" + $blockedTabId + ", " + $allowedTabId + ")") | Out-Null

            # <lang>
            #   <zh-CN>`PortalCfg_Tabs.TabId` 与 `PortalCfg_Modules.ModuleId` 都是 IDENTITY 列：为了保持确定性标识（移除才精确、证据脚本的期望值才稳定），
            #   这里在同一会话内按表逐个打开 IDENTITY_INSERT，插入后立刻关闭；SQL Server 同一时刻只允许一张表为 ON，故不能同时打开。</zh-CN>
            #   <en>`PortalCfg_Tabs.TabId` and `PortalCfg_Modules.ModuleId` are both IDENTITY columns: to keep deterministic identifiers (so removal stays exact and the evidence script's expectations stay stable),
            #   IDENTITY_INSERT is enabled per table within this one session and closed immediately after the insert; SQL Server allows only one table ON at a time, so both cannot be open together.</en>
            # </lang>
            Invoke-PortalStatement 'SET IDENTITY_INSERT PortalCfg_Tabs ON' | Out-Null
            Invoke-PortalStatement ("INSERT INTO PortalCfg_Tabs (TabId, TabName, TabOrder, AccessRoles, ShowMobile, MobileTabName, PortalId) VALUES (" + $blockedTabId + ", N'P77-Fix-Blocked', " + $blockedTabOrder + ", N'TestRole;', 0, N'P77-Fix-Blocked', 1)") | Out-Null
            Invoke-PortalStatement ("INSERT INTO PortalCfg_Tabs (TabId, TabName, TabOrder, AccessRoles, ShowMobile, MobileTabName, PortalId) VALUES (" + $allowedTabId + ", N'P77-Fix-Allowed', " + $allowedTabOrder + ", N'All Users;', 0, N'P77-Fix-Allowed', 1)") | Out-Null
            Invoke-PortalStatement 'SET IDENTITY_INSERT PortalCfg_Tabs OFF' | Out-Null

            Invoke-PortalStatement 'SET IDENTITY_INSERT PortalCfg_Modules ON' | Out-Null
            Invoke-PortalStatement ("INSERT INTO PortalCfg_Modules (ModuleId, ModuleTitle, ModuleOrder, EditRoles, PaneName, ShowMobile, CacheTimeout, ModuleDefId, TabId) VALUES (" + $blockedModuleId + ", N'P77 fixture workbench (blocked)', 1, N'Admins;', N'ContentPane', 0, 0, " + $WorkbenchDefinitionId + ", " + $blockedTabId + ")") | Out-Null
            Invoke-PortalStatement ("INSERT INTO PortalCfg_Modules (ModuleId, ModuleTitle, ModuleOrder, EditRoles, PaneName, ShowMobile, CacheTimeout, ModuleDefId, TabId) VALUES (" + $allowedModuleId + ", N'P77 fixture workbench (allowed)', 1, N'Admins;', N'ContentPane', 0, 0, " + $WorkbenchDefinitionId + ", " + $allowedTabId + ")") | Out-Null
            Invoke-PortalStatement 'SET IDENTITY_INSERT PortalCfg_Modules OFF' | Out-Null

            Write-Output ("MULTI-INSTANCE-APPLIED blocked(tab={0},order={1},TestRole) allowed(tab={2},order={3},AllUsers) existing(tab=1010,order=21)" -f $blockedTabId, $blockedTabOrder, $allowedTabId, $allowedTabOrder)
        }

        'MultiInstanceOff' {
            $removedModules = Invoke-PortalStatement ("DELETE FROM PortalCfg_Modules WHERE ModuleId IN (" + $blockedModuleId + ", " + $allowedModuleId + ")")
            $removedTabs = Invoke-PortalStatement ("DELETE FROM PortalCfg_Tabs WHERE TabId IN (" + $blockedTabId + ", " + $allowedTabId + ")")
            Write-Output ("MULTI-INSTANCE-REMOVED modules={0} tabs={1}" -f $removedModules, $removedTabs)
        }

        'List' {
            Write-Output '--- 主题覆盖 ---'
            Invoke-PortalQuery "SELECT TabId, ThemeName, UpdatedBy FROM PortalCfg_TabThemeOverrides ORDER BY TabId" | ForEach-Object { '  ' + $_ }
            Write-Output '--- 协同工作台实例 ---'
            Invoke-PortalQuery ("SELECT ModuleId, ModuleDefId, TabId FROM PortalCfg_Modules WHERE ModuleDefId = " + $WorkbenchDefinitionId + " ORDER BY ModuleId") | ForEach-Object { '  ' + $_ }
            Write-Output '--- 临时页签 ---'
            Invoke-PortalQuery ("SELECT TabId, TabOrder, AccessRoles, TabName FROM PortalCfg_Tabs WHERE TabId IN (" + $blockedTabId + ", " + $allowedTabId + ") ORDER BY TabId") | ForEach-Object { '  ' + $_ }
        }
    }
}
finally {
    $connection.Close()
}
