/*
    P6.3 员工基础表迁移。
    P6.3 employee foundation migration.

    本脚本可重复执行；应用程序不会在启动时自动执行它。
    This script is idempotent; the application never runs it automatically at startup.

    第一版员工表只保存门户业务所需的最小主数据；手机号、身份证号等高敏个人信息暂不入库。
    The first employee table stores only minimal master data required by Portal business flows; highly sensitive
    personal data such as mobile phone numbers and government identifiers are intentionally not stored yet.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'[dbo].[PortalBiz_OrganizationUnits]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_OrganizationUnits must be created before PortalBiz_Employees.', 16, 1)
    RETURN
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_Employees]') AND type IN (N'U'))
BEGIN
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

        CONSTRAINT [PK_PortalBiz_Employees]
            PRIMARY KEY CLUSTERED ([EmployeeId]),
        CONSTRAINT [FK_PortalBiz_Employees_OrganizationUnits]
            FOREIGN KEY ([OrganizationUnitId]) REFERENCES [dbo].[PortalBiz_OrganizationUnits] ([OrganizationUnitId]),
        CONSTRAINT [UQ_PortalBiz_Employees_EmployeeCode]
            UNIQUE ([EmployeeCode]),
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

IF OBJECT_ID(N'[dbo].[PortalBiz_Employees]', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.PortalBiz_Employees', N'EmployeeCode') < 128
BEGIN
    IF EXISTS (SELECT * FROM sys.check_constraints WHERE [name] = N'CK_PortalBiz_Employees_EmployeeCode')
    BEGIN
        ALTER TABLE [dbo].[PortalBiz_Employees]
        DROP CONSTRAINT [CK_PortalBiz_Employees_EmployeeCode]
    END

    IF EXISTS (SELECT * FROM sys.key_constraints WHERE [name] = N'UQ_PortalBiz_Employees_EmployeeCode')
    BEGIN
        ALTER TABLE [dbo].[PortalBiz_Employees]
        DROP CONSTRAINT [UQ_PortalBiz_Employees_EmployeeCode]
    END

    ALTER TABLE [dbo].[PortalBiz_Employees]
    ALTER COLUMN [EmployeeCode] NVARCHAR(64) NOT NULL

    IF NOT EXISTS (SELECT * FROM sys.key_constraints WHERE [name] = N'UQ_PortalBiz_Employees_EmployeeCode')
    BEGIN
        ALTER TABLE [dbo].[PortalBiz_Employees]
        ADD CONSTRAINT [UQ_PortalBiz_Employees_EmployeeCode]
            UNIQUE ([EmployeeCode])
    END

    IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE [name] = N'CK_PortalBiz_Employees_EmployeeCode')
    BEGIN
        ALTER TABLE [dbo].[PortalBiz_Employees]
        ADD CONSTRAINT [CK_PortalBiz_Employees_EmployeeCode]
            CHECK ([EmployeeCode] = LTRIM(RTRIM([EmployeeCode])) AND NULLIF([EmployeeCode], N'') IS NOT NULL)
    END
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_Employees_OrganizationStatus' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_Employees]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_Employees_OrganizationStatus]
    ON [dbo].[PortalBiz_Employees] ([OrganizationUnitId], [EmploymentStatus], [DisplayName])
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_Employees_WorkEmail' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_Employees]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_Employees_WorkEmail]
    ON [dbo].[PortalBiz_Employees] ([WorkEmail])
    WHERE [WorkEmail] IS NOT NULL AND [WorkEmail] <> N''
END
GO
