/*
    P23.2 受治理业务参考数据目录迁移。
    P23.2 governed business reference-data catalog migration.

    本脚本可重复执行；应用程序不会在启动时自动执行它。
    This script is idempotent; the application never runs it automatically at startup.

    目录只保存低敏稳定键、显示名、说明、排序、启停和维护元数据。
    事实记录保存 ValueKey；已被使用的键只能停用，不能删除、改名或复用。
    The catalog stores only low-sensitivity stable keys, display names, descriptions,
    ordering, activation, and maintenance metadata. Fact records store ValueKey;
    used keys may be deactivated but must not be deleted, renamed, or reused.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_ReferenceData]') AND type IN (N'U'))
BEGIN
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

        CONSTRAINT [PK_PortalBiz_ReferenceData]
            PRIMARY KEY CLUSTERED ([ReferenceDataId]),
        CONSTRAINT [UX_PortalBiz_ReferenceData_SetValue]
            UNIQUE ([ReferenceSetKey], [ValueKey]),
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

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_ReferenceData_SetActiveOrder' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_ReferenceData]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_ReferenceData_SetActiveOrder]
    ON [dbo].[PortalBiz_ReferenceData] ([ReferenceSetKey], [IsActive], [SortOrder], [ValueKey])
END
GO

DECLARE @Seed TABLE
(
    [ReferenceSetKey] NVARCHAR(80) NOT NULL,
    [ValueKey] NVARCHAR(80) NOT NULL,
    [DisplayName] NVARCHAR(120) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [SortOrder] INT NOT NULL
);

INSERT INTO @Seed ([ReferenceSetKey], [ValueKey], [DisplayName], [Description], [SortOrder])
VALUES
    (N'CollaborationItemType', N'General', N'通用协同', N'适用于未分类的低敏协同事项。', 10),
    (N'CollaborationItemType', N'Content', N'资料/内容协同', N'适用于资料和内容类低敏协同事项。', 20),
    (N'CollaborationItemType', N'Operations', N'资源/运维协同', N'适用于资源和运维类低敏协同事项。', 30),
    (N'CollaborationItemType', N'Workflow', N'业务流程协同', N'适用于业务流程类低敏协同事项。', 40),
    (N'CollaborationPriority', N'Normal', N'普通', N'默认处理优先级。', 10),
    (N'CollaborationPriority', N'Important', N'重要', N'需要优先处理的低敏事项。', 20);

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
