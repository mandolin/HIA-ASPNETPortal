#requires -Version 5.1
<#
.SYNOPSIS
  <lang>
    <zh-CN>读取开发库 `PortalCfg_Tabs`，输出 `{TabID, TabOrder, TabName}` 的 JSON 数组，供语义门禁
    （如 `Test-PortalSemanticMarkupEvidence.mjs`）在运行时发现页签序号，替代硬编码的 `tabindex`。</zh-CN>
    <en>Reads `PortalCfg_Tabs` from the development database and emits a JSON array of
    `{TabID, TabOrder, TabName}`, so that semantic gates (e.g. `Test-PortalSemanticMarkupEvidence.mjs`)
    can discover tab orders at runtime instead of relying on hardcoded `tabindex` values.</en>
  </lang>
.DESCRIPTION
  <lang>
    <zh-CN>本机没有可用的 `SqlServer` 模块（`Invoke-Sqlcmd` 不可用），因此脚本直接使用 ADO.NET
    （`System.Data.SqlClient`）打开连接并填充 `DataTable`，最后以 JSON 输出。连接串默认为本项目开发验证库
    LocalDB `(localdb)\MSSQLLocalDB` 的 `Portal`；真实环境请显式传入 `-ConnectionString`。
    页签序号会随新页签插入而位移（`TabOrder` 是真实的位移来源），故任何硬编码 `tabindex` 都会漂移。</zh-CN>
    <en>Because no usable `SqlServer` module exists on this machine (`Invoke-Sqlcmd` is unavailable), the
    script uses ADO.NET (`System.Data.SqlClient`) directly to open a connection and fill a `DataTable`, then
    emits JSON. The connection string defaults to this project's development LocalDB
    `(localdb)\MSSQLLocalDB` database `Portal`; pass `-ConnectionString` explicitly for a real environment.
    Tab orders shift whenever a new tab is inserted (`TabOrder` is the real source of that shift), so any
    hardcoded `tabindex` drifts out of sync.</en>
  </lang>
.PARAMETER ConnectionString
  <lang>
    <zh-CN>目标数据库连接串；默认使用开发 LocalDB。真实环境请显式传入，不得把真实连接串写进本脚本默认值。</zh-CN>
    <en>Target database connection string; defaults to the development LocalDB. Pass it explicitly for a real
    environment — never hardcode a real connection string into this script's default.</en>
  </lang>
.EXAMPLE
  pwsh -NoProfile -File .\dev\scripts\Get-PortalTabDiscovery.ps1
  <lang>
    <zh-CN>以开发库默认连接输出全部页签的 JSON。</zh-CN>
    <en>Emits JSON for all tabs using the default development connection.</en>
  </lang>
#>
[CmdletBinding()]
param(
    [string]$ConnectionString = 'Server=(localdb)\MSSQLLocalDB;Database=Portal;Trusted_Connection=True;'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>连接对象在 `finally` 中关闭：即使查询抛异常也不会泄漏数据库连接，这是本脚本所有 ADO.NET 调用的统一约束。
#   未设置为 `'Stop'` 以外的偏好时，非终止错误会被忽略，故这里显式用 Stop 配合 try/finally 保证释放。</zh-CN>
#   <en>The connection is closed in `finally`: even if the query throws, no database connection leaks — a uniform
#   constraint for every ADO.NET call here. Because non-terminating errors would otherwise be ignored, `Stop` is
#   combined with try/finally to guarantee release.</en>
# </lang>
$connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    # <lang>
    #   <zh-CN>只取 `TabID`（页签主键，稳定）与 `TabOrder`（`tabindex` 的真实来源）以及 `TabName`（诊断用），
    #   避免全列读取带来的无关开销与将来的架构漂移。</zh-CN>
    #   <en>Only `TabID` (the stable tab primary key), `TabOrder` (the true source of `tabindex`) and `TabName`
    #   (diagnostics) are selected, avoiding the overhead and future schema drift of a full-column read.</en>
    # </lang>
    $command.CommandText = 'SELECT TabID, TabOrder, TabName FROM PortalCfg_Tabs ORDER BY TabID'
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
    $table = New-Object System.Data.DataTable
    # <lang>
    #   <zh-CN>`Fill` 返回受影响行数，与 JSON 输出无关，故丢弃以免污染成功流。</zh-CN>
    #   <en>`Fill` returns the number of rows populated, which is irrelevant to the JSON output and therefore
    #   discarded so it cannot contaminate the success stream.</en>
    # </lang>
    $adapter.Fill($table) | Out-Null
    # <lang>
    #   <zh-CN>行转换为显式类型的小对象：DataTable 的行带有 DataRow 语义，直接序列化会产生多余列与不可控类型。</zh-CN>
    #   <en>Rows are converted into small explicitly typed objects: `DataRow` carries extra semantics that would
    #   serialize into redundant columns with unpredictable types.</en>
    # </lang>
    $rows = @($table.Rows | ForEach-Object {
        [pscustomobject]@{
            TabID    = [int]$_.TabID
            TabOrder = [int]$_.TabOrder
            TabName  = [string]$_.TabName
        }
    })
} finally {
    if ($connection.State -ne [System.Data.ConnectionState]::Closed) { $connection.Close() }
}

# <lang>
#   <zh-CN>以压缩 JSON 写入成功流供调用方（Node）直接 `JSON.parse`；诊断/提示一律走 `Write-Host`，不污染 JSON。</zh-CN>
#   <en>Compact JSON goes to the success stream for the caller (Node) to `JSON.parse` directly; every diagnostic
#   goes through `Write-Host` so it never contaminates the JSON.</en>
# </lang>
ConvertTo-Json -InputObject $rows -Compress
