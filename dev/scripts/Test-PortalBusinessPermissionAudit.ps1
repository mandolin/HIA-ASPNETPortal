<#
.SYNOPSIS
    Checks the P12.4 business permission and audit contract without changing the database.

.DESCRIPTION
    中文：本脚本只做静态门禁，确认 P12.4 拆分后的业务权限键、Admin 兼容 seed、
    页面授权入口、待办分派键和关键运营审计调用保持一致。
    English: This script performs static checks only, ensuring P12.4 fine-grained business
    permission keys, Admin compatibility seeds, page authorization gates, work-item assignment,
    and key operation-audit calls stay aligned.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path,

    [string]$OutputJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$checks = New-Object 'System.Collections.Generic.List[object]'

function Add-BusinessPermissionCheck {
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

$authorization = Get-PortalText 'src/Portal/Components/PortalAuthorization.cs'
$permissions = Get-PortalText 'src/Portal.Components/PortalPermissions.cs'
$rolePermissionsSql = Get-PortalText 'src/Setup/PortalCfg_RolePermissions.sql'
$workItemsCode = Get-PortalText 'src/Portal/Admin/WorkItems.aspx.cs'
$correctionAdminCode = Get-PortalText 'src/Portal/Admin/EmployeeProfileCorrectionRequests.aspx.cs'
$correctionModuleCode = Get-PortalText 'src/Portal/DesktopModules/EmployeeProfileCorrectionRequest/EmployeeProfileCorrectionRequest.ascx.cs'
$employeeDirectoryCode = Get-PortalText 'src/Portal/Admin/EmployeeDirectory.aspx.cs'
$employeeEditCode = Get-PortalText 'src/Portal/Admin/EmployeeEdit.aspx.cs'
$organizationEditCode = Get-PortalText 'src/Portal/Admin/OrganizationUnitEdit.aspx.cs'
$bindingEditCode = Get-PortalText 'src/Portal/Admin/UserEmployeeBindingEdit.aspx.cs'
$auditEvents = Get-PortalText 'src/Portal/Components/PortalOperationAuditEvents.cs'

$authorizationOk = Test-ContainsAll $authorization @(
    'public static bool HasAnyPermission(params string[] permissionKeys)',
    'public static bool EnsureAnyPermission(HttpContext context, params string[] permissionKeys)',
    'TryNormalizePermissionKeys',
    'PermissionKeys='
)
Add-BusinessPermissionCheck `
    -Code 'P12-BIZAUTH-ANY-PERMISSION' `
    -Status $(if ($authorizationOk) { 'Pass' } else { 'Fail' }) `
    -Message 'PortalAuthorization exposes any-permission helpers for fine-grained transition gates.' `
    -Evidence 'src/Portal/Components/PortalAuthorization.cs'

$permissionKeys = @(
    'EmployeeDirectory.View',
    'EmployeeDirectory.Edit',
    'EmployeeDirectory.Bind',
    'EmployeeProfileCorrectionRequest.Review',
    'EmployeeProfileCorrectionRequest.Cancel',
    'EmployeeProfileCorrectionRequest.Admin',
    'Business.WorkItems.View',
    'Business.WorkItems.Handle',
    'Business.WorkItems.Admin'
)
$permissionRegistryOk = (Test-ContainsAll $permissions $permissionKeys) -and
    (Test-ContainsAll $rolePermissionsSql $permissionKeys)
Add-BusinessPermissionCheck `
    -Code 'P12-BIZAUTH-PERMISSION-REGISTRY' `
    -Status $(if ($permissionRegistryOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Fine-grained business permissions are registered in code and seeded for the Admins compatibility role.' `
    -Evidence 'PortalPermissions.cs; PortalCfg_RolePermissions.sql'

$workItemGateOk = Test-ContainsAll $workItemsCode @(
    'PortalAuthorization.EnsureAnyPermission',
    'PortalPermissionKeys.BusinessWorkItemsView',
    'PortalPermissionKeys.BusinessWorkItemsAdmin',
    'GetAdminWorkItems'
)
Add-BusinessPermissionCheck `
    -Code 'P12-BIZAUTH-WORKITEM-VIEW-GATE' `
    -Status $(if ($workItemGateOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Work-item administration list uses a read permission while preserving the legacy aggregate permission.' `
    -Evidence 'src/Portal/Admin/WorkItems.aspx.cs'

$correctionGateOk = Test-ContainsAll $correctionAdminCode @(
    'EnsureCanViewRequests',
    'EnsureCanApplyRequestStatus',
    'PortalPermissionKeys.EmployeeProfileCorrectionRequestReview',
    'PortalPermissionKeys.EmployeeProfileCorrectionRequestCancel',
    'PortalPermissionKeys.EmployeeProfileCorrectionRequestAdmin',
    'IsSupportedTargetStatus',
    'PortalOperationAudit.Record',
    'EmployeeProfileCorrectionReviewed'
)
Add-BusinessPermissionCheck `
    -Code 'P12-BIZAUTH-CORRECTION-GATE' `
    -Status $(if ($correctionGateOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Employee-profile correction administration separates review and cancel commands and keeps audit recording.' `
    -Evidence 'src/Portal/Admin/EmployeeProfileCorrectionRequests.aspx.cs'

$workItemAssignmentOk = Test-ContainsAll $correctionModuleCode @(
    'TryEnsureWorkItem(result.RequestId, profile.EmployeeId, fieldName)',
    'AssignedRoleKey = PortalPermissionKeys.EmployeeProfileCorrectionRequestReview',
    'EmployeeProfileCorrectionRequested',
    'PortalOperationAudit.Record'
)
Add-BusinessPermissionCheck `
    -Code 'P12-BIZAUTH-CORRECTION-WORKITEM-ASSIGNMENT' `
    -Status $(if ($workItemAssignmentOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Correction requests assign new work items to the review permission rather than the legacy aggregate key.' `
    -Evidence 'EmployeeProfileCorrectionRequest.ascx.cs'

$directoryGateOk = (Test-ContainsAll $employeeDirectoryCode @(
    'PortalAuthorization.EnsureAnyPermission',
    'PortalPermissionKeys.EmployeeDirectoryView',
    'PortalPermissionKeys.EmployeeDirectoryEdit'
)) -and
(Test-ContainsAll $employeeEditCode @(
    'PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.EmployeeDirectoryEdit)',
    'EmployeeCreated',
    'EmployeeUpdated',
    'PortalOperationAudit.Record'
)) -and
(Test-ContainsAll $organizationEditCode @(
    'PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.EmployeeDirectoryEdit)',
    'OrganizationUnitCreated',
    'OrganizationUnitUpdated',
    'PortalOperationAudit.Record'
)) -and
(Test-ContainsAll $bindingEditCode @(
    'PortalPermissionKeys.EmployeeDirectoryBind',
    'UserEmployeeBound',
    'UserEmployeeUnbound',
    'PortalOperationAudit.Record'
))
Add-BusinessPermissionCheck `
    -Code 'P12-BIZAUTH-DIRECTORY-GATES' `
    -Status $(if ($directoryGateOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Employee directory view, edit, and binding pages use separate permissions and keep high-value audit calls.' `
    -Evidence 'EmployeeDirectory.aspx.cs; EmployeeEdit.aspx.cs; OrganizationUnitEdit.aspx.cs; UserEmployeeBindingEdit.aspx.cs'

$auditBoundaryOk = Test-ContainsAll $auditEvents @(
    'EnterpriseDirectoryCategory',
    'BusinessModuleCategory',
    'EmployeeCreated',
    'EmployeeUpdated',
    'UserEmployeeBound',
    'UserEmployeeUnbound',
    'EmployeeProfileCorrectionRequested',
    'EmployeeProfileCorrectionReviewed'
)
Add-BusinessPermissionCheck `
    -Code 'P12-BIZAUDIT-EVENT-CATALOG' `
    -Status $(if ($auditBoundaryOk) { 'Pass' } else { 'Fail' }) `
    -Message 'Operation-audit event catalog covers the first employee-directory and correction-request business actions.' `
    -Evidence 'src/Portal/Components/PortalOperationAuditEvents.cs'

$failedChecks = @($checks | Where-Object { $_.Status -eq 'Fail' })
$warningChecks = @($checks | Where-Object { $_.Status -eq 'Warning' })
$result = [pscustomobject]@{
    GeneratedUtc   = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    TotalChecks    = $checks.Count
    FailedChecks   = $failedChecks.Count
    WarningChecks  = $warningChecks.Count
    Checks         = @($checks.ToArray())
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
    throw ('Portal business permission/audit smoke test failed: ' + (($failedChecks | ForEach-Object { $_.Code }) -join ', '))
}
