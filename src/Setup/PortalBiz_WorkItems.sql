/*
    P12.3 轻量待办记录迁移。
    P12.3 lightweight work-item record migration.

    本脚本可重复执行；应用程序不会在启动时自动执行它。
    This script is idempotent; the application never runs it automatically at startup.

    第一版只保存业务对象、办理状态、分派目标和低敏摘要，不保存密码、Cookie、Token、
    连接串、证件号、薪资或其它高敏个人资料。
    The first version stores only business object identifiers, handling status, assignee target,
    and low-sensitivity summaries. It stores no passwords, cookies, tokens, connection strings,
    government ids, compensation data, or other high-sensitivity personal data.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_WorkItems.', 16, 1)
    RETURN
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]') AND type IN (N'U'))
BEGIN
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

        CONSTRAINT [PK_PortalBiz_WorkItems]
            PRIMARY KEY CLUSTERED ([WorkItemId]),
        CONSTRAINT [FK_PortalBiz_WorkItems_AssignedUser]
            FOREIGN KEY ([AssignedUserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        CONSTRAINT [CK_PortalBiz_WorkItems_BusinessKind]
            CHECK ([BusinessKind] = LTRIM(RTRIM([BusinessKind])) AND NULLIF([BusinessKind], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_WorkItems_BusinessId]
            CHECK ([BusinessId] = LTRIM(RTRIM([BusinessId])) AND NULLIF([BusinessId], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_WorkItems_Title]
            CHECK ([Title] = LTRIM(RTRIM([Title])) AND NULLIF([Title], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_WorkItems_Summary]
            CHECK ([Summary] IS NULL OR ([Summary] = LTRIM(RTRIM([Summary])) AND NULLIF([Summary], N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_WorkItems_Status]
            CHECK ([WorkItemStatus] IN (N'Open', N'InProgress', N'Completed', N'Cancelled', N'Expired')),
        CONSTRAINT [CK_PortalBiz_WorkItems_Assignment]
            CHECK ([AssignedUserId] IS NOT NULL OR ([AssignedRoleKey] IS NOT NULL AND NULLIF(LTRIM(RTRIM([AssignedRoleKey])), N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_WorkItems_CreatedBy]
            CHECK ([CreatedBy] = LTRIM(RTRIM([CreatedBy])) AND NULLIF([CreatedBy], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_WorkItems_CompletedBy]
            CHECK ([CompletedBy] IS NULL OR ([CompletedBy] = LTRIM(RTRIM([CompletedBy])) AND NULLIF([CompletedBy], N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_WorkItems_CompletionState]
            CHECK (
                ([WorkItemStatus] IN (N'Open', N'InProgress') AND [CompletedUtc] IS NULL AND [CompletedBy] IS NULL)
                OR
                ([WorkItemStatus] IN (N'Completed', N'Cancelled', N'Expired') AND [CompletedUtc] IS NOT NULL AND [CompletedBy] IS NOT NULL)
            )
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'UX_PortalBiz_WorkItems_ActiveBusiness' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]'))
BEGIN
    CREATE UNIQUE INDEX [UX_PortalBiz_WorkItems_ActiveBusiness]
    ON [dbo].[PortalBiz_WorkItems] ([BusinessKind], [BusinessId])
    WHERE [WorkItemStatus] IN (N'Open', N'InProgress')
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_WorkItems_StatusCreated' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_WorkItems_StatusCreated]
    ON [dbo].[PortalBiz_WorkItems] ([WorkItemStatus], [CreatedUtc] DESC, [WorkItemId] DESC)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_WorkItems_AssignedUserStatus' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_WorkItems_AssignedUserStatus]
    ON [dbo].[PortalBiz_WorkItems] ([AssignedUserId], [WorkItemStatus], [CreatedUtc] DESC)
    WHERE [AssignedUserId] IS NOT NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_WorkItems_AssignedRoleStatus' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_WorkItems]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_WorkItems_AssignedRoleStatus]
    ON [dbo].[PortalBiz_WorkItems] ([AssignedRoleKey], [WorkItemStatus], [CreatedUtc] DESC)
    WHERE [AssignedRoleKey] IS NOT NULL
END
GO
