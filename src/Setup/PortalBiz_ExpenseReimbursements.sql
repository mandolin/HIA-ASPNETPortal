-- <lang>
--   <zh-CN>费用报销业务表。与员工资料更正同构：独立于结构配置库、幂等可重复执行、含 FK/CHECK/索引。</zh-CN>
--   <en>Expense-reimbursement business table. Isomorphic to employee-profile correction: isolated from the structural config store, idempotent, with FK/CHECK/indexes.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'PortalBiz_ExpenseReimbursements')
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
