/*
    P12.3 轻量待办事件迁移。
    P12.3 lightweight work-item event migration.

    本脚本可重复执行；应用程序不会在启动时自动执行它。
    This script is idempotent; the application never runs it automatically at startup.

    事件表保存待办状态流转和办理备注，是业务记录，不等同于运行时日志或运营审计。
    The event table stores work-item state transitions and handling notes as business records.
    It is distinct from runtime logs and operation audits.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_WorkItems must be created before PortalBiz_WorkItemEvents.', 16, 1)
    RETURN
END
GO

IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_WorkItemEvents.', 16, 1)
    RETURN
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkItemEvents]') AND type IN (N'U'))
BEGIN
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

        CONSTRAINT [PK_PortalBiz_WorkItemEvents]
            PRIMARY KEY CLUSTERED ([EventId]),
        CONSTRAINT [FK_PortalBiz_WorkItemEvents_WorkItems]
            FOREIGN KEY ([WorkItemId]) REFERENCES [dbo].[PortalBiz_WorkItems] ([WorkItemId]) ON DELETE CASCADE,
        CONSTRAINT [FK_PortalBiz_WorkItemEvents_ActorUser]
            FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
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

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_WorkItemEvents_WorkItemUtc' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkItemEvents]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_WorkItemEvents_WorkItemUtc]
    ON [dbo].[PortalBiz_WorkItemEvents] ([WorkItemId], [OccurredUtc] DESC, [EventId] DESC)
END
GO
