/*
    P2.1 系统设置运行级表迁移脚本。
    P2.1 runtime system settings migration script.

    说明：
    1. 本脚本只创建非敏感运行级设置表和审计表。
    2. 本脚本不会被应用启动流程自动执行，需要由开发/部署人员显式执行。
    3. 连接串、密码、Token、证书、密钥等敏感值不得写入这些表。
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_SystemSettings]') AND type IN (N'U'))
BEGIN
    -- 当前值表：保存非敏感运行级设置的数据库覆盖值。
    -- Current values table: stores database overrides for non-sensitive runtime settings.
    CREATE TABLE [dbo].[PortalCfg_SystemSettings]
    (
        [SettingKey] NVARCHAR(200) NOT NULL,
        [SettingValue] NVARCHAR(MAX) NULL,
        [ValueType] NVARCHAR(50) NOT NULL,
        [SourceLevel] NVARCHAR(50) NOT NULL
            CONSTRAINT [DF_PortalCfg_SystemSettings_SourceLevel] DEFAULT (N'Database'),
        [CanDelete] BIT NOT NULL
            CONSTRAINT [DF_PortalCfg_SystemSettings_CanDelete] DEFAULT ((1)),
        [UpdatedBy] NVARCHAR(100) NULL,
        [UpdatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalCfg_SystemSettings_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
        [RowVersion] ROWVERSION NOT NULL,

        CONSTRAINT [PK_PortalCfg_SystemSettings]
            PRIMARY KEY CLUSTERED ([SettingKey]),

        CONSTRAINT [CK_PortalCfg_SystemSettings_ValueType]
            CHECK ([ValueType] IN (N'Boolean', N'Integer', N'String', N'Enum', N'Path', N'Duration')),

        CONSTRAINT [CK_PortalCfg_SystemSettings_SourceLevel]
            CHECK ([SourceLevel] = N'Database')
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_SystemSettingAudits]') AND type IN (N'U'))
BEGIN
    -- 审计表：记录非敏感运行级设置的在线修改历史。
    -- Audit table: records online change history for non-sensitive runtime settings.
    CREATE TABLE [dbo].[PortalCfg_SystemSettingAudits]
    (
        [AuditId] BIGINT IDENTITY(1, 1) NOT NULL,
        [SettingKey] NVARCHAR(200) NOT NULL,
        [ChangeType] NVARCHAR(20) NOT NULL,
        [OldValue] NVARCHAR(MAX) NULL,
        [NewValue] NVARCHAR(MAX) NULL,
        [ChangedBy] NVARCHAR(100) NOT NULL,
        [ChangedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalCfg_SystemSettingAudits_ChangedUtc] DEFAULT (SYSUTCDATETIME()),
        [ChangeReason] NVARCHAR(500) NULL,
        [ClientIp] NVARCHAR(64) NULL,
        [UserAgent] NVARCHAR(400) NULL,
        [CorrelationId] NVARCHAR(64) NULL,

        CONSTRAINT [PK_PortalCfg_SystemSettingAudits]
            PRIMARY KEY CLUSTERED ([AuditId]),

        CONSTRAINT [CK_PortalCfg_SystemSettingAudits_ChangeType]
            CHECK ([ChangeType] IN (N'Insert', N'Update', N'Delete'))
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalCfg_SystemSettingAudits_SettingKey_ChangedUtc' AND object_id = OBJECT_ID(N'[dbo].[PortalCfg_SystemSettingAudits]'))
BEGIN
    CREATE INDEX [IX_PortalCfg_SystemSettingAudits_SettingKey_ChangedUtc]
    ON [dbo].[PortalCfg_SystemSettingAudits] ([SettingKey], [ChangedUtc] DESC)
END
GO

-- 第一批运行级设置 seed：只写入非敏感、可在线管理的业务/运营设置。
-- Initial runtime setting seeds: non-sensitive settings only.
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Security.AllowSelfRegistration')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Security.AllowSelfRegistration', N'false', N'Boolean', N'Database', 0, N'system', SYSUTCDATETIME())
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Security.RequireRegistrationApproval')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Security.RequireRegistrationApproval', N'true', N'Boolean', N'Database', 0, N'system', SYSUTCDATETIME())
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Registration.InviteDefaultExpiryDays')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Registration.InviteDefaultExpiryDays', N'7', N'Integer', N'Database', 0, N'system', SYSUTCDATETIME())
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Registration.AllowPendingEmployeeBinding')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Registration.AllowPendingEmployeeBinding', N'false', N'Boolean', N'Database', 0, N'system', SYSUTCDATETIME())
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Documents.MaxUploadBytes')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Documents.MaxUploadBytes', N'10485760', N'Integer', N'Database', 0, N'system', SYSUTCDATETIME())
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Theme.Name')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Theme.Name', N'EnterpriseLight', N'Enum', N'Database', 1, N'system', SYSUTCDATETIME())
END
GO
