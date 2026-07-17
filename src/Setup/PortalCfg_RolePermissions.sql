/*
    P5.3 角色权限映射迁移。
    P5.3 role-permission mapping migration.

    此脚本可重复执行；应用程序不会在启动时自动执行它。
    This script is idempotent; the application never runs it automatically at startup.

    权限定义保存在代码和文档中，数据库只保存角色到稳定权限键的映射。
    Permission definitions live in code and documentation; the database stores only role-to-stable-key mappings.
*/

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_RolePermissions]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[PortalCfg_RolePermissions]
    (
        [RoleId] INT NOT NULL,
        [PermissionKey] NVARCHAR(120) NOT NULL,
        [IsEnabled] BIT NOT NULL
            CONSTRAINT [DF_PortalCfg_RolePermissions_IsEnabled] DEFAULT (1),
        [UpdatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalCfg_RolePermissions_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedBy] NVARCHAR(100) NOT NULL
            CONSTRAINT [DF_PortalCfg_RolePermissions_UpdatedBy] DEFAULT (N'system'),
        [Notes] NVARCHAR(400) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        CONSTRAINT [PK_PortalCfg_RolePermissions]
            PRIMARY KEY CLUSTERED ([RoleId], [PermissionKey]),
        CONSTRAINT [FK_PortalCfg_RolePermissions_Roles]
            FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Portal_Roles] ([RoleID]) ON DELETE CASCADE,
        CONSTRAINT [CK_PortalCfg_RolePermissions_PermissionKey]
            CHECK (LEN(LTRIM(RTRIM([PermissionKey]))) > 0)
    )
END
GO

DECLARE @AdminRoleId INT;
SELECT TOP (1) @AdminRoleId = [RoleID]
FROM [dbo].[Portal_Roles]
WHERE [RoleName] = N'Admins'
ORDER BY [RoleID];

IF @AdminRoleId IS NOT NULL
BEGIN
    DECLARE @Permissions TABLE
    (
        [PermissionKey] NVARCHAR(120) NOT NULL PRIMARY KEY
    );

    INSERT INTO @Permissions ([PermissionKey])
    VALUES
        (N'Settings.View'),
        (N'Settings.Edit'),
        (N'Settings.SensitiveView'),
        (N'Ops.Health.View'),
        (N'Ops.Diagnostics.View'),
        (N'Ops.Diagnostics.Detail'),
        (N'Audit.Operation.View'),
        (N'Admin.Users.View'),
        (N'Admin.Users.Edit'),
        (N'Admin.Users.ResetPassword'),
        (N'Admin.Roles.Edit'),
        (N'EmployeeDirectory.Bind'),
        (N'EmployeeProfileConfirm.View'),
        (N'EmployeeProfileConfirm.Confirm'),
        (N'EmployeeProfileConfirm.Admin'),
        (N'EmployeeProfileCorrectionRequest.View'),
        (N'EmployeeProfileCorrectionRequest.Submit'),
        (N'EmployeeProfileCorrectionRequest.Admin'),
        (N'Theme.View'),
        (N'Theme.Edit'),
        (N'Module.Catalog.View'),
        (N'Module.Catalog.Edit'),
        (N'Module.Definition.Edit'),
        (N'Portal.Tabs.Edit'),
        (N'Portal.Modules.Edit'),
        (N'Content.RawHtml.Edit'),
        (N'Content.Upload.Manage');

    INSERT INTO [dbo].[PortalCfg_RolePermissions]
        ([RoleId], [PermissionKey], [IsEnabled], [UpdatedUtc], [UpdatedBy], [Notes])
    SELECT
        @AdminRoleId,
        [Permissions].[PermissionKey],
        1,
        SYSUTCDATETIME(),
        N'P5.3Seed',
        N'Admins compatibility grant.'
    FROM @Permissions AS [Permissions]
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [dbo].[PortalCfg_RolePermissions] AS [Existing]
        WHERE [Existing].[RoleId] = @AdminRoleId
          AND [Existing].[PermissionKey] = [Permissions].[PermissionKey]
    );
END
GO
