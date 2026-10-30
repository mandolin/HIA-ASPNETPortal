-- <lang>
--   <zh-CN>为请假申请表补后台审核所需的审核人/时间/意见列（D23：后台审核页）。幂等，可重复执行。</zh-CN>
--   <en>Adds reviewer/time/comment columns required by the admin review page (D23) to the leave-request table. Idempotent, safe to re-run.</en>
-- </lang>
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PortalBiz_LeaveRequests]') AND name = N'ReviewedByUserId')
BEGIN
    ALTER TABLE [dbo].[PortalBiz_LeaveRequests]
        ADD [ReviewedByUserId] INT NULL,
            [ReviewedBy] NVARCHAR(100) NULL,
            [ReviewedUtc] DATETIME2(0) NULL,
            [ReviewComment] NVARCHAR(1000) NULL;
END
GO
