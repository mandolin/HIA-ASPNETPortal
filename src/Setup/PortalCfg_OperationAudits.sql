/*
<lang>
  <zh-CN>P2.4 运营审计表迁移。此脚本可重复执行，应用程序不会在启动时自动执行它；审计内容只保存低敏摘要、对象标识和关联事件编号，不应写入密码、连接串、Token 或请求正文。</zh-CN>
  <en>P2.4 operations-audit table migration. This script is idempotent and the application never runs it automatically at startup; audit content stores only low-sensitivity summaries, target identifiers, and related event ids, and must not contain passwords, connection strings, tokens, or request bodies.</en>
</lang>
*/

-- <lang>
--   <zh-CN>建表保护确保重复执行时不会删除或重建既有审计记录。</zh-CN>
--   <en>The create-table guard ensures repeated execution never deletes or rebuilds existing audit records.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_OperationAudits]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>运营审计表记录后台敏感操作的低敏投影，按时间、类别、动作和目标对象支持后续查询。</zh-CN>
    --   <en>The operations-audit table records low-sensitivity projections of sensitive admin operations and supports later queries by time, category, action, and target object.</en>
    -- </lang>
    CREATE TABLE [dbo].[PortalCfg_OperationAudits]
    (
        -- <lang>
        --   <zh-CN>自增 AuditId 提供稳定排序补充键，避免相同 UTC 秒内多条记录排序不确定。</zh-CN>
        --   <en>The identity AuditId supplies a stable tie-breaker so multiple records in the same UTC second do not sort ambiguously.</en>
        -- </lang>
        [AuditId] BIGINT IDENTITY(1, 1) NOT NULL,
        [OccurredUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalCfg_OperationAudits_OccurredUtc] DEFAULT (SYSUTCDATETIME()),
        [Category] NVARCHAR(80) NOT NULL,
        [Action] NVARCHAR(80) NOT NULL,
        [Outcome] NVARCHAR(20) NOT NULL
            CONSTRAINT [DF_PortalCfg_OperationAudits_Outcome] DEFAULT (N'Success'),
        -- <lang>
        --   <zh-CN>Actor、Target 与 Summary 均为展示/过滤字段，调用方必须在写入前完成净化和长度裁剪。</zh-CN>
        --   <en>Actor, Target, and Summary are display and filter fields, so callers must sanitize and truncate them before writing.</en>
        -- </lang>
        [ActorUserName] NVARCHAR(100) NOT NULL,
        [TargetType] NVARCHAR(80) NOT NULL,
        [TargetId] NVARCHAR(200) NOT NULL,
        [Summary] NVARCHAR(500) NOT NULL,
        [RelatedEventId] NVARCHAR(64) NULL,
        [ClientIp] NVARCHAR(64) NULL,
        [UserAgent] NVARCHAR(400) NULL,
        [CorrelationId] NVARCHAR(64) NULL,

        CONSTRAINT [PK_PortalCfg_OperationAudits]
            PRIMARY KEY CLUSTERED ([AuditId]),

        -- <lang>
        --   <zh-CN>Outcome 白名单限制审计状态枚举，防止自由文本破坏后台筛选语义。</zh-CN>
        --   <en>The Outcome whitelist constrains audit status values so free text cannot break admin filtering semantics.</en>
        -- </lang>
        CONSTRAINT [CK_PortalCfg_OperationAudits_Outcome]
            CHECK ([Outcome] IN (N'Success', N'Failure', N'Skipped'))
    )
END
GO

-- <lang>
--   <zh-CN>时间倒序索引服务最近操作列表和详情翻页，并以 AuditId 作为相同时间点的稳定次序。</zh-CN>
--   <en>The descending time index serves recent-operation lists and detail paging, using AuditId as the stable order within identical timestamps.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalCfg_OperationAudits_OccurredUtc' AND object_id = OBJECT_ID(N'[dbo].[PortalCfg_OperationAudits]'))
BEGIN
    CREATE INDEX [IX_PortalCfg_OperationAudits_OccurredUtc]
    ON [dbo].[PortalCfg_OperationAudits] ([OccurredUtc] DESC, [AuditId] DESC)
END
GO

-- <lang>
--   <zh-CN>类别/动作索引用于后台按操作类型定位审计记录，时间列仍保持倒序以便优先展示最新证据。</zh-CN>
--   <en>The category/action index lets admin screens locate records by operation type while the time column remains descending so the newest evidence appears first.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalCfg_OperationAudits_CategoryActionUtc' AND object_id = OBJECT_ID(N'[dbo].[PortalCfg_OperationAudits]'))
BEGIN
    CREATE INDEX [IX_PortalCfg_OperationAudits_CategoryActionUtc]
    ON [dbo].[PortalCfg_OperationAudits] ([Category], [Action], [OccurredUtc] DESC)
END
GO

-- <lang>
--   <zh-CN>目标对象索引用于追踪单个用户、模块或设置项的变更历史，不依赖全文搜索。</zh-CN>
--   <en>The target-object index tracks change history for one user, module, or setting without requiring full-text search.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalCfg_OperationAudits_TargetUtc' AND object_id = OBJECT_ID(N'[dbo].[PortalCfg_OperationAudits]'))
BEGIN
    CREATE INDEX [IX_PortalCfg_OperationAudits_TargetUtc]
    ON [dbo].[PortalCfg_OperationAudits] ([TargetType], [TargetId], [OccurredUtc] DESC)
END
GO
