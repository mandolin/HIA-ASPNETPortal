/*
<lang>
  <zh-CN>P6.3 组织单元基础表迁移。本脚本可重复执行且不会由应用启动流程自动执行；第一版组织模型只表达单父级树，矩阵组织、兼职、多岗位和历史归属留给后续扩展。</zh-CN>
  <en>P6.3 organization-unit foundation migration. This script is idempotent and is not executed automatically by application startup; the first organization model only represents a single-parent tree, with matrix organization, part-time assignment, multiple positions, and history left for later expansion.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，确保组织树约束和过滤索引在目标 SQL Server 上按预期判断空父级与空编码。</zh-CN>
--   <en>Enable standard NULL comparison semantics so organization-tree constraints and filtered indexes evaluate empty parent ids and empty codes as expected on the target SQL Server.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护后续组织表、索引和约束名称在迁移工具与手工执行时一致解析。</zh-CN>
--   <en>Enable quoted identifiers so later organization table, index, and constraint names parse consistently in both migration tooling and manual execution.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>建表保护让脚本可重复执行，并保留已经维护过的组织编码、父子关系和排序值。</zh-CN>
--   <en>The create-table guard keeps the script repeatable while preserving already maintained organization codes, parent-child relationships, and sort values.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_OrganizationUnits]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>组织单元表保存低敏目录数据；它是员工主数据的上游引用，不承载岗位、兼职或历史归属事实。</zh-CN>
    --   <en>The organization-unit table stores low-sensitivity catalog data; it is an upstream reference for employee master data and does not carry position, part-time, or historical assignment facts.</en>
    -- </lang>
    CREATE TABLE [dbo].[PortalBiz_OrganizationUnits]
    (
        [OrganizationUnitId] INT IDENTITY(1,1) NOT NULL,
        [ParentOrganizationUnitId] INT NULL,
        [OrganizationCode] NVARCHAR(100) NULL,
        [DisplayName] NVARCHAR(150) NOT NULL,
        [SortOrder] INT NOT NULL
            CONSTRAINT [DF_PortalBiz_OrganizationUnits_SortOrder] DEFAULT (0),
        [IsActive] BIT NOT NULL
            CONSTRAINT [DF_PortalBiz_OrganizationUnits_IsActive] DEFAULT (1),
        [CreatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_OrganizationUnits_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(100) NULL,
        [UpdatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_OrganizationUnits_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        -- <lang>
        --   <zh-CN>主键为内部技术标识；外部或人工可读引用使用可空但唯一的 OrganizationCode。</zh-CN>
        --   <en>The primary key is an internal technical identifier; external or human-readable references use the optional but unique OrganizationCode.</en>
        -- </lang>
        CONSTRAINT [PK_PortalBiz_OrganizationUnits]
            PRIMARY KEY CLUSTERED ([OrganizationUnitId]),
        -- <lang>
        --   <zh-CN>单父级外键表达当前阶段的树结构，并刻意不支持矩阵组织或多父级路径。</zh-CN>
        --   <en>The single-parent foreign key represents the current tree structure and intentionally does not support matrix organization or multiple parent paths.</en>
        -- </lang>
        CONSTRAINT [FK_PortalBiz_OrganizationUnits_Parent]
            FOREIGN KEY ([ParentOrganizationUnitId]) REFERENCES [dbo].[PortalBiz_OrganizationUnits] ([OrganizationUnitId]),
        -- <lang>
        --   <zh-CN>文本约束在数据库边界拒绝空白名称、空白编码和自引用父级，避免后续目录查询出现不可解释节点。</zh-CN>
        --   <en>Text constraints reject blank names, blank codes, and self-parenting at the database boundary to avoid inexplicable nodes in later catalog queries.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_OrganizationUnits_DisplayName]
            CHECK ([DisplayName] = LTRIM(RTRIM([DisplayName])) AND NULLIF([DisplayName], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_OrganizationUnits_OrganizationCode]
            CHECK ([OrganizationCode] IS NULL OR ([OrganizationCode] = LTRIM(RTRIM([OrganizationCode])) AND NULLIF([OrganizationCode], N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_OrganizationUnits_NotSelfParent]
            CHECK ([ParentOrganizationUnitId] IS NULL OR [ParentOrganizationUnitId] <> [OrganizationUnitId])
    )
END
GO

-- <lang>
--   <zh-CN>组织编码过滤唯一索引允许未编码的临时节点存在，但一旦提供编码就必须在全局目录内唯一。</zh-CN>
--   <en>The filtered unique organization-code index allows temporary uncoded nodes, but any supplied code must be unique in the global catalog.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'UX_PortalBiz_OrganizationUnits_OrganizationCode' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_OrganizationUnits]'))
BEGIN
    CREATE UNIQUE INDEX [UX_PortalBiz_OrganizationUnits_OrganizationCode]
    ON [dbo].[PortalBiz_OrganizationUnits] ([OrganizationCode])
    WHERE [OrganizationCode] IS NOT NULL AND [OrganizationCode] <> N''
END
GO

-- <lang>
--   <zh-CN>父级排序索引支撑组织树展开和下级列表展示，排序键保持低敏且稳定。</zh-CN>
--   <en>The parent-sort index supports organization tree expansion and child-list rendering, with low-sensitivity and stable ordering keys.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_OrganizationUnits_ParentSort' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_OrganizationUnits]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_OrganizationUnits_ParentSort]
    ON [dbo].[PortalBiz_OrganizationUnits] ([ParentOrganizationUnitId], [SortOrder], [DisplayName])
END
GO
