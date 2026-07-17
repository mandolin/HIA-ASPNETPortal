/*
    P6.2 企业用户资料扩展迁移。
    P6.2 enterprise user-profile extension migration.

    本脚本可重复执行；应用程序不会在启动时自动执行它。
    This script is idempotent; the application never runs it automatically at startup.

    本脚本不改变 Portal_Users 的门户账号主体地位，只补充登录名、展示名、昵称、偏好邮箱和账号状态。
    This script does not change Portal_Users as the Portal account authority; it only adds login name,
    display name, nickname, preferred email, and account status metadata.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF EXISTS
(
    SELECT [Name]
    FROM [dbo].[Portal_Users]
    GROUP BY [Name]
    HAVING COUNT(*) > 1
)
BEGIN
    RAISERROR(N'Portal_Users.Name contains duplicates. Resolve duplicate legacy names before creating PortalBiz_UserProfiles.', 16, 1)
    RETURN
END
GO

IF EXISTS
(
    SELECT 1
    FROM [dbo].[Portal_Users]
    WHERE NULLIF(LTRIM(RTRIM([Name])), N'') IS NULL
        OR [Name] <> LTRIM(RTRIM([Name]))
)
BEGIN
    RAISERROR(N'Portal_Users.Name contains blank or leading/trailing whitespace values. Resolve invalid legacy names before creating PortalBiz_UserProfiles.', 16, 1)
    RETURN
END
GO

IF EXISTS
(
    SELECT NULLIF(LTRIM(RTRIM([Email])), N'') AS [PreferredEmail]
    FROM [dbo].[Portal_Users]
    WHERE NULLIF(LTRIM(RTRIM([Email])), N'') IS NOT NULL
    GROUP BY NULLIF(LTRIM(RTRIM([Email])), N'')
    HAVING COUNT(*) > 1
)
BEGIN
    RAISERROR(N'Portal_Users.Email contains duplicate non-empty normalized values. Resolve duplicate legacy emails before creating PortalBiz_UserProfiles.', 16, 1)
    RETURN
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_UserProfiles]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[PortalBiz_UserProfiles]
    (
        [UserId] INT NOT NULL,
        [LoginName] NVARCHAR(100) NOT NULL,
        [DisplayName] NVARCHAR(150) NULL,
        [Nickname] NVARCHAR(100) NULL,
        [PreferredEmail] NVARCHAR(256) NULL,
        [Status] NVARCHAR(40) NOT NULL
            CONSTRAINT [DF_PortalBiz_UserProfiles_Status] DEFAULT (N'Active'),
        [StatusReason] NVARCHAR(200) NULL,
        [CreatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_UserProfiles_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(100) NULL,
        [UpdatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_UserProfiles_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        CONSTRAINT [PK_PortalBiz_UserProfiles]
            PRIMARY KEY CLUSTERED ([UserId]),
        CONSTRAINT [FK_PortalBiz_UserProfiles_Users]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Portal_Users] ([UserID]) ON DELETE CASCADE,
        CONSTRAINT [UQ_PortalBiz_UserProfiles_LoginName]
            UNIQUE ([LoginName]),
        CONSTRAINT [CK_PortalBiz_UserProfiles_Status]
            CHECK ([Status] IN (N'Active', N'PendingApproval', N'PendingEmployeeBinding', N'Disabled', N'Left', N'Locked'))
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'UX_PortalBiz_UserProfiles_PreferredEmail' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_UserProfiles]'))
BEGIN
    CREATE UNIQUE INDEX [UX_PortalBiz_UserProfiles_PreferredEmail]
    ON [dbo].[PortalBiz_UserProfiles] ([PreferredEmail])
    WHERE [PreferredEmail] IS NOT NULL AND [PreferredEmail] <> N''
END
GO

IF OBJECT_ID(N'[dbo].[PortalCfg_UserRegistrations]', N'U') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[PortalBiz_UserProfiles]
        ([UserId], [LoginName], [DisplayName], [PreferredEmail], [Status], [CreatedBy], [UpdatedBy])
    SELECT
        [Users].[UserID],
        [Users].[Name],
        [Users].[Name],
        NULLIF(LTRIM(RTRIM([Users].[Email])), N''),
        CASE
            WHEN [Registrations].[Status] = N'PendingApproval' THEN N'PendingApproval'
            WHEN [Registrations].[Status] = N'Rejected' THEN N'Disabled'
            ELSE N'Active'
        END,
        N'system-seed',
        N'system-seed'
    FROM [dbo].[Portal_Users] AS [Users]
    LEFT JOIN [dbo].[PortalCfg_UserRegistrations] AS [Registrations]
        ON [Registrations].[UserId] = [Users].[UserID]
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [dbo].[PortalBiz_UserProfiles] AS [Profiles]
        WHERE [Profiles].[UserId] = [Users].[UserID]
    )
END
ELSE
BEGIN
    INSERT INTO [dbo].[PortalBiz_UserProfiles]
        ([UserId], [LoginName], [DisplayName], [PreferredEmail], [Status], [CreatedBy], [UpdatedBy])
    SELECT
        [Users].[UserID],
        [Users].[Name],
        [Users].[Name],
        NULLIF(LTRIM(RTRIM([Users].[Email])), N''),
        N'Active',
        N'system-seed',
        N'system-seed'
    FROM [dbo].[Portal_Users] AS [Users]
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [dbo].[PortalBiz_UserProfiles] AS [Profiles]
        WHERE [Profiles].[UserId] = [Users].[UserID]
    )
END
GO
