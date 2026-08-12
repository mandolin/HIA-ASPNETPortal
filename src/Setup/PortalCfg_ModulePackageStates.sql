/*
<lang>
  <zh-CN>P3.2 部署式模块包启用状态迁移脚本。状态只关联已部署包的稳定 `PackageId`，不替换或改写旧 `ModuleDefinitions` 表；没有状态行的已验证包按启用处理，保持部署后的最小可用性。本脚本不会由应用启动流程自动执行，必须由开发或部署人员显式执行。</zh-CN>
  <en>P3.2 trusted-deployment module-package state migration script. State is associated only with the stable `PackageId` of deployed packages and does not replace or rewrite the legacy `ModuleDefinitions` table; verified packages without a state row are treated as enabled to preserve minimum availability after deployment. This script is not executed by application startup and must be run explicitly by development or deployment staff.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，保持包状态表中可选备注和非空主键的约束判断稳定。</zh-CN>
--   <en>Enable standard NULL comparison semantics so constraint checks for optional notes and non-null primary keys in the package-state table remain stable.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护模块包状态表、默认约束和主键名称在部署脚本中一致解析。</zh-CN>
--   <en>Enable quoted identifiers so module-package state table, default constraint, and primary-key names parse consistently in deployment scripts.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>建表保护使脚本可重复执行，并保留管理员已经维护过的包启停状态和备注。</zh-CN>
--   <en>The create-table guard keeps the script repeatable and preserves package enablement state and notes already maintained by administrators.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalCfg_ModulePackageStates]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>包状态表只存储部署包的运行开关和低敏维护备注，不承载模块定义清单或上传包内容。</zh-CN>
    --   <en>The package-state table stores only runtime enablement flags and low-sensitivity maintenance notes for deployed packages, not module-definition catalogs or uploaded package content.</en>
    -- </lang>
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

        -- <lang>
        --   <zh-CN>`PackageId` 是唯一业务键；默认启用策略让缺失状态行的已验证包保持兼容可用。</zh-CN>
        --   <en>`PackageId` is the unique business key; the default-enabled policy keeps verified packages without explicit state rows compatibly available.</en>
        -- </lang>
        CONSTRAINT [PK_PortalCfg_ModulePackageStates]
            PRIMARY KEY CLUSTERED ([PackageId])
    )
END
GO
