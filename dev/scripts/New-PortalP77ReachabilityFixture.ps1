<#
.SYNOPSIS
<lang>
  <en>Creates, lists, or removes the P77 work-item reachability fixture in the development database.</en>
  <zh-CN>在开发库中创建、列出或移除 P77 待办可达性夹具。</zh-CN>
</lang>

.DESCRIPTION
<lang>
  <en>
  The fixture seeds three open work items assigned to the acceptance administrator so the front-end
  "My To-Do Items" module can be exercised for all three reachability states: a collaboration item and a
  profile-correction request that must resolve to their hosting tabs, and a business application that has no
  module definition and must therefore degrade to the hint. Rows carry the marker CreatedBy='P77RuntimeVerify'
  and the BusinessId prefix 'P77-RUNTIME-' so Remove can delete exactly this fixture and nothing else.
  The script connects to the same local development database the site uses; it never prints credentials and
  never touches any other table.
  </en>
  <zh-CN>
  本夹具向验收管理员分派三条开放待办，使前台「我的待办」可以覆盖全部三种可达性状态：一条必须解析到承载页签的
  协同事项、一条同样可解析的资料更正请求，以及一条没有模块定义、必须降级为提示的业务申请。记录以
  CreatedBy='P77RuntimeVerify' 与 BusinessId 前缀 'P77-RUNTIME-' 作为标记，使 Remove 只删除本夹具、
  不触碰其它数据。脚本连接站点所用的同一本地开发库；绝不输出凭据，也不写其它任何表。
  </zh-CN>
</lang>

.PARAMETER Action
<lang>
  <en>Seed creates the three fixture rows, List reports them, and Remove deletes them.</en>
  <zh-CN>Seed 创建三条夹具记录，List 报告现状，Remove 删除它们。</zh-CN>
</lang>

.PARAMETER DataSource
<lang>
  <en>Development database data source; defaults to the local development LocalDB instance used by the site.</en>
  <zh-CN>开发库数据源；默认为站点使用的本地开发 LocalDB 实例。</zh-CN>
</lang>

.PARAMETER Database
<lang>
  <en>Development database name; defaults to Portal.</en>
  <zh-CN>开发库名称；默认为 Portal。</zh-CN>
</lang>

.PARAMETER AssignedUserId
<lang>
  <en>Portal user identifier the fixture rows are assigned to; must be an account that holds the work-item view permission.</en>
  <zh-CN>夹具记录分派到的门户用户标识；必须是持有待办查看权限的账号。</zh-CN>
</lang>

.EXAMPLE
<lang>
  <en>pwsh -File dev/scripts/New-PortalP77ReachabilityFixture.ps1 -Action Seed</en>
  <zh-CN>pwsh -File dev/scripts/New-PortalP77ReachabilityFixture.ps1 -Action Seed</zh-CN>
</lang>

.NOTES
<lang>
  <en>Companion evidence script: dev/scripts/Test-PortalWorkItemReachabilityEvidence.mjs. Remember to run Remove after the evidence run so the development database returns to its earlier state.</en>
  <zh-CN>配套证据脚本：dev/scripts/Test-PortalWorkItemReachabilityEvidence.mjs。证据跑完后请执行 Remove，使开发库回到先前状态。</zh-CN>
</lang>
#>
[CmdletBinding()]
param(
    [ValidateSet('Seed', 'List', 'Remove')]
    [string]$Action = 'Seed',

    [string]$DataSource = '(localdb)\MSSQLLocalDB',

    [string]$Database = 'Portal',

    [int]$AssignedUserId = 1637
)

$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>夹具标记集中在此，Seed 与 Remove 必须使用同一组值，否则清理会漏掉记录。</zh-CN>
#   <en>The fixture marker lives here so Seed and Remove always use the same values; diverging would leave rows behind.</en>
# </lang>
$fixtureMarker = 'P77RuntimeVerify'
$fixtureIdPrefix = 'P77-RUNTIME-'

$connectionString = 'Server=' + $DataSource + ';Database=' + $Database + ';Integrated Security=true;Connect Timeout=10'
$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
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
        for ($i = 0; $i -lt $reader.FieldCount; $i++) {
            $values.Add('' + $reader.GetValue($i))
        }
        $rows.Add(($values -join ' | '))
    }
    $reader.Close()
    return $rows
}

# <lang>
#   <zh-CN>三条夹具记录的标题与证据脚本的断言一一对应，改标题必须同时改证据脚本。
#   `Summary` 只描述造数意图，**不写任何具体模块定义 ID 或承载页签 ID**：这些是部署事实，会随产品演进变化，
#   早先写死的「1013 → 1010」「1012 → 1009」「业务申请无模块定义行」在 1012 双挂载与 1067 上线后全部过时，
#   把过时结论留在造数文本里比没有文本更误导。证据脚本按 `W-anp-P77.1-benchmark.md` L43 的三态契约不变量断言，
#   不再依赖这些描述。</zh-CN>
#   <en>The three fixture titles map one-to-one onto the evidence-script assertions, so a title change requires an evidence
#   change. The `Summary` describes seeding intent only and **contains no module-definition or hosting-tab ids**: those are
#   deployment facts that evolve. The previously hard-coded "1013 -> 1010", "1012 -> 1009" and "business application has
#   no module definition" all went stale once 1012 was mounted twice and 1067 went live; leaving stale conclusions in
#   seeded text is more misleading than having none. The evidence script asserts the three-state invariant per
#   `W-anp-P77.1-benchmark.md` L43 and no longer depends on these descriptions.</en>
# </lang>
$fixtureRows = @(
    @('CollaborationItem', 'P77-RUNTIME-COLLAB-1', 'P77 运行期验证：协同事项', '造数：协同事项待办，用于验证解析器按当前注册事实给出可达链接或降级提示', 7),
    @('EmployeeProfileCorrectionRequest', 'P77-RUNTIME-CORR-1', 'P77 运行期验证：资料更正', '造数：资料更正待办且已超期，用于验证超期标记与三态契约', -2),
    @('BusinessApplication', 'P77-RUNTIME-BAPP-1', 'P77 运行期验证：业务申请', '造数：业务申请待办，用于验证解析器按当前注册事实给出可达链接或降级提示', 3)
)

# <lang>
#   <zh-CN>权限事实快照：证据脚本是 Node（无 SQL 驱动），而本夹具已持有数据库连接，故由夹具把"当前部署里
#   非管理员是否该看到待办"这一**权威事实**写成 JSON，证据脚本读取后推导"无权限用户"态的期望。
#   这样权限配置演进（例如 All Users 被授予 View）后两个脚本都无需改动，且不会把配置决策误报成产品缺陷
#   （W-anp-P139 口径：环境前置/部署事实 ≠ 代码缺陷）。快照落在 gitignore 的 temp 下，Remove 时一并清理。</zh-CN>
#   <en>Permission-fact snapshot: the evidence script is Node (no SQL driver) while this fixture already holds a DB
#   connection, so the fixture writes the **authoritative fact** "should a non-administrator see work items in the current
#   deployment" to JSON; the evidence script reads it to derive the expectation for the "user without permission" state.
#   Permission-configuration changes (e.g. All Users being granted View) then require no change to either script, and a
#   deployment decision is never misreported as a product defect (W-anp-P139: environment/deployment facts are not code
#   defects). The snapshot lives under gitignored temp and is removed together with the seeded rows.</en>
# </lang>
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$factsPath = Join-Path $repoRoot 'temp\p77\reachability\permission-facts.json'

try {
    if ($Action -eq 'Remove') {
        $removed = Invoke-PortalStatement ("DELETE FROM PortalBiz_WorkItems WHERE CreatedBy = N'" + $fixtureMarker + "' AND BusinessId LIKE N'" + $fixtureIdPrefix + "%'")
        if (Test-Path -LiteralPath $factsPath) { Remove-Item -LiteralPath $factsPath -Force }
        Write-Output ('REMOVED=' + $removed)
        return
    }

    if ($Action -eq 'Seed') {
        # <lang><zh-CN>查询 All Users 角色是否持有待办查看/管理权限：门户把该角色授予所有已登录用户，故它一旦持有，
        # "无权限用户"在当前部署下就不存在，证据脚本应期望该账号可见（而非期望被挡）。</zh-CN>
        # <en>Check whether the All Users role holds a work-item view/admin permission: the portal grants that role to
        # every signed-in user, so once it holds one, a "user without permission" does not exist in this deployment and
        # the evidence script should expect the account to see the list (rather than expect it to be hidden).</en></lang>
        $permissionQuery = @'
SELECT COUNT(*)
FROM PortalCfg_RolePermissions p
JOIN Portal_Roles r ON r.RoleID = p.RoleId
WHERE r.RoleName = N'All Users'
  AND p.IsEnabled = 1
  AND (p.PermissionKey = N'Business.WorkItems.View' OR p.PermissionKey = N'Business.WorkItems.Admin');
'@
        $allUsersHasPermission = [int](Invoke-PortalQuery $permissionQuery)[0] -gt 0
        $factsDir = Split-Path -Parent $factsPath
        if (-not (Test-Path -LiteralPath $factsDir)) { New-Item -ItemType Directory -Force -Path $factsDir | Out-Null }
        $facts = [pscustomobject]@{
            generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
            allUsersHoldsWorkItemPermission = $allUsersHasPermission
            # <lang><zh-CN>由权限事实直接推导的期望：true 表示非管理员应能看到待办列表。</zh-CN><en>Expectation derived from the permission fact: true means a non-administrator should see the work-item list.</en></lang>
            nonAdminShouldSeeWorkItems = $allUsersHasPermission
            basis = 'PortalCfg_RolePermissions x Portal_Roles (All Users), IsEnabled=1, Business.WorkItems.View|Admin'
        }
        [System.IO.File]::WriteAllText($factsPath, ($facts | ConvertTo-Json -Depth 4), [System.Text.UTF8Encoding]::new($false))
        Write-Output ('FACTS nonAdminShouldSeeWorkItems=' + $allUsersHasPermission)

        # <lang>
        #   <zh-CN>先清理同标记的历史记录：活动业务对象唯一索引只允许同一业务对象有一个开放/处理中待办，重复执行否则会冲突。</zh-CN>
        #   <en>Clear prior rows with the same marker first, because the active-business unique index allows one open or in-progress work item per business object and a repeated run would otherwise conflict.</en>
        # </lang>
        Invoke-PortalStatement ("DELETE FROM PortalBiz_WorkItems WHERE CreatedBy = N'" + $fixtureMarker + "' AND BusinessId LIKE N'" + $fixtureIdPrefix + "%'") | Out-Null

        foreach ($row in $fixtureRows) {
            $sql = "INSERT INTO PortalBiz_WorkItems (BusinessKind, BusinessId, Title, Summary, WorkItemStatus, AssignedUserId, CreatedUtc, CreatedBy, DueUtc) VALUES (" +
                "N'" + $row[0] + "', N'" + $row[1] + "', N'" + $row[2] + "', N'" + $row[3] + "', N'Open', " + $AssignedUserId +
                ", DATEADD(day, -2, SYSUTCDATETIME()), N'" + $fixtureMarker + "', DATEADD(day, " + $row[4] + ", SYSUTCDATETIME()))"
            Invoke-PortalStatement $sql | Out-Null
            Write-Output ('SEEDED ' + $row[1])
        }
    }

    Write-Output '--- fixture rows ---'
    Invoke-PortalQuery ("SELECT WorkItemId, BusinessKind, BusinessId, WorkItemStatus, AssignedUserId, CONVERT(varchar(19), DueUtc, 120) FROM PortalBiz_WorkItems WHERE CreatedBy = N'" + $fixtureMarker + "' ORDER BY WorkItemId") |
        ForEach-Object { '  ' + $_ }
}
finally {
    $connection.Close()
}
