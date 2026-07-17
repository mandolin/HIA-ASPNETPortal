/*
    P6.3 门户账号与员工绑定基础表迁移。
    P6.3 Portal-user to employee binding foundation migration.

    本脚本可重复执行；应用程序不会在启动时自动执行它。
    This script is idempotent; the application never runs it automatically at startup.

    第一版只允许一个门户账号和一个员工之间存在一条当前有效绑定。绑定变化会影响员工工号登录，
    后续服务层必须同步写运营审计并递增目标用户安全版本。
    The first version allows only one currently active binding between a Portal account and an employee. Binding
    changes affect employee-code sign-in; later service code must record operations audit and increment the target
    user's security version.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_UserEmployeeBindings.', 16, 1)
    RETURN
END
GO

IF OBJECT_ID(N'[dbo].[PortalBiz_Employees]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_Employees must be created before PortalBiz_UserEmployeeBindings.', 16, 1)
    RETURN
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_UserEmployeeBindings]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[PortalBiz_UserEmployeeBindings]
    (
        [BindingId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [EmployeeId] INT NOT NULL,
        [BindingStatus] NVARCHAR(40) NOT NULL
            CONSTRAINT [DF_PortalBiz_UserEmployeeBindings_BindingStatus] DEFAULT (N'Active'),
        [BoundUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_UserEmployeeBindings_BoundUtc] DEFAULT (SYSUTCDATETIME()),
        [BoundBy] NVARCHAR(100) NULL,
        [EndedUtc] DATETIME2(0) NULL,
        [EndedBy] NVARCHAR(100) NULL,
        [Reason] NVARCHAR(200) NULL,
        [UpdatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_UserEmployeeBindings_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        CONSTRAINT [PK_PortalBiz_UserEmployeeBindings]
            PRIMARY KEY CLUSTERED ([BindingId]),
        CONSTRAINT [FK_PortalBiz_UserEmployeeBindings_Users]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Portal_Users] ([UserID]) ON DELETE CASCADE,
        CONSTRAINT [FK_PortalBiz_UserEmployeeBindings_Employees]
            FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[PortalBiz_Employees] ([EmployeeId]),
        CONSTRAINT [CK_PortalBiz_UserEmployeeBindings_Status]
            CHECK ([BindingStatus] IN (N'Active', N'Pending', N'Disabled', N'Ended')),
        CONSTRAINT [CK_PortalBiz_UserEmployeeBindings_EndedUtc]
            CHECK ([BindingStatus] <> N'Ended' OR [EndedUtc] IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_UserEmployeeBindings_Reason]
            CHECK ([Reason] IS NULL OR ([Reason] = LTRIM(RTRIM([Reason])) AND NULLIF([Reason], N'') IS NOT NULL))
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'UX_PortalBiz_UserEmployeeBindings_ActiveUser' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_UserEmployeeBindings]'))
BEGIN
    CREATE UNIQUE INDEX [UX_PortalBiz_UserEmployeeBindings_ActiveUser]
    ON [dbo].[PortalBiz_UserEmployeeBindings] ([UserId])
    WHERE [BindingStatus] = N'Active'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'UX_PortalBiz_UserEmployeeBindings_ActiveEmployee' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_UserEmployeeBindings]'))
BEGIN
    CREATE UNIQUE INDEX [UX_PortalBiz_UserEmployeeBindings_ActiveEmployee]
    ON [dbo].[PortalBiz_UserEmployeeBindings] ([EmployeeId])
    WHERE [BindingStatus] = N'Active'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_UserEmployeeBindings_Status' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_UserEmployeeBindings]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_UserEmployeeBindings_Status]
    ON [dbo].[PortalBiz_UserEmployeeBindings] ([BindingStatus], [EmployeeId], [UserId])
END
GO
