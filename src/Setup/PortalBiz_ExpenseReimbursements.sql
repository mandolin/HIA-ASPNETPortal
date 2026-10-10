-- <lang>
--   <zh-CN>费用报销业务表迁移。本脚本可重复执行，应用程序不会在启动时自动执行它；只保存申请人、类别、金额、事由与发票附件名，不保存密码、Cookie、Token、连接串、证件号、薪资或其它高敏个人资料。</zh-CN>
--   <en>Expense-reimbursement business table migration. This script is idempotent and the application never runs it automatically at startup; it stores only the applicant, category, amount, reason and invoice attachment name, and stores no passwords, cookies, tokens, connection strings, government ids, compensation data, or other high-sensitivity personal data.</en>
-- </lang>

-- <lang>
--   <zh-CN>启用标准 NULL 比较语义，保证金额约束中的 NULL 分支按 SQL Server 基线执行。</zh-CN>
--   <en>Enable standard NULL comparison semantics so NULL branches in the amount constraint execute on the SQL Server baseline.</en>
-- </lang>
SET ANSI_NULLS ON
GO

-- <lang>
--   <zh-CN>启用引号标识符，保护报销表 DDL 对象名和约束名稳定解析。</zh-CN>
--   <en>Enable quoted identifiers so reimbursement DDL object and constraint names parse consistently.</en>
-- </lang>
SET QUOTED_IDENTIFIER ON
GO

-- <lang>
--   <zh-CN>费用报销归属于门户用户；缺少用户表时停止迁移，避免创建无归属人的报销结构。</zh-CN>
--   <en>Reimbursements belong to portal users; stop migration when the user table is missing to avoid creating an ownerless reimbursement structure.</en>
-- </lang>
IF OBJECT_ID(N'[dbo].[Portal_Users]', N'U') IS NULL
BEGIN
    RAISERROR(N'Portal_Users must exist before PortalBiz_ExpenseReimbursements.', 16, 1)
    RETURN
END
GO

-- <lang>
--   <zh-CN>建表保护让重复执行不会重建既有报销单、状态或索引。</zh-CN>
--   <en>The create-table guard prevents repeated execution from rebuilding existing reimbursements, status or indexes.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_ExpenseReimbursements]') AND type IN (N'U'))
BEGIN
    CREATE TABLE dbo.PortalBiz_ExpenseReimbursements (
        RequestId BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        Category NVARCHAR(20) NOT NULL,
        Amount DECIMAL(12,2) NOT NULL,
        Reason NVARCHAR(500) NOT NULL,
        AttachmentName NVARCHAR(260) NULL,
        Status NVARCHAR(16) NOT NULL CONSTRAINT DF_PortalBiz_ExpenseReimbursements_Status DEFAULT (N'Pending'),
        CreatedUtc DATETIME2 NOT NULL CONSTRAINT DF_PortalBiz_ExpenseReimbursements_CreatedUtc DEFAULT (SYSUTCDATETIME()),
        RowVersion ROWVERSION,
        CONSTRAINT CK_PortalBiz_ExpenseReimbursements_AmountPositive CHECK (Amount > 0),
        CONSTRAINT FK_PortalBiz_ExpenseReimbursements_User FOREIGN KEY (UserId) REFERENCES dbo.Portal_Users (UserId)
    );

    CREATE INDEX IX_PortalBiz_ExpenseReimbursements_User ON dbo.PortalBiz_ExpenseReimbursements (UserId, CreatedUtc DESC);
END
GO
