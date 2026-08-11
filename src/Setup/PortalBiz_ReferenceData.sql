/*
<lang>
  <zh-CN>P23.2 受治理业务参考数据目录迁移。本脚本可重复执行且不会由应用启动流程自动执行；目录只保存低敏稳定键、显示名、说明、排序、启停和维护元数据，事实记录保存 `ValueKey`，已被使用的键只能停用，不能删除、改名或复用。</zh-CN>
  <en>P23.2 governed business reference-data catalog migration. This script is idempotent and is not executed automatically by application startup; the catalog stores only low-sensitivity stable keys, display names, descriptions, ordering, activation, and maintenance metadata, fact records store `ValueKey`, and used keys may be deactivated but must not be deleted, renamed, or reused.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，保证参考集键、显示名和说明字段的空值/空白约束稳定执行。</zh-CN>
--   <en>Enable standard NULL comparison semantics so null and blank constraints for reference-set keys, display names, and descriptions execute consistently.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护参考数据表、唯一约束和查询索引名称在各执行入口中一致解析。</zh-CN>
--   <en>Enable quoted identifiers so reference-data table, unique constraint, and query-index names parse consistently across execution entry points.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>建表保护保留既有参考数据和已停用键，避免补跑迁移时破坏事实表保存的 `ValueKey` 引用。</zh-CN>
--   <en>The create-table guard preserves existing reference data and deactivated keys, avoiding damage to `ValueKey` references stored by fact tables during reruns.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_ReferenceData]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>参考数据目录将业务枚举提升为可治理数据；它只保存低敏展示与维护元数据，不保存事实记录。</zh-CN>
    --   <en>The reference-data catalog promotes business enumerations into governed data; it stores only low-sensitivity display and maintenance metadata, not fact records.</en>
    -- </lang>
    CREATE TABLE [dbo].[PortalBiz_ReferenceData]
    (
        [ReferenceDataId] BIGINT IDENTITY(1,1) NOT NULL,
        [ReferenceSetKey] NVARCHAR(80) NOT NULL,
        [ValueKey] NVARCHAR(80) NOT NULL,
        [DisplayName] NVARCHAR(120) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [SortOrder] INT NOT NULL
            CONSTRAINT [DF_PortalBiz_ReferenceData_SortOrder] DEFAULT ((0)),
        [IsActive] BIT NOT NULL
            CONSTRAINT [DF_PortalBiz_ReferenceData_IsActive] DEFAULT ((1)),
        [IsSystemSeed] BIT NOT NULL
            CONSTRAINT [DF_PortalBiz_ReferenceData_IsSystemSeed] DEFAULT ((0)),
        [CreatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_ReferenceData_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(100) NOT NULL,
        [UpdatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_ReferenceData_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedBy] NVARCHAR(100) NOT NULL,
        [RowVersion] ROWVERSION NOT NULL,

        -- <lang>
        --   <zh-CN>主键为内部技术标识，业务稳定性由 ReferenceSetKey 与 ValueKey 的组合唯一约束承担。</zh-CN>
        --   <en>The primary key is an internal technical id, while business stability is carried by the unique pair of ReferenceSetKey and ValueKey.</en>
        -- </lang>
        CONSTRAINT [PK_PortalBiz_ReferenceData]
            PRIMARY KEY CLUSTERED ([ReferenceDataId]),
        CONSTRAINT [UX_PortalBiz_ReferenceData_SetValue]
            UNIQUE ([ReferenceSetKey], [ValueKey]),
        -- <lang>
        --   <zh-CN>文本约束阻止空白键和空白显示名进入目录；维护人字段同样必须是低敏且可追溯的非空文本。</zh-CN>
        --   <en>Text constraints prevent blank keys and blank display names from entering the catalog; maintainer fields must also be low-sensitivity, traceable, non-empty text.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_ReferenceData_SetKey]
            CHECK ([ReferenceSetKey] = LTRIM(RTRIM([ReferenceSetKey])) AND NULLIF([ReferenceSetKey], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_ReferenceData_ValueKey]
            CHECK ([ValueKey] = LTRIM(RTRIM([ValueKey])) AND NULLIF([ValueKey], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_ReferenceData_DisplayName]
            CHECK ([DisplayName] = LTRIM(RTRIM([DisplayName])) AND NULLIF([DisplayName], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_ReferenceData_Description]
            CHECK ([Description] IS NULL OR ([Description] = LTRIM(RTRIM([Description])) AND NULLIF([Description], N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_ReferenceData_CreatedBy]
            CHECK ([CreatedBy] = LTRIM(RTRIM([CreatedBy])) AND NULLIF([CreatedBy], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_ReferenceData_UpdatedBy]
            CHECK ([UpdatedBy] = LTRIM(RTRIM([UpdatedBy])) AND NULLIF([UpdatedBy], N'') IS NOT NULL)
    )
END
GO

-- <lang>
--   <zh-CN>参考集启用/排序索引支撑只读目录查询按集合、启停和 SortOrder 返回稳定显示顺序。</zh-CN>
--   <en>The set/active/order index supports read-only catalog queries returning stable display order by set, activation state, and SortOrder.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_ReferenceData_SetActiveOrder' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_ReferenceData]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_ReferenceData_SetActiveOrder]
    ON [dbo].[PortalBiz_ReferenceData] ([ReferenceSetKey], [IsActive], [SortOrder], [ValueKey])
END
GO

-- <lang>
--   <zh-CN>表变量保存本脚本负责的最小系统 seed；其生命周期只存在于当前批次，不读取外部文件或私有配置。</zh-CN>
--   <en>The table variable holds the minimal system seed owned by this script; its lifetime is only the current batch and it reads no external files or private configuration.</en>
-- </lang>
DECLARE @Seed TABLE
(
    [ReferenceSetKey] NVARCHAR(80) NOT NULL,
    [ValueKey] NVARCHAR(80) NOT NULL,
    [DisplayName] NVARCHAR(120) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [SortOrder] INT NOT NULL
);

-- <lang>
--   <zh-CN>系统 seed 仅覆盖协作类型和协作优先级的低敏初始选项，排序值预留后续扩展间隙。</zh-CN>
--   <en>The system seed covers only low-sensitivity initial options for collaboration type and collaboration priority, with sort values leaving gaps for later expansion.</en>
-- </lang>
INSERT INTO @Seed ([ReferenceSetKey], [ValueKey], [DisplayName], [Description], [SortOrder])
VALUES
    (N'CollaborationItemType', N'General', N'通用协同', N'适用于未分类的低敏协同事项。', 10),
    (N'CollaborationItemType', N'Content', N'资料/内容协同', N'适用于资料和内容类低敏协同事项。', 20),
    (N'CollaborationItemType', N'Operations', N'资源/运维协同', N'适用于资源和运维类低敏协同事项。', 30),
    (N'CollaborationItemType', N'Workflow', N'业务流程协同', N'适用于业务流程类低敏协同事项。', 40),
    (N'CollaborationPriority', N'Normal', N'普通', N'默认处理优先级。', 10),
    (N'CollaborationPriority', N'Important', N'重要', N'需要优先处理的低敏事项。', 20);

-- <lang>
--   <zh-CN>实际插入使用 `NOT EXISTS` 保护已维护目录项；系统 seed 标记帮助后续治理区分初始项和管理员维护项。</zh-CN>
--   <en>The real insert uses `NOT EXISTS` to protect already maintained catalog entries; the system-seed marker helps later governance distinguish initial entries from administrator-maintained ones.</en>
-- </lang>
INSERT INTO [dbo].[PortalBiz_ReferenceData]
    ([ReferenceSetKey], [ValueKey], [DisplayName], [Description], [SortOrder], [IsActive], [IsSystemSeed], [CreatedUtc], [CreatedBy], [UpdatedUtc], [UpdatedBy])
SELECT
    [Seed].[ReferenceSetKey],
    [Seed].[ValueKey],
    [Seed].[DisplayName],
    [Seed].[Description],
    [Seed].[SortOrder],
    1,
    1,
    SYSUTCDATETIME(),
    N'P23.2 Seed',
    SYSUTCDATETIME(),
    N'P23.2 Seed'
FROM @Seed AS [Seed]
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[PortalBiz_ReferenceData] AS [Existing]
    WHERE [Existing].[ReferenceSetKey] = [Seed].[ReferenceSetKey]
      AND [Existing].[ValueKey] = [Seed].[ValueKey]
);
GO
