<#
.SYNOPSIS
    Generates a development/test SQL seed for the P12.5 sample business scenario.

.DESCRIPTION
    中文：本脚本只生成 SQL 文件，不连接数据库、不创建门户用户、不写密码。生成的 SQL 仅用于开发或测试库，
    通过已有测试用户补齐组织、员工、用户资料和账号员工绑定，从而支持员工工号登录与资料更正样板路径。
    English: This script only generates a SQL file. It does not connect to a database, create Portal users, or write
    passwords. The generated SQL is for development or test databases only; it enriches an existing test user with
    organization, employee, profile, and binding records for the employee-code sign-in and correction-request sample path.
#>
[CmdletBinding()]
param(
    [string]$OutputPath = (Join-Path (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path 'temp/p12.5/PortalP12SampleScenario.sql'),

    [string]$PortalUserName = 'P12EmployeeDemo',

    [string]$EmployeeCode = 'P12-EMP-001',

    [string]$EmployeeDisplayName = 'P12 Test Employee',

    [string]$PreferredName = 'P12 Employee',

    [string]$WorkEmail = 'p12.employee@example.invalid',

    [string]$OrganizationCode = 'P12-DEMO-ORG',

    [string]$OrganizationName = 'P12 Demo Organization',

    [switch]$AllowRebind
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-SqlNVarCharLiteral {
    param([string]$Value)

    if ($null -eq $Value) {
        return 'NULL'
    }

    return "N'" + ($Value -replace "'", "''") + "'"
}

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

$allowRebindValue = if ($AllowRebind) { '1' } else { '0' }
$sql = @"
/*
    P12.5 员工资料更正样板路径开发/测试数据。
    P12.5 development/test data for the employee-profile correction sample path.

    重要边界 / Important boundary:
    1. 本 SQL 只应在开发库或测试库手动执行，不应直接用于生产库。
       Run this SQL manually in development or test databases only; do not run it directly in production.
    2. 本 SQL 不创建 Portal 用户，不设置或修改密码。请先用后台或现有脚本创建测试用户。
       This SQL does not create Portal users and does not set or change passwords. Create the test user first.
    3. 默认不改写已有 active 绑定冲突；如确需重绑测试数据，将 @AllowRebind 改为 1。
       By default, existing active binding conflicts are not overwritten. Set @AllowRebind to 1 only for test rebinding.
*/

SET NOCOUNT ON;

DECLARE @PortalUserName NVARCHAR(100) = $(ConvertTo-SqlNVarCharLiteral $PortalUserName);
DECLARE @EmployeeCode NVARCHAR(64) = $(ConvertTo-SqlNVarCharLiteral $EmployeeCode);
DECLARE @EmployeeDisplayName NVARCHAR(150) = $(ConvertTo-SqlNVarCharLiteral $EmployeeDisplayName);
DECLARE @PreferredName NVARCHAR(100) = $(ConvertTo-SqlNVarCharLiteral $PreferredName);
DECLARE @WorkEmail NVARCHAR(256) = $(ConvertTo-SqlNVarCharLiteral $WorkEmail);
DECLARE @OrganizationCode NVARCHAR(100) = $(ConvertTo-SqlNVarCharLiteral $OrganizationCode);
DECLARE @OrganizationName NVARCHAR(150) = $(ConvertTo-SqlNVarCharLiteral $OrganizationName);
DECLARE @AllowRebind BIT = $allowRebindValue;
DECLARE @Actor NVARCHAR(100) = N'P12.5SampleSeed';

IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_UserProfiles]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_OrganizationUnits]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_Employees]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_UserEmployeeBindings]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_EmployeeProfileCorrectionRequests]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[PortalBiz_WorkItemEvents]', N'U') IS NULL
BEGIN
    RAISERROR(N'P12 sample scenario requires P6.2/P6.3/P6.4/P12.3 business tables. Run migrations first.', 16, 1);
    RETURN;
END

DECLARE @UserId INT;
SELECT TOP (1) @UserId = [UserID]
FROM [dbo].[Portal_Users]
WHERE [Name] = @PortalUserName
ORDER BY [UserID];

IF @UserId IS NULL
BEGIN
    RAISERROR(N'Test Portal user was not found. Create a development/test user first; this SQL does not create passwords.', 16, 1);
    RETURN;
END

DECLARE @OrganizationUnitId INT;
SELECT @OrganizationUnitId = [OrganizationUnitId]
FROM [dbo].[PortalBiz_OrganizationUnits]
WHERE [OrganizationCode] = @OrganizationCode;

IF @OrganizationUnitId IS NULL
BEGIN
    INSERT INTO [dbo].[PortalBiz_OrganizationUnits]
        ([ParentOrganizationUnitId], [OrganizationCode], [DisplayName], [SortOrder], [IsActive], [CreatedBy], [UpdatedBy])
    VALUES
        (NULL, @OrganizationCode, @OrganizationName, 1205, 1, @Actor, @Actor);

    SET @OrganizationUnitId = CONVERT(INT, SCOPE_IDENTITY());
END
ELSE
BEGIN
    UPDATE [dbo].[PortalBiz_OrganizationUnits]
    SET [DisplayName] = @OrganizationName,
        [IsActive] = 1,
        [UpdatedUtc] = SYSUTCDATETIME(),
        [UpdatedBy] = @Actor
    WHERE [OrganizationUnitId] = @OrganizationUnitId;
END

DECLARE @EmployeeId INT;
SELECT @EmployeeId = [EmployeeId]
FROM [dbo].[PortalBiz_Employees]
WHERE [EmployeeCode] = @EmployeeCode;

IF @EmployeeId IS NULL
BEGIN
    INSERT INTO [dbo].[PortalBiz_Employees]
        ([EmployeeCode], [DisplayName], [PreferredName], [WorkEmail], [OrganizationUnitId],
         [EmploymentStatus], [JoinedUtc], [SourceSystem], [CreatedBy], [UpdatedBy])
    VALUES
        (@EmployeeCode, @EmployeeDisplayName, @PreferredName, @WorkEmail, @OrganizationUnitId,
         N'Active', SYSUTCDATETIME(), N'P12.5Sample', @Actor, @Actor);

    SET @EmployeeId = CONVERT(INT, SCOPE_IDENTITY());
END
ELSE
BEGIN
    UPDATE [dbo].[PortalBiz_Employees]
    SET [DisplayName] = @EmployeeDisplayName,
        [PreferredName] = @PreferredName,
        [WorkEmail] = @WorkEmail,
        [OrganizationUnitId] = @OrganizationUnitId,
        [EmploymentStatus] = N'Active',
        [SourceSystem] = N'P12.5Sample',
        [UpdatedUtc] = SYSUTCDATETIME(),
        [UpdatedBy] = @Actor
    WHERE [EmployeeId] = @EmployeeId;
END

IF EXISTS
(
    SELECT 1
    FROM [dbo].[PortalBiz_UserEmployeeBindings]
    WHERE [BindingStatus] = N'Active'
      AND [UserId] = @UserId
      AND [EmployeeId] <> @EmployeeId
)
BEGIN
    IF @AllowRebind = 0
    BEGIN
        RAISERROR(N'The test user already has another active employee binding. Review manually or set @AllowRebind = 1 in a test database.', 16, 1);
        RETURN;
    END

    UPDATE [dbo].[PortalBiz_UserEmployeeBindings]
    SET [BindingStatus] = N'Ended',
        [EndedUtc] = SYSUTCDATETIME(),
        [EndedBy] = @Actor,
        [Reason] = N'P12.5 sample rebind.',
        [UpdatedUtc] = SYSUTCDATETIME(),
        [UpdatedBy] = @Actor
    WHERE [BindingStatus] = N'Active'
      AND [UserId] = @UserId
      AND [EmployeeId] <> @EmployeeId;
END

IF EXISTS
(
    SELECT 1
    FROM [dbo].[PortalBiz_UserEmployeeBindings]
    WHERE [BindingStatus] = N'Active'
      AND [EmployeeId] = @EmployeeId
      AND [UserId] <> @UserId
)
BEGIN
    IF @AllowRebind = 0
    BEGIN
        RAISERROR(N'The test employee already has another active user binding. Review manually or set @AllowRebind = 1 in a test database.', 16, 1);
        RETURN;
    END

    UPDATE [dbo].[PortalBiz_UserEmployeeBindings]
    SET [BindingStatus] = N'Ended',
        [EndedUtc] = SYSUTCDATETIME(),
        [EndedBy] = @Actor,
        [Reason] = N'P12.5 sample rebind.',
        [UpdatedUtc] = SYSUTCDATETIME(),
        [UpdatedBy] = @Actor
    WHERE [BindingStatus] = N'Active'
      AND [EmployeeId] = @EmployeeId
      AND [UserId] <> @UserId;
END

IF EXISTS
(
    SELECT 1
    FROM [dbo].[PortalBiz_UserEmployeeBindings]
    WHERE [BindingStatus] = N'Active'
      AND [UserId] = @UserId
      AND [EmployeeId] = @EmployeeId
)
BEGIN
    UPDATE [dbo].[PortalBiz_UserEmployeeBindings]
    SET [Reason] = N'P12.5 sample active binding.',
        [UpdatedUtc] = SYSUTCDATETIME(),
        [UpdatedBy] = @Actor
    WHERE [BindingStatus] = N'Active'
      AND [UserId] = @UserId
      AND [EmployeeId] = @EmployeeId;
END
ELSE
BEGIN
    INSERT INTO [dbo].[PortalBiz_UserEmployeeBindings]
        ([UserId], [EmployeeId], [BindingStatus], [BoundBy], [Reason], [UpdatedBy])
    VALUES
        (@UserId, @EmployeeId, N'Active', @Actor, N'P12.5 sample active binding.', @Actor);
END

IF EXISTS (SELECT 1 FROM [dbo].[PortalBiz_UserProfiles] WHERE [UserId] = @UserId)
BEGIN
    UPDATE [dbo].[PortalBiz_UserProfiles]
    SET [DisplayName] = @EmployeeDisplayName,
        [Nickname] = @PreferredName,
        [PreferredEmail] = @WorkEmail,
        [Status] = N'Active',
        [StatusReason] = N'P12.5 sample scenario.',
        [UpdatedUtc] = SYSUTCDATETIME(),
        [UpdatedBy] = @Actor
    WHERE [UserId] = @UserId;
END
ELSE
BEGIN
    INSERT INTO [dbo].[PortalBiz_UserProfiles]
        ([UserId], [LoginName], [DisplayName], [Nickname], [PreferredEmail], [Status], [StatusReason], [CreatedBy], [UpdatedBy])
    SELECT
        [UserID],
        [Name],
        @EmployeeDisplayName,
        @PreferredName,
        @WorkEmail,
        N'Active',
        N'P12.5 sample scenario.',
        @Actor,
        @Actor
    FROM [dbo].[Portal_Users]
    WHERE [UserID] = @UserId;
END

SELECT
    @UserId AS [PortalUserId],
    @PortalUserName AS [PortalUserName],
    @EmployeeId AS [EmployeeId],
    @EmployeeCode AS [EmployeeCode],
    @OrganizationUnitId AS [OrganizationUnitId],
    @OrganizationCode AS [OrganizationCode],
    N'Use the normal password of the existing test user; employee code is only a sign-in identifier.' AS [CredentialBoundary];
"@

$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
Write-Utf8NoBomFile -Path $resolvedOutputPath -Content ($sql + [Environment]::NewLine)
Write-Host ('P12.5 sample scenario SQL generated: {0}' -f $resolvedOutputPath)
Write-Host 'This script did not connect to any database and did not create or store passwords.'
