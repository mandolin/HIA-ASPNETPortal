/*
<lang>
  <zh-CN>P12.3 轻量待办记录迁移。本脚本可重复执行，应用程序不会在启动时自动执行它；第一版只保存业务对象、办理状态、分派目标和低敏摘要，不保存密码、Cookie、Token、连接串、证件号、薪资或其它高敏个人资料。</zh-CN>
  <en>P12.3 lightweight work-item record migration. This script is idempotent and the application never runs it automatically at startup; the first version stores only business object identifiers, handling status, assignee target, and low-sensitivity summaries, and stores no passwords, cookies, tokens, connection strings, government ids, compensation data, or other high-sensitivity personal data.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，保证分派和完成状态约束中的 NULL 分支按 SQL Server 基线执行。</zh-CN>
--   <en>Enable standard NULL comparison semantics so NULL branches in assignment and completion-state constraints execute on the SQL Server baseline.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护待办表 DDL 对象名和约束名稳定解析。</zh-CN>
--   <en>Enable quoted identifiers so work-item DDL object and constraint names parse consistently.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>待办分派到旧用户或角色；缺少用户表时停止迁移，避免创建不可分派的待办结构。</zh-CN>
--   <en>Work items assign to legacy users or roles; stop migration when the user table is missing to avoid creating an unassignable work-item structure.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_WorkItems.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>建表保护让重复执行不会重建既有待办、完成状态或分派目标。</zh-CN>
--   <en>The create-table guard prevents repeated execution from rebuilding existing work items, completion state, or assignee targets.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>轻量待办表是业务对象的可处理投影，不保存完整业务正文或审计日志。</zh-CN>
    --   <en>The lightweight work-item table is a handleable projection of business objects and does not store full business bodies or audit logs.</en>
    -- </lang>
    CREATE TABLE [dbo].[PortalBiz_WorkItems]
    (
        [WorkItemId] BIGINT IDENTITY(1,1) NOT NULL,
        [BusinessKind] NVARCHAR(80) NOT NULL,
        [BusinessId] NVARCHAR(80) NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Summary] NVARCHAR(500) NULL,
        [WorkItemStatus] NVARCHAR(20) NOT NULL
            CONSTRAINT [DF_PortalBiz_WorkItems_Status] DEFAULT (N'Open'),
        [AssignedUserId] INT NULL,
        [AssignedRoleKey] NVARCHAR(120) NULL,
        [CreatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_WorkItems_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(100) NOT NULL,
        [DueUtc] DATETIME2(0) NULL,
        [CompletedUtc] DATETIME2(0) NULL,
        [CompletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        -- <lang>
        --   <zh-CN>主键为技术标识，业务去重由后续活动业务对象唯一索引承担。</zh-CN>
        --   <en>The primary key is a technical identifier, while business de-duplication is enforced later by the active-business unique index.</en>
        -- </lang>
        CONSTRAINT [PK_PortalBiz_WorkItems]
            PRIMARY KEY CLUSTERED ([WorkItemId]),
        -- <lang>
        --   <zh-CN>分派用户外键可为空，因为待办也可以分派给角色池。</zh-CN>
        --   <en>The assigned-user foreign key may be null because work items can also be assigned to a role pool.</en>
        -- </lang>
        CONSTRAINT [FK_PortalBiz_WorkItems_AssignedUser]
            FOREIGN KEY ([AssignedUserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        -- <lang>
        --   <zh-CN>业务对象键、标题和摘要必须为空或已裁剪有效值，避免空白数据污染队列。</zh-CN>
        --   <en>Business object keys, titles, and summaries must be null or trimmed valid values so whitespace data does not pollute queues.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_WorkItems_BusinessKind]
            CHECK ([BusinessKind] = LTRIM(RTRIM([BusinessKind])) AND NULLIF([BusinessKind], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_WorkItems_BusinessId]
            CHECK ([BusinessId] = LTRIM(RTRIM([BusinessId])) AND NULLIF([BusinessId], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_WorkItems_Title]
            CHECK ([Title] = LTRIM(RTRIM([Title])) AND NULLIF([Title], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_WorkItems_Summary]
            CHECK ([Summary] IS NULL OR ([Summary] = LTRIM(RTRIM([Summary])) AND NULLIF([Summary], N'') IS NOT NULL)),
        -- <lang>
        --   <zh-CN>待办状态白名单定义第一版处理队列的有限状态集合。</zh-CN>
        --   <en>The work-item status whitelist defines the first-version finite state set for handling queues.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_WorkItems_Status]
            CHECK ([WorkItemStatus] IN (N'Open', N'InProgress', N'Completed', N'Cancelled', N'Expired')),
        -- <lang>
        --   <zh-CN>待办必须分派给用户或角色，防止创建无人能处理的开放记录。</zh-CN>
        --   <en>A work item must be assigned to a user or role, preventing open records that no one can handle.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_WorkItems_Assignment]
            CHECK ([AssignedUserId] IS NOT NULL OR ([AssignedRoleKey] IS NOT NULL AND NULLIF(LTRIM(RTRIM([AssignedRoleKey])), N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_WorkItems_CreatedBy]
            CHECK ([CreatedBy] = LTRIM(RTRIM([CreatedBy])) AND NULLIF([CreatedBy], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_WorkItems_CompletedBy]
            CHECK ([CompletedBy] IS NULL OR ([CompletedBy] = LTRIM(RTRIM([CompletedBy])) AND NULLIF([CompletedBy], N'') IS NOT NULL)),
        -- <lang>
        --   <zh-CN>完成类状态必须同时记录完成时间和办理人；开放状态不得提前写入完成信息。</zh-CN>
        --   <en>Completion-like states must record both completion time and actor, while open states must not carry completion data prematurely.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_WorkItems_CompletionState]
            CHECK (
                ([WorkItemStatus] IN (N'Open', N'InProgress') AND [CompletedUtc] IS NULL AND [CompletedBy] IS NULL)
                OR
                ([WorkItemStatus] IN (N'Completed', N'Cancelled', N'Expired') AND [CompletedUtc] IS NOT NULL AND [CompletedBy] IS NOT NULL)
            )
    )
END
GO

-- <lang>
--   <zh-CN>活动业务对象唯一索引保证同一业务对象同一时间只有一个开放/处理中待办。</zh-CN>
--   <en>The active-business unique index ensures one business object has at most one open or in-progress work item at a time.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'UX_PortalBiz_WorkItems_ActiveBusiness' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]'))
BEGIN
    CREATE UNIQUE INDEX [UX_PortalBiz_WorkItems_ActiveBusiness]
    ON [dbo].[PortalBiz_WorkItems] ([BusinessKind], [BusinessId])
    WHERE [WorkItemStatus] IN (N'Open', N'InProgress')
END
GO

-- <lang>
--   <zh-CN>状态/创建时间索引服务通用待办队列，按状态过滤并优先显示最新创建记录。</zh-CN>
--   <en>The status/created-time index serves general work-item queues, filtering by state and showing newly created records first.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_WorkItems_StatusCreated' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_WorkItems_StatusCreated]
    ON [dbo].[PortalBiz_WorkItems] ([WorkItemStatus], [CreatedUtc] DESC, [WorkItemId] DESC)
END
GO

-- <lang>
--   <zh-CN>分派用户筛选索引服务个人待办，只覆盖有明确用户分派的记录。</zh-CN>
--   <en>The assigned-user filtered index serves personal work queues and covers only records assigned to a specific user.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_WorkItems_AssignedUserStatus' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_WorkItems_AssignedUserStatus]
    ON [dbo].[PortalBiz_WorkItems] ([AssignedUserId], [WorkItemStatus], [CreatedUtc] DESC)
    WHERE [AssignedUserId] IS NOT NULL
END
GO

-- <lang>
--   <zh-CN>分派角色筛选索引服务角色池待办，只覆盖通过角色领取或处理的记录。</zh-CN>
--   <en>The assigned-role filtered index serves role-pool work queues and covers only records handled or claimed through roles.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_WorkItems_AssignedRoleStatus' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_WorkItems_AssignedRoleStatus]
    ON [dbo].[PortalBiz_WorkItems] ([AssignedRoleKey], [WorkItemStatus], [CreatedUtc] DESC)
    WHERE [AssignedRoleKey] IS NOT NULL
END
GO
