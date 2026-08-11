/*
<lang>
  <zh-CN>P21.3 企业协同事项主表迁移。本脚本可重复执行，应用程序不会在启动时自动执行它；第一版只保存低敏事项主数据、状态、发起人、负责人、组织、期限和最近办理意见，不保存附件、富文本、评论、评分、搜索索引、密码、Cookie、Token、连接串、证件号、薪资或具体行业字段。</zh-CN>
  <en>P21.3 enterprise collaboration-item fact migration. This script is idempotent and the application never runs it automatically at startup; the first version stores only low-sensitivity item facts including current state, initiator, owner, organization, due date, and latest handling comment, and stores no attachments, rich text, comments, ratings, search indexes, passwords, cookies, tokens, connection strings, government ids, compensation data, or domain-specific fields.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，确保可空负责人、期限和完成时间约束按 SQL Server 基线求值。</zh-CN>
--   <en>Enable standard NULL comparison semantics so nullable owner, due-date, and completion-time constraints evaluate on the SQL Server baseline.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护协同事项 DDL 中的对象名和约束名稳定解析。</zh-CN>
--   <en>Enable quoted identifiers so object and constraint names in collaboration-item DDL parse consistently.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>协同事项依赖旧用户表承载发起人、负责人和最近办理人；缺失时停止迁移避免孤立业务表。</zh-CN>
--   <en>Collaboration items depend on the legacy user table for initiators, owners, and latest actors; stop migration when it is missing to avoid an orphan business table.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_CollaborationItems.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>建表保护让脚本可重复执行，同时不覆盖既有协同事项状态、负责人或最近办理意见。</zh-CN>
--   <en>The create-table guard keeps the script repeatable without overwriting existing item states, owners, or latest handling comments.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItems]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>协同事项主表记录当前事实快照；细粒度动作流水由事件表保存，避免主表承担审计日志职责。</zh-CN>
    --   <en>The collaboration-item table records the current fact snapshot; fine-grained action history belongs in event tables so this table does not act as an audit log.</en>
    -- </lang>
    CREATE TABLE [dbo].[PortalBiz_CollaborationItems]
    (
        [ItemId] BIGINT IDENTITY(1,1) NOT NULL,
        [ItemCode] NVARCHAR(40) NOT NULL,
        [ItemTypeKey] NVARCHAR(80) NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Summary] NVARCHAR(500) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [ItemStatus] NVARCHAR(20) NOT NULL
            CONSTRAINT [DF_PortalBiz_CollaborationItems_Status] DEFAULT (N'Submitted'),
        [InitiatorUserId] INT NOT NULL,
        [InitiatorEmployeeId] INT NULL,
        [OwnerUserId] INT NULL,
        [OwnerRoleKey] NVARCHAR(120) NULL,
        [OrganizationUnitId] INT NULL,
        [PriorityKey] NVARCHAR(20) NULL,
        [DueUtc] DATETIME2(0) NULL,
        [SubmittedUtc] DATETIME2(0) NULL,
        [CompletedUtc] DATETIME2(0) NULL,
        [ClosedUtc] DATETIME2(0) NULL,
        [LastActionUtc] DATETIME2(0) NULL,
        [LastActionByUserId] INT NULL,
        [LastActionComment] NVARCHAR(1000) NULL,
        [CreatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_CollaborationItems_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(100) NOT NULL,
        [UpdatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_CollaborationItems_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedBy] NVARCHAR(100) NOT NULL,
        [RowVersion] ROWVERSION NOT NULL,

        -- <lang>
        --   <zh-CN>技术主键服务内部关系，唯一 ItemCode 服务展示、跳转和外部引用。</zh-CN>
        --   <en>The technical primary key serves internal relations, while unique ItemCode serves display, navigation, and external references.</en>
        -- </lang>
        CONSTRAINT [PK_PortalBiz_CollaborationItems]
            PRIMARY KEY CLUSTERED ([ItemId]),
        CONSTRAINT [UX_PortalBiz_CollaborationItems_Code]
            UNIQUE ([ItemCode]),
        -- <lang>
        --   <zh-CN>发起人、负责人和最近办理人都引用旧用户表；负责人和最近办理人可为空以支持角色池或未处理状态。</zh-CN>
        --   <en>Initiator, owner, and latest actor all reference the legacy user table; owner and latest actor may be null for role-pool or unhandled states.</en>
        -- </lang>
        CONSTRAINT [FK_PortalBiz_CollaborationItems_InitiatorUser]
            FOREIGN KEY ([InitiatorUserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        CONSTRAINT [FK_PortalBiz_CollaborationItems_OwnerUser]
            FOREIGN KEY ([OwnerUserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        CONSTRAINT [FK_PortalBiz_CollaborationItems_LastActionUser]
            FOREIGN KEY ([LastActionByUserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        CONSTRAINT [CK_PortalBiz_CollaborationItems_Code]
            CHECK ([ItemCode] = LTRIM(RTRIM([ItemCode])) AND NULLIF([ItemCode], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_CollaborationItems_Type]
            CHECK ([ItemTypeKey] = LTRIM(RTRIM([ItemTypeKey])) AND NULLIF([ItemTypeKey], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_CollaborationItems_Title]
            CHECK ([Title] = LTRIM(RTRIM([Title])) AND NULLIF([Title], N'') IS NOT NULL),
        -- <lang>
        --   <zh-CN>可选摘要、负责人角色、优先级和最近意见均限制为空或有效值，避免不可见空白污染列表筛选。</zh-CN>
        --   <en>Optional summary, owner role, priority, and latest comment values are constrained to null or valid content so invisible whitespace does not pollute list filters.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_CollaborationItems_Summary]
            CHECK ([Summary] IS NULL OR ([Summary] = LTRIM(RTRIM([Summary])) AND NULLIF([Summary], N'') IS NOT NULL)),
        -- <lang>
        --   <zh-CN>事项状态白名单定义第一版协同事项有限状态机。</zh-CN>
        --   <en>The item-status whitelist defines the first-version collaboration-item finite state machine.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_CollaborationItems_Status]
            CHECK ([ItemStatus] IN (N'Draft', N'Submitted', N'InProgress', N'Returned', N'Completed', N'Rejected', N'Cancelled', N'Closed')),
        CONSTRAINT [CK_PortalBiz_CollaborationItems_OwnerRole]
            CHECK ([OwnerRoleKey] IS NULL OR ([OwnerRoleKey] = LTRIM(RTRIM([OwnerRoleKey])) AND NULLIF([OwnerRoleKey], N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_CollaborationItems_Priority]
            CHECK ([PriorityKey] IS NULL OR [PriorityKey] IN (N'Normal', N'Important')),
        CONSTRAINT [CK_PortalBiz_CollaborationItems_LastActionComment]
            CHECK ([LastActionComment] IS NULL OR ([LastActionComment] = LTRIM(RTRIM([LastActionComment])) AND NULLIF([LastActionComment], N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_CollaborationItems_CreatedBy]
            CHECK ([CreatedBy] = LTRIM(RTRIM([CreatedBy])) AND NULLIF([CreatedBy], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_CollaborationItems_UpdatedBy]
            CHECK ([UpdatedBy] = LTRIM(RTRIM([UpdatedBy])) AND NULLIF([UpdatedBy], N'') IS NOT NULL),
        -- <lang>
        --   <zh-CN>非终态处理中事项必须可分派到用户或角色，防止出现无人可见的悬空事项。</zh-CN>
        --   <en>Non-terminal active items must be assignable to a user or role, preventing floating items that no one can see.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_CollaborationItems_Assignment]
            CHECK (
                [ItemStatus] IN (N'Draft', N'Completed', N'Rejected', N'Cancelled', N'Closed')
                OR [OwnerUserId] IS NOT NULL
                OR ([OwnerRoleKey] IS NOT NULL AND NULLIF(LTRIM(RTRIM([OwnerRoleKey])), N'') IS NOT NULL)
            ),
        -- <lang>
        --   <zh-CN>完成类状态必须有完成时间，未完成状态不得提前写入完成时间。</zh-CN>
        --   <en>Completion-like states require a completion time, while unfinished states must not carry one prematurely.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_CollaborationItems_CompletionUtc]
            CHECK (
                ([ItemStatus] IN (N'Completed', N'Rejected', N'Cancelled', N'Closed') AND [CompletedUtc] IS NOT NULL)
                OR
                ([ItemStatus] IN (N'Draft', N'Submitted', N'InProgress', N'Returned') AND [CompletedUtc] IS NULL)
            ),
        -- <lang>
        --   <zh-CN>ClosedUtc 只允许在 Closed 状态出现，使关闭时间与状态保持一一对应。</zh-CN>
        --   <en>ClosedUtc appears only in the Closed state, keeping close time and status in a one-to-one relationship.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_CollaborationItems_ClosedUtc]
            CHECK (
                ([ItemStatus] = N'Closed' AND [ClosedUtc] IS NOT NULL)
                OR
                ([ItemStatus] <> N'Closed' AND [ClosedUtc] IS NULL)
            )
    )
END
GO

-- <lang>
--   <zh-CN>状态/最近动作索引用于工作台列表，优先按状态筛选再按最近办理时间倒序排序。</zh-CN>
--   <en>The status/latest-action index serves workbench lists, filtering by state first and then ordering by most recent handling time.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_CollaborationItems_StatusAction' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItems]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_CollaborationItems_StatusAction]
    ON [dbo].[PortalBiz_CollaborationItems] ([ItemStatus], [LastActionUtc] DESC, [ItemId] DESC)
END
GO

-- <lang>
--   <zh-CN>发起人索引用于“我发起的事项”列表，避免按全表扫描回溯个人事项。</zh-CN>
--   <en>The initiator index supports “items I initiated” lists without scanning the whole table for one user's records.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_CollaborationItems_Initiator' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItems]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_CollaborationItems_Initiator]
    ON [dbo].[PortalBiz_CollaborationItems] ([InitiatorUserId], [LastActionUtc] DESC, [ItemId] DESC)
END
GO

-- <lang>
--   <zh-CN>负责人用户筛选索引只覆盖存在 OwnerUserId 的行，服务个人待处理事项。</zh-CN>
--   <en>The owner-user filtered index covers only rows with OwnerUserId and serves personal pending-item queues.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_CollaborationItems_OwnerUserStatus' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItems]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_CollaborationItems_OwnerUserStatus]
    ON [dbo].[PortalBiz_CollaborationItems] ([OwnerUserId], [ItemStatus], [LastActionUtc] DESC)
    WHERE [OwnerUserId] IS NOT NULL
END
GO

-- <lang>
--   <zh-CN>负责人角色筛选索引只覆盖角色池事项，服务按角色领取或处理的队列。</zh-CN>
--   <en>The owner-role filtered index covers only role-pool items and serves queues claimed or handled by role.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_CollaborationItems_OwnerRoleStatus' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItems]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_CollaborationItems_OwnerRoleStatus]
    ON [dbo].[PortalBiz_CollaborationItems] ([OwnerRoleKey], [ItemStatus], [LastActionUtc] DESC)
    WHERE [OwnerRoleKey] IS NOT NULL
END
GO
