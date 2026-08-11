/*
<lang>
  <zh-CN>P21.3 企业协同事项事件迁移。本脚本可重复执行，应用程序不会在启动时自动执行它；本表保存协同事项自身的流程动作事实，与 PortalBiz_WorkItems 的待办投影事件、PortalCfg_OperationAudits 的运营审计和运行时诊断日志分离。</zh-CN>
  <en>P21.3 enterprise collaboration-item event migration. This script is idempotent and the application never runs it automatically at startup; this table stores workflow action facts for collaboration items themselves and remains separate from PortalBiz_WorkItems projection events, PortalCfg_OperationAudits operational audits, and runtime diagnostic logs.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，保证可空状态和办理人约束按 SQL Server 基线判断。</zh-CN>
--   <en>Enable standard NULL comparison semantics so nullable status and actor constraints evaluate on the SQL Server baseline.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护事件表 DDL 对象名和约束名稳定解析。</zh-CN>
--   <en>Enable quoted identifiers so event-table DDL object and constraint names parse consistently.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>事件表依赖协同事项主表；缺失主表时停止，避免创建无法级联到事项的事件孤岛。</zh-CN>
--   <en>The event table depends on the collaboration-item fact table; stop when it is missing to avoid an event island that cannot cascade from items.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItems]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_CollaborationItems must be created before PortalBiz_CollaborationItemEvents.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>事件 actor 可指向旧用户表；缺失用户表时停止，避免办理人引用失去认证边界。</zh-CN>
--   <en>Event actors may point to the legacy user table; stop when it is missing so actor references do not lose their authentication boundary.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_CollaborationItemEvents.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>建表保护确保重复执行不重建既有流程事件流水。</zh-CN>
--   <en>The create-table guard ensures repeated execution does not rebuild existing workflow event history.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItemEvents]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>协同事项事件表记录业务动作事实和状态变化，不承载运行时日志或运营审计职责。</zh-CN>
    --   <en>The collaboration-item event table records business action facts and state changes, not runtime logging or operational-audit duties.</en>
    -- </lang>
    CREATE TABLE [dbo].[PortalBiz_CollaborationItemEvents]
    (
        [EventId] BIGINT IDENTITY(1,1) NOT NULL,
        [ItemId] BIGINT NOT NULL,
        [OccurredUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_CollaborationItemEvents_OccurredUtc] DEFAULT (SYSUTCDATETIME()),
        [ActionKey] NVARCHAR(40) NOT NULL,
        [ActorUserId] INT NULL,
        [ActorName] NVARCHAR(100) NOT NULL,
        [FromStatus] NVARCHAR(20) NULL,
        [ToStatus] NVARCHAR(20) NULL,
        [Comment] NVARCHAR(1000) NULL,
        [EventDataJson] NVARCHAR(MAX) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        -- <lang>
        --   <zh-CN>事件主键提供稳定排序，事项外键级联删除其业务事件历史。</zh-CN>
        --   <en>The event primary key provides stable ordering, and the item foreign key cascades deletion of that item's business event history.</en>
        -- </lang>
        CONSTRAINT [PK_PortalBiz_CollaborationItemEvents]
            PRIMARY KEY CLUSTERED ([EventId]),
        CONSTRAINT [FK_PortalBiz_CollaborationItemEvents_Items]
            FOREIGN KEY ([ItemId]) REFERENCES [dbo].[PortalBiz_CollaborationItems] ([ItemId]) ON DELETE CASCADE,
        -- <lang>
        --   <zh-CN>ActorUserId 可为空以兼容系统动作，但 ActorName 必须保存低敏展示名称。</zh-CN>
        --   <en>ActorUserId may be null for system actions, but ActorName must keep a low-sensitivity display name.</en>
        -- </lang>
        CONSTRAINT [FK_PortalBiz_CollaborationItemEvents_ActorUser]
            FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        -- <lang>
        --   <zh-CN>动作和状态白名单将事件流限制在协同事项的有限流程语义内。</zh-CN>
        --   <en>Action and status whitelists keep the event stream within the finite workflow semantics of collaboration items.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_CollaborationItemEvents_Action]
            CHECK ([ActionKey] IN (N'CreateDraft', N'Submit', N'Start', N'Complete', N'Return', N'Reject', N'Cancel', N'Close')),
        CONSTRAINT [CK_PortalBiz_CollaborationItemEvents_ActorName]
            CHECK ([ActorName] = LTRIM(RTRIM([ActorName])) AND NULLIF([ActorName], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_CollaborationItemEvents_FromStatus]
            CHECK ([FromStatus] IS NULL OR [FromStatus] IN (N'Draft', N'Submitted', N'InProgress', N'Returned', N'Completed', N'Rejected', N'Cancelled', N'Closed')),
        CONSTRAINT [CK_PortalBiz_CollaborationItemEvents_ToStatus]
            CHECK ([ToStatus] IS NULL OR [ToStatus] IN (N'Draft', N'Submitted', N'InProgress', N'Returned', N'Completed', N'Rejected', N'Cancelled', N'Closed')),
        CONSTRAINT [CK_PortalBiz_CollaborationItemEvents_Comment]
            CHECK ([Comment] IS NULL OR ([Comment] = LTRIM(RTRIM([Comment])) AND NULLIF([Comment], N'') IS NOT NULL))
    )
END
GO

-- <lang>
--   <zh-CN>事项/时间索引服务单个事项的事件时间线，EventId 作为同秒事件的稳定排序补充。</zh-CN>
--   <en>The item/time index serves one item's event timeline, using EventId as a stable tie-breaker for events in the same second.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_CollaborationItemEvents_ItemUtc' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItemEvents]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_CollaborationItemEvents_ItemUtc]
    ON [dbo].[PortalBiz_CollaborationItemEvents] ([ItemId], [OccurredUtc] DESC, [EventId] DESC)
END
GO
