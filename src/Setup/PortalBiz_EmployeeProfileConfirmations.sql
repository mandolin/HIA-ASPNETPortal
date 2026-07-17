/*
    P6.4 员工资料确认业务模块迁移。
    P6.4 employee-profile confirmation business-module migration.

    本脚本可重复执行；应用程序不会在启动时自动执行它。
    This script is idempotent; the application never runs it automatically at startup.

    第一版采用追加式确认记录，保存员工看到并确认的低敏资料快照；不保存密码、Cookie、Token、
    身份证号、手机号、薪资、绩效或其它高敏个人资料。
    The first version uses append-only confirmation records and stores the low-sensitivity profile snapshot the
    employee saw and confirmed. It stores no passwords, cookies, tokens, government ids, mobile phone numbers,
    compensation, performance data, or other high-sensitivity personal data.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'[dbo].[PortalBiz_Employees]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_Employees must be created before PortalBiz_EmployeeProfileConfirmations.', 16, 1)
    RETURN
END
GO

IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_EmployeeProfileConfirmations.', 16, 1)
    RETURN
END
GO

IF OBJECT_ID(N'[dbo].[PortalBiz_UserEmployeeBindings]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_UserEmployeeBindings must be created before PortalBiz_EmployeeProfileConfirmations.', 16, 1)
    RETURN
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_EmployeeProfileConfirmations]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[PortalBiz_EmployeeProfileConfirmations]
    (
        [ConfirmationId] BIGINT IDENTITY(1,1) NOT NULL,
        [EmployeeId] INT NOT NULL,
        [UserId] INT NOT NULL,
        [BindingId] INT NOT NULL,
        [ConfirmedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_EmployeeProfileConfirmations_ConfirmedUtc] DEFAULT (SYSUTCDATETIME()),
        [ConfirmedBy] NVARCHAR(100) NOT NULL,
        [SnapshotEmployeeCode] NVARCHAR(64) NOT NULL,
        [SnapshotDisplayName] NVARCHAR(150) NOT NULL,
        [SnapshotPreferredName] NVARCHAR(100) NULL,
        [SnapshotWorkEmail] NVARCHAR(256) NULL,
        [SnapshotOrganizationDisplayName] NVARCHAR(150) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        CONSTRAINT [PK_PortalBiz_EmployeeProfileConfirmations]
            PRIMARY KEY CLUSTERED ([ConfirmationId]),
        CONSTRAINT [FK_PortalBiz_EmployeeProfileConfirmations_Employees]
            FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[PortalBiz_Employees] ([EmployeeId]),
        CONSTRAINT [FK_PortalBiz_EmployeeProfileConfirmations_Users]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        CONSTRAINT [FK_PortalBiz_EmployeeProfileConfirmations_Bindings]
            FOREIGN KEY ([BindingId]) REFERENCES [dbo].[PortalBiz_UserEmployeeBindings] ([BindingId]),
        CONSTRAINT [CK_PortalBiz_EmployeeProfileConfirmations_ConfirmedBy]
            CHECK ([ConfirmedBy] = LTRIM(RTRIM([ConfirmedBy])) AND NULLIF([ConfirmedBy], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_EmployeeProfileConfirmations_SnapshotEmployeeCode]
            CHECK ([SnapshotEmployeeCode] = LTRIM(RTRIM([SnapshotEmployeeCode])) AND NULLIF([SnapshotEmployeeCode], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_EmployeeProfileConfirmations_SnapshotDisplayName]
            CHECK ([SnapshotDisplayName] = LTRIM(RTRIM([SnapshotDisplayName])) AND NULLIF([SnapshotDisplayName], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_EmployeeProfileConfirmations_SnapshotPreferredName]
            CHECK ([SnapshotPreferredName] IS NULL OR ([SnapshotPreferredName] = LTRIM(RTRIM([SnapshotPreferredName])) AND NULLIF([SnapshotPreferredName], N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_EmployeeProfileConfirmations_SnapshotWorkEmail]
            CHECK ([SnapshotWorkEmail] IS NULL OR ([SnapshotWorkEmail] = LTRIM(RTRIM([SnapshotWorkEmail])) AND NULLIF([SnapshotWorkEmail], N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_EmployeeProfileConfirmations_SnapshotOrganization]
            CHECK ([SnapshotOrganizationDisplayName] IS NULL OR ([SnapshotOrganizationDisplayName] = LTRIM(RTRIM([SnapshotOrganizationDisplayName])) AND NULLIF([SnapshotOrganizationDisplayName], N'') IS NOT NULL))
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_EmployeeProfileConfirmations_EmployeeUser' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_EmployeeProfileConfirmations]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_EmployeeProfileConfirmations_EmployeeUser]
    ON [dbo].[PortalBiz_EmployeeProfileConfirmations] ([EmployeeId], [UserId], [ConfirmedUtc] DESC, [ConfirmationId] DESC)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_EmployeeProfileConfirmations_User' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_EmployeeProfileConfirmations]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_EmployeeProfileConfirmations_User]
    ON [dbo].[PortalBiz_EmployeeProfileConfirmations] ([UserId], [ConfirmedUtc] DESC, [ConfirmationId] DESC)
END
GO
