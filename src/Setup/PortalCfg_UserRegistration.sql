/*
    P2.3 用户注册审核与临时注册链接迁移脚本。
    P2.3 user registration review and temporary invitation migration script.

    说明：
    1. 本脚本不会由应用启动流程自动执行，需要由开发/部署人员显式执行。
    2. 本脚本不改变 Portal_Users 的认证主体地位，只补充注册审核和邀请链接元数据。
    3. 既有用户会被 seed 为 Approved，避免旧账号在迁移后无法登录。
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_RegistrationInvites]') AND type IN (N'U'))
BEGIN
    -- 临时注册链接表：后续系统管理 UI 会负责创建和停用这些链接。
    -- Temporary invitation table: future admin UI will create and disable these links.
    CREATE TABLE [dbo].[PortalCfg_RegistrationInvites]
    (
        [InviteCode] NVARCHAR(64) NOT NULL,
        [Description] NVARCHAR(200) NULL,
        [ExpiresUtc] DATETIME2(0) NOT NULL,
        [MaxUses] INT NULL,
        [UsedCount] INT NOT NULL
            CONSTRAINT [DF_PortalCfg_RegistrationInvites_UsedCount] DEFAULT ((0)),
        [IsEnabled] BIT NOT NULL
            CONSTRAINT [DF_PortalCfg_RegistrationInvites_IsEnabled] DEFAULT ((1)),
        [RequireEmployeeCode] BIT NOT NULL
            CONSTRAINT [DF_PortalCfg_RegistrationInvites_RequireEmployeeCode] DEFAULT ((1)),
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalCfg_RegistrationInvites_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
        [RowVersion] ROWVERSION NOT NULL,

        CONSTRAINT [PK_PortalCfg_RegistrationInvites]
            PRIMARY KEY CLUSTERED ([InviteCode]),

        CONSTRAINT [CK_PortalCfg_RegistrationInvites_MaxUses]
            CHECK ([MaxUses] IS NULL OR [MaxUses] > 0),

        CONSTRAINT [CK_PortalCfg_RegistrationInvites_UsedCount]
            CHECK ([UsedCount] >= 0)
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_UserRegistrations]') AND type IN (N'U'))
BEGIN
    -- 注册审核表：一条 Portal_Users 记录最多对应一条审核元数据。
    -- Registration review table: each Portal_Users row can have at most one review metadata row.
    CREATE TABLE [dbo].[PortalCfg_UserRegistrations]
    (
        [RegistrationId] INT IDENTITY(1, 1) NOT NULL,
        [UserId] INT NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [RequiresApproval] BIT NOT NULL
            CONSTRAINT [DF_PortalCfg_UserRegistrations_RequiresApproval] DEFAULT ((1)),
        [EmployeeCode] NVARCHAR(100) NULL,
        [InviteCode] NVARCHAR(64) NULL,
        [RegisteredUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalCfg_UserRegistrations_RegisteredUtc] DEFAULT (SYSUTCDATETIME()),
        [ApprovedUtc] DATETIME2(0) NULL,
        [ApprovedBy] NVARCHAR(100) NULL,
        [RejectedUtc] DATETIME2(0) NULL,
        [RejectedBy] NVARCHAR(100) NULL,
        [ReviewNote] NVARCHAR(500) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        CONSTRAINT [PK_PortalCfg_UserRegistrations]
            PRIMARY KEY CLUSTERED ([RegistrationId]),

        CONSTRAINT [UQ_PortalCfg_UserRegistrations_UserId]
            UNIQUE ([UserId]),

        CONSTRAINT [CK_PortalCfg_UserRegistrations_Status]
            CHECK ([Status] IN (N'Approved', N'PendingApproval', N'Rejected')),

        CONSTRAINT [FK_PortalCfg_UserRegistrations_Users]
            FOREIGN KEY ([UserId])
            REFERENCES [dbo].[Portal_Users] ([UserID])
            ON DELETE CASCADE,

        CONSTRAINT [FK_PortalCfg_UserRegistrations_Invites]
            FOREIGN KEY ([InviteCode])
            REFERENCES [dbo].[PortalCfg_RegistrationInvites] ([InviteCode])
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalCfg_UserRegistrations_Status_RegisteredUtc' AND object_id = OBJECT_ID(N'[dbo].[PortalCfg_UserRegistrations]'))
BEGIN
    CREATE INDEX [IX_PortalCfg_UserRegistrations_Status_RegisteredUtc]
    ON [dbo].[PortalCfg_UserRegistrations] ([Status], [RegisteredUtc] DESC)
END
GO

-- 既有用户按已批准导入，避免迁移后阻断旧账号登录。
-- Seed existing users as approved so migration does not block legacy accounts.
INSERT INTO [dbo].[PortalCfg_UserRegistrations]
    ([UserId], [Status], [RequiresApproval], [RegisteredUtc], [ApprovedUtc], [ApprovedBy])
SELECT
    [UserID],
    N'Approved',
    0,
    SYSUTCDATETIME(),
    SYSUTCDATETIME(),
    N'system-legacy'
FROM [dbo].[Portal_Users] AS [Users]
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[PortalCfg_UserRegistrations] AS [Registrations]
    WHERE [Registrations].[UserId] = [Users].[UserID]
)
GO
