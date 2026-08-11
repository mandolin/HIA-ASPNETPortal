/*
<lang>
  <zh-CN>P2.3 用户注册审核与临时注册链接迁移脚本。本脚本不会由应用启动流程自动执行，需要由开发/部署人员显式执行；它不改变 Portal_Users 的认证主体地位，只补充注册审核和邀请链接元数据；既有用户会被 seed 为 Approved，避免旧账号在迁移后无法登录。</zh-CN>
  <en>P2.3 user-registration review and temporary invitation migration script. The application does not run this at startup and developers or deployment operators must execute it explicitly; it does not change Portal_Users as the authentication authority and only adds registration-review and invitation metadata; existing users are seeded as Approved so legacy accounts are not blocked after migration.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，使后续约束和索引创建遵循 SQL Server 迁移基线。</zh-CN>
--   <en>Enable standard NULL comparison semantics so later constraints and indexes follow the SQL Server migration baseline.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护表名、列名和约束名在部署环境中稳定解析。</zh-CN>
--   <en>Enable quoted identifiers so table, column, and constraint names parse consistently in deployment environments.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>邀请表建表保护确保重复执行时不重建既有邀请码和使用计数。</zh-CN>
--   <en>The invitation-table guard ensures repeated execution does not rebuild existing invitation codes or usage counts.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_RegistrationInvites]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>临时注册链接表：后续系统管理 UI 会负责创建和停用这些链接，脚本只建立持久化结构。</zh-CN>
    --   <en>Temporary invitation table: future admin UI creates and disables these links, while this script only creates the persistence structure.</en>
    -- </lang>
    CREATE TABLE [dbo].[PortalCfg_RegistrationInvites]
    (
        [InviteCode] NVARCHAR(64) NOT NULL,
        [Description] NVARCHAR(200) NULL,
        [ExpiresUtc] DATETIME2(0) NOT NULL,
        -- <lang>
        --   <zh-CN>MaxUses 允许为空表示不限次数；UsedCount 记录已使用次数并由约束保持非负。</zh-CN>
        --   <en>MaxUses may be null to mean unlimited use; UsedCount records consumed uses and is constrained to remain non-negative.</en>
        -- </lang>
        [MaxUses] INT NULL,
        [UsedCount] INT NOT NULL
            CONSTRAINT [DF_PortalCfg_RegistrationInvites_UsedCount] DEFAULT ((0)),
        [IsEnabled] BIT NOT NULL
            CONSTRAINT [DF_PortalCfg_RegistrationInvites_IsEnabled] DEFAULT ((1)),
        [RequireEmployeeCode] BIT NOT NULL
            CONSTRAINT [DF_PortalCfg_RegistrationInvites_RequireEmployeeCode] DEFAULT ((1)),
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalCfg_RegistrationInvites_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
        [RowVersion] ROWVERSION NOT NULL,

        CONSTRAINT [PK_PortalCfg_RegistrationInvites]
            PRIMARY KEY CLUSTERED ([InviteCode]),

        -- <lang>
        --   <zh-CN>MaxUses 只接受正数或 NULL，避免 0 次邀请码被误认为有效容量。</zh-CN>
        --   <en>MaxUses accepts only positive numbers or NULL so a zero-use invitation is not mistaken for valid capacity.</en>
        -- </lang>
        CONSTRAINT [CK_PortalCfg_RegistrationInvites_MaxUses]
            CHECK ([MaxUses] IS NULL OR [MaxUses] > 0),

        -- <lang>
        --   <zh-CN>UsedCount 不能为负，防止手工修复或脚本重放制造超额可用次数。</zh-CN>
        --   <en>UsedCount cannot be negative, preventing manual repair or script replay from manufacturing extra available uses.</en>
        -- </lang>
        CONSTRAINT [CK_PortalCfg_RegistrationInvites_UsedCount]
            CHECK ([UsedCount] >= 0)
    )
END
GO

-- <lang>
--   <zh-CN>注册审核表建表保护确保重复执行时保留既有审核状态、员工编号和审核备注。</zh-CN>
--   <en>The registration-review table guard preserves existing review status, employee codes, and review notes across repeated execution.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_UserRegistrations]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>注册审核表：一条 Portal_Users 记录最多对应一条审核元数据，认证主体仍在 Portal_Users。</zh-CN>
    --   <en>Registration review table: each Portal_Users row can have at most one review metadata row, while the authentication authority remains Portal_Users.</en>
    -- </lang>
    CREATE TABLE [dbo].[PortalCfg_UserRegistrations]
    (
        [RegistrationId] INT IDENTITY(1, 1) NOT NULL,
        [UserId] INT NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [RequiresApproval] BIT NOT NULL
            CONSTRAINT [DF_PortalCfg_UserRegistrations_RequiresApproval] DEFAULT ((1)),
        [EmployeeCode] NVARCHAR(100) NULL,
        [InviteCode] NVARCHAR(64) NULL,
        [RegisteredUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalCfg_UserRegistrations_RegisteredUtc] DEFAULT (SYSUTCDATETIME()),
        [ApprovedUtc] DATETIME2(0) NULL,
        [ApprovedBy] NVARCHAR(100) NULL,
        [RejectedUtc] DATETIME2(0) NULL,
        [RejectedBy] NVARCHAR(100) NULL,
        [ReviewNote] NVARCHAR(500) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        CONSTRAINT [PK_PortalCfg_UserRegistrations]
            PRIMARY KEY CLUSTERED ([RegistrationId]),

        -- <lang>
        --   <zh-CN>UserId 唯一约束保证每个认证用户只有一份审核元数据，避免后台显示冲突状态。</zh-CN>
        --   <en>The UserId unique constraint ensures each authenticated user has only one review metadata row, avoiding conflicting statuses in admin screens.</en>
        -- </lang>
        CONSTRAINT [UQ_PortalCfg_UserRegistrations_UserId]
            UNIQUE ([UserId]),

        -- <lang>
        --   <zh-CN>Status 白名单限制审核状态枚举，防止自由文本破坏审批分支和索引筛选。</zh-CN>
        --   <en>The Status whitelist constrains review statuses so free text cannot break approval branches or indexed filtering.</en>
        -- </lang>
        CONSTRAINT [CK_PortalCfg_UserRegistrations_Status]
            CHECK ([Status] IN (N'Approved', N'PendingApproval', N'Rejected')),

        -- <lang>
        --   <zh-CN>用户外键级联删除审核元数据，避免旧用户删除后留下孤立注册记录。</zh-CN>
        --   <en>The user foreign key cascades review metadata deletion so removed legacy users do not leave orphan registration records.</en>
        -- </lang>
        CONSTRAINT [FK_PortalCfg_UserRegistrations_Users]
            FOREIGN KEY ([UserId])
            REFERENCES [dbo].[Portal_Users] ([UserID])
            ON DELETE CASCADE,

        -- <lang>
        --   <zh-CN>邀请外键保留注册链接来源，允许为空以兼容管理员直接创建或旧账号 seed。</zh-CN>
        --   <en>The invitation foreign key preserves the invitation source and may be null for administrator-created users or legacy-account seeding.</en>
        -- </lang>
        CONSTRAINT [FK_PortalCfg_UserRegistrations_Invites]
            FOREIGN KEY ([InviteCode])
            REFERENCES [dbo].[PortalCfg_RegistrationInvites] ([InviteCode])
    )
END
GO

-- <lang>
--   <zh-CN>状态/注册时间索引服务后台审核队列，优先按状态过滤再按最新注册时间倒序展示。</zh-CN>
--   <en>The status/registered-time index serves the admin review queue, filtering by status first and then showing newest registrations first.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalCfg_UserRegistrations_Status_RegisteredUtc' AND object_id = OBJECT_ID(N'[dbo].[PortalCfg_UserRegistrations]'))
BEGIN
    CREATE INDEX [IX_PortalCfg_UserRegistrations_Status_RegisteredUtc]
    ON [dbo].[PortalCfg_UserRegistrations] ([Status], [RegisteredUtc] DESC)
END
GO

-- <lang>
--   <zh-CN>既有用户按已批准导入，避免迁移后阻断旧账号登录；只为尚无审核元数据的用户补行。</zh-CN>
--   <en>Existing users are imported as approved so migration does not block legacy accounts; rows are added only for users without review metadata.</en>
-- </lang>
INSERT INTO [dbo].[PortalCfg_UserRegistrations]
    ([UserId], [Status], [RequiresApproval], [RegisteredUtc], [ApprovedUtc], [ApprovedBy])
SELECT
    [UserID],
    N'Approved',
    0,
    SYSUTCDATETIME(),
    SYSUTCDATETIME(),
    N'system-legacy'
FROM [dbo].[Portal_Users] AS [Users]
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[PortalCfg_UserRegistrations] AS [Registrations]
    WHERE [Registrations].[UserId] = [Users].[UserID]
)
GO
