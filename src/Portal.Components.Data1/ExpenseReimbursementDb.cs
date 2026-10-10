using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SqlClient;
using System.Linq;
using Portal;
using Portal.Components;

namespace Portal.Components.Data1
{
    /// <summary><lang>
    ///   <zh-CN>费用报销数据访问实现；业务数据落在 PortalBiz_ExpenseReimbursements（与员工资料更正同构，独立于结构配置库）。</zh-CN>
    ///   <en>Expense-reimbursement data-access implementation; business data lives in PortalBiz_ExpenseReimbursements (isomorphic to employee-profile correction, isolated from the structural config store).</en>
    /// </lang></summary>
    [Export(typeof(IExpenseReimbursementDb))]
    public sealed class ExpenseReimbursementDb : IExpenseReimbursementDb
    {
        private const string TableName = "PortalBiz_ExpenseReimbursements";

        private static bool HasTable(PortalBizDbContext ctx, string name)
        {
            try
            {
                var cnt = ctx.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @Name;",
                    new SqlParameter("@Name", name)).SingleOrDefault();
                return cnt > 0;
            }
            catch
            {
                return false;
            }
        }

        public bool IsSchemaAvailable()
        {
            try
            {
                using (var ctx = new PortalBizDbContext())
                {
                    return HasTable(ctx, TableName);
                }
            }
            catch (Exception ex)
            {
                PortalErrorLog.Record(ex, "ExpenseReimbursementDb.IsSchemaAvailable");
                return false;
            }
        }

        public ExpenseReimbursementResult CreateRequest(ExpenseReimbursementCreateRequest request)
        {
            try
            {
                using (var ctx = new PortalBizDbContext())
                {
                    var id = ctx.Database.SqlQuery<long>(
                        "INSERT INTO dbo.PortalBiz_ExpenseReimbursements (UserId, Category, Amount, Reason, AttachmentName, Status, CreatedUtc) " +
                        "OUTPUT INSERTED.RequestId VALUES (@UserId, @Category, @Amount, @Reason, @AttachmentName, N'Pending', SYSUTCDATETIME());",
                        new SqlParameter("@UserId", request.UserId),
                        new SqlParameter("@Category", (object)request.Category ?? DBNull.Value),
                        new SqlParameter("@Amount", request.Amount),
                        new SqlParameter("@Reason", (object)request.Reason ?? DBNull.Value),
                        new SqlParameter("@AttachmentName", (object)request.AttachmentName ?? DBNull.Value)).Single();
                    return new ExpenseReimbursementResult { Ok = true, RequestId = id };
                }
            }
            catch (Exception ex)
            {
                PortalErrorLog.Record(ex, "ExpenseReimbursementDb.CreateRequest");
                return new ExpenseReimbursementResult { Ok = false, ErrorCode = "DbError" };
            }
        }

        public IReadOnlyList<ExpenseReimbursementInfo> GetRecentByUser(int userId, int limit)
        {
            try
            {
                using (var ctx = new PortalBizDbContext())
                {
                    return ctx.Database.SqlQuery<ExpenseReimbursementInfo>(
                        "SELECT TOP (@Limit) RequestId, Category, Amount, Reason, AttachmentName, Status, CreatedUtc " +
                        "FROM dbo.PortalBiz_ExpenseReimbursements WHERE UserId = @UserId ORDER BY CreatedUtc DESC;",
                        new SqlParameter("@Limit", limit),
                        new SqlParameter("@UserId", userId)).ToList();
                }
            }
            catch (Exception ex)
            {
                PortalErrorLog.Record(ex, "ExpenseReimbursementDb.GetRecentByUser");
                return new List<ExpenseReimbursementInfo>();
            }
        }
    }
}
