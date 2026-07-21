<#
.SYNOPSIS
    Checks the P12.3 lightweight work-item contract without changing the database.

.DESCRIPTION
    中文：本脚本只做静态门禁，确认轻量待办的 SQL、契约、Unity 注册、后台入口、
    员工资料更正同步点和迁移工具声明仍保持连通。
    English: This script performs static checks only, ensuring the lightweight work-item SQL,
    contracts, Unity registration, administration entry, employee-profile correction sync points,
    and migration-tool declarations remain connected.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path,

    [string]$OutputJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$checks = New-Object 'System.Collections.Generic.List[object]'

function Add-WorkItemCheck {
    param(
        [Parameter(Mandatory = $true)][string]$Code,
        [Parameter(Mandatory = $true)][ValidateSet('Pass', 'Warning', 'Fail', 'Info')][string]$Status,
        [Parameter(Mandatory = $true)][string]$Message,
        [string]$Evidence = ''
    )

    $checks.Add([pscustomobject]@{
            Code     = $Code
            Status   = $Status
            Message  = $Message
            Evidence = $Evidence
        })
}

function Get-PortalText {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file is missing: $RelativePath"
    }

    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

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

$workItemsSql = Get-PortalText 'src/Setup/PortalBiz_WorkItems.sql'
$workItemEventsSql = Get-PortalText 'src/Setup/PortalBiz_WorkItemEvents.sql'
$workItemContract = Get-PortalText 'src/Portal.Components/IPortalWorkItemDb.cs'
$workItemDb = Get-PortalText 'src/Portal.Components.Data1/PortalWorkItemDb.cs'
$workItemPage = Get-PortalText 'src/Portal/Admin/WorkItems.aspx'
$workItemPageCode = Get-PortalText 'src/Portal/Admin/WorkItems.aspx.cs'
$correctionModule = Get-PortalText 'src/Portal/DesktopModules/EmployeeProfileCorrectionRequest/EmployeeProfileCorrectionRequest.ascx.cs'
$correctionAdminMarkup = Get-PortalText 'src/Portal/Admin/EmployeeProfileCorrectionRequests.aspx'
$correctionAdminCode = Get-PortalText 'src/Portal/Admin/EmployeeProfileCorrectionRequests.aspx.cs'
$permissions = Get-PortalText 'src/Portal.Components/PortalPermissions.cs'
$rolePermissionsSql = Get-PortalText 'src/Setup/PortalCfg_RolePermissions.sql'
$unityCfg = Get-PortalText 'src/Portal/Config/UnityCfg.xml'
$componentsProject = Get-PortalText 'src/Portal.Components/Portal.Components.csproj'
$dataProject = Get-PortalText 'src/Portal.Components.Data1/Portal.Components.Data1.csproj'
$portalProject = Get-PortalText 'src/Portal/Portal.csproj'
$manifestScript = Get-PortalText 'dev/scripts/Get-PortalMigrationManifest.ps1'
$matrixScript = Get-PortalText 'dev/scripts/Test-PortalSqlVersionMatrix.ps1'
$compatibilityScript = Get-PortalText 'dev/scripts/Test-PortalSqlCompatibility.ps1'

$workItemSqlOk = Test-ContainsAll $workItemsSql @(
    '[dbo].[PortalBiz_WorkItems]',
    '[WorkItemStatus] IN (N''Open'', N''InProgress'', N''Completed'', N''Cancelled'', N''Expired'')',
    '[UX_PortalBiz_WorkItems_ActiveBusiness]',
    '[IX_PortalBiz_WorkItems_AssignedRoleStatus]',
    'Portal_Users must exist before PortalBiz_WorkItems.'
)
Add-WorkItemCheck `
    -Code 'P12-WORKITEM-SQL' `
    -Status $(if ($workItemSqlOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Work-item SQL defines the P12.3 table, stable statuses, assignment indexes, and base dependency.' `
    -Evidence 'src/Setup/PortalBiz_WorkItems.sql'

$workItemEventSqlOk = Test-ContainsAll $workItemEventsSql @(
    '[dbo].[PortalBiz_WorkItemEvents]',
    '[FK_PortalBiz_WorkItemEvents_WorkItems]',
    '[EventType] IN (N''Created'', N''Claimed'', N''Approved'', N''Rejected'', N''Cancelled'', N''Commented'', N''Completed'', N''Reopened'')',
    '[IX_PortalBiz_WorkItemEvents_WorkItemUtc]',
    'PortalBiz_WorkItems must be created before PortalBiz_WorkItemEvents.'
)
Add-WorkItemCheck `
    -Code 'P12-WORKITEM-EVENT-SQL' `
    -Status $(if ($workItemEventSqlOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Work-item event SQL defines event history and references work items.' `
    -Evidence 'src/Setup/PortalBiz_WorkItemEvents.sql'

$contractOk = (Test-ContainsAll $workItemContract @(
    'bool IsSchemaAvailable()',
    'PortalWorkItemResult EnsureWorkItem',
    'PortalWorkItemResult CompleteBusinessWorkItem',
    'IList<PortalWorkItemInfo> GetAdminWorkItems',
    'IList<PortalWorkItemEventInfo> GetWorkItemEvents'
)) -and
(Test-ContainsAll (Get-PortalText 'src/Portal.Components/PortalWorkItemStatuses.cs') @('Open', 'InProgress', 'Completed', 'Cancelled', 'Expired')) -and
(Test-ContainsAll (Get-PortalText 'src/Portal.Components/PortalWorkItemBusinessKinds.cs') @('EmployeeProfileCorrectionRequest'))
Add-WorkItemCheck `
    -Code 'P12-WORKITEM-CONTRACT' `
    -Status $(if ($contractOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Work-item component contracts expose schema check, create, complete, list, and event-query operations.' `
    -Evidence 'src/Portal.Components/IPortalWorkItemDb.cs'

$dataServiceOk = Test-ContainsAll $workItemDb @(
    'public sealed class PortalWorkItemDb : IPortalWorkItemDb',
    'public bool IsSchemaAvailable()',
    'public PortalWorkItemResult EnsureWorkItem',
    'public PortalWorkItemResult CompleteBusinessWorkItem',
    'public IList<PortalWorkItemInfo> GetAdminWorkItems',
    'public IList<PortalWorkItemEventInfo> GetWorkItemEvents',
    'Portal work-item schema is unavailable.'
)
Add-WorkItemCheck `
    -Code 'P12-WORKITEM-DATA-SERVICE' `
    -Status $(if ($dataServiceOk) { 'Pass' } else { 'Fail' }) `
    -Message 'PortalWorkItemDb provides the first SQL Server-backed lightweight work-item implementation.' `
    -Evidence 'src/Portal.Components.Data1/PortalWorkItemDb.cs'

$registrationOk = (Test-ContainsAll $unityCfg @('IPortalWorkItemDb', 'PortalWorkItemDb')) -and
    (Test-ContainsAll $componentsProject @(
        'IPortalWorkItemDb.cs',
        'PortalWorkItemCreateRequest.cs',
        'PortalWorkItemCompletionRequest.cs',
        'PortalWorkItemInfo.cs',
        'PortalWorkItemEventInfo.cs',
        'PortalWorkItemStatuses.cs'
    )) -and
    (Test-ContainsAll $dataProject @('PortalWorkItemDb.cs')) -and
    (Test-ContainsAll $portalProject @('Admin\WorkItems.aspx', 'Admin\WorkItems.aspx.cs', 'Admin\WorkItems.aspx.designer.cs'))
Add-WorkItemCheck `
    -Code 'P12-WORKITEM-PROJECT-WIRING' `
    -Status $(if ($registrationOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Unity and Visual Studio project files include the work-item contracts, implementation, and administration page.' `
    -Evidence 'UnityCfg.xml; *.csproj'

$permissionOk = (Test-ContainsAll $permissions @(
    'BusinessWorkItemsView',
    'Business.WorkItems.View',
    'BusinessWorkItemsHandle',
    'Business.WorkItems.Handle',
    'BusinessWorkItemsAdmin',
    'Business.WorkItems.Admin'
)) -and (Test-ContainsAll $rolePermissionsSql @(
    'Business.WorkItems.View',
    'Business.WorkItems.Handle',
    'Business.WorkItems.Admin'
))
Add-WorkItemCheck `
    -Code 'P12-WORKITEM-PERMISSION' `
    -Status $(if ($permissionOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Work-item administration has a stable permission key and Admins seed.' `
    -Evidence 'PortalPermissions.cs; PortalCfg_RolePermissions.sql'

$adminPageOk = (Test-ContainsAll $workItemPage @(
    'Inherits="ASPNET.StarterKit.Portal.WorkItems"',
    'StatusFilterList',
    'Current Work Items',
    'Correction Requests'
)) -and (Test-ContainsAll $workItemPageCode @(
    'PortalAuthorization.EnsureAnyPermission',
    'PortalPermissionKeys.BusinessWorkItemsView',
    'PortalPermissionKeys.BusinessWorkItemsAdmin',
    'WorkItemDb.IsSchemaAvailable()',
    'GetAdminWorkItems',
    'EmployeeProfileCorrectionRequests.aspx'
))
Add-WorkItemCheck `
    -Code 'P12-WORKITEM-ADMIN-PAGE' `
    -Status $(if ($adminPageOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Admin/WorkItems.aspx exposes the read-only P12.3 work-item list with permission gating.' `
    -Evidence 'src/Portal/Admin/WorkItems.aspx'

$businessSyncOk = (Test-ContainsAll $correctionModule @(
    'IPortalWorkItemDb WorkItemDb',
    'TryEnsureWorkItem(result.RequestId, profile.EmployeeId, fieldName)',
    'PortalWorkItemBusinessKinds.EmployeeProfileCorrectionRequest',
    'AssignedRoleKey = PortalPermissionKeys.EmployeeProfileCorrectionRequestReview'
)) -and (Test-ContainsAll $correctionAdminCode @(
    'IPortalWorkItemDb WorkItemDb',
    'TryCompleteWorkItem(result.RequestId, targetStatus, reviewNote)',
    'CompleteBusinessWorkItem',
    'MapWorkItemEventType',
    'MapWorkItemStatus'
)) -and (Test-ContainsAll $correctionAdminMarkup @(
    'href="WorkItems.aspx"',
    'Text="Approve"',
    'Text="Cancel"',
    'Text="Reject"'
))
Add-WorkItemCheck `
    -Code 'P12-WORKITEM-BUSINESS-SYNC' `
    -Status $(if ($businessSyncOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Employee profile correction requests now create and complete lightweight work items as a non-blocking side path.' `
    -Evidence 'EmployeeProfileCorrectionRequest*.cs; EmployeeProfileCorrectionRequests.aspx'

$migrationToolOk = (Test-ContainsAll $manifestScript @(
    'src/Setup/PortalBiz_WorkItems.sql',
    'src/Setup/PortalBiz_WorkItemEvents.sql',
    'BusinessWorkflow'
)) -and (Test-ContainsAll $matrixScript @(
    'PortalBiz_WorkItems.sql',
    'PortalBiz_WorkItemEvents.sql'
)) -and (Test-ContainsAll $compatibilityScript @(
    'ApplyP12WorkItemMigration',
    'RequireP12WorkItemMigration',
    'PortalBiz_WorkItems',
    'PortalBiz_WorkItemEvents'
))
Add-WorkItemCheck `
    -Code 'P12-WORKITEM-MIGRATION-TOOLS' `
    -Status $(if ($migrationToolOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Migration manifest, SQL version matrix, and optional SQL compatibility checks know about the P12.3 work-item scripts.' `
    -Evidence 'dev/scripts/*Migration*.ps1; Test-PortalSql*.ps1'

$failedChecks = @($checks | Where-Object { $_.Status -eq 'Fail' })
$warningChecks = @($checks | Where-Object { $_.Status -eq 'Warning' })
$result = [pscustomobject]@{
    GeneratedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    TotalChecks  = $checks.Count
    FailedChecks = $failedChecks.Count
    WarningChecks = $warningChecks.Count
    Checks       = @($checks.ToArray())
}

if ($OutputJson) {
    $jsonPath = if ([System.IO.Path]::IsPathRooted($OutputJson)) {
        $OutputJson
    }
    else {
        Join-Path $RepoRoot $OutputJson
    }
    $jsonDirectory = Split-Path -Parent $jsonPath
    if (-not [string]::IsNullOrWhiteSpace($jsonDirectory)) {
        New-Item -ItemType Directory -Force -Path $jsonDirectory | Out-Null
    }

    [System.IO.File]::WriteAllText(
        $jsonPath,
        ($result | ConvertTo-Json -Depth 5),
        [System.Text.UTF8Encoding]::new($false))
}

$result

if ($failedChecks.Count -gt 0) {
    throw ('Portal work-item smoke test failed: ' + (($failedChecks | ForEach-Object { $_.Code }) -join ', '))
}
