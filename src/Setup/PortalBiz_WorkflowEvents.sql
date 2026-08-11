/*
<lang>
  <zh-CN>P19.4 轻量流程事件迁移。本表保存业务事实的流程动作流水，与 PortalBiz_WorkItems 的待办投影事件分离。</zh-CN>
  <en>P19.4 lightweight workflow-event migration. This table stores workflow action facts and remains separate from work-item projection events in PortalBiz_WorkItems.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，保证可空 From/To 状态和评论约束按 SQL Server 基线求值。</zh-CN>
--   <en>Enable standard NULL comparison semantics so nullable From/To status and comment constraints evaluate on the SQL Server baseline.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护流程事件 DDL 对象名和约束名稳定解析。</zh-CN>
--   <en>Enable quoted identifiers so workflow-event DDL object and constraint names parse consistently.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>流程事件 actor 可关联旧用户表；缺失用户表时停止迁移以保护办理人引用边界。</zh-CN>
--   <en>Workflow event actors may reference the legacy user table; stop migration when it is missing to preserve actor-reference boundaries.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_WorkflowEvents.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>建表保护保证重复执行不会重建既有业务流程事件。</zh-CN>
--   <en>The create-table guard ensures repeated execution does not rebuild existing business workflow events.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkflowEvents]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>轻量流程事件表以 BusinessKind/BusinessId 关联抽象业务事实，不强绑定某一个业务主表。</zh-CN>
    --   <en>The lightweight workflow-event table links to abstract business facts through BusinessKind/BusinessId instead of binding to one specific business table.</en>
    -- </lang>
    CREATE TABLE [dbo].[PortalBiz_WorkflowEvents]
    (
        [WorkflowEventId] BIGINT IDENTITY(1,1) NOT NULL,
        [BusinessKind] NVARCHAR(80) NOT NULL,
        [BusinessId] NVARCHAR(80) NOT NULL,
        [OccurredUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_WorkflowEvents_OccurredUtc] DEFAULT (SYSUTCDATETIME()),
        [ActionKey] NVARCHAR(40) NOT NULL,
        [ActorUserId] INT NULL,
        [ActorName] NVARCHAR(100) NOT NULL,
        [FromStatus] NVARCHAR(20) NULL,
        [ToStatus] NVARCHAR(20) NULL,
        [Comment] NVARCHAR(1000) NULL,
        [EventDataJson] NVARCHAR(MAX) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        -- <lang>
        --   <zh-CN>主键提供稳定事件顺序；ActorUserId 可为空以允许系统动作保留 ActorName 展示名。</zh-CN>
        --   <en>The primary key provides stable event order; ActorUserId may be null so system actions can still retain an ActorName display value.</en>
        -- </lang>
        CONSTRAINT [PK_PortalBiz_WorkflowEvents]
            PRIMARY KEY CLUSTERED ([WorkflowEventId]),
        CONSTRAINT [FK_PortalBiz_WorkflowEvents_ActorUser]
            FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        -- <lang>
        --   <zh-CN>业务键、动作和状态白名单约束抽象申请流程，防止自由文本破坏流程回放。</zh-CN>
        --   <en>Business keys plus action and status whitelists constrain the abstract application workflow so free text cannot break workflow replay.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_WorkflowEvents_BusinessKind]
            CHECK ([BusinessKind] = LTRIM(RTRIM([BusinessKind])) AND NULLIF([BusinessKind], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_WorkflowEvents_BusinessId]
            CHECK ([BusinessId] = LTRIM(RTRIM([BusinessId])) AND NULLIF([BusinessId], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_WorkflowEvents_ActionKey]
            CHECK ([ActionKey] IN (N'CreateDraft', N'Submit', N'Claim', N'Approve', N'Return', N'Reject', N'Withdraw', N'Close')),
        CONSTRAINT [CK_PortalBiz_WorkflowEvents_ActorName]
            CHECK ([ActorName] = LTRIM(RTRIM([ActorName])) AND NULLIF([ActorName], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_WorkflowEvents_FromStatus]
            CHECK ([FromStatus] IS NULL OR [FromStatus] IN (N'Draft', N'Submitted', N'InReview', N'Returned', N'Approved', N'Rejected', N'Withdrawn', N'Closed')),
        CONSTRAINT [CK_PortalBiz_WorkflowEvents_ToStatus]
            CHECK ([ToStatus] IS NULL OR [ToStatus] IN (N'Draft', N'Submitted', N'InReview', N'Returned', N'Approved', N'Rejected', N'Withdrawn', N'Closed')),
        CONSTRAINT [CK_PortalBiz_WorkflowEvents_Comment]
            CHECK ([Comment] IS NULL OR ([Comment] = LTRIM(RTRIM([Comment])) AND NULLIF([Comment], N'') IS NOT NULL))
    )
END
GO

-- <lang>
--   <zh-CN>业务对象/时间索引服务按 BusinessKind 与 BusinessId 回放单个业务对象流程时间线。</zh-CN>
--   <en>The business-object/time index replays the workflow timeline for one business object by BusinessKind and BusinessId.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_WorkflowEvents_BusinessUtc' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkflowEvents]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_WorkflowEvents_BusinessUtc]
    ON [dbo].[PortalBiz_WorkflowEvents] ([BusinessKind], [BusinessId], [OccurredUtc] DESC, [WorkflowEventId] DESC)
END
GO
