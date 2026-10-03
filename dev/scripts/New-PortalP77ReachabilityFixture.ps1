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
#   <zh-CN>三条夹具记录的标题与证据脚本的断言一一对应，改标题必须同时改证据脚本。</zh-CN>
#   <en>The three fixture titles map one-to-one onto the evidence-script assertions, so a title change requires an evidence change.</en>
# </lang>
$fixtureRows = @(
    @('CollaborationItem', 'P77-RUNTIME-COLLAB-1', 'P77 运行期验证：协同事项', '可办理目标：协同工作台（模块定义 1013 → 承载页签 1010）', 7),
    @('EmployeeProfileCorrectionRequest', 'P77-RUNTIME-CORR-1', 'P77 运行期验证：资料更正', '可办理目标：资料更正（模块定义 1012 → 承载页签 1009），且已超期', -2),
    @('BusinessApplication', 'P77-RUNTIME-BAPP-1', 'P77 运行期验证：业务申请', '不可办理：该业务无模块定义行，反查应降级为提示', 3)
)

try {
    if ($Action -eq 'Remove') {
        $removed = Invoke-PortalStatement ("DELETE FROM PortalBiz_WorkItems WHERE CreatedBy = N'" + $fixtureMarker + "' AND BusinessId LIKE N'" + $fixtureIdPrefix + "%'")
        Write-Output ('REMOVED=' + $removed)
        return
    }

    if ($Action -eq 'Seed') {
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
