/*
    P23.6 协同事项评论与状态规则扩展迁移。
    P23.6 collaboration-item comment and workflow-rule extension migration.

    本脚本只扩展既有 P21 事项事件时间线；不创建平行评论表、不引入附件二进制，
    也不把评论解释为流程状态变更。
    This script extends the existing P21 item-event timeline only. It creates no parallel
    comment table, introduces no attachment binary, and never treats a comment as a workflow state change.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItems]', N'U') IS NULL
   OR OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItemEvents]', N'U') IS NULL
BEGIN
    RAISERROR(N'P21 collaboration-item and event tables must exist before P23.6 comment/workflow migration.', 16, 1)
    RETURN
END
GO

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

ALTER TABLE [dbo].[PortalBiz_CollaborationItemEvents]
ALTER COLUMN [ActionKey] NVARCHAR(40) NULL
GO

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
