/*
<lang>
  <zh-CN>P6.4 员工资料更正请求业务模块迁移。本脚本可重复执行且不会由应用启动流程自动执行；第一版只保存字段级文本更正请求和最小管理员处理状态，不保存附件、身份证号、手机号、薪资、绩效或其它高敏个人资料，也不直接修改员工主数据。</zh-CN>
  <en>P6.4 employee-profile correction-request business-module migration. This script is idempotent and is not executed automatically by application startup; the first version stores only field-level text correction requests and minimal administrator review status, with no attachments, government ids, mobile phone numbers, compensation, performance data, or other high-sensitivity personal data, and it does not directly update employee master data.</en>
</lang>
*/

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，保证审核状态、可选备注和审核人字段的检查约束可靠区分空值。</zh-CN>
--   <en>Enable standard NULL comparison semantics so review-state, optional-note, and reviewer-field check constraints reliably distinguish null values.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，确保更正请求表、约束和队列索引名称在所有执行入口中一致解析。</zh-CN>
--   <en>Enable quoted identifiers so correction-request table, constraint, and queue-index names parse consistently across all execution entry points.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>更正请求必须锚定员工主数据；员工表缺失时中止，避免产生不能归属到目录主体的请求。</zh-CN>
--   <en>Correction requests must anchor to employee master data; stop when the employee table is missing to avoid requests that cannot belong to a directory identity.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[PortalBiz_Employees]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_Employees must be created before PortalBiz_EmployeeProfileCorrectionRequests.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>更正请求必须锚定门户账号主体，用于记录提交人和审核人所在的账号边界。</zh-CN>
--   <en>Correction requests must anchor to the Portal account authority, capturing the account boundary for submitter and reviewer identities.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_EmployeeProfileCorrectionRequests.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>绑定表证明提交时的账号-员工关系；没有绑定表时不创建请求表，避免审计链断裂。</zh-CN>
--   <en>The binding table proves the account-employee relationship at submission time; without it the request table is not created, avoiding a broken audit chain.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[PortalBiz_UserEmployeeBindings]', N'U') IS NULL
BEGIN
    RAISERROR(N'PortalBiz_UserEmployeeBindings must be created before PortalBiz_EmployeeProfileCorrectionRequests.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>建表保护保留已有更正队列、审核记录和行版本，支持部署补跑而不改变业务状态。</zh-CN>
--   <en>The create-table guard preserves existing correction queues, review records, and row versions, supporting deployment reruns without changing business state.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_EmployeeProfileCorrectionRequests]') AND type IN (N'U'))
BEGIN
    -- <lang>
    --   <zh-CN>更正请求表只保存可审核的低敏字段建议和管理员处理结果；员工主数据更新由后续受控服务执行。</zh-CN>
    --   <en>The correction-request table stores only reviewable low-sensitivity field proposals and administrator outcomes; employee master-data updates are performed later by controlled services.</en>
    -- </lang>
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

        -- <lang>
        --   <zh-CN>主键使用追加式 BIGINT 标识；外键固定员工、账号和绑定，保留提交时身份上下文。</zh-CN>
        --   <en>The primary key uses an append-only BIGINT identifier; foreign keys pin employee, account, and binding to preserve identity context at submission time.</en>
        -- </lang>
        CONSTRAINT [PK_PortalBiz_EmployeeProfileCorrectionRequests]
            PRIMARY KEY CLUSTERED ([RequestId]),
        CONSTRAINT [FK_PortalBiz_EmployeeProfileCorrectionRequests_Employees]
            FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[PortalBiz_Employees] ([EmployeeId]),
        CONSTRAINT [FK_PortalBiz_EmployeeProfileCorrectionRequests_Users]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Portal_Users] ([UserID]),
        CONSTRAINT [FK_PortalBiz_EmployeeProfileCorrectionRequests_Bindings]
            FOREIGN KEY ([BindingId]) REFERENCES [dbo].[PortalBiz_UserEmployeeBindings] ([BindingId]),
        -- <lang>
        --   <zh-CN>字段与文本约束把请求限定在低敏可更正字段内，并阻止空白建议值进入审核队列。</zh-CN>
        --   <en>Field and text constraints limit requests to low-sensitivity correctable fields and prevent blank proposed values from entering the review queue.</en>
        -- </lang>
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
        -- <lang>
        --   <zh-CN>审核状态约束确保 Submitted 请求没有审核人/审核时间，而所有非 Submitted 状态必须有完整审核元数据。</zh-CN>
        --   <en>The review-state constraint ensures Submitted requests have no reviewer or review time, while every non-Submitted state must carry complete review metadata.</en>
        -- </lang>
        CONSTRAINT [CK_PortalBiz_EmployeeProfileCorrectionRequests_ReviewState]
            CHECK (
                ([RequestStatus] = N'Submitted' AND [ReviewedUtc] IS NULL AND [ReviewedBy] IS NULL)
                OR
                ([RequestStatus] <> N'Submitted' AND [ReviewedUtc] IS NOT NULL AND [ReviewedBy] IS NOT NULL)
            )
    )
END
GO

-- <lang>
--   <zh-CN>状态队列索引支撑管理员按待处理/已处理状态和提交时间倒序查看更正请求。</zh-CN>
--   <en>The status queue index supports administrator review of correction requests by processing state and descending submission time.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_EmployeeProfileCorrectionRequests_Status' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_EmployeeProfileCorrectionRequests]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_EmployeeProfileCorrectionRequests_Status]
    ON [dbo].[PortalBiz_EmployeeProfileCorrectionRequests] ([RequestStatus], [SubmittedUtc] DESC, [RequestId] DESC)
END
GO

-- <lang>
--   <zh-CN>员工/用户时间线索引用于个人中心和员工详情页查看同一身份上下文下的最近更正请求。</zh-CN>
--   <en>The employee/user timeline index supports personal-center and employee-detail views of recent correction requests under the same identity context.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PortalBiz_EmployeeProfileCorrectionRequests_EmployeeUser' AND object_id = OBJECT_ID(N'[dbo].[PortalBiz_EmployeeProfileCorrectionRequests]'))
BEGIN
    CREATE INDEX [IX_PortalBiz_EmployeeProfileCorrectionRequests_EmployeeUser]
    ON [dbo].[PortalBiz_EmployeeProfileCorrectionRequests] ([EmployeeId], [UserId], [SubmittedUtc] DESC, [RequestId] DESC)
END
GO
