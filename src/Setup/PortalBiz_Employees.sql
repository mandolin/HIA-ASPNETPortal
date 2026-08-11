/*
<lang>
  <zh-CN>P6.3 员工基础表迁移。本脚本可重复执行且不会由应用启动流程自动执行；第一版员工表只保存门户业务所需的最小主数据，手机号、身份证号等高敏个人信息暂不入库。</zh-CN>
  <en>P6.3 employee foundation migration. This script is idempotent and is not executed automatically by application startup; the first employee table stores only minimal master data required by Portal business flows, while highly sensitive personal data such as mobile phone numbers and government identifiers is intentionally not stored.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，保证员工组织、邮箱和离职时间约束的空值判断稳定。</zh-CN>
--   <en>Enable standard NULL comparison semantics so null handling for employee organization, email, and leaving-date constraints remains stable.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，确保员工表 DDL 在 Visual Studio、脚本执行器和 SQL Server Management Studio 中一致解析。</zh-CN>
--   <en>Enable quoted identifiers so employee-table DDL parses consistently in Visual Studio, script runners, and SQL Server Management Studio.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>员工表依赖组织目录；缺失时立即中止，避免创建无法绑定部门外键的半成品员工主数据。</zh-CN>
--   <en>The employee table depends on the organization catalog; fail immediately when it is missing to avoid half-built employee master data without department foreign keys.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[PortalBiz_OrganizationUnits]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_OrganizationUnits must be created before PortalBiz_Employees.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>建表保护保留既有员工主数据，并允许部署脚本在多环境初始化或补跑时安全重入。</zh-CN>
--   <en>The create-table guard preserves existing employee master data and lets deployment scripts safely re-enter during multi-environment initialization or repair runs.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_Employees]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>员工表聚焦低敏身份目录字段；登录凭据、联系方式扩展和高敏 HR 信息不在本表建模。</zh-CN>
    --   <en>The employee table focuses on low-sensitivity identity-directory fields; login credentials, contact extensions, and sensitive HR data are not modeled here.</en>
    -- </lang>
    CREATE TABLE [dbo].[PortalBiz_Employees]
    (
        [EmployeeId] INT IDENTITY(1,1) NOT NULL,
        [EmployeeCode] NVARCHAR(64) NOT NULL,
        [DisplayName] NVARCHAR(150) NOT NULL,
        [PreferredName] NVARCHAR(100) NULL,
        [WorkEmail] NVARCHAR(256) NULL,
        [OrganizationUnitId] INT NULL,
        [EmploymentStatus] NVARCHAR(40) NOT NULL
            CONSTRAINT [DF_PortalBiz_Employees_EmploymentStatus] DEFAULT (N'Active'),
        [JoinedUtc] DATETIME2(0) NULL,
        [LeftUtc] DATETIME2(0) NULL,
        [SourceSystem] NVARCHAR(80) NOT NULL
            CONSTRAINT [DF_PortalBiz_Employees_SourceSystem] DEFAULT (N'Portal'),
        [CreatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_Employees_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(100) NULL,
        [UpdatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_Employees_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        -- <lang>
        --   <zh-CN>自增主键承担内部引用，员工工号使用唯一约束保持登录和绑定入口稳定。</zh-CN>
        --   <en>The identity primary key handles internal references, while the employee-code unique constraint keeps sign-in and binding entry points stable.</en>
        -- </lang>
        CONSTRAINT [PK_PortalBiz_Employees]
            PRIMARY KEY CLUSTERED ([EmployeeId]),
        CONSTRAINT [FK_PortalBiz_Employees_OrganizationUnits]
            FOREIGN KEY ([OrganizationUnitId]) REFERENCES [dbo].[PortalBiz_OrganizationUnits] ([OrganizationUnitId]),
        CONSTRAINT [UQ_PortalBiz_Employees_EmployeeCode]
            UNIQUE ([EmployeeCode]),
        -- <lang>
        --   <zh-CN>文本和状态检查在数据库边界阻断空白主数据，并要求离职状态必须携带离职时间。</zh-CN>
        --   <en>Text and status checks block blank master data at the database boundary and require a leaving timestamp whenever the employee is marked as left.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_Employees_EmployeeCode]
            CHECK ([EmployeeCode] = LTRIM(RTRIM([EmployeeCode])) AND NULLIF([EmployeeCode], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_Employees_DisplayName]
            CHECK ([DisplayName] = LTRIM(RTRIM([DisplayName])) AND NULLIF([DisplayName], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_Employees_PreferredName]
            CHECK ([PreferredName] IS NULL OR ([PreferredName] = LTRIM(RTRIM([PreferredName])) AND NULLIF([PreferredName], N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_Employees_WorkEmail]
            CHECK ([WorkEmail] IS NULL OR ([WorkEmail] = LTRIM(RTRIM([WorkEmail])) AND NULLIF([WorkEmail], N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_Employees_SourceSystem]
            CHECK ([SourceSystem] = LTRIM(RTRIM([SourceSystem])) AND NULLIF([SourceSystem], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_Employees_EmploymentStatus]
            CHECK ([EmploymentStatus] IN (N'Active', N'Pending', N'Suspended', N'Left')),
        CONSTRAINT [CK_PortalBiz_Employees_LeftUtc]
            CHECK ([EmploymentStatus] <> N'Left' OR [LeftUtc] IS NOT NULL)
    )
END
GO

-- <lang>
--   <zh-CN>兼容性收敛块修正早期环境可能出现的 EmployeeCode 长度漂移；重建约束时只触碰该列相关约束。</zh-CN>
--   <en>The compatibility convergence block fixes possible EmployeeCode length drift in early environments; when rebuilding constraints it touches only constraints related to that column.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[PortalBiz_Employees]', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.PortalBiz_Employees', N'EmployeeCode') < 128
BEGIN
    -- <lang>
    --   <zh-CN>修改非空列长度前先移除依赖检查约束，避免 SQL Server 阻止列定义收敛。</zh-CN>
    --   <en>Remove the dependent check constraint before changing the non-null column length so SQL Server does not block the column-definition convergence.</en>
    -- </lang>
    IF EXISTS (SELECT * FROM sys.check_constraints WHERE [name] = N'CK_PortalBiz_Employees_EmployeeCode')
    BEGIN
        ALTER TABLE [dbo].[PortalBiz_Employees]
        DROP CONSTRAINT [CK_PortalBiz_Employees_EmployeeCode]
    END

    -- <lang>
    --   <zh-CN>唯一约束同样依赖 EmployeeCode 列定义；先移除再恢复以保持工号唯一语义不变。</zh-CN>
    --   <en>The unique constraint also depends on the EmployeeCode definition; remove and restore it so employee-code uniqueness remains unchanged.</en>
    -- </lang>
    IF EXISTS (SELECT * FROM sys.key_constraints WHERE [name] = N'UQ_PortalBiz_Employees_EmployeeCode')
    BEGIN
        ALTER TABLE [dbo].[PortalBiz_Employees]
        DROP CONSTRAINT [UQ_PortalBiz_Employees_EmployeeCode]
    END

    ALTER TABLE [dbo].[PortalBiz_Employees]
    ALTER COLUMN [EmployeeCode] NVARCHAR(64) NOT NULL

    -- <lang>
    --   <zh-CN>长度收敛后恢复唯一约束，确保已有调用仍可把工号作为稳定业务键。</zh-CN>
    --   <en>Restore the unique constraint after length convergence so existing callers can still treat employee code as a stable business key.</en>
    -- </lang>
    IF NOT EXISTS (SELECT * FROM sys.key_constraints WHERE [name] = N'UQ_PortalBiz_Employees_EmployeeCode')
    BEGIN
        ALTER TABLE [dbo].[PortalBiz_Employees]
        ADD CONSTRAINT [UQ_PortalBiz_Employees_EmployeeCode]
            UNIQUE ([EmployeeCode])
    END

    -- <lang>
    --   <zh-CN>恢复非空白检查，确保兼容性修正不会放宽员工工号的数据质量门槛。</zh-CN>
    --   <en>Restore the non-blank check so the compatibility fix does not loosen the data-quality gate for employee codes.</en>
    -- </lang>
    IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE [name] = N'CK_PortalBiz_Employees_EmployeeCode')
    BEGIN
        ALTER TABLE [dbo].[PortalBiz_Employees]
        ADD CONSTRAINT [CK_PortalBiz_Employees_EmployeeCode]
            CHECK ([EmployeeCode] = LTRIM(RTRIM([EmployeeCode])) AND NULLIF([EmployeeCode], N'') IS NOT NULL)
    END
END
GO

-- <lang>
--   <zh-CN>组织/状态索引支撑员工目录按部门、在职状态和展示名排序检索。</zh-CN>
--   <en>The organization/status index supports employee-directory lookup by department, employment status, and display-name ordering.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_Employees_OrganizationStatus' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_Employees]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_Employees_OrganizationStatus]
    ON [dbo].[PortalBiz_Employees] ([OrganizationUnitId], [EmploymentStatus], [DisplayName])
END
GO

-- <lang>
--   <zh-CN>工作邮箱过滤索引用于登录名解析和资料查找，只索引存在实际邮箱的低敏目录项。</zh-CN>
--   <en>The filtered work-email index supports sign-in identifier resolution and profile lookup while indexing only low-sensitivity directory entries that actually have email values.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_Employees_WorkEmail' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_Employees]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_Employees_WorkEmail]
    ON [dbo].[PortalBiz_Employees] ([WorkEmail])
    WHERE [WorkEmail] IS NOT NULL AND [WorkEmail] <> N''
END
GO
