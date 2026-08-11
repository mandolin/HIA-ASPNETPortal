/*
<lang>
  <zh-CN>P6.4 员工资料确认业务模块迁移。本脚本可重复执行且不会由应用启动流程自动执行；第一版采用追加式确认记录，只保存员工看到并确认的低敏资料快照，不保存密码、Cookie、Token、身份证号、手机号、薪资、绩效或其它高敏个人资料。</zh-CN>
  <en>P6.4 employee-profile confirmation business-module migration. This script is idempotent and is not executed automatically by application startup; the first version uses append-only confirmation records and stores only the low-sensitivity profile snapshot the employee saw and confirmed, with no passwords, cookies, tokens, government ids, mobile phone numbers, compensation, performance data, or other high-sensitivity personal data.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，确保快照字段和确认人检查约束在空值场景下稳定执行。</zh-CN>
--   <en>Enable standard NULL comparison semantics so snapshot-field and confirmer check constraints execute consistently around null values.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护确认表、外键和时间线索引名称在不同执行器中一致解析。</zh-CN>
--   <en>Enable quoted identifiers so confirmation table, foreign key, and timeline index names parse consistently across executors.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>确认记录必须锚定员工主数据；员工表缺失时立即中止，避免无法解释的确认快照。</zh-CN>
--   <en>Confirmation records must anchor to employee master data; stop immediately if the employee table is missing to avoid unexplained confirmation snapshots.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[PortalBiz_Employees]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_Employees must be created before PortalBiz_EmployeeProfileConfirmations.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>确认记录必须锚定门户账号主体，便于审计是谁在登录态下完成确认。</zh-CN>
--   <en>Confirmation records must anchor to the Portal account authority so audits can identify who confirmed while signed in.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_EmployeeProfileConfirmations.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>确认记录还需要绑定行来证明账号和员工在确认时的关系，缺失绑定表时不创建确认表。</zh-CN>
--   <en>Confirmation records also need the binding row to prove the account-employee relationship at confirmation time, so the table is not created without the binding table.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[PortalBiz_UserEmployeeBindings]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_UserEmployeeBindings must be created before PortalBiz_EmployeeProfileConfirmations.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>建表保护保留既有追加式确认历史；重复执行脚本不会压缩、覆盖或重写旧确认记录。</zh-CN>
--   <en>The create-table guard preserves existing append-only confirmation history; repeated execution does not compact, overwrite, or rewrite old confirmation records.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_EmployeeProfileConfirmations]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>确认表保存当时展示给员工的低敏快照，支持后续审计“确认时看到的内容”，而不是读取可变主数据。</zh-CN>
    --   <en>The confirmation table stores the low-sensitivity snapshot shown to the employee at the time, supporting later audits of “what was seen when confirming” instead of rereading mutable master data.</en>
    -- </lang>
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

        -- <lang>
        --   <zh-CN>主键使用追加式 BIGINT 标识；外键同时固定员工、账号和绑定三端，防止后续身份变化抹平确认来源。</zh-CN>
        --   <en>The primary key uses an append-only BIGINT identifier; foreign keys pin employee, account, and binding endpoints so later identity changes do not erase the confirmation source.</en>
        -- </lang>
        CONSTRAINT [PK_PortalBiz_EmployeeProfileConfirmations]
            PRIMARY KEY CLUSTERED ([ConfirmationId]),
        CONSTRAINT [FK_PortalBiz_EmployeeProfileConfirmations_Employees]
            FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[PortalBiz_Employees] ([EmployeeId]),
        CONSTRAINT [FK_PortalBiz_EmployeeProfileConfirmations_Users]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        CONSTRAINT [FK_PortalBiz_EmployeeProfileConfirmations_Bindings]
            FOREIGN KEY ([BindingId]) REFERENCES [dbo].[PortalBiz_UserEmployeeBindings] ([BindingId]),
        -- <lang>
        --   <zh-CN>快照文本约束阻断空白确认人和空白关键字段，可选字段为空时表示当时没有该低敏资料。</zh-CN>
        --   <en>Snapshot text constraints block blank confirmer and blank key fields, while optional null fields mean the low-sensitivity data did not exist at that time.</en>
        -- </lang>
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

-- <lang>
--   <zh-CN>员工/用户时间线索引用于查询某员工在某账号下的最近确认记录，按 UTC 时间和追加标识倒序。</zh-CN>
--   <en>The employee/user timeline index supports querying the latest confirmation for an employee under a user account, ordered by UTC time and append-only id descending.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_EmployeeProfileConfirmations_EmployeeUser' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_EmployeeProfileConfirmations]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_EmployeeProfileConfirmations_EmployeeUser]
    ON [dbo].[PortalBiz_EmployeeProfileConfirmations] ([EmployeeId], [UserId], [ConfirmedUtc] DESC, [ConfirmationId] DESC)
END
GO

-- <lang>
--   <zh-CN>用户时间线索引用于个人中心或审计视角下按账号查看最近确认历史。</zh-CN>
--   <en>The user timeline index supports viewing recent confirmation history from the personal-center or audit perspective by account.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_EmployeeProfileConfirmations_User' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_EmployeeProfileConfirmations]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_EmployeeProfileConfirmations_User]
    ON [dbo].[PortalBiz_EmployeeProfileConfirmations] ([UserId], [ConfirmedUtc] DESC, [ConfirmationId] DESC)
END
GO
