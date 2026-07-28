<#
.SYNOPSIS
    Checks the P23.6 collaboration-item comment and workflow-rule implementation without changing a database.

.DESCRIPTION
    中文：本脚本执行 P23.6 静态门禁，核对事件时间线扩展、状态机、服务端身份复核、评论可见范围、
    超期只读投影、页面审计及迁移登记。它不读取连接串，也不连接数据库。
    English: This script performs P23.6 static gates for the event-timeline extension, state machine,
    server-side identity recheck, comment visibility, read-only overdue projection, page audits, and
    migration registration. It reads no connection string and never connects to a database.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path,

    [string]$OutputJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$checks = New-Object 'System.Collections.Generic.List[object]'

function Add-WorkflowCheck {
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

function Get-PortalText {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file is missing: $RelativePath"
    }

    return [System.IO.File]::ReadAllText($path, [System.Text.UTF8Encoding]::new($false))
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

$migrationSql = Get-PortalText 'src/Setup/PortalBiz_CollaborationItemCommentWorkflow.sql'
$dataAccess = Get-PortalText 'src/Portal.Components.Data1/CollaborationItemDb.cs'
$contract = Get-PortalText 'src/Portal.Components/ICollaborationItemDb.cs'
$componentProject = Get-PortalText 'src/Portal.Components/Portal.Components.csproj'
$workflowActions = Get-PortalText 'src/Portal.Components/PortalCollaborationItemActions.cs'
$eventTypes = Get-PortalText 'src/Portal.Components/PortalCollaborationItemEventTypes.cs'
$visibilityScopes = Get-PortalText 'src/Portal.Components/PortalCollaborationItemVisibilityScopes.cs'
$workbenchMarkup = Get-PortalText 'src/Portal/DesktopModules/EnterpriseCapabilityWorkbench/EnterpriseCapabilityWorkbench.ascx'
$workbenchCode = Get-PortalText 'src/Portal/DesktopModules/EnterpriseCapabilityWorkbench/EnterpriseCapabilityWorkbench.ascx.cs'
$adminMarkup = Get-PortalText 'src/Portal/Admin/CollaborationItems.aspx'
$adminCode = Get-PortalText 'src/Portal/Admin/CollaborationItems.aspx.cs'
$auditEvents = Get-PortalText 'src/Portal/Components/PortalOperationAuditEvents.cs'
$manifestScript = Get-PortalText 'dev/scripts/Get-PortalMigrationManifest.ps1'
$matrixScript = Get-PortalText 'dev/scripts/Test-PortalSqlVersionMatrix.ps1'
$compatibilityScript = Get-PortalText 'dev/scripts/Test-PortalSqlCompatibility.ps1'

$migrationOk = (Test-ContainsAll $migrationSql @(
        'PortalBiz_CollaborationItemEvents',
        '[EventType] NVARCHAR(30) NOT NULL',
        '[VisibilityScope] NVARCHAR(30) NOT NULL',
        'ALTER COLUMN [ActionKey] NVARCHAR(40) NULL',
        "N'WorkflowAction'",
        "N'Comment'",
        "N'Resubmit'",
        'CK_PortalBiz_CollaborationItemEvents_Shape',
        'IX_PortalBiz_CollaborationItemEvents_ItemVisibilityUtc'
    )) -and -not $migrationSql.Contains('PortalBiz_CollaborationItemComments')
Add-WorkflowCheck -Code 'P23-WORKFLOW-MIGRATION' -Status $(if ($migrationOk) { 'Pass' } else { 'Fail' }) -Message 'The idempotent migration extends the existing event timeline without a parallel comment table.' -Evidence 'src/Setup/PortalBiz_CollaborationItemCommentWorkflow.sql'

$contractOk = (Test-ContainsAll $contract @('GetVisibleEvents', 'AddComment')) -and
    (Test-ContainsAll $workflowActions @('Resubmit')) -and
    (Test-ContainsAll $eventTypes @('WorkflowAction', 'Comment')) -and
    (Test-ContainsAll $visibilityScopes @('ItemParticipants', 'Administrators')) -and
    (Test-ContainsAll $componentProject @('CollaborationItemCommentCreateRequest.cs', 'CollaborationItemCommentResult.cs', 'CollaborationItemEventInfo.cs', 'PortalCollaborationItemEventTypes.cs', 'PortalCollaborationItemVisibilityScopes.cs'))
Add-WorkflowCheck -Code 'P23-WORKFLOW-CONTRACT' -Status $(if ($contractOk) { 'Pass' } else { 'Fail' }) -Message 'Stable event, visibility, resubmission, and comment contracts are compiled by the legacy component project.' -Evidence 'ICollaborationItemDb.cs; Portal.Components.csproj'

$serverAuthorizationOk = Test-ContainsAll $dataAccess @(
    'usersDb.FindUserById',
    'rolesDb.GetPermissionKeysByUserName',
    'CanApplyAction',
    'CanParticipate',
    'PortalCollaborationItemActions.Resubmit',
    "@ActionKey = N'Resubmit' AND [ItemStatus] = N'Returned'",
    "@ActionKey = N'Submit' AND [ItemStatus] = N'Draft'",
    'ActionRequiresComment',
    'A plain-text handling comment is required for this action.',
    'WHERE [ItemId] = @ItemId'
)
Add-WorkflowCheck -Code 'P23-WORKFLOW-SERVER-AUTH' -Status $(if ($serverAuthorizationOk) { 'Pass' } else { 'Fail' }) -Message 'State writes re-resolve the actor and retain an atomic current-status predicate for the full P23.5 transition matrix.' -Evidence 'src/Portal.Components.Data1/CollaborationItemDb.cs'

$commentMethodStart = $dataAccess.IndexOf('public CollaborationItemCommentResult AddComment', [StringComparison]::Ordinal)
$actionMethodStart = $dataAccess.IndexOf('public CollaborationItemResult ApplyAction', [StringComparison]::Ordinal)
$commentMethod = if ($commentMethodStart -ge 0 -and $actionMethodStart -gt $commentMethodStart) {
    $dataAccess.Substring($commentMethodStart, $actionMethodStart - $commentMethodStart)
}
else {
    ''
}
$commentBoundaryOk = (Test-ContainsAll $dataAccess @(
        'public IList<CollaborationItemEventInfo> GetVisibleEvents'
    )) -and (Test-ContainsAll $commentMethod @(
        'public CollaborationItemCommentResult AddComment',
        "N'Comment', NULL, @VisibilityScope",
        'Only collaboration-item administrators can add administrator-visible comments.',
        'The plain-text comment cannot exceed 1000 characters.'
    )) -and (Test-ContainsAll $dataAccess @(
        "[Event].[EventType] = N'WorkflowAction'",
        "[Event].[VisibilityScope] = N'ItemParticipants'"
    )) -and -not $commentMethod.Contains('UPDATE [dbo].[PortalBiz_CollaborationItems]')
Add-WorkflowCheck -Code 'P23-WORKFLOW-COMMENT-BOUNDARY' -Status $(if ($commentBoundaryOk) { 'Pass' } else { 'Fail' }) -Message 'Comments have server-side participant visibility checks and do not update the item fact or latest workflow comment projection.' -Evidence 'src/Portal.Components.Data1/CollaborationItemDb.cs'

$uiAndAuditOk = (Test-ContainsAll $workbenchMarkup @('AddParticipantComment', 'Resubmit')) -and
    (Test-ContainsAll $workbenchCode @('CollaborationItemDb.AddComment', 'CollaborationItemDb.ApplyAction', 'PortalOperationAuditEvents.CollaborationItemCommentAdded', 'PortalOperationAuditEvents.CollaborationItemResubmitted', 'GetVisibleEvents')) -and
    (Test-ContainsAll $adminMarkup @('AddParticipantComment', 'AddAdministratorComment', 'Resubmit')) -and
    (Test-ContainsAll $adminCode @('TryAddComment', 'PortalOperationAuditEvents.CollaborationItemStarted', 'PortalOperationAuditEvents.CollaborationItemResubmitted', 'PortalOperationAuditEvents.CollaborationItemCommentAdded', 'TryEnsureResubmittedWorkItem')) -and
    (Test-ContainsAll $auditEvents @('CollaborationItemStarted', 'CollaborationItemResubmitted', 'CollaborationItemCommentAdded'))
Add-WorkflowCheck -Code 'P23-WORKFLOW-UI-AUDIT' -Status $(if ($uiAndAuditOk) { 'Pass' } else { 'Fail' }) -Message 'Workbench and administration pages expose only the governed comment/resubmission paths and audit successful actions without copying comment bodies.' -Evidence 'EnterpriseCapabilityWorkbench; CollaborationItems.aspx; PortalOperationAuditEvents.cs'

$toolingOk = (Test-ContainsAll $manifestScript @('PortalBiz_CollaborationItemCommentWorkflow.sql', 'P23.6')) -and
    (Test-ContainsAll $matrixScript @('PortalBiz_CollaborationItemCommentWorkflow.sql')) -and
    (Test-ContainsAll $compatibilityScript @('ApplyP23CollaborationCommentWorkflowMigration', 'RequireP23CollaborationCommentWorkflowMigration', 'P23.6 collaboration comment/workflow schema'))
Add-WorkflowCheck -Code 'P23-WORKFLOW-MIGRATION-TOOLING' -Status $(if ($toolingOk) { 'Pass' } else { 'Fail' }) -Message 'Migration manifest, SQL version matrix, and opt-in live compatibility checks include P23.6.' -Evidence 'Get-PortalMigrationManifest.ps1; Test-PortalSqlVersionMatrix.ps1; Test-PortalSqlCompatibility.ps1'

$failedChecks = @($checks | Where-Object { $_.Status -eq 'Fail' })
$warningChecks = @($checks | Where-Object { $_.Status -eq 'Warning' })
$result = [pscustomobject]@{
    GeneratedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    TotalChecks = $checks.Count
    FailedChecks = $failedChecks.Count
    WarningChecks = $warningChecks.Count
    Checks = @($checks.ToArray())
}

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
    throw ('Portal collaboration workflow smoke test failed: ' + (($failedChecks | ForEach-Object { $_.Code }) -join ', '))
}
