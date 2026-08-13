<#
.SYNOPSIS
    Checks the P23.2 governed reference-data contract without changing a database.

.DESCRIPTION
    <lang>
      <zh-CN>本脚本执行静态门禁，确认 P23.2 参考数据迁移、只读目录服务、Unity 注册、类型/优先级消费者、写入层复核及迁移工具保持一致。它不读取连接串，也不连接数据库。</zh-CN>
      <en>This script performs static gates for the P23.2 reference-data migration, catalog reader, Unity registration, type/priority consumers, write-layer validation, and migration tooling. It reads no connection string and never connects to a database.</en>
    </lang>
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path,

    [string]$OutputJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$checks = New-Object 'System.Collections.Generic.List[object]'

# <lang>
#   <zh-CN>追加参考数据静态检查结果；Evidence 只保存调用方提供的低敏路径或说明。</zh-CN>
#   <en>Adds a reference-data static check result; Evidence contains only low-sensitivity paths or descriptions supplied by the caller.</en>
# </lang>
function Add-ReferenceDataCheck {
    param(
        [Parameter(Mandatory = $true)][string]$Code,
        [Parameter(Mandatory = $true)][ValidateSet('Pass', 'Warning', 'Fail', 'Info')][string]$Status,
        [Parameter(Mandatory = $true)][string]$Message,
        [string]$Evidence = ''
    )

    $checks.Add([pscustomobject]@{
            Code = $Code
            Status = $Status
            Message = $Message
            Evidence = $Evidence
        })
}

# <lang>
#   <zh-CN>读取仓库内必需文本；缺失文件立即失败，读取动作不连接数据库或解析连接串。</zh-CN>
#   <en>Reads required repository text; a missing file fails immediately, and reading never connects to a database or parses a connection string.</en>
# </lang>
function Get-PortalText {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file is missing: $RelativePath"
    }

    return [System.IO.File]::ReadAllText($path, [System.Text.UTF8Encoding]::new($false))
}

# <lang>
#   <zh-CN>检查文本是否按序包含全部稳定断言片段；这是静态契约检查，不执行片段代表的动作。</zh-CN>
#   <en>Checks whether text contains every stable assertion fragment; this is a static contract check and does not execute the represented actions.</en>
# </lang>
function Test-ContainsAll {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string[]]$Needles
    )

    foreach ($needle in $Needles) {
        if ($Text.IndexOf($needle, [StringComparison]::Ordinal) -lt 0) {
            return $false
        }
    }

    return $true
}

# <lang>
#   <zh-CN>以下阶段只读取 SQL、C#、ASPX、XML、项目和迁移工具文本，验证参考数据契约的静态连通性。</zh-CN>
#   <en>The following stage reads SQL, C#, ASPX, XML, project, and migration-tool text only to verify static reference-data contract connectivity.</en>
# </lang>
$referenceDataSql = Get-PortalText 'src/Setup/PortalBiz_ReferenceData.sql'
$referenceDataContract = Get-PortalText 'src/Portal.Components/IReferenceDataDb.cs'
$referenceDataItem = Get-PortalText 'src/Portal.Components/ReferenceDataItem.cs'
$referenceDataSets = Get-PortalText 'src/Portal.Components/PortalReferenceDataSets.cs'
$referenceDataDb = Get-PortalText 'src/Portal.Components.Data1/ReferenceDataDb.cs'
$collaborationDb = Get-PortalText 'src/Portal.Components.Data1/CollaborationItemDb.cs'
$workbenchCode = Get-PortalText 'src/Portal/DesktopModules/EnterpriseCapabilityWorkbench/EnterpriseCapabilityWorkbench.ascx.cs'
$adminMarkup = Get-PortalText 'src/Portal/Admin/CollaborationItems.aspx'
$adminCode = Get-PortalText 'src/Portal/Admin/CollaborationItems.aspx.cs'
$unityConfig = Get-PortalText 'src/Portal/Config/UnityCfg.xml'
$componentProject = Get-PortalText 'src/Portal.Components/Portal.Components.csproj'
$dataProject = Get-PortalText 'src/Portal.Components.Data1/Portal.Components.Data1.csproj'
$manifestScript = Get-PortalText 'dev/scripts/Get-PortalMigrationManifest.ps1'
$matrixScript = Get-PortalText 'dev/scripts/Test-PortalSqlVersionMatrix.ps1'
$compatibilityScript = Get-PortalText 'dev/scripts/Test-PortalSqlCompatibility.ps1'

$sqlCatalogOk = (Test-ContainsAll $referenceDataSql @(
        '[dbo].[PortalBiz_ReferenceData]',
        '[ReferenceSetKey]',
        '[ValueKey]',
        '[DisplayName]',
        '[IsActive]',
        '[IsSystemSeed]',
        '[UX_PortalBiz_ReferenceData_SetValue]',
        "N'CollaborationItemType'",
        "N'CollaborationPriority'",
        "N'General'",
        "N'Important'"
    )) -and -not $referenceDataSql.Contains('MERGE ')
Add-ReferenceDataCheck -Code 'P23-REFDATA-SQL-CATALOG' -Status $(if ($sqlCatalogOk) { 'Pass' } else { 'Fail' }) -Message 'Reference-data migration defines stable keyed catalog rows, idempotent seeds, and avoids MERGE.' -Evidence 'src/Setup/PortalBiz_ReferenceData.sql'

$contractOk = (Test-ContainsAll $referenceDataContract @('IReferenceDataDb', 'TryGetActiveItems')) -and
    (Test-ContainsAll $referenceDataItem @('ReferenceSetKey', 'ValueKey', 'DisplayName', 'IsActive')) -and
    (Test-ContainsAll $referenceDataSets @('CollaborationItemType', 'CollaborationPriority', 'GetFallbackItems', 'TryResolveFallbackValue')) -and
    (Test-ContainsAll $referenceDataDb @('PortalBiz_ReferenceData', 'WHERE [ReferenceSetKey] = @ReferenceSetKey', 'AND [IsActive] = 1', 'ORDER BY [SortOrder] ASC, [ValueKey] ASC'))
Add-ReferenceDataCheck -Code 'P23-REFDATA-READ-CONTRACT' -Status $(if ($contractOk) { 'Pass' } else { 'Fail' }) -Message 'Read-only catalog contract preserves stable keys, ordered active reads, and an undeployed-schema fallback.' -Evidence 'IReferenceDataDb.cs; ReferenceDataItem.cs; PortalReferenceDataSets.cs; ReferenceDataDb.cs'

$registrationOk = (Test-ContainsAll $unityConfig @('<register type="IReferenceDataDb" mapTo="ReferenceDataDb">')) -and
    (Test-ContainsAll $componentProject @('ReferenceDataItem.cs', 'IReferenceDataDb.cs', 'PortalReferenceDataSets.cs')) -and
    (Test-ContainsAll $dataProject @('ReferenceDataDb.cs'))
Add-ReferenceDataCheck -Code 'P23-REFDATA-REGISTRATION' -Status $(if ($registrationOk) { 'Pass' } else { 'Fail' }) -Message 'Reference-data service is registered and included in both legacy project files.' -Evidence 'UnityCfg.xml; Portal.Components.csproj; Portal.Components.Data1.csproj'

$consumerOk = (Test-ContainsAll $workbenchCode @('IReferenceDataDb ReferenceDataDb', 'BindReferenceDataList(ItemTypeList, PortalReferenceDataSets.CollaborationItemType)', 'BindReferenceDataList(PriorityList, PortalReferenceDataSets.CollaborationPriority)')) -and
    (Test-ContainsAll $adminMarkup @('<asp:DropDownList ID="ItemTypeList"', '<asp:DropDownList ID="PriorityList"')) -and
    (Test-ContainsAll $adminCode @('IReferenceDataDb ReferenceDataDb', 'BindReferenceDataLists()', 'ItemTypeKey = ItemTypeList.SelectedValue', 'BindReferenceDataList(PriorityList, PortalReferenceDataSets.CollaborationPriority)'))
Add-ReferenceDataCheck -Code 'P23-REFDATA-CONSUMERS' -Status $(if ($consumerOk) { 'Pass' } else { 'Fail' }) -Message 'Workbench and administration create forms consume the same governed type and priority source.' -Evidence 'EnterpriseCapabilityWorkbench.ascx.cs; CollaborationItems.aspx; CollaborationItems.aspx.cs'

$writeGateOk = Test-ContainsAll $collaborationDb @(
    'IReferenceDataDb referenceDataDb',
    'TryResolveActiveReferenceValue(PortalReferenceDataSets.CollaborationItemType',
    'TryResolveActiveReferenceValue(PortalReferenceDataSets.CollaborationPriority',
    'The collaboration item type is not allowed.',
    'The collaboration item priority is not allowed.',
    'TryResolveFallbackValue'
)
Add-ReferenceDataCheck -Code 'P23-REFDATA-WRITE-GATE' -Status $(if ($writeGateOk) { 'Pass' } else { 'Fail' }) -Message 'Collaboration-item writes canonicalize and reject type or priority values outside the active catalog or controlled fallback.' -Evidence 'src/Portal.Components.Data1/CollaborationItemDb.cs'

$toolingOk = (Test-ContainsAll $manifestScript @('PortalBiz_ReferenceData.sql', 'BusinessReferenceData')) -and
    (Test-ContainsAll $matrixScript @('PortalBiz_ReferenceData.sql')) -and
    (Test-ContainsAll $compatibilityScript @('ApplyP23ReferenceDataMigration', 'RequireP23ReferenceDataMigration', 'P23.2 reference-data seed coverage'))
Add-ReferenceDataCheck -Code 'P23-REFDATA-MIGRATION-TOOLING' -Status $(if ($toolingOk) { 'Pass' } else { 'Fail' }) -Message 'Migration manifest, SQL version matrix, and opt-in live compatibility checks include the P23.2 catalog.' -Evidence 'Get-PortalMigrationManifest.ps1; Test-PortalSqlVersionMatrix.ps1; Test-PortalSqlCompatibility.ps1'

# <lang>
#   <zh-CN>汇总只反映静态断言结果；通过不等于数据库迁移、Unity 运行时或业务页面已经回归。</zh-CN>
#   <en>The summary reflects static assertions only; passing does not prove database migration, Unity runtime, or business-page regression.</en>
# </lang>
$failedChecks = @($checks | Where-Object { $_.Status -eq 'Fail' })
$warningChecks = @($checks | Where-Object { $_.Status -eq 'Warning' })
$result = [pscustomobject]@{
    GeneratedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    TotalChecks = $checks.Count
    FailedChecks = $failedChecks.Count
    WarningChecks = $warningChecks.Count
    Checks = @($checks.ToArray())
}

# <lang>
#   <zh-CN>只有显式提供 OutputJson 才写入 JSON；失败检查通过异常退出，不伪造通过结果。</zh-CN>
#   <en>JSON is written only when OutputJson is explicitly supplied; failed checks exit through an exception and never fabricate a pass.</en>
# </lang>
if ($OutputJson) {
    $jsonPath = if ([System.IO.Path]::IsPathRooted($OutputJson)) { $OutputJson } else { Join-Path $RepoRoot $OutputJson }
    $jsonDirectory = Split-Path -Parent $jsonPath
    if (-not [string]::IsNullOrWhiteSpace($jsonDirectory)) {
        New-Item -ItemType Directory -Force -Path $jsonDirectory | Out-Null
    }

    [System.IO.File]::WriteAllText($jsonPath, ($result | ConvertTo-Json -Depth 5), [System.Text.UTF8Encoding]::new($false))
}

$result

if ($failedChecks.Count -gt 0) {
    throw ('Portal reference-data smoke test failed: ' + (($failedChecks | ForEach-Object { $_.Code }) -join ', '))
}
