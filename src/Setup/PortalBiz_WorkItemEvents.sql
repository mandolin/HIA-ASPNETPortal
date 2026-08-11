/*
<lang>
  <zh-CN>P12.3 轻量待办事件迁移。本脚本可重复执行，应用程序不会在启动时自动执行它；事件表保存待办状态流转和办理备注，是业务记录，不等同于运行时日志或运营审计。</zh-CN>
  <en>P12.3 lightweight work-item event migration. This script is idempotent and the application never runs it automatically at startup; the event table stores work-item state transitions and handling notes as business records, distinct from runtime logs and operation audits.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，保证可空状态和评论约束按 SQL Server 基线执行。</zh-CN>
--   <en>Enable standard NULL comparison semantics so nullable status and comment constraints execute on the SQL Server baseline.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护待办事件 DDL 对象名和约束名稳定解析。</zh-CN>
--   <en>Enable quoted identifiers so work-item event DDL object and constraint names parse consistently.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>待办事件依赖待办主表；缺失时停止，避免事件无法级联回业务待办。</zh-CN>
--   <en>Work-item events depend on the work-item fact table; stop when it is missing so events can still cascade back to business work items.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_WorkItems must be created before PortalBiz_WorkItemEvents.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>事件 actor 可关联旧用户表；缺失时停止以避免办理人引用失去认证边界。</zh-CN>
--   <en>Event actors may reference the legacy user table; stop when it is missing so actor references do not lose their authentication boundary.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_WorkItemEvents.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>建表保护确保重复执行不会重建既有待办事件流水。</zh-CN>
--   <en>The create-table guard ensures repeated execution does not rebuild existing work-item event history.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkItemEvents]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>待办事件表保存状态流转事实和低敏办理备注，不承担运行时诊断或运营审计职责。</zh-CN>
    --   <en>The work-item event table stores state-transition facts and low-sensitivity handling notes, not runtime diagnostics or operational-audit duties.</en>
    -- </lang>
    CREATE TABLE [dbo].[PortalBiz_WorkItemEvents]
    (
        [EventId] BIGINT IDENTITY(1,1) NOT NULL,
        [WorkItemId] BIGINT NOT NULL,
        [OccurredUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_WorkItemEvents_OccurredUtc] DEFAULT (SYSUTCDATETIME()),
        [EventType] NVARCHAR(40) NOT NULL,
        [ActorUserId] INT NULL,
        [ActorName] NVARCHAR(100) NOT NULL,
        [FromStatus] NVARCHAR(20) NULL,
        [ToStatus] NVARCHAR(20) NULL,
        [Comment] NVARCHAR(1000) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        -- <lang>
        --   <zh-CN>事件主键提供稳定排序，待办外键级联删除其业务事件历史。</zh-CN>
        --   <en>The event primary key provides stable ordering, and the work-item foreign key cascades deletion of its business event history.</en>
-- </lang>
        CONSTRAINT [PK_PortalBiz_WorkItemEvents]
            PRIMARY KEY CLUSTERED ([EventId]),
        CONSTRAINT [FK_PortalBiz_WorkItemEvents_WorkItems]
            FOREIGN KEY ([WorkItemId]) REFERENCES [dbo].[PortalBiz_WorkItems] ([WorkItemId]) ON DELETE CASCADE,
        -- <lang>
        --   <zh-CN>ActorUserId 可为空以兼容系统事件，但 ActorName 必须保存低敏展示名称。</zh-CN>
        --   <en>ActorUserId may be null for system events, but ActorName must keep a low-sensitivity display name.</en>
        -- </lang>
        CONSTRAINT [FK_PortalBiz_WorkItemEvents_ActorUser]
            FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        -- <lang>
        --   <zh-CN>事件类型和状态白名单将待办事件限制在第一版处理队列的有限语义内。</zh-CN>
        --   <en>Event-type and status whitelists keep work-item events within the finite semantics of the first-version handling queue.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_WorkItemEvents_EventType]
            CHECK ([EventType] IN (N'Created', N'Claimed', N'Approved', N'Rejected', N'Cancelled', N'Commented', N'Completed', N'Reopened')),
        CONSTRAINT [CK_PortalBiz_WorkItemEvents_ActorName]
            CHECK ([ActorName] = LTRIM(RTRIM([ActorName])) AND NULLIF([ActorName], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_WorkItemEvents_FromStatus]
            CHECK ([FromStatus] IS NULL OR [FromStatus] IN (N'Open', N'InProgress', N'Completed', N'Cancelled', N'Expired')),
        CONSTRAINT [CK_PortalBiz_WorkItemEvents_ToStatus]
            CHECK ([ToStatus] IS NULL OR [ToStatus] IN (N'Open', N'InProgress', N'Completed', N'Cancelled', N'Expired')),
        CONSTRAINT [CK_PortalBiz_WorkItemEvents_Comment]
            CHECK ([Comment] IS NULL OR ([Comment] = LTRIM(RTRIM([Comment])) AND NULLIF([Comment], N'') IS NOT NULL))
    )
END
GO

-- <lang>
--   <zh-CN>待办/时间索引用于回放单个待办的办理时间线，EventId 作为同秒事件的稳定排序补充。</zh-CN>
--   <en>The work-item/time index replays one work item's handling timeline, using EventId as a stable tie-breaker for events in the same second.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_WorkItemEvents_WorkItemUtc' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkItemEvents]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_WorkItemEvents_WorkItemUtc]
    ON [dbo].[PortalBiz_WorkItemEvents] ([WorkItemId], [OccurredUtc] DESC, [EventId] DESC)
END
GO
