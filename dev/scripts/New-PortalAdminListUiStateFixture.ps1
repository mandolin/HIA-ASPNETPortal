<#
.SYNOPSIS
  <lang>
    <zh-CN>为后台列表 UI 注入证明可逆地调整一条待办的状态。</zh-CN>
    <en>Reversibly adjusts one work item's status for the admin-list UI injection proof.</en>
  </lang>
.DESCRIPTION
  <lang>
    <zh-CN>`Test-PortalAdminListUiEvidence.mjs` 对 `Admin/WorkItems.aspx` 硬编码 `expectEmptyState: true`，而该页默认筛选 `Open`（`WorkItems.aspx.cs` 的 `BindStatusFilter`），开发库里存在一条正式业务待办（WorkItemId=204, status=Open）使基线恒红。本脚本在基线前把它改为 `InProgress`（`CK_PortalBiz_WorkItems_CompletionState` 允许 InProgress 且完成字段为 NULL），页面默认视图即零条、空态渲染；证明结束后改回 `Open`。只更新状态列、不删任何行、不触碰完成事实，故完全可逆。仅限一次性开发/测试库使用。</zh-CN>
    <en>`Test-PortalAdminListUiEvidence.mjs` hard-codes `expectEmptyState: true` for `Admin/WorkItems.aspx`, while that page defaults to the `Open` filter (`BindStatusFilter` in `WorkItems.aspx.cs`); a real business work item (WorkItemId=204, status=Open) in the dev database keeps the baseline red. Before the baseline this script sets it to `InProgress` (allowed by `CK_PortalBiz_WorkItems_CompletionState` with NULL completion fields), so the default view has zero rows and renders the empty state; afterwards it restores `Open`. Only the status column is updated — no rows are deleted and no completion facts are touched — so the change is fully reversible. Disposable development/test databases only.</en>
  </lang>
#>
[CmdletBinding()]
param(
    # <lang><zh-CN>Hide：把 204 从 Open 改为 InProgress；Restore：改回 Open。</zh-CN><en>Hide: change 204 from Open to InProgress; Restore: change it back to Open.</en></lang>
    [Parameter(Mandatory)]
    [ValidateSet('Hide', 'Restore')]
    [string]$Action
)

$ErrorActionPreference = 'Stop'

# <lang><zh-CN>连接串只从外置配置读取，既不回显内容也不写入仓库文件。</zh-CN><en>The connection string is read only from the external config; it is neither echoed nor written into any repository file.</en></lang>
$configPath = Join-Path $env:USERPROFILE 'Web\HIA-ASPNETPortal\dev\connectionStrings.config'
[xml]$document = Get-Content $configPath -Raw -Encoding UTF8
$connectionString = ''
foreach ($entry in $document.connectionStrings.add) {
    if ($entry.name -eq 'Portal') { $connectionString = $entry.connectionString }
}
if (-not $connectionString) { throw 'Portal connection string was not found in the external config.' }

# <lang><zh-CN>更新条件同时限定当前状态，重复执行不会误改已处于目标状态的行；finally 保证连接释放。</zh-CN><en>The WHERE clause also pins the current status, so repeated runs cannot rewrite an already-adjusted row; finally disposes the connection.</en></lang>
$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
$connection.Open()
try {
    $command = $connection.CreateCommand()
    if ($Action -eq 'Hide') {
        $command.CommandText = "UPDATE PortalBiz_WorkItems SET WorkItemStatus = N'InProgress' WHERE WorkItemId = 204 AND WorkItemStatus = N'Open'"
    }
    else {
        $command.CommandText = "UPDATE PortalBiz_WorkItems SET WorkItemStatus = N'Open' WHERE WorkItemId = 204 AND WorkItemStatus = N'InProgress'"
    }
    $rows = $command.ExecuteNonQuery()
    Write-Output ("[fixture] WorkItemId=204 " + $Action + " rows=" + $rows)
}
finally {
    $connection.Close()
}
