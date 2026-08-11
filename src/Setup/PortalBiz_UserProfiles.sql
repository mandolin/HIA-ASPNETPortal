/*
<lang>
  <zh-CN>P6.2 企业用户资料扩展迁移。本脚本可重复执行且不会由应用启动流程自动执行；它不改变 `Portal_Users` 的门户账号主体地位，只补充登录名、展示名、昵称、偏好邮箱和账号状态元数据。</zh-CN>
  <en>P6.2 enterprise user-profile extension migration. This script is idempotent and is not executed automatically by application startup; it does not change `Portal_Users` as the Portal account authority, and only adds login name, display name, nickname, preferred email, and account-status metadata.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，保证旧用户邮箱规范化和过滤唯一索引的空值行为稳定。</zh-CN>
--   <en>Enable standard NULL comparison semantics so legacy-user email normalization and filtered unique indexes handle nulls consistently.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护资料表、约束、索引与旧 `Portal_Users` 引用名称一致解析。</zh-CN>
--   <en>Enable quoted identifiers so profile table, constraint, index, and legacy `Portal_Users` reference names parse consistently.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>创建唯一 LoginName 前先阻断旧 `Portal_Users.Name` 重复值，避免迁移中途落入半成品状态。</zh-CN>
--   <en>Block duplicate legacy `Portal_Users.Name` values before creating unique LoginName values to avoid a partially migrated state.</en>
-- </lang>
IF EXISTS
(
    SELECT [Name]
    FROM [dbo].[Portal_Users]
    GROUP BY [Name]
    HAVING COUNT(*) > 1
)
BEGIN
    RAISERROR(N'Portal_Users.Name contains duplicates. Resolve duplicate legacy names before creating PortalBiz_UserProfiles.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>旧用户名必须先归一到非空、无首尾空白的形式；否则后续登录解析会出现不可复现的匹配差异。</zh-CN>
--   <en>Legacy user names must first normalize to non-empty values without surrounding whitespace; otherwise later sign-in resolution would have non-reproducible matching differences.</en>
-- </lang>
IF EXISTS
(
    SELECT 1
    FROM [dbo].[Portal_Users]
    WHERE NULLIF(LTRIM(RTRIM([Name])), N'') IS NULL
        OR [Name] <> LTRIM(RTRIM([Name]))
)
BEGIN
    RAISERROR(N'Portal_Users.Name contains blank or leading/trailing whitespace values. Resolve invalid legacy names before creating PortalBiz_UserProfiles.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>偏好邮箱建立过滤唯一索引前先检查旧邮箱去空白后的重复值，保护邮箱登录候选的单一映射。</zh-CN>
--   <en>Check duplicate normalized legacy emails before creating the filtered unique preferred-email index, protecting the one-to-one mapping for email sign-in candidates.</en>
-- </lang>
IF EXISTS
(
    SELECT NULLIF(LTRIM(RTRIM([Email])), N'') AS [PreferredEmail]
    FROM [dbo].[Portal_Users]
    WHERE NULLIF(LTRIM(RTRIM([Email])), N'') IS NOT NULL
    GROUP BY NULLIF(LTRIM(RTRIM([Email])), N'')
    HAVING COUNT(*) > 1
)
BEGIN
    RAISERROR(N'Portal_Users.Email contains duplicate non-empty normalized values. Resolve duplicate legacy emails before creating PortalBiz_UserProfiles.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>建表保护允许补跑迁移而不覆盖已经补全的企业资料、昵称、状态或审计字段。</zh-CN>
--   <en>The create-table guard allows rerunning the migration without overwriting already completed enterprise profiles, nicknames, statuses, or audit fields.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_UserProfiles]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>用户资料表以 `Portal_Users` 为账号权威，只保存登录展示和账号状态扩展，不保存密码或认证票据。</zh-CN>
    --   <en>The user-profile table keeps `Portal_Users` as the account authority and stores only sign-in display and account-status extensions, not passwords or authentication tickets.</en>
    -- </lang>
    CREATE TABLE [dbo].[PortalBiz_UserProfiles]
    (
        [UserId] INT NOT NULL,
        [LoginName] NVARCHAR(100) NOT NULL,
        [DisplayName] NVARCHAR(150) NULL,
        [Nickname] NVARCHAR(100) NULL,
        [PreferredEmail] NVARCHAR(256) NULL,
        [Status] NVARCHAR(40) NOT NULL
            CONSTRAINT [DF_PortalBiz_UserProfiles_Status] DEFAULT (N'Active'),
        [StatusReason] NVARCHAR(200) NULL,
        [CreatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_UserProfiles_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(100) NULL,
        [UpdatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_UserProfiles_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        -- <lang>
        --   <zh-CN>主键沿用旧用户标识并启用级联删除，保证资料扩展不会脱离账号主体孤立存在。</zh-CN>
        --   <en>The primary key reuses the legacy user id with cascade delete so the profile extension cannot outlive its account authority.</en>
        -- </lang>
        CONSTRAINT [PK_PortalBiz_UserProfiles]
            PRIMARY KEY CLUSTERED ([UserId]),
        CONSTRAINT [FK_PortalBiz_UserProfiles_Users]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Portal_Users] ([UserID]) ON DELETE CASCADE,
        CONSTRAINT [UQ_PortalBiz_UserProfiles_LoginName]
            UNIQUE ([LoginName]),
        -- <lang>
        --   <zh-CN>状态白名单表达注册审核、员工绑定、禁用、离职和锁定等业务门禁，而不是认证密码状态。</zh-CN>
        --   <en>The status whitelist represents business gates such as registration review, employee binding, disabled, left, and locked states, not authentication password state.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_UserProfiles_Status]
            CHECK ([Status] IN (N'Active', N'PendingApproval', N'PendingEmployeeBinding', N'Disabled', N'Left', N'Locked'))
    )
END
GO

-- <lang>
--   <zh-CN>偏好邮箱过滤唯一索引支持邮箱登录解析；空邮箱不参与唯一性，避免强迫历史账号补填邮箱。</zh-CN>
--   <en>The filtered preferred-email unique index supports email sign-in resolution; empty emails do not participate in uniqueness, avoiding mandatory email backfill for legacy accounts.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'UX_PortalBiz_UserProfiles_PreferredEmail' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_UserProfiles]'))
BEGIN
    CREATE UNIQUE INDEX [UX_PortalBiz_UserProfiles_PreferredEmail]
    ON [dbo].[PortalBiz_UserProfiles] ([PreferredEmail])
    WHERE [PreferredEmail] IS NOT NULL AND [PreferredEmail] <> N''
END
GO

-- <lang>
--   <zh-CN>注册表存在时，初始资料状态从注册审核状态投影而来，使待审和拒绝账号在企业资料层保持可见。</zh-CN>
--   <en>When the registration table exists, initial profile status is projected from registration-review status so pending and rejected accounts remain visible at the enterprise-profile layer.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[PortalCfg_UserRegistrations]', N'U') IS NOT NULL
BEGIN
    -- <lang>
    --   <zh-CN>`system-seed` 标记迁移生成的低敏资料行；`NOT EXISTS` 保护已由管理员维护过的资料不被覆盖。</zh-CN>
    --   <en>`system-seed` marks low-sensitivity profile rows generated by migration; `NOT EXISTS` protects profiles already maintained by administrators from being overwritten.</en>
    -- </lang>
    INSERT INTO [dbo].[PortalBiz_UserProfiles]
        ([UserId], [LoginName], [DisplayName], [PreferredEmail], [Status], [CreatedBy], [UpdatedBy])
    SELECT
        [Users].[UserID],
        [Users].[Name],
        [Users].[Name],
        NULLIF(LTRIM(RTRIM([Users].[Email])), N''),
        CASE
            WHEN [Registrations].[Status] = N'PendingApproval' THEN N'PendingApproval'
            WHEN [Registrations].[Status] = N'Rejected' THEN N'Disabled'
            ELSE N'Active'
        END,
        N'system-seed',
        N'system-seed'
    FROM [dbo].[Portal_Users] AS [Users]
    LEFT JOIN [dbo].[PortalCfg_UserRegistrations] AS [Registrations]
        ON [Registrations].[UserId] = [Users].[UserID]
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [dbo].[PortalBiz_UserProfiles] AS [Profiles]
        WHERE [Profiles].[UserId] = [Users].[UserID]
    )
END
ELSE
BEGIN
    -- <lang>
    --   <zh-CN>没有注册审核表的旧环境按 Active 建立资料扩展，保持旧门户登录行为的兼容起点。</zh-CN>
    --   <en>Legacy environments without the registration-review table receive Active profile extensions, preserving the compatible starting point for old Portal sign-in behavior.</en>
    -- </lang>
    INSERT INTO [dbo].[PortalBiz_UserProfiles]
        ([UserId], [LoginName], [DisplayName], [PreferredEmail], [Status], [CreatedBy], [UpdatedBy])
    SELECT
        [Users].[UserID],
        [Users].[Name],
        [Users].[Name],
        NULLIF(LTRIM(RTRIM([Users].[Email])), N''),
        N'Active',
        N'system-seed',
        N'system-seed'
    FROM [dbo].[Portal_Users] AS [Users]
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [dbo].[PortalBiz_UserProfiles] AS [Profiles]
        WHERE [Profiles].[UserId] = [Users].[UserID]
    )
END
GO
