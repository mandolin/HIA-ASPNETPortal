/*
<lang>
  <zh-CN>P3.1 Tab 主题覆盖表迁移脚本。仅保存已部署主题包的名称，不保存 CSS、JavaScript、ZIP、外部 URL 或任意资源内容；不建立外键，以免历史部署缺少扩展表时阻断旧门户配置，运行时代码仍会校验 Tab 和主题包；所有时间使用 UTC datetime2(0)，以 SQL Server 2016+ 为基准。</zh-CN>
  <en>P3.1 tab-theme override migration script. It stores only deployed theme-package names, never CSS, JavaScript, ZIP files, external URLs, or arbitrary resource content; it intentionally avoids foreign keys so legacy deployments without extension tables are not blocked, while runtime code still validates tabs and theme packages; all times use UTC datetime2(0) on the SQL Server 2016+ baseline.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，确保后续对象创建与 SQL Server 默认迁移约定一致。</zh-CN>
--   <en>Enable standard NULL comparison semantics so later object creation follows the SQL Server migration convention.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护包含约束名、表名和列名的 DDL 在部署环境中按预期解析。</zh-CN>
--   <en>Enable quoted identifiers so DDL containing constraint, table, and column names parses as expected in deployment environments.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>迁移脚本采用幂等建表保护；目标表已存在时不重建、不改列也不覆盖既有覆盖值。</zh-CN>
--   <en>The migration uses an idempotent create-table guard; when the target table already exists, it is not rebuilt, columns are not changed, and existing overrides are not overwritten.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_TabThemeOverrides]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>Tab 覆盖表：一条记录代表一个门户 Tab 对已部署主题的可选覆盖，ThemeName 仍需由应用层校验为可信部署包。</zh-CN>
    --   <en>Tab override table: each row represents one optional deployed-theme override for a portal tab, and ThemeName still requires application-layer validation as a trusted deployed package.</en>
    -- </lang>
    CREATE TABLE [dbo].[PortalCfg_TabThemeOverrides]
    (
        [TabId] INT NOT NULL,
        [ThemeName] NVARCHAR(64) NOT NULL,
        [UpdatedBy] NVARCHAR(100) NULL,
        -- <lang>
        --   <zh-CN>更新时间默认采用 UTC，避免多时区管理员操作在审计和回退诊断中出现本地时间歧义。</zh-CN>
        --   <en>The update time defaults to UTC to avoid local-time ambiguity in audit and fallback diagnostics across administrator time zones.</en>
        -- </lang>
        [UpdatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalCfg_TabThemeOverrides_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
        -- <lang>
        --   <zh-CN>RowVersion 仅用于并发观察和后续管理 UI 的冲突检测，不承载业务时间语义。</zh-CN>
        --   <en>RowVersion is only for concurrency observation and future admin-UI conflict detection; it carries no business-time meaning.</en>
        -- </lang>
        [RowVersion] ROWVERSION NOT NULL,

        -- <lang>
        --   <zh-CN>TabId 作为聚集主键，保证一个 Tab 同时最多只有一个主题覆盖值。</zh-CN>
        --   <en>TabId is the clustered primary key so one tab can have at most one theme override at a time.</en>
        -- </lang>
        CONSTRAINT [PK_PortalCfg_TabThemeOverrides]
            PRIMARY KEY CLUSTERED ([TabId])
    )
END
GO
