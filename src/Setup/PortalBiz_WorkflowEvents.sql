/*
    P19.4 轻量流程事件迁移。
    P19.4 lightweight workflow-event migration.

    本表保存业务事实的流程动作流水，与 PortalBiz_WorkItems 的待办投影事件分离。
    This table stores workflow action facts and stays separate from work-item projection events
    in PortalBiz_WorkItems.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_WorkflowEvents.', 16, 1)
    RETURN
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkflowEvents]') AND type IN (N'U'))
BEGIN
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

        CONSTRAINT [PK_PortalBiz_WorkflowEvents]
            PRIMARY KEY CLUSTERED ([WorkflowEventId]),
        CONSTRAINT [FK_PortalBiz_WorkflowEvents_ActorUser]
            FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
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

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_WorkflowEvents_BusinessUtc' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkflowEvents]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_WorkflowEvents_BusinessUtc]
    ON [dbo].[PortalBiz_WorkflowEvents] ([BusinessKind], [BusinessId], [OccurredUtc] DESC, [WorkflowEventId] DESC)
END
GO
