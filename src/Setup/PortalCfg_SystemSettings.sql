/*
<lang>
  <zh-CN>P2.1 系统设置运行级表迁移脚本。本脚本只创建非敏感运行级设置表和审计表，不会被应用启动流程自动执行，需要由开发/部署人员显式执行；连接串、密码、Token、证书、密钥等敏感值不得写入这些表。</zh-CN>
  <en>P2.1 runtime system settings migration script. This script creates only non-sensitive runtime setting and audit tables, is not executed by application startup, and must be run explicitly by development/deployment staff; connection strings, passwords, tokens, certificates, keys, and other sensitive values must not be written to these tables.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，确保设置值、审计旧值/新值和可选客户端字段的空值语义一致。</zh-CN>
--   <en>Enable standard NULL comparison semantics so setting values, audit old/new values, and optional client fields share consistent null semantics.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护运行设置表、审计表、约束和索引名称在迁移执行中一致解析。</zh-CN>
--   <en>Enable quoted identifiers so runtime setting table, audit table, constraint, and index names parse consistently during migration execution.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>建表保护保留已部署环境中的运行级覆盖值，避免补跑迁移时覆盖管理员选择。</zh-CN>
--   <en>The create-table guard preserves runtime override values in deployed environments and avoids overwriting administrator choices during reruns.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_SystemSettings]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>当前值表保存非敏感运行级设置的数据库覆盖值；敏感配置仍必须来自受控配置源或外置 secret 管理。</zh-CN>
    --   <en>The current-value table stores database overrides for non-sensitive runtime settings; sensitive configuration must still come from governed configuration sources or external secret management.</en>
    -- </lang>
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

        -- <lang>
        --   <zh-CN>值类型白名单让运行时解析器按已知类型转换，避免自由文本被误当敏感或不可验证配置。</zh-CN>
        --   <en>The value-type whitelist lets the runtime parser convert known types and prevents free text from being mistaken for sensitive or unverifiable configuration.</en>
        -- </lang>
        CONSTRAINT [CK_PortalCfg_SystemSettings_ValueType]
            CHECK ([ValueType] IN (N'Boolean', N'Integer', N'String', N'Enum', N'Path', N'Duration')),

        -- <lang>
        --   <zh-CN>来源级别固定为 Database，明确本表只表达数据库覆盖层，不取代 appSettings、环境变量或外置凭据来源。</zh-CN>
        --   <en>The source level is fixed to Database, making clear this table represents only the database override layer and does not replace appSettings, environment variables, or external credential sources.</en>
        -- </lang>
        CONSTRAINT [CK_PortalCfg_SystemSettings_SourceLevel]
            CHECK ([SourceLevel] = N'Database')
    )
END
GO

-- <lang>
--   <zh-CN>审计表独立建表保护保留设置变更历史；补跑迁移不会清空已记录的在线修改轨迹。</zh-CN>
--   <en>The separate audit-table guard preserves setting-change history; rerunning the migration does not clear already recorded online modification traces.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_SystemSettingAudits]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>审计表记录非敏感运行级设置的在线修改历史，可包含低敏客户端上下文但不得包含 secret 原文。</zh-CN>
    --   <en>The audit table records online change history for non-sensitive runtime settings and may include low-sensitivity client context, but must not contain secret plaintext.</en>
    -- </lang>
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

        -- <lang>
        --   <zh-CN>变更类型白名单限制审计事件为插入、更新或删除，便于运行期报告按固定语义聚合。</zh-CN>
        --   <en>The change-type whitelist limits audit events to insert, update, or delete so runtime reports can aggregate them with fixed semantics.</en>
        -- </lang>
        CONSTRAINT [CK_PortalCfg_SystemSettingAudits_ChangeType]
            CHECK ([ChangeType] IN (N'Insert', N'Update', N'Delete'))
    )
END
GO

-- <lang>
--   <zh-CN>设置键/时间索引支撑按某个设置查看最近修改历史，倒序时间用于管理员审计页优先显示最新事件。</zh-CN>
--   <en>The setting-key/time index supports viewing recent modification history for a setting, with descending time allowing administrator audit pages to show newest events first.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalCfg_SystemSettingAudits_SettingKey_ChangedUtc' AND object_id = OBJECT_ID(N'[dbo].[PortalCfg_SystemSettingAudits]'))
BEGIN
    CREATE INDEX [IX_PortalCfg_SystemSettingAudits_SettingKey_ChangedUtc]
    ON [dbo].[PortalCfg_SystemSettingAudits] ([SettingKey], [ChangedUtc] DESC)
END
GO

-- <lang>
--   <zh-CN>第一批运行级设置 seed 只写入非敏感、可在线管理的安全/注册/文档/主题开关；每项都用 `NOT EXISTS` 保护管理员已有值。</zh-CN>
--   <en>The initial runtime setting seed writes only non-sensitive, online-manageable security, registration, document, and theme switches; each item uses `NOT EXISTS` to protect administrator-maintained values.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Security.AllowSelfRegistration')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Security.AllowSelfRegistration', N'false', N'Boolean', N'Database', 0, N'system', SYSUTCDATETIME())
END
GO

-- <lang>
--   <zh-CN>注册审批默认开启，保护自助注册不会绕过管理员或企业身份绑定流程。</zh-CN>
--   <en>Registration approval defaults to enabled so self-registration cannot bypass administrator review or enterprise-identity binding flows.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Security.RequireRegistrationApproval')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Security.RequireRegistrationApproval', N'true', N'Boolean', N'Database', 0, N'system', SYSUTCDATETIME())
END
GO

-- <lang>
--   <zh-CN>登录密码加密要求默认开启，表达旧 MD5 样本外的新凭据写入必须走加密存储。</zh-CN>
--   <en>The encrypted-login-password requirement defaults to enabled, expressing that new credential writes outside legacy MD5 samples must use encrypted storage.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Security.RequireEncryptedLoginPassword')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Security.RequireEncryptedLoginPassword', N'true', N'Boolean', N'Database', 0, N'system', SYSUTCDATETIME())
END
GO

-- <lang>
--   <zh-CN>密码最小长度 seed 是低敏策略数值，运行时仍会结合硬下限和复杂度类别共同校验。</zh-CN>
--   <en>The password minimum-length seed is a low-sensitivity policy value, and runtime validation still combines it with hard lower bounds and complexity categories.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Security.Password.MinimumLength')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Security.Password.MinimumLength', N'8', N'Integer', N'Database', 0, N'system', SYSUTCDATETIME())
END
GO

-- <lang>
--   <zh-CN>必需字符类别数 seed 只保存策略阈值，不保存任何用户密码样本或失败尝试详情。</zh-CN>
--   <en>The required-category-count seed stores only a policy threshold and no user password samples or failed-attempt details.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Security.Password.RequiredCategoryCount')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Security.Password.RequiredCategoryCount', N'3', N'Integer', N'Database', 0, N'system', SYSUTCDATETIME())
END
GO

-- <lang>
--   <zh-CN>弱口令字典开关默认开启，允许运行期策略在不暴露字典内容的情况下拒绝明显弱密码。</zh-CN>
--   <en>The weak-dictionary switch defaults to enabled, allowing runtime policy to reject obviously weak passwords without exposing dictionary contents here.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Security.Password.WeakDictionaryEnabled')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Security.Password.WeakDictionaryEnabled', N'true', N'Boolean', N'Database', 0, N'system', SYSUTCDATETIME())
END
GO

-- <lang>
--   <zh-CN>账号上下文词拒绝开关默认开启，防止密码直接包含登录名、姓名或员工号等低敏身份片段。</zh-CN>
--   <en>The context-term rejection switch defaults to enabled, preventing passwords from directly containing low-sensitivity identity fragments such as login name, display name, or employee code.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Security.Password.DisallowContextTerms')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Security.Password.DisallowContextTerms', N'true', N'Boolean', N'Database', 0, N'system', SYSUTCDATETIME())
END
GO

-- <lang>
--   <zh-CN>邀请默认有效期 seed 是低敏天数策略，实际到期时间仍由服务层按 UTC 时间计算。</zh-CN>
--   <en>The invitation default-expiry seed is a low-sensitivity day-count policy, while actual expiration timestamps are still calculated by the service layer in UTC.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Registration.InviteDefaultExpiryDays')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Registration.InviteDefaultExpiryDays', N'7', N'Integer', N'Database', 0, N'system', SYSUTCDATETIME())
END
GO

-- <lang>
--   <zh-CN>待员工绑定注册默认关闭，避免账号先进入可用状态后再补企业身份导致授权边界模糊。</zh-CN>
--   <en>Pending employee-binding registration defaults to disabled, avoiding accounts becoming usable before enterprise identity is attached and blurring authorization boundaries.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Registration.AllowPendingEmployeeBinding')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Registration.AllowPendingEmployeeBinding', N'false', N'Boolean', N'Database', 0, N'system', SYSUTCDATETIME())
END
GO

-- <lang>
--   <zh-CN>文档上传上限 seed 是非敏感整数策略，实际上传路径、扩展名和内容检查仍由文档策略代码执行。</zh-CN>
--   <en>The document upload-size seed is a non-sensitive integer policy, while upload path, extension, and content checks remain enforced by document policy code.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Documents.MaxUploadBytes')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Documents.MaxUploadBytes', N'10485760', N'Integer', N'Database', 0, N'system', SYSUTCDATETIME())
END
GO

-- <lang>
--   <zh-CN>主题名 seed 给新环境提供默认 UI 主题；该值不是路径，也不授权读取任意主题目录。</zh-CN>
--   <en>The theme-name seed provides a default UI theme for new environments; the value is not a path and does not authorize reading arbitrary theme directories.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortalCfg_SystemSettings] WHERE [SettingKey] = N'Portal.Theme.Name')
BEGIN
    INSERT INTO [dbo].[PortalCfg_SystemSettings]
        ([SettingKey], [SettingValue], [ValueType], [SourceLevel], [CanDelete], [UpdatedBy], [UpdatedUtc])
    VALUES
        (N'Portal.Theme.Name', N'EnterpriseLight', N'Enum', N'Database', 1, N'system', SYSUTCDATETIME())
END
GO
