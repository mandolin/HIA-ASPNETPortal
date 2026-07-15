/*
    P5.2 用户凭据与安全版本迁移。
    P5.2 user credential and security-version migration.

    此脚本可重复执行；应用程序不会在启动时自动执行它。
    This script is idempotent; the application never runs it automatically at startup.

    Portal_Users.Password 暂时保留为旧 MD5 迁移样本；新建、注册和重置凭据必须写入本脚本创建的表。
    Portal_Users.Password is temporarily retained as a legacy MD5 migration sample; newly created, registered,
    or reset credentials must be written to the tables created by this script.
*/

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_UserCredentials]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[Portal_UserCredentials]
    (
        [UserId] INT NOT NULL,
        [CredentialVersion] INT NOT NULL
            CONSTRAINT [DF_Portal_UserCredentials_CredentialVersion] DEFAULT (1),
        [PasswordFormat] NVARCHAR(40) NOT NULL,
        [PasswordHash] VARBINARY(64) NOT NULL,
        [PasswordSalt] VARBINARY(32) NOT NULL,
        [IterationCount] INT NOT NULL,
        [CreatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_Portal_UserCredentials_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_Portal_UserCredentials_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
        [LastVerifiedUtc] DATETIME2(0) NULL,
        [LegacyUpgradedUtc] DATETIME2(0) NULL,
        [RequiresReset] BIT NOT NULL
            CONSTRAINT [DF_Portal_UserCredentials_RequiresReset] DEFAULT (0),
        [ResetReason] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        CONSTRAINT [PK_Portal_UserCredentials]
            PRIMARY KEY CLUSTERED ([UserId]),
        CONSTRAINT [FK_Portal_UserCredentials_Users]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Portal_Users] ([UserID]) ON DELETE CASCADE,
        CONSTRAINT [CK_Portal_UserCredentials_CredentialVersion]
            CHECK ([CredentialVersion] > 0),
        CONSTRAINT [CK_Portal_UserCredentials_IterationCount]
            CHECK ([IterationCount] > 0),
        CONSTRAINT [CK_Portal_UserCredentials_PasswordHash]
            CHECK (DATALENGTH([PasswordHash]) > 0),
        CONSTRAINT [CK_Portal_UserCredentials_PasswordSalt]
            CHECK (DATALENGTH([PasswordSalt]) > 0)
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_UserSecurityStates]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[Portal_UserSecurityStates]
    (
        [UserId] INT NOT NULL,
        [SecurityVersion] BIGINT NOT NULL
            CONSTRAINT [DF_Portal_UserSecurityStates_SecurityVersion] DEFAULT (1),
        [ChangedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_Portal_UserSecurityStates_ChangedUtc] DEFAULT (SYSUTCDATETIME()),
        [ChangeReason] NVARCHAR(100) NOT NULL
            CONSTRAINT [DF_Portal_UserSecurityStates_ChangeReason] DEFAULT (N'LegacySeed'),
        [RowVersion] ROWVERSION NOT NULL,

        CONSTRAINT [PK_Portal_UserSecurityStates]
            PRIMARY KEY CLUSTERED ([UserId]),
        CONSTRAINT [FK_Portal_UserSecurityStates_Users]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Portal_Users] ([UserID]) ON DELETE CASCADE,
        CONSTRAINT [CK_Portal_UserSecurityStates_SecurityVersion]
            CHECK ([SecurityVersion] >= 0)
    )
END
GO

INSERT INTO [dbo].[Portal_UserSecurityStates] ([UserId], [SecurityVersion], [ChangedUtc], [ChangeReason])
SELECT [UserID], 1, SYSUTCDATETIME(), N'LegacySeed'
FROM [dbo].[Portal_Users] AS users
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[Portal_UserSecurityStates] AS states
    WHERE states.[UserId] = users.[UserId]
)
GO
