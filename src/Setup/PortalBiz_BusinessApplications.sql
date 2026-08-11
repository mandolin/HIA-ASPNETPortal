/*
<lang>
  <zh-CN>P19.4 抽象业务申请迁移。本脚本可重复执行，应用程序不会在启动时自动执行它；第一版只保存低敏申请正文、状态、申请人和最近审核意见，不保存附件、密码、Cookie、Token、连接串、证件号、薪资或具体领域专业字段。</zh-CN>
  <en>P19.4 abstract business-application migration. This script is idempotent and the application never runs it automatically at startup; the first version stores only low-sensitivity request text, state, applicant, and latest review comment, and stores no attachments, passwords, cookies, tokens, connection strings, government ids, compensation data, or domain-specific professional fields.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，保持业务迁移表的约束行为与 SQL Server 基线一致。</zh-CN>
--   <en>Enable standard NULL comparison semantics so constraint behavior for the business migration table matches the SQL Server baseline.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护后续 DDL 中的表名、列名和约束名稳定解析。</zh-CN>
--   <en>Enable quoted identifiers so later table, column, and constraint names parse consistently.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>业务申请表依赖旧认证用户表；缺失时 fail fast，避免创建无法建立申请人外键的半成品结构。</zh-CN>
--   <en>The business-application table depends on the legacy authentication user table; fail fast when it is missing to avoid creating a half-built structure without applicant foreign keys.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_BusinessApplications.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>建表保护保证重复执行时保留既有申请数据、状态和审核意见。</zh-CN>
--   <en>The create-table guard preserves existing applications, states, and review comments across repeated execution.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_BusinessApplications]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>抽象业务申请表只保存低敏流程事实；具体行业字段和附件由后续专用模块另行建模。</zh-CN>
    --   <en>The abstract business-application table stores only low-sensitivity workflow facts; domain-specific fields and attachments are modeled later by specialized modules.</en>
    -- </lang>
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

        -- <lang>
        --   <zh-CN>主键使用自增技术标识，业务展示和外部引用通过唯一 ApplicationCode 保持稳定。</zh-CN>
        --   <en>The primary key uses an identity technical id, while business display and external references remain stable through the unique ApplicationCode.</en>
        -- </lang>
        CONSTRAINT [PK_PortalBiz_BusinessApplications]
            PRIMARY KEY CLUSTERED ([ApplicationId]),
        CONSTRAINT [UX_PortalBiz_BusinessApplications_Code]
            UNIQUE ([ApplicationCode]),
        -- <lang>
        --   <zh-CN>申请人和最近审核人均指向旧用户表；审核人可为空以表达未进入最终审核状态。</zh-CN>
        --   <en>The applicant and latest reviewer both point to the legacy user table; the reviewer may be null while the application has not entered a final review state.</en>
        -- </lang>
        CONSTRAINT [FK_PortalBiz_BusinessApplications_ApplicantUser]
            FOREIGN KEY ([ApplicantUserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        CONSTRAINT [FK_PortalBiz_BusinessApplications_ReviewerUser]
            FOREIGN KEY ([ReviewedByUserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        -- <lang>
        --   <zh-CN>文本键和标题类字段必须已裁剪且非空，避免后台筛选或链接生成拿到不可见空白值。</zh-CN>
        --   <en>Text keys and title-like fields must be trimmed and non-empty so admin filtering and link generation never receive invisible whitespace values.</en>
        -- </lang>
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
        -- <lang>
        --   <zh-CN>状态白名单约束第一版抽象流程的有限状态机，防止自由文本破坏审批分支。</zh-CN>
        --   <en>The status whitelist constrains the first-version abstract workflow state machine so free text cannot break approval branches.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_BusinessApplications_Status]
            CHECK ([ApplicationStatus] IN (N'Draft', N'Submitted', N'InReview', N'Returned', N'Approved', N'Rejected', N'Withdrawn', N'Closed')),
        CONSTRAINT [CK_PortalBiz_BusinessApplications_CreatedBy]
            CHECK ([CreatedBy] = LTRIM(RTRIM([CreatedBy])) AND NULLIF([CreatedBy], N'') IS NOT NULL),
        CONSTRAINT [CK_PortalBiz_BusinessApplications_UpdatedBy]
            CHECK ([UpdatedBy] = LTRIM(RTRIM([UpdatedBy])) AND NULLIF([UpdatedBy], N'') IS NOT NULL),
        -- <lang>
        --   <zh-CN>审核完成类状态必须有审核时间；草稿、提交和审核中状态不得提前绑定审核人。</zh-CN>
        --   <en>Review-complete states require a review time, while draft, submitted, and in-review states must not bind a reviewer prematurely.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_BusinessApplications_ReviewState]
            CHECK (
                ([ApplicationStatus] IN (N'Draft', N'Submitted', N'InReview') AND [ReviewedUtc] IS NULL AND [ReviewedByUserId] IS NULL)
                OR
                ([ApplicationStatus] IN (N'Returned', N'Approved', N'Rejected', N'Withdrawn', N'Closed') AND [ReviewedUtc] IS NOT NULL)
            )
    )
END
GO

-- <lang>
--   <zh-CN>状态/提交时间索引服务审核队列和状态筛选，使用 ApplicationId 作为同秒记录的稳定排序补充。</zh-CN>
--   <en>The status/submitted-time index serves review queues and status filters, using ApplicationId as a stable tie-breaker within the same second.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_BusinessApplications_StatusSubmitted' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_BusinessApplications]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_BusinessApplications_StatusSubmitted]
    ON [dbo].[PortalBiz_BusinessApplications] ([ApplicationStatus], [SubmittedUtc] DESC, [ApplicationId] DESC)
END
GO

-- <lang>
--   <zh-CN>申请人索引用于“我的申请”列表，按最近提交时间倒序展示个人申请记录。</zh-CN>
--   <en>The applicant index supports the “my applications” list, showing a user's own requests by most recent submitted time.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_BusinessApplications_Applicant' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_BusinessApplications]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_BusinessApplications_Applicant]
    ON [dbo].[PortalBiz_BusinessApplications] ([ApplicantUserId], [SubmittedUtc] DESC, [ApplicationId] DESC)
END
GO
