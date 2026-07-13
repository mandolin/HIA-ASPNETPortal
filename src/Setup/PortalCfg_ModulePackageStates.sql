/*
    P3.2 部署式模块包启用状态迁移脚本。
    P3.2 trusted-deployment module-package state migration script.

    说明：
    1. 状态只关联已部署包的稳定 PackageId，不替换或改写旧 ModuleDefinitions 表。
    2. 没有状态行的已验证包按启用处理，保持部署后的最小可用性。
    3. 本脚本不会由应用启动流程自动执行，必须由开发或部署人员显式执行。
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_ModulePackageStates]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[PortalCfg_ModulePackageStates]
    (
        [PackageId] NVARCHAR(100) NOT NULL,
        [IsEnabled] BIT NOT NULL
            CONSTRAINT [DF_PortalCfg_ModulePackageStates_IsEnabled] DEFAULT ((1)),
        [Note] NVARCHAR(500) NULL,
        [UpdatedBy] NVARCHAR(100) NOT NULL,
        [UpdatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalCfg_ModulePackageStates_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
        [RowVersion] ROWVERSION NOT NULL,

        CONSTRAINT [PK_PortalCfg_ModulePackageStates]
            PRIMARY KEY CLUSTERED ([PackageId])
    )
END
GO
