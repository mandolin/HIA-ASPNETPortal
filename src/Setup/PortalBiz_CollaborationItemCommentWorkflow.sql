/*
<lang>
  <zh-CN>P23.6 协同事项评论与状态规则扩展迁移。本脚本只扩展既有 P21 事项事件时间线；不创建平行评论表、不引入附件二进制，也不把评论解释为流程状态变更。</zh-CN>
  <en>P23.6 collaboration-item comment and workflow-rule extension migration. This script extends the existing P21 item-event timeline only; it creates no parallel comment table, introduces no attachment binary, and never treats a comment as a workflow state change.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，确保新增事件形态约束能可靠区分评论事件所需的空状态字段。</zh-CN>
--   <en>Enable standard NULL comparison semantics so the new event-shape constraint reliably distinguishes the null state fields required by comment events.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护事件扩展列、约束和索引名称在迁移执行中一致解析。</zh-CN>
--   <en>Enable quoted identifiers so event-extension column, constraint, and index names parse consistently during migration execution.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>评论/工作流扩展依赖 P21 协同事项事实表与事件表；缺失时 fail fast，避免改造半条事件时间线。</zh-CN>
--   <en>The comment/workflow extension depends on the P21 collaboration item fact table and event table; fail fast when either is missing to avoid upgrading only half of the event timeline.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItems]', N'U') IS NULL
   OR OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItemEvents]', N'U') IS NULL
BEGIN
    RAISERROR(N'P21 collaboration-item and event tables must exist before P23.6 comment/workflow migration.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>缺失 `EventType` 时补列，并把既有事件回填为 `WorkflowAction`，保持历史事件继续表达状态动作。</zh-CN>
--   <en>When `EventType` is missing, add it and backfill existing events as `WorkflowAction` so historical events continue to represent state actions.</en>
-- </lang>
IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItemEvents]')
      AND name = N'EventType'
)
BEGIN
    ALTER TABLE [dbo].[PortalBiz_CollaborationItemEvents]
    ADD [EventType] NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_PortalBiz_CollaborationItemEvents_EventType]
        DEFAULT (N'WorkflowAction') WITH VALUES
END
GO

-- <lang>
--   <zh-CN>缺失 `VisibilityScope` 时补列，并把既有事件默认设为事项参与者可见，避免历史事件突然暴露给更宽范围。</zh-CN>
--   <en>When `VisibilityScope` is missing, add it and default existing events to item-participant visibility so historical events are not suddenly exposed to a broader audience.</en>
-- </lang>
IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItemEvents]')
      AND name = N'VisibilityScope'
)
BEGIN
    ALTER TABLE [dbo].[PortalBiz_CollaborationItemEvents]
    ADD [VisibilityScope] NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_PortalBiz_CollaborationItemEvents_VisibilityScope]
        DEFAULT (N'ItemParticipants') WITH VALUES
END
GO

-- <lang>
--   <zh-CN>旧 ActionKey 检查约束只适用于工作流动作；迁移先移除它，随后用同时覆盖动作和评论的新形态约束替代。</zh-CN>
--   <en>The old ActionKey check constraint applies only to workflow actions; the migration removes it first and later replaces it with a new shape constraint covering both actions and comments.</en>
-- </lang>
IF EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItemEvents]')
      AND name = N'CK_PortalBiz_CollaborationItemEvents_Action'
)
BEGIN
    ALTER TABLE [dbo].[PortalBiz_CollaborationItemEvents]
    DROP CONSTRAINT [CK_PortalBiz_CollaborationItemEvents_Action]
END
GO

-- <lang>
--   <zh-CN>允许 ActionKey 为空，使评论事件可以表达“无流程动作”的追加式沟通记录。</zh-CN>
--   <en>Allow ActionKey to be null so comment events can represent append-only communication records without workflow actions.</en>
-- </lang>
ALTER TABLE [dbo].[PortalBiz_CollaborationItemEvents]
ALTER COLUMN [ActionKey] NVARCHAR(40) NULL
GO

-- <lang>
--   <zh-CN>事件类型白名单将事件时间线限定为工作流动作或评论，避免自由文本类型破坏读取端分派逻辑。</zh-CN>
--   <en>The event-type whitelist limits the event timeline to workflow actions or comments, preventing free-text types from breaking reader dispatch logic.</en>
-- </lang>
IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItemEvents]')
      AND name = N'CK_PortalBiz_CollaborationItemEvents_EventType'
)
BEGIN
    ALTER TABLE [dbo].[PortalBiz_CollaborationItemEvents]
    ADD CONSTRAINT [CK_PortalBiz_CollaborationItemEvents_EventType]
        CHECK ([EventType] IN (N'WorkflowAction', N'Comment'))
END
GO

-- <lang>
--   <zh-CN>可见范围白名单将评论/动作事件限定为事项参与者或管理员，后续读取端可据此执行 fail-closed 过滤。</zh-CN>
--   <en>The visibility-scope whitelist limits comment/action events to item participants or administrators, allowing later readers to apply fail-closed filtering.</en>
-- </lang>
IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItemEvents]')
      AND name = N'CK_PortalBiz_CollaborationItemEvents_VisibilityScope'
)
BEGIN
    ALTER TABLE [dbo].[PortalBiz_CollaborationItemEvents]
    ADD CONSTRAINT [CK_PortalBiz_CollaborationItemEvents_VisibilityScope]
        CHECK ([VisibilityScope] IN (N'ItemParticipants', N'Administrators'))
END
GO

-- <lang>
--   <zh-CN>形态约束把工作流动作和评论事件分开校验：动作必须有合法 ActionKey，评论必须有 Actor、Comment 且不携带状态迁移或 JSON 扩展。</zh-CN>
--   <en>The shape constraint validates workflow actions and comment events separately: actions must carry a valid ActionKey, while comments must carry Actor and Comment and must not carry state transitions or JSON extensions.</en>
-- </lang>
IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItemEvents]')
      AND name = N'CK_PortalBiz_CollaborationItemEvents_Shape'
)
BEGIN
    ALTER TABLE [dbo].[PortalBiz_CollaborationItemEvents]
    ADD CONSTRAINT [CK_PortalBiz_CollaborationItemEvents_Shape]
        CHECK
        (
            (
                [EventType] = N'WorkflowAction'
                AND [ActionKey] IN (N'CreateDraft', N'Submit', N'Start', N'Complete', N'Return', N'Resubmit', N'Reject', N'Cancel', N'Close')
            )
            OR
            (
                [EventType] = N'Comment'
                AND [ActionKey] IS NULL
                AND [ActorUserId] IS NOT NULL
                AND [FromStatus] IS NULL
                AND [ToStatus] IS NULL
                AND [Comment] IS NOT NULL
                AND [EventDataJson] IS NULL
            )
        )
END
GO

-- <lang>
--   <zh-CN>事项/可见性/时间线索引支撑协同详情页按可见范围读取最近事件，并保持同一时间下 EventId 倒序稳定。</zh-CN>
--   <en>The item/visibility/timeline index supports collaboration detail pages reading recent events by visibility scope, with descending EventId stabilizing same-timestamp ordering.</en>
-- </lang>
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PortalBiz_CollaborationItemEvents_ItemVisibilityUtc'
      AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItemEvents]')
)
BEGIN
    CREATE INDEX [IX_PortalBiz_CollaborationItemEvents_ItemVisibilityUtc]
    ON [dbo].[PortalBiz_CollaborationItemEvents] ([ItemId], [VisibilityScope], [OccurredUtc] DESC, [EventId] DESC)
END
GO
