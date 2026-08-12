/*
<lang>
  <zh-CN>P5.2 用户凭据与安全版本迁移。本脚本可重复执行且不会由应用启动流程自动执行；`Portal_Users.Password` 暂时保留为旧 MD5 迁移样本，新建、注册和重置凭据必须写入本脚本创建的表。</zh-CN>
  <en>P5.2 user credential and security-version migration. This script is idempotent and is not executed automatically by application startup; `Portal_Users.Password` is temporarily retained as a legacy MD5 migration sample, and newly created, registered, or reset credentials must be written to the tables created by this script.</en>
</lang>
*/

-- <lang>
--   <zh-CN>建表保护保留已迁移凭据哈希、盐和重置状态；补跑脚本不会重写任何真实凭据材料。</zh-CN>
--   <en>The create-table guard preserves migrated credential hashes, salts, and reset state; rerunning the script does not rewrite any real credential material.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_UserCredentials]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>凭据表把新密码材料从旧用户表中分离出来，保存哈希、盐、成本和迁移状态，不保存明文密码。</zh-CN>
    --   <en>The credential table separates new password material from the legacy user table and stores hash, salt, cost, and migration state, never plaintext passwords.</en>
    -- </lang>
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

        -- <lang>
        --   <zh-CN>主键沿用用户标识并级联删除，确保凭据材料不能在账号删除后孤立保留。</zh-CN>
        --   <en>The primary key reuses the user id with cascade delete so credential material cannot remain orphaned after account deletion.</en>
        -- </lang>
        CONSTRAINT [PK_Portal_UserCredentials]
            PRIMARY KEY CLUSTERED ([UserId]),
        CONSTRAINT [FK_Portal_UserCredentials_Users]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Portal_Users] ([UserID]) ON DELETE CASCADE,
        CONSTRAINT [CK_Portal_UserCredentials_CredentialVersion]
            CHECK ([CredentialVersion] > 0),
        -- <lang>
        --   <zh-CN>成本、哈希和盐检查只保证结构非空；算法、长度和升级策略仍由服务层凭据策略控制。</zh-CN>
        --   <en>Cost, hash, and salt checks guarantee only non-empty structure; algorithm, length, and upgrade policy remain controlled by service-layer credential policy.</en>
        -- </lang>
        CONSTRAINT [CK_Portal_UserCredentials_IterationCount]
            CHECK ([IterationCount] > 0),
        CONSTRAINT [CK_Portal_UserCredentials_PasswordHash]
            CHECK (DATALENGTH([PasswordHash]) > 0),
        CONSTRAINT [CK_Portal_UserCredentials_PasswordSalt]
            CHECK (DATALENGTH([PasswordSalt]) > 0)
    )
END
GO

-- <lang>
--   <zh-CN>安全状态表独立保存用户级安全版本，用于让角色/绑定/凭据变化使旧 Cookie 或票据失效。</zh-CN>
--   <en>The security-state table independently stores the per-user security version used to invalidate old cookies or tickets after role, binding, or credential changes.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portal_UserSecurityStates]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>安全版本表不保存密码材料；它只记录版本号、变更时间和低敏原因。</zh-CN>
    --   <en>The security-version table stores no password material; it records only the version number, change time, and low-sensitivity reason.</en>
    -- </lang>
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

        -- <lang>
        --   <zh-CN>用户标识仍是唯一键并级联删除，保证安全版本生命周期跟随账号主体。</zh-CN>
        --   <en>The user id remains the unique key with cascade delete, keeping security-version lifetime tied to the account authority.</en>
        -- </lang>
        CONSTRAINT [PK_Portal_UserSecurityStates]
            PRIMARY KEY CLUSTERED ([UserId]),
        CONSTRAINT [FK_Portal_UserSecurityStates_Users]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Portal_Users] ([UserID]) ON DELETE CASCADE,
        CONSTRAINT [CK_Portal_UserSecurityStates_SecurityVersion]
            CHECK ([SecurityVersion] >= 0)
    )
END
GO

-- <lang>
--   <zh-CN>LegacySeed 为旧账号补齐初始安全版本；`NOT EXISTS` 保护已经被运行时提升过的安全版本不被回退。</zh-CN>
--   <en>LegacySeed backfills an initial security version for old accounts; `NOT EXISTS` protects security versions already advanced by runtime behavior from being rolled back.</en>
-- </lang>
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
