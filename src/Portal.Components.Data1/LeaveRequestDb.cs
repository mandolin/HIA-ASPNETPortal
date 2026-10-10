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
    ///   <zh-CN>请假申请数据访问实现；业务数据落在 PortalBiz_LeaveRequests（与员工资料更正同构，独立于结构配置库）。</zh-CN>
    ///   <en>Leave-request data-access implementation; business data lives in PortalBiz_LeaveRequests (isomorphic to employee-profile correction, isolated from the structural config store).</en>
    /// </lang></summary>
    [Export(typeof(ILeaveRequestDb))]
    public sealed class LeaveRequestDb : ILeaveRequestDb
    {
        private const string TableName = "PortalBiz_LeaveRequests";

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
                PortalErrorLog.Record(ex, "LeaveRequestDb.IsSchemaAvailable");
                return false;
            }
        }

        public LeaveRequestResult CreateRequest(LeaveRequestCreateRequest request)
        {
            try
            {
                using (var ctx = new PortalBizDbContext())
                {
                    var id = ctx.Database.SqlQuery<long>(
                        "INSERT INTO dbo.PortalBiz_LeaveRequests (UserId, LeaveType, StartDate, EndDate, Days, Reason, AttachmentName, Status, CreatedUtc) " +
                        "OUTPUT INSERTED.RequestId VALUES (@UserId, @LeaveType, @StartDate, @EndDate, @Days, @Reason, @AttachmentName, N'Pending', SYSUTCDATETIME());",
                        new SqlParameter("@UserId", request.UserId),
                        new SqlParameter("@LeaveType", (object)request.LeaveType ?? DBNull.Value),
                        new SqlParameter("@StartDate", request.StartDate),
                        new SqlParameter("@EndDate", request.EndDate),
                        new SqlParameter("@Days", request.Days),
                        new SqlParameter("@Reason", (object)request.Reason ?? DBNull.Value),
                        new SqlParameter("@AttachmentName", (object)request.AttachmentName ?? DBNull.Value)).Single();
                    return new LeaveRequestResult { Ok = true, RequestId = id };
                }
            }
            catch (Exception ex)
            {
                PortalErrorLog.Record(ex, "LeaveRequestDb.CreateRequest");
                return new LeaveRequestResult { Ok = false, ErrorCode = "DbError" };
            }
        }

        public IReadOnlyList<LeaveRequestInfo> GetRecentByUser(int userId, int limit)
        {
            try
            {
                using (var ctx = new PortalBizDbContext())
                {
                    return ctx.Database.SqlQuery<LeaveRequestInfo>(
                        "SELECT TOP (@Limit) RequestId, LeaveType, StartDate, EndDate, Days, Reason, AttachmentName, Status, CreatedUtc " +
                        "FROM dbo.PortalBiz_LeaveRequests WHERE UserId = @UserId ORDER BY CreatedUtc DESC;",
                        new SqlParameter("@Limit", limit),
                        new SqlParameter("@UserId", userId)).ToList();
                }
            }
            catch (Exception ex)
            {
                PortalErrorLog.Record(ex, "LeaveRequestDb.GetRecentByUser");
                return new List<LeaveRequestInfo>();
            }
        }
    }
}
