SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- 表单构建器（独立模块 MVP）提交表：承载以独立模块形式提供的表单采集结果。
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_FormSubmissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PortalBiz_FormSubmissions](
        [SubmissionId] BIGINT IDENTITY(1,1) NOT NULL,
        [ModuleId] INT NOT NULL,
        [FormId] NVARCHAR(100) NOT NULL,
        [SubmittedByUserId] INT NULL,
        [SubmittedBy] NVARCHAR(100) NULL,
        [SubmittedUtc] DATETIME2(0) NOT NULL CONSTRAINT [DF_PortalBiz_FormSubmissions_SubmittedUtc] DEFAULT (SYSUTCDATETIME()),
        [Data] NVARCHAR(MAX) NOT NULL,
        [RowVersion] ROWVERSION NOT NULL,
     CONSTRAINT [PK_PortalBiz_FormSubmissions] PRIMARY KEY CLUSTERED ([SubmissionId])
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_FormSubmissions]') AND name = N'IX_PortalBiz_FormSubmissions_ModuleId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PortalBiz_FormSubmissions_ModuleId]
        ON [dbo].[PortalBiz_FormSubmissions]([ModuleId] ASC, [SubmittedUtc] DESC);
END
GO
