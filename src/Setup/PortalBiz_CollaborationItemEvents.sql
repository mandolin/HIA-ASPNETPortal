/*
    P21.3 企业协同事项事件迁移。
    P21.3 enterprise collaboration-item event migration.

    本脚本可重复执行；应用程序不会在启动时自动执行它。
    This script is idempotent; the application never runs it automatically at startup.

    本表保存协同事项自身的流程动作事实，与 PortalBiz_WorkItems 的待办投影事件、
    PortalCfg_OperationAudits 的运营审计和运行时诊断日志分离。
    This table stores workflow action facts for collaboration items themselves and stays
    separate from PortalBiz_WorkItems projection events, PortalCfg_OperationAudits
    operational audits, and runtime diagnostic logs.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItems]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_CollaborationItems must be created before PortalBiz_CollaborationItemEvents.', 16, 1)
    RETURN
END
GO

IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_CollaborationItemEvents.', 16, 1)
    RETURN
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItemEvents]') AND type IN (N'U'))
BEGIN
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

        CONSTRAINT [PK_PortalBiz_CollaborationItemEvents]
            PRIMARY KEY CLUSTERED ([EventId]),
        CONSTRAINT [FK_PortalBiz_CollaborationItemEvents_Items]
            FOREIGN KEY ([ItemId]) REFERENCES [dbo].[PortalBiz_CollaborationItems] ([ItemId]) ON DELETE CASCADE,
        CONSTRAINT [FK_PortalBiz_CollaborationItemEvents_ActorUser]
            FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
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

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_CollaborationItemEvents_ItemUtc' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItemEvents]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_CollaborationItemEvents_ItemUtc]
    ON [dbo].[PortalBiz_CollaborationItemEvents] ([ItemId], [OccurredUtc] DESC, [EventId] DESC)
END
GO
