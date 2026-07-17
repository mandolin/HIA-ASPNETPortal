/*
    P6.4 员工资料更正请求业务模块迁移。
    P6.4 employee-profile correction-request business-module migration.

    本脚本可重复执行；应用程序不会在启动时自动执行它。
    This script is idempotent; the application never runs it automatically at startup.

    第一版只保存字段级文本更正请求和最小管理员处理状态，不保存附件、身份证号、手机号、薪资、
    绩效或其它高敏个人资料，也不直接修改员工主数据。
    The first version stores only field-level text correction requests and minimal administrator review status.
    It stores no attachments, government ids, mobile phone numbers, compensation, performance data, or other
    high-sensitivity personal data, and it does not directly update employee master data.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'[dbo].[PortalBiz_Employees]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_Employees must be created before PortalBiz_EmployeeProfileCorrectionRequests.', 16, 1)
    RETURN
END
GO

IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_EmployeeProfileCorrectionRequests.', 16, 1)
    RETURN
END
GO

IF OBJECT_ID(N'[dbo].[PortalBiz_UserEmployeeBindings]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_UserEmployeeBindings must be created before PortalBiz_EmployeeProfileCorrectionRequests.', 16, 1)
    RETURN
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_EmployeeProfileCorrectionRequests]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[PortalBiz_EmployeeProfileCorrectionRequests]
    (
        [RequestId] BIGINT IDENTITY(1,1) NOT NULL,
        [EmployeeId] INT NOT NULL,
        [UserId] INT NOT NULL,
        [BindingId] INT NOT NULL,
        [SubmittedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_EmployeeProfileCorrectionRequests_SubmittedUtc] DEFAULT (SYSUTCDATETIME()),
        [SubmittedBy] NVARCHAR(100) NOT NULL,
        [FieldName] NVARCHAR(100) NOT NULL,
        [CurrentValueSnapshot] NVARCHAR(512) NULL,
        [ProposedValue] NVARCHAR(512) NOT NULL,
        [RequestNote] NVARCHAR(1000) NULL,
        [RequestStatus] NVARCHAR(20) NOT NULL
            CONSTRAINT [DF_PortalBiz_EmployeeProfileCorrectionRequests_Status] DEFAULT (N'Submitted'),
        [ReviewedUtc] DATETIME2(0) NULL,
        [ReviewedBy] NVARCHAR(100) NULL,
        [ReviewNote] NVARCHAR(1000) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        CONSTRAINT [PK_PortalBiz_EmployeeProfileCorrectionRequests]
            PRIMARY KEY CLUSTERED ([RequestId]),
        CONSTRAINT [FK_PortalBiz_EmployeeProfileCorrectionRequests_Employees]
            FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[PortalBiz_Employees] ([EmployeeId]),
        CONSTRAINT [FK_PortalBiz_EmployeeProfileCorrectionRequests_Users]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        CONSTRAINT [FK_PortalBiz_EmployeeProfileCorrectionRequests_Bindings]
            FOREIGN KEY ([BindingId]) REFERENCES [dbo].[PortalBiz_UserEmployeeBindings] ([BindingId]),
        CONSTRAINT [CK_PortalBiz_EmployeeProfileCorrectionRequests_SubmittedBy]
            CHECK ([SubmittedBy] = LTRIM(RTRIM([SubmittedBy])) AND NULLIF([SubmittedBy], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_EmployeeProfileCorrectionRequests_FieldName]
            CHECK ([FieldName] IN (N'DisplayName', N'PreferredName', N'WorkEmail', N'OrganizationDisplayName')),
        CONSTRAINT [CK_PortalBiz_EmployeeProfileCorrectionRequests_ProposedValue]
            CHECK ([ProposedValue] = LTRIM(RTRIM([ProposedValue])) AND NULLIF([ProposedValue], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_EmployeeProfileCorrectionRequests_RequestStatus]
            CHECK ([RequestStatus] IN (N'Submitted', N'Reviewed', N'Closed', N'Rejected')),
        CONSTRAINT [CK_PortalBiz_EmployeeProfileCorrectionRequests_RequestNote]
            CHECK ([RequestNote] IS NULL OR ([RequestNote] = LTRIM(RTRIM([RequestNote])) AND NULLIF([RequestNote], N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_EmployeeProfileCorrectionRequests_ReviewedBy]
            CHECK ([ReviewedBy] IS NULL OR ([ReviewedBy] = LTRIM(RTRIM([ReviewedBy])) AND NULLIF([ReviewedBy], N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_EmployeeProfileCorrectionRequests_ReviewNote]
            CHECK ([ReviewNote] IS NULL OR ([ReviewNote] = LTRIM(RTRIM([ReviewNote])) AND NULLIF([ReviewNote], N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_EmployeeProfileCorrectionRequests_ReviewState]
            CHECK (
                ([RequestStatus] = N'Submitted' AND [ReviewedUtc] IS NULL AND [ReviewedBy] IS NULL)
                OR
                ([RequestStatus] <> N'Submitted' AND [ReviewedUtc] IS NOT NULL AND [ReviewedBy] IS NOT NULL)
            )
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_EmployeeProfileCorrectionRequests_Status' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_EmployeeProfileCorrectionRequests]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_EmployeeProfileCorrectionRequests_Status]
    ON [dbo].[PortalBiz_EmployeeProfileCorrectionRequests] ([RequestStatus], [SubmittedUtc] DESC, [RequestId] DESC)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_EmployeeProfileCorrectionRequests_EmployeeUser' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_EmployeeProfileCorrectionRequests]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_EmployeeProfileCorrectionRequests_EmployeeUser]
    ON [dbo].[PortalBiz_EmployeeProfileCorrectionRequests] ([EmployeeId], [UserId], [SubmittedUtc] DESC, [RequestId] DESC)
END
GO
