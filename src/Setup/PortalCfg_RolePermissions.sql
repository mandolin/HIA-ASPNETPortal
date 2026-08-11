/*
<lang>
  <zh-CN>P5.3 角色权限映射迁移。此脚本可重复执行，应用程序不会在启动时自动执行它；权限定义保存在代码和文档中，数据库只保存角色到稳定权限键的映射。</zh-CN>
  <en>P5.3 role-permission mapping migration. This script is idempotent and the application never runs it automatically at startup; permission definitions live in code and documentation, while the database stores only role-to-stable-key mappings.</en>
</lang>
*/

-- <lang>
--   <zh-CN>建表保护避免重复迁移破坏既有授权映射，尤其是管理员后续在线调整的 IsEnabled 与 Notes。</zh-CN>
--   <en>The create-table guard prevents repeated migrations from damaging existing authorization mappings, especially IsEnabled and Notes values later edited by administrators.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_RolePermissions]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>角色权限表只表达“角色拥有某稳定权限键”的配置事实，不定义权限本身的语义。</zh-CN>
    --   <en>The role-permission table expresses only the configuration fact that a role has a stable permission key; it does not define the permission semantics themselves.</en>
    -- </lang>
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

        -- <lang>
        --   <zh-CN>复合主键保证同一角色/权限键只有一行，外键跟随旧角色删除清理配置载体。</zh-CN>
        --   <en>The composite primary key ensures one row per role/permission key, and the foreign key cleans this configuration carrier when a legacy role is deleted.</en>
        -- </lang>
        CONSTRAINT [PK_PortalCfg_RolePermissions]
            PRIMARY KEY CLUSTERED ([RoleId], [PermissionKey]),
        CONSTRAINT [FK_PortalCfg_RolePermissions_Roles]
            FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Portal_Roles] ([RoleID]) ON DELETE CASCADE,
        -- <lang>
        --   <zh-CN>权限键不能为空白，防止空字符串被误认为可授权的稳定 capability。</zh-CN>
        --   <en>Permission keys cannot be blank, preventing an empty string from being mistaken for an authorizable stable capability.</en>
        -- </lang>
        CONSTRAINT [CK_PortalCfg_RolePermissions_PermissionKey]
            CHECK (LEN(LTRIM(RTRIM([PermissionKey]))) > 0)
    )
END
GO

/*
<lang>
  <zh-CN>All Users 是旧门户的虚拟访问角色。细粒度权限映射需要外键目标，因此仅为映射维护一个无成员关系的配置载体；运行时不会把用户写入该角色。</zh-CN>
  <en>All Users is a legacy virtual access role. Fine-grained permission mappings need a foreign-key target, so this script maintains only a configuration carrier with no membership; runtime never adds users to this role.</en>
</lang>
*/
IF NOT EXISTS
(
    SELECT 1
    FROM [dbo].[Portal_Roles]
    WHERE [RoleName] = N'All Users'
)
BEGIN
    INSERT INTO [dbo].[Portal_Roles]
        ([PortalID], [RoleName])
    VALUES
        (0, N'All Users');
END
GO

-- <lang>
--   <zh-CN>`@AdminRoleId` 保存旧 Admins 角色的单一种子目标；缺失该角色时本脚本不创建替代管理员身份。</zh-CN>
--   <en>`@AdminRoleId` stores the single seed target for the legacy Admins role; when that role is missing, this script does not create a substitute administrator identity.</en>
-- </lang>
DECLARE @AdminRoleId INT;

-- <lang>
--   <zh-CN>按最小 RoleID 选择 Admins，兼容历史库中可能存在重复角色名的非理想状态。</zh-CN>
--   <en>Select the smallest RoleID for Admins to remain compatible with legacy databases that may contain duplicate role names.</en>
-- </lang>
SELECT TOP (1) @AdminRoleId = [RoleID]
FROM [dbo].[Portal_Roles]
WHERE [RoleName] = N'Admins'
ORDER BY [RoleID];

-- <lang>
--   <zh-CN>只有存在 Admins 角色时才写入兼容授权，避免在损坏数据库中制造悬空权限映射。</zh-CN>
--   <en>Compatibility grants are inserted only when the Admins role exists, avoiding orphan permission mappings in damaged databases.</en>
-- </lang>
IF @AdminRoleId IS NOT NULL
BEGIN
    -- <lang>
    --   <zh-CN>`@Permissions` 是本次种子的稳定权限键集合，生命周期仅限 Admins 兼容授权插入批次。</zh-CN>
    --   <en>`@Permissions` is the stable permission-key set for this seed and lives only for the Admins compatibility-grant insert batch.</en>
    -- </lang>
    DECLARE @Permissions TABLE
    (
        [PermissionKey] NVARCHAR(120) NOT NULL PRIMARY KEY
    );

    -- <lang>
    --   <zh-CN>权限清单覆盖后台设置、诊断、用户、员工、业务流程、主题、模块和内容管理；语义仍由代码常量定义。</zh-CN>
    --   <en>The permission list covers settings, diagnostics, users, employees, business workflows, themes, modules, and content administration; semantics remain defined by code constants.</en>
    -- </lang>
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
        (N'EmployeeDirectory.View'),
        (N'EmployeeDirectory.Edit'),
        (N'EmployeeDirectory.Bind'),
        (N'EmployeeProfileConfirm.View'),
        (N'EmployeeProfileConfirm.Confirm'),
        (N'EmployeeProfileConfirm.Admin'),
        (N'EmployeeProfileCorrectionRequest.View'),
        (N'EmployeeProfileCorrectionRequest.Submit'),
        (N'EmployeeProfileCorrectionRequest.Review'),
        (N'EmployeeProfileCorrectionRequest.Cancel'),
        (N'EmployeeProfileCorrectionRequest.Admin'),
        (N'Business.WorkItems.View'),
        (N'Business.WorkItems.Handle'),
        (N'Business.WorkItems.Admin'),
        (N'Business.Application.Submit'),
        (N'Business.Application.ViewOwn'),
        (N'Business.Application.Review'),
        (N'Business.Application.Admin'),
        (N'Business.Workflow.View'),
        (N'Business.Workflow.Admin'),
        (N'Business.Collaboration.Create'),
        (N'Business.Collaboration.ViewOwn'),
        (N'Business.Collaboration.Handle'),
        (N'Business.Collaboration.ViewAll'),
        (N'Business.Collaboration.Admin'),
        (N'Business.Collaboration.Events.View'),
        (N'Theme.View'),
        (N'Theme.Edit'),
        (N'Module.Catalog.View'),
        (N'Module.Catalog.Edit'),
        (N'Module.Definition.Edit'),
        (N'Portal.Tabs.Edit'),
        (N'Portal.Modules.Edit'),
        (N'Content.RawHtml.Edit'),
        (N'Content.Upload.Manage');

    -- <lang>
    --   <zh-CN>只插入缺失授权，保留管理员已存在映射的启停状态、更新时间和备注。</zh-CN>
    --   <en>Insert only missing grants, preserving the enabled state, update time, and notes of mappings administrators already have.</en>
    -- </lang>
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

-- <lang>
--   <zh-CN>`@AllUsersRoleId` 保存虚拟 All Users 配置载体的角色编号；缺失时后续公共提交权限不会写入。</zh-CN>
--   <en>`@AllUsersRoleId` stores the role id for the virtual All Users configuration carrier; if absent, later public submission grants are not written.</en>
-- </lang>
DECLARE @AllUsersRoleId INT;
SELECT TOP (1) @AllUsersRoleId = [RoleID]
FROM [dbo].[Portal_Roles]
WHERE [RoleName] = N'All Users'
ORDER BY [RoleID];

-- <lang>
--   <zh-CN>All Users 仅获得抽象业务申请与协作项创建/自查权限，不获得后台审核或全量查看权限。</zh-CN>
--   <en>All Users receives only abstract business-application and collaboration-item create/own-view grants, not admin review or global-view permissions.</en>
-- </lang>
IF @AllUsersRoleId IS NOT NULL
BEGIN
    -- <lang>
    --   <zh-CN>`@AllUsersPermissions` 是公共提交能力的最小白名单，生命周期只覆盖本批插入。</zh-CN>
    --   <en>`@AllUsersPermissions` is the minimal whitelist for public submission capabilities and lives only for this insert batch.</en>
    -- </lang>
    DECLARE @AllUsersPermissions TABLE
    (
        [PermissionKey] NVARCHAR(120) NOT NULL PRIMARY KEY
    );

    -- <lang>
    --   <zh-CN>公共权限清单刻意排除 Handle、ViewAll 和 Admin，避免虚拟角色越权。</zh-CN>
    --   <en>The public permission list intentionally excludes Handle, ViewAll, and Admin to prevent privilege expansion through the virtual role.</en>
    -- </lang>
    INSERT INTO @AllUsersPermissions ([PermissionKey])
    VALUES
        (N'Business.Application.Submit'),
        (N'Business.Application.ViewOwn'),
        (N'Business.Collaboration.Create'),
        (N'Business.Collaboration.ViewOwn');

    -- <lang>
    --   <zh-CN>只写入缺失公共授权，使部署人员可在重复执行前后保留手工禁用或备注。</zh-CN>
    --   <en>Insert only missing public grants so deployment operators can preserve manual disables or notes across repeated runs.</en>
    -- </lang>
    INSERT INTO [dbo].[PortalCfg_RolePermissions]
        ([RoleId], [PermissionKey], [IsEnabled], [UpdatedUtc], [UpdatedBy], [Notes])
    SELECT
        @AllUsersRoleId,
        [Permissions].[PermissionKey],
        1,
        SYSUTCDATETIME(),
        N'P21.3Seed',
        N'All Users grant for abstract business application and collaboration-item submission.'
    FROM @AllUsersPermissions AS [Permissions]
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [dbo].[PortalCfg_RolePermissions] AS [Existing]
        WHERE [Existing].[RoleId] = @AllUsersRoleId
          AND [Existing].[PermissionKey] = [Permissions].[PermissionKey]
    );
END
GO
