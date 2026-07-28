/*
    P21.3 企业协同事项主表迁移。
    P21.3 enterprise collaboration-item fact migration.

    本脚本可重复执行；应用程序不会在启动时自动执行它。
    This script is idempotent; the application never runs it automatically at startup.

    第一版只保存低敏事项主数据、状态、发起人、负责人、组织、期限和最近办理意见。
    不保存附件、富文本、评论、评分、搜索索引、密码、Cookie、Token、连接串、证件号、
    薪资或具体行业字段。
    The first version stores only low-sensitivity item facts: current state, initiator,
    owner, organization, due date, and latest handling comment. It stores no attachments,
    rich text, comments, ratings, search indexes, passwords, cookies, tokens, connection
    strings, government ids, compensation data, or domain-specific fields.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_CollaborationItems.', 16, 1)
    RETURN
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItems]') AND type IN (N'U'))
BEGIN
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

        CONSTRAINT [PK_PortalBiz_CollaborationItems]
            PRIMARY KEY CLUSTERED ([ItemId]),
        CONSTRAINT [UX_PortalBiz_CollaborationItems_Code]
            UNIQUE ([ItemCode]),
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
        CONSTRAINT [CK_PortalBiz_CollaborationItems_Summary]
            CHECK ([Summary] IS NULL OR ([Summary] = LTRIM(RTRIM([Summary])) AND NULLIF([Summary], N'') IS NOT NULL)),
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
        CONSTRAINT [CK_PortalBiz_CollaborationItems_Assignment]
            CHECK (
                [ItemStatus] IN (N'Draft', N'Completed', N'Rejected', N'Cancelled', N'Closed')
                OR [OwnerUserId] IS NOT NULL
                OR ([OwnerRoleKey] IS NOT NULL AND NULLIF(LTRIM(RTRIM([OwnerRoleKey])), N'') IS NOT NULL)
            ),
        CONSTRAINT [CK_PortalBiz_CollaborationItems_CompletionUtc]
            CHECK (
                ([ItemStatus] IN (N'Completed', N'Rejected', N'Cancelled', N'Closed') AND [CompletedUtc] IS NOT NULL)
                OR
                ([ItemStatus] IN (N'Draft', N'Submitted', N'InProgress', N'Returned') AND [CompletedUtc] IS NULL)
            ),
        CONSTRAINT [CK_PortalBiz_CollaborationItems_ClosedUtc]
            CHECK (
                ([ItemStatus] = N'Closed' AND [ClosedUtc] IS NOT NULL)
                OR
                ([ItemStatus] <> N'Closed' AND [ClosedUtc] IS NULL)
            )
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_CollaborationItems_StatusAction' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItems]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_CollaborationItems_StatusAction]
    ON [dbo].[PortalBiz_CollaborationItems] ([ItemStatus], [LastActionUtc] DESC, [ItemId] DESC)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_CollaborationItems_Initiator' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItems]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_CollaborationItems_Initiator]
    ON [dbo].[PortalBiz_CollaborationItems] ([InitiatorUserId], [LastActionUtc] DESC, [ItemId] DESC)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_CollaborationItems_OwnerUserStatus' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItems]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_CollaborationItems_OwnerUserStatus]
    ON [dbo].[PortalBiz_CollaborationItems] ([OwnerUserId], [ItemStatus], [LastActionUtc] DESC)
    WHERE [OwnerUserId] IS NOT NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_CollaborationItems_OwnerRoleStatus' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_CollaborationItems]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_CollaborationItems_OwnerRoleStatus]
    ON [dbo].[PortalBiz_CollaborationItems] ([OwnerRoleKey], [ItemStatus], [LastActionUtc] DESC)
    WHERE [OwnerRoleKey] IS NOT NULL
END
GO
