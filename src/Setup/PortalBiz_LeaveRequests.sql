-- <lang>
--   <zh-CN>请假申请业务表。与员工资料更正同构：独立于结构配置库、幂等可重复执行、含 FK/CHECK/索引。</zh-CN>
--   <en>Leave-request business table. Isomorphic to employee-profile correction: isolated from the structural config store, idempotent, with FK/CHECK/indexes.</en>
-- </lang>
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'PortalBiz_LeaveRequests')
BEGIN
    CREATE TABLE dbo.PortalBiz_LeaveRequests (
        RequestId BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        LeaveType NVARCHAR(20) NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NOT NULL,
        Days DECIMAL(5,1) NOT NULL,
        Reason NVARCHAR(500) NOT NULL,
        AttachmentName NVARCHAR(260) NULL,
        Status NVARCHAR(16) NOT NULL CONSTRAINT DF_PortalBiz_LeaveRequests_Status DEFAULT (N'Pending'),
        CreatedUtc DATETIME2 NOT NULL CONSTRAINT DF_PortalBiz_LeaveRequests_CreatedUtc DEFAULT (SYSUTCDATETIME()),
        RowVersion ROWVERSION,
        CONSTRAINT CK_PortalBiz_LeaveRequests_DaysPositive CHECK (Days > 0),
        CONSTRAINT CK_PortalBiz_LeaveRequests_EndAfterStart CHECK (EndDate >= StartDate),
        CONSTRAINT FK_PortalBiz_LeaveRequests_User FOREIGN KEY (UserId) REFERENCES dbo.Portal_Users (UserId)
    );

    CREATE INDEX IX_PortalBiz_LeaveRequests_User ON dbo.PortalBiz_LeaveRequests (UserId, CreatedUtc DESC);
END
GO
