/*
<lang>
  <zh-CN>页签名与模块标题多语言化迁移脚本。为 `PortalCfg_Tabs` 增加英文名列、其它语言 JSON 列与导航稳定键列，为 `PortalCfg_Modules` 增加英文名列与其它语言 JSON 列；中文列复用既有的 `TabName` / `MobileTabName` / `ModuleTitle`，不新增中文列，以保证渲染点永远有值可回退。
  脚本只做加列与一次性回填，**不改写既有 `TabName` / `ModuleTitle` 的显示值** —— 显示值改为中文的步骤必须排在门控键与保护名判定切到 `NavigationKey` 之后，否则会先击穿"按名识别"的管理策略。
  其它语言 JSON 列的取值形态为 `{"ja-JP":"ホーム","fr-FR":"Accueil"}`：以文化名为键、该文化下的显示名为值；它由 CHECK 约束 `ISJSON` 保证是合法 JSON 文本，但**不解析、不校验键名是否为受支持的文化**，那属于应用层解析器的职责。
  `NavigationKey` 与显示名解耦：门控稳定键由它派生，因此显示名可以自由本地化而不改变 `Tab.*` 键。
  JSON 函数（`ISJSON`）要求数据库兼容级别 130 及以上，与本项目 SQL Server 2016+ 基线一致；原生 `json` 类型与 `CREATE JSON INDEX` 属于 SQL Server 2025，本脚本不使用。
  所有语句均幂等：列已存在则跳过，回填只补空值，不覆盖既有数据。
</lang>
  <en>Multilingual tab-name and module-title migration script. It adds an English-name column, an other-languages JSON column, and a navigation stable-key column to `PortalCfg_Tabs`, plus an English-name column and an other-languages JSON column to `PortalCfg_Modules`. The Chinese side reuses the existing `TabName` / `MobileTabName` / `ModuleTitle` columns instead of adding new ones so renderers always have a value to fall back to.
  The script only adds columns and performs one-time backfills; it does **not** rewrite existing `TabName` / `ModuleTitle` display values — switching display values to Chinese must be sequenced after the gate key and protected-name checks move to `NavigationKey`, otherwise the "identify by name" administration policies would be broken first.
  The other-languages JSON column takes the shape `{"ja-JP":"ホーム","fr-FR":"Accueil"}`: culture name as key, localized display name as value. A CHECK constraint using `ISJSON` guarantees it is valid JSON text, but the script neither parses it nor validates that keys are supported cultures — that belongs to the application-layer resolver.
  `NavigationKey` decouples the stable key from the display name: gate keys derive from it, so display names can be localized freely without changing `Tab.*` keys.
  JSON functions (`ISJSON`) require database compatibility level 130 or higher, consistent with this project's SQL Server 2016+ baseline; the native `json` type and `CREATE JSON INDEX` are SQL Server 2025 features and are not used here.
  Every statement is idempotent: existing columns are skipped and backfills only fill blanks, never overwriting existing data.
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

-- ---------------------------------------------------------------------------
-- PortalCfg_Tabs：英文列 / 其它语言 JSON 列 / 导航稳定键列
-- ---------------------------------------------------------------------------

-- <lang>
--   <zh-CN>英文名列：与既有 `TabName` 的排序规则保持一致，避免后续比较或联接出现排序规则冲突。</zh-CN>
--   <en>English-name column: it keeps the same collation as the existing `TabName` so later comparisons or joins cannot hit a collation conflict.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]') AND name = N'TabNameEn')
BEGIN
    ALTER TABLE [dbo].[PortalCfg_Tabs]
        ADD [TabNameEn] NVARCHAR(150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
END
GO

-- <lang>
--   <zh-CN>其它语言 JSON 列；用 NVARCHAR(MAX) 而非原生 json 类型，后者需要 SQL Server 2025。</zh-CN>
--   <en>Other-languages JSON column; it uses NVARCHAR(MAX) rather than the native json type, which requires SQL Server 2025.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]') AND name = N'TabNameLocalizedJson')
BEGIN
    ALTER TABLE [dbo].[PortalCfg_Tabs]
        ADD [TabNameLocalizedJson] NVARCHAR(MAX) NULL
END
GO

-- <lang>
--   <zh-CN>移动端名同样需要三列，否则移动端页头会退化成单语，与桌面端不一致。</zh-CN>
--   <en>The mobile name needs the same three-way treatment, otherwise the mobile header degrades to a single language and diverges from the desktop one.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]') AND name = N'MobileTabNameEn')
BEGIN
    ALTER TABLE [dbo].[PortalCfg_Tabs]
        ADD [MobileTabNameEn] NVARCHAR(150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]') AND name = N'MobileTabNameLocalizedJson')
BEGIN
    ALTER TABLE [dbo].[PortalCfg_Tabs]
        ADD [MobileTabNameLocalizedJson] NVARCHAR(MAX) NULL
END
GO

-- <lang>
--   <zh-CN>导航稳定键：门控键与显示名解耦的关键列。可空，运行时在它为空时回退 `TabName`，保证历史部署零回归。</zh-CN>
--   <en>Navigation stable key: the column that decouples the gate key from the display name. It is nullable and the runtime falls back to `TabName` when it is blank, keeping legacy deployments regression-free.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]') AND name = N'NavigationKey')
BEGIN
    ALTER TABLE [dbo].[PortalCfg_Tabs]
        ADD [NavigationKey] NVARCHAR(150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
END
GO

-- ---------------------------------------------------------------------------
-- PortalCfg_Modules：英文列 / 其它语言 JSON 列
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]') AND name = N'ModuleTitleEn')
BEGIN
    ALTER TABLE [dbo].[PortalCfg_Modules]
        ADD [ModuleTitleEn] NVARCHAR(150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]') AND name = N'ModuleTitleLocalizedJson')
BEGIN
    ALTER TABLE [dbo].[PortalCfg_Modules]
        ADD [ModuleTitleLocalizedJson] NVARCHAR(MAX) NULL
END
GO

-- ---------------------------------------------------------------------------
-- CHECK 约束：JSON 列只允许 NULL 或合法 JSON 文本
-- ---------------------------------------------------------------------------

-- <lang>
--   <zh-CN>只约束"是合法 JSON"，不约束键名或值长度：键名是否为受支持的文化由应用层解析器判定，
--   在数据库层做文化白名单会把"支持哪些语言"从代码决策变成迁移决策，反而更难演进。
--   空字符串被排除：空串应写 NULL，避免前端出现"有值但显示不出来"的空标签。</zh-CN>
--   <en>The constraint only enforces "valid JSON", never key names or value lengths: whether a key is a supported culture is decided by the application-layer resolver, and putting a culture allowlist in the database would move "which languages are supported" from a code decision to a migration decision, making it harder to evolve.
--   An empty string is excluded: blanks should be written as NULL so the UI never sees a value that exists but renders as nothing.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]') AND name = N'CK_PortalCfg_Tabs_TabNameLocalizedJson')
BEGIN
    ALTER TABLE [dbo].[PortalCfg_Tabs]
        ADD CONSTRAINT [CK_PortalCfg_Tabs_TabNameLocalizedJson]
            CHECK ([TabNameLocalizedJson] IS NULL OR ISJSON([TabNameLocalizedJson]) = 1)
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Tabs]') AND name = N'CK_PortalCfg_Tabs_MobileTabNameLocalizedJson')
BEGIN
    ALTER TABLE [dbo].[PortalCfg_Tabs]
        ADD CONSTRAINT [CK_PortalCfg_Tabs_MobileTabNameLocalizedJson]
            CHECK ([MobileTabNameLocalizedJson] IS NULL OR ISJSON([MobileTabNameLocalizedJson]) = 1)
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'[dbo].[PortalCfg_Modules]') AND name = N'CK_PortalCfg_Modules_ModuleTitleLocalizedJson')
BEGIN
    ALTER TABLE [dbo].[PortalCfg_Modules]
        ADD CONSTRAINT [CK_PortalCfg_Modules_ModuleTitleLocalizedJson]
            CHECK ([ModuleTitleLocalizedJson] IS NULL OR ISJSON([ModuleTitleLocalizedJson]) = 1)
END
GO

-- ---------------------------------------------------------------------------
-- 回填：导航稳定键
-- ---------------------------------------------------------------------------

-- <lang>
--   <zh-CN>把现有页签名固化为稳定键，使改造前后 `Tab.*` 门控键**逐字节一致** —— 这是零回归的关键。
--   只补空值：已人工指定过稳定键的页签不被覆盖。</zh-CN>
--   <en>Freeze the current tab names as stable keys so the `Tab.*` gate keys stay **byte-identical** before and after the change — the crux of a zero-regression migration. Only blanks are filled: tabs whose stable key was set manually are never overwritten.</en>
-- </lang>
UPDATE [dbo].[PortalCfg_Tabs]
SET [NavigationKey] = [TabName]
WHERE [NavigationKey] IS NULL
   OR LTRIM(RTRIM([NavigationKey])) = N''
GO

-- ---------------------------------------------------------------------------
-- 回填：已知种子页签的英文名
-- ---------------------------------------------------------------------------

-- <lang>
--   <zh-CN>只对**已知**的种子英文页签回填英文名，不用"看起来像英文"的启发式判断 ——
--   启发式会把 `P64-Test-EmployeeProfileConfirm` 一类混合标识误判成英文或中文，而这类误判只能靠人工发现。
--   显式清单取自安装脚本的种子数据与导航 registry 中已登记的 `Tab.*` 键。
--   注意：此处**不改** `TabName` 的显示值，显示值改为中文的步骤排在门控键解耦之后。</zh-CN>
--   <en>Backfill English names only for **known** seeded English tabs, never with a "looks like English" heuristic — a heuristic would misclassify mixed identifiers such as `P64-Test-EmployeeProfileConfirm`, and such mistakes can only be caught by a human. The explicit list comes from the installer seed data and from the `Tab.*` keys already registered in the navigation registry.
--   Note: the `TabName` display value is deliberately **not** changed here; switching display values to Chinese is sequenced after the gate-key decoupling.</en>
-- </lang>
UPDATE [dbo].[PortalCfg_Tabs]
SET [TabNameEn] = [TabName]
WHERE ([TabNameEn] IS NULL OR LTRIM(RTRIM([TabNameEn])) = N'')
  AND [TabName] IN (N'Home', N'Employee Info', N'Product Info', N'Discussions', N'About the Portal', N'Admin')
GO
