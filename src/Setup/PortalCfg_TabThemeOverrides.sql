/*
    P3.1 Tab 主题覆盖表迁移脚本。
    P3.1 tab-theme override migration script.

    说明：
    1. 仅保存已部署主题包的名称，不保存 CSS、JavaScript、ZIP、外部 URL 或任意资源内容。
    2. 不建立外键，避免历史部署中缺少扩展表时阻断旧门户配置；运行时代码仍会校验 Tab 和主题包。
    3. 所有时间使用 UTC datetime2(0)，以 SQL Server 2016+ 为基准。
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_TabThemeOverrides]') AND type IN (N'U'))
BEGIN
    -- Tab 覆盖表：一条记录代表一个门户 Tab 对已部署主题的可选覆盖。
    -- Tab override table: one optional deployed-theme override per portal tab.
    CREATE TABLE [dbo].[PortalCfg_TabThemeOverrides]
    (
        [TabId] INT NOT NULL,
        [ThemeName] NVARCHAR(64) NOT NULL,
        [UpdatedBy] NVARCHAR(100) NULL,
        [UpdatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalCfg_TabThemeOverrides_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
        [RowVersion] ROWVERSION NOT NULL,

        CONSTRAINT [PK_PortalCfg_TabThemeOverrides]
            PRIMARY KEY CLUSTERED ([TabId])
    )
END
GO
