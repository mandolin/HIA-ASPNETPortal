/*
    P19.4 抽象业务申请迁移。
    P19.4 abstract business-application migration.

    本脚本可重复执行；应用程序不会在启动时自动执行它。
    This script is idempotent; the application never runs it automatically at startup.

    第一版只保存低敏申请正文、状态、申请人和最近审核意见，不保存附件、密码、
    Cookie、Token、连接串、证件号、薪资或具体领域专业字段。
    The first version stores only low-sensitivity request text, state, applicant, and latest review
    comment. It stores no attachments, passwords, cookies, tokens, connection strings,
    government ids, compensation data, or domain-specific professional fields.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_BusinessApplications.', 16, 1)
    RETURN
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_BusinessApplications]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[PortalBiz_BusinessApplications]
    (
        [ApplicationId] BIGINT IDENTITY(1,1) NOT NULL,
        [ApplicationCode] NVARCHAR(40) NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [CategoryKey] NVARCHAR(80) NULL,
        [Summary] NVARCHAR(500) NULL,
        [Body] NVARCHAR(MAX) NULL,
        [ApplicantUserId] INT NOT NULL,
        [ApplicantEmployeeId] INT NULL,
        [OrganizationUnitId] INT NULL,
        [ReviewRoleKey] NVARCHAR(120) NOT NULL,
        [ApplicationStatus] NVARCHAR(20) NOT NULL
            CONSTRAINT [DF_PortalBiz_BusinessApplications_Status] DEFAULT (N'Submitted'),
        [SubmittedUtc] DATETIME2(0) NULL,
        [ReviewedUtc] DATETIME2(0) NULL,
        [ReviewedByUserId] INT NULL,
        [ReviewComment] NVARCHAR(1000) NULL,
        [CreatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_BusinessApplications_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(100) NOT NULL,
        [UpdatedUtc] DATETIME2(0) NOT NULL
            CONSTRAINT [DF_PortalBiz_BusinessApplications_UpdatedUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedBy] NVARCHAR(100) NOT NULL,
        [RowVersion] ROWVERSION NOT NULL,

        CONSTRAINT [PK_PortalBiz_BusinessApplications]
            PRIMARY KEY CLUSTERED ([ApplicationId]),
        CONSTRAINT [UX_PortalBiz_BusinessApplications_Code]
            UNIQUE ([ApplicationCode]),
        CONSTRAINT [FK_PortalBiz_BusinessApplications_ApplicantUser]
            FOREIGN KEY ([ApplicantUserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        CONSTRAINT [FK_PortalBiz_BusinessApplications_ReviewerUser]
            FOREIGN KEY ([ReviewedByUserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        CONSTRAINT [CK_PortalBiz_BusinessApplications_Code]
            CHECK ([ApplicationCode] = LTRIM(RTRIM([ApplicationCode])) AND NULLIF([ApplicationCode], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_BusinessApplications_Title]
            CHECK ([Title] = LTRIM(RTRIM([Title])) AND NULLIF([Title], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_BusinessApplications_Category]
            CHECK ([CategoryKey] IS NULL OR ([CategoryKey] = LTRIM(RTRIM([CategoryKey])) AND NULLIF([CategoryKey], N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_BusinessApplications_Summary]
            CHECK ([Summary] IS NULL OR ([Summary] = LTRIM(RTRIM([Summary])) AND NULLIF([Summary], N'') IS NOT NULL)),
        CONSTRAINT [CK_PortalBiz_BusinessApplications_ReviewRole]
            CHECK ([ReviewRoleKey] = LTRIM(RTRIM([ReviewRoleKey])) AND NULLIF([ReviewRoleKey], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_BusinessApplications_Status]
            CHECK ([ApplicationStatus] IN (N'Draft', N'Submitted', N'InReview', N'Returned', N'Approved', N'Rejected', N'Withdrawn', N'Closed')),
        CONSTRAINT [CK_PortalBiz_BusinessApplications_CreatedBy]
            CHECK ([CreatedBy] = LTRIM(RTRIM([CreatedBy])) AND NULLIF([CreatedBy], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_BusinessApplications_UpdatedBy]
            CHECK ([UpdatedBy] = LTRIM(RTRIM([UpdatedBy])) AND NULLIF([UpdatedBy], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_BusinessApplications_ReviewState]
            CHECK (
                ([ApplicationStatus] IN (N'Draft', N'Submitted', N'InReview') AND [ReviewedUtc] IS NULL AND [ReviewedByUserId] IS NULL)
                OR
                ([ApplicationStatus] IN (N'Returned', N'Approved', N'Rejected', N'Withdrawn', N'Closed') AND [ReviewedUtc] IS NOT NULL)
            )
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_BusinessApplications_StatusSubmitted' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_BusinessApplications]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_BusinessApplications_StatusSubmitted]
    ON [dbo].[PortalBiz_BusinessApplications] ([ApplicationStatus], [SubmittedUtc] DESC, [ApplicationId] DESC)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_BusinessApplications_Applicant' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_BusinessApplications]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_BusinessApplications_Applicant]
    ON [dbo].[PortalBiz_BusinessApplications] ([ApplicantUserId], [SubmittedUtc] DESC, [ApplicationId] DESC)
END
GO
