/*
    P6.3 组织单元基础表迁移。
    P6.3 organization-unit foundation migration.

    本脚本可重复执行；应用程序不会在启动时自动执行它。
    This script is idempotent; the application never runs it automatically at startup.

    第一版组织模型只表达单父级树。矩阵组织、兼职、多岗位和历史归属后续再扩展。
    The first organization model only represents a single-parent tree. Matrix organization, part-time
    assignments, multiple positions, and history will be extended later.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_OrganizationUnits]') AND type IN (N'U'))
BEGIN
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

        CONSTRAINT [PK_PortalBiz_OrganizationUnits]
            PRIMARY KEY CLUSTERED ([OrganizationUnitId]),
        CONSTRAINT [FK_PortalBiz_OrganizationUnits_Parent]
            FOREIGN KEY ([ParentOrganizationUnitId]) REFERENCES [dbo].[PortalBiz_OrganizationUnits] ([OrganizationUnitId]),
        CONSTRAINT [CK_PortalBiz_OrganizationUnits_DisplayName]
            CHECK ([DisplayName] = LTRIM(RTRIM([DisplayName])) AND NULLIF([DisplayName], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_OrganizationUnits_OrganizationCode]
            CHECK ([OrganizationCode] IS NULL OR ([OrganizationCode] = LTRIM(RTRIM([OrganizationCode])) AND NULLIF([OrganizationCode], N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_OrganizationUnits_NotSelfParent]
            CHECK ([ParentOrganizationUnitId] IS NULL OR [ParentOrganizationUnitId] <> [OrganizationUnitId])
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'UX_PortalBiz_OrganizationUnits_OrganizationCode' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_OrganizationUnits]'))
BEGIN
    CREATE UNIQUE INDEX [UX_PortalBiz_OrganizationUnits_OrganizationCode]
    ON [dbo].[PortalBiz_OrganizationUnits] ([OrganizationCode])
    WHERE [OrganizationCode] IS NOT NULL AND [OrganizationCode] <> N''
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_OrganizationUnits_ParentSort' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_OrganizationUnits]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_OrganizationUnits_ParentSort]
    ON [dbo].[PortalBiz_OrganizationUnits] ([ParentOrganizationUnitId], [SortOrder], [DisplayName])
END
GO
