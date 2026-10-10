using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary><lang>
    ///   <zh-CN>请假申请数据访问实现；业务数据落在 PortalBiz_LeaveRequests（与员工资料更正同构，独立于结构配置库）。PortalBizDbContext 由 Unity 以单例形式注入外置连接串。</zh-CN>
    ///   <en>Leave-request data-access implementation; business data lives in PortalBiz_LeaveRequests (isomorphic to employee-profile correction, isolated from the structural config store). PortalBizDbContext is injected by Unity as a singleton carrying the external connection string.</en>
    /// </lang></summary>
    public sealed class LeaveRequestDb : ILeaveRequestDb
    {
        private const string TableName = "PortalBiz_LeaveRequests";
        private readonly PortalBizDbContext context;

        /// <summary><lang><zh-CN>通过 Unity 构造函数注入企业业务基础数据上下文。</zh-CN><en>Enterprise business foundation data context injected by Unity via constructor.</en></lang></summary>
        /// <param name="context"><l><zh-CN>企业业务基础数据上下文。</zh-CN><en>Enterprise business foundation data context.</en></l></param>
        public LeaveRequestDb(PortalBizDbContext context)
        {
            this.context = context;
        }

        private bool HasTable(string name)
        {
            try
            {
                var cnt = context.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @Name;",
                    new SqlParameter("@Name", name)).SingleOrDefault();
                return cnt > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary><lang>
        ///   <zh-CN>探测业务表 <c>PortalBiz_LeaveRequests</c> 是否存在，作为降级与运行期装配的前置检查。</zh-CN>
        ///   <en>Probes whether the business table <c>PortalBiz_LeaveRequests</c> exists, used as a precondition for graceful degradation and runtime assembly.</en>
        /// </lang></summary>
        /// <returns><l><zh-CN>表存在返回 <c>true</c>，否则 <c>false</c>（含探测异常）。</zh-CN><en><c>true</c> when the table exists, otherwise <c>false</c> (including probe exceptions).</en></l></returns>
        public bool IsSchemaAvailable()
        {
            try
            {
                return HasTable(TableName);
            }
            catch
            {
                return false;
            }
        }

        /// <summary><lang>
        ///   <zh-CN>插入一条请假申请，状态固定为 <c>Pending</c>，并以 <c>OUTPUT INSERTED</c> 取回自增主键。</zh-CN>
        ///   <en>Inserts a leave request with status fixed to <c>Pending</c> and returns the identity key via <c>OUTPUT INSERTED</c>.</en>
        /// </lang></summary>
        /// <param name="request"><l><zh-CN>请假申请创建请求（含申请人、类型、起止日期、天数、事由与附件名）。</zh-CN><en>Leave-request creation request (applicant, type, start/end dates, days, reason, attachment name).</en></l></param>
        /// <returns><l><zh-CN>成功时 <see cref="LeaveRequestResult.Ok"/> 为 <c>true</c> 且携带新主键；失败返回低敏失败结果。</zh-CN><en>On success <see cref="LeaveRequestResult.Ok"/> is <c>true</c> with the new identity key; otherwise a low-sensitivity failure result.</en></l></returns>
        public LeaveRequestResult CreateRequest(LeaveRequestCreateRequest request)
        {
            try
            {
                var id = context.Database.SqlQuery<long>(
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
            catch
            {
                return new LeaveRequestResult { Ok = false, ErrorCode = "DbError" };
            }
        }

        /// <summary><lang>
        ///   <zh-CN>按申请人取最近若干条请假申请（按创建时间倒序），供模块“我的请假”列表展示。</zh-CN>
        ///   <en>Returns the most recent leave requests for a user (descending by creation time) for the module's "My Leave" list.</en>
        /// </lang></summary>
        /// <param name="userId"><l><zh-CN>申请人用户标识。</zh-CN><en>Applicant user identifier.</en></l></param>
        /// <param name="limit"><l><zh-CN>返回条数上限。</zh-CN><en>Maximum number of rows to return.</en></l></param>
        /// <returns><l><zh-CN>只读请假申请列表；查询异常时返回空列表而非抛错。</zh-CN><en>A read-only list of leave requests; returns an empty list on query failure instead of throwing.</en></l></returns>
        public IReadOnlyList<LeaveRequestInfo> GetRecentByUser(int userId, int limit)
        {
            try
            {
                return context.Database.SqlQuery<LeaveRequestInfo>(
                    "SELECT TOP (@Limit) RequestId, LeaveType, StartDate, EndDate, Days, Reason, AttachmentName, Status, CreatedUtc " +
                    "FROM dbo.PortalBiz_LeaveRequests WHERE UserId = @UserId ORDER BY CreatedUtc DESC;",
                    new SqlParameter("@Limit", limit),
                    new SqlParameter("@UserId", userId)).ToList();
            }
            catch
            {
                return new List<LeaveRequestInfo>();
            }
        }
    }
}
