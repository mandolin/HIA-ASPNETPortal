using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>基于 <see cref="PortalBizDbContext"/> 的员工资料更正请求数据访问实现。</zh-CN>
    ///   <en>Employee-profile correction-request data-access implementation backed by <see cref="PortalBizDbContext"/>.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>此实现只写入请求和管理员处理状态，不直接修改员工主数据。员工主数据修改仍应通过员工目录后台或正式审批机制完成。读取路径在缺表或异常时软失败，写入路径返回低敏失败消息，详细异常由调用方按场景写诊断日志。</zh-CN>
    ///   <en>This implementation writes only requests and administrator review states. Employee master-data changes must still be performed through the employee-directory administration area or the formal approval workflow. Read paths fail softly when schema is missing or unavailable; write paths return low-sensitivity failures and leave detailed diagnostics to callers.</en>
    /// </lang>
    /// </remarks>
    public sealed class EmployeeProfileCorrectionRequestDb : IEmployeeProfileCorrectionRequestDb
    {
        // <lang>
        //   <zh-CN>表名常量只用于当前 SQL Server 实现；多数据库方言阶段会把这些稳定名映射到 provider-specific 查询。</zh-CN>
        //   <en>Table-name constants are used only by the current SQL Server implementation; the multi-provider phase will map these stable names to provider-specific queries.</en>
        // </lang>
        private const string RequestTableName = "PortalBiz_EmployeeProfileCorrectionRequests";
        private const string EmployeeTableName = "PortalBiz_Employees";
        private const string BindingTableName = "PortalBiz_UserEmployeeBindings";
        private const string UserTableName = "Portal_Users";
        private readonly PortalBizDbContext context;

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工主数据管理门面，供审核通过后回写更正结果；可为 null，此时回写路径直接失败而不是回退到旧的无回写行为。</zh-CN>
        ///   <en>Employee master-data administration facade used to write back an approved correction; it may be null, in which case the write-back path fails instead of silently reverting to the previous no-write behavior.</en>
        /// </lang>
        /// </summary>
        private readonly IEmployeeDirectoryAdminDb employeeAdminDb;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化员工资料更正请求数据访问实现。</zh-CN>
        ///   <en>Initializes the employee-profile correction-request data-access implementation.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>企业业务基础数据上下文。</zh-CN>
        ///   <en>Enterprise business foundation data context.</en>
        /// </l>
        /// </param>
        /// <param name="employeeAdminDb">
        /// <l>
        ///   <zh-CN>员工主数据管理门面；容器自动装配。为空时审核通过无法回写主数据，会以明确失败返回。</zh-CN>
        ///   <en>Employee master-data administration facade, wired automatically by the container. When it is null, an approval cannot write master data back and returns an explicit failure.</en>
        /// </l>
        /// </param>
        public EmployeeProfileCorrectionRequestDb(PortalBizDbContext context, IEmployeeDirectoryAdminDb employeeAdminDb = null)
        {
            this.context = context;
            this.employeeAdminDb = employeeAdminDb;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查更正请求表和依赖的员工绑定基础表是否可用。</zh-CN>
        ///   <en>Checks whether the correction-request table and required employee-binding foundation tables are available.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>相关表可用时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the related tables are available.</en>
        /// </l>
        /// </returns>
        public bool IsSchemaAvailable()
        {
            // <lang>
            //   <zh-CN>资料更正模块依赖请求、员工、绑定和旧用户表；任一表缺失都返回不可用，页面据此显示低敏提示。</zh-CN>
            //   <en>The correction module depends on request, employee, binding and legacy user tables; any missing table makes the module unavailable and lets the page show a low-sensitivity message.</en>
            // </lang>
            return HasTable(RequestTableName) &&
                   HasTable(EmployeeTableName) &&
                   HasTable(BindingTableName) &&
                   HasTable(UserTableName);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定门户用户当前可提交更正请求的员工资料。</zh-CN>
        ///   <en>Reads the employee profile for which the specified Portal user may submit a correction request.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>门户用户标识。</zh-CN>
        ///   <en>Portal user identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>当前资料视图；缺表、未绑定或非 Active 状态时为空。</zh-CN>
        ///   <en>Current profile view, or null when schema is missing, no active binding exists, or the employee is not active.</en>
        /// </l>
        /// </returns>
        public EmployeeProfileCorrectionProfileView GetCurrentProfileForUser(int userId)
        {
            if (userId <= 0 || !IsSchemaAvailable())
            {
                return null;
            }

            try
            {
                // <lang>
                //   <zh-CN>读取当前用户最近的 Active 员工绑定，且员工自身也必须 Active；页面展示资料不信任客户端传入的员工标识。</zh-CN>
                //   <en>Read the current user's latest active employee binding and require the employee to be active as well; profile display does not trust an employee id supplied by the client.</en>
                // </lang>
                ProfileProjection row = context.Database.SqlQuery<ProfileProjection>(
                    @"
SELECT TOP (1)
    [Employee].[EmployeeId],
    [Employee].[EmployeeCode],
    [Employee].[DisplayName],
    [Employee].[PreferredName],
    [Employee].[WorkEmail],
    [Organization].[DisplayName] AS [OrganizationDisplayName],
    [Employee].[EmploymentStatus],
    [Binding].[BindingId],
    [Binding].[BoundUtc]
FROM [dbo].[PortalBiz_UserEmployeeBindings] AS [Binding]
INNER JOIN [dbo].[PortalBiz_Employees] AS [Employee]
    ON [Employee].[EmployeeId] = [Binding].[EmployeeId]
LEFT JOIN [dbo].[PortalBiz_OrganizationUnits] AS [Organization]
    ON [Organization].[OrganizationUnitId] = [Employee].[OrganizationUnitId]
WHERE [Binding].[UserId] = @p0
  AND [Binding].[BindingStatus] = N'Active'
  AND [Employee].[EmploymentStatus] = N'Active'
ORDER BY [Binding].[BoundUtc] DESC, [Binding].[BindingId] DESC;",
                    userId).SingleOrDefault();

                return row == null
                    ? null
                    : new EmployeeProfileCorrectionProfileView(
                        row.EmployeeId,
                        row.EmployeeCode,
                        row.DisplayName,
                        row.PreferredName,
                        row.WorkEmail,
                        row.OrganizationDisplayName,
                        row.EmploymentStatus,
                        row.BindingId,
                        row.BoundUtc);
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>只读资料入口软失败，避免缺表或迁移中的数据库状态打断前台门户首页。</zh-CN>
                //   <en>The read-only profile entry fails softly so missing tables or in-progress migrations do not break the portal home page.</en>
                // </lang>
                return null;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定门户用户最近提交的更正请求。</zh-CN>
        ///   <en>Reads recent correction requests submitted by the specified Portal user.</en>
        /// </lang>
        /// </summary>
        /// <param name="userId">
        /// <l>
        ///   <zh-CN>门户用户标识。</zh-CN>
        ///   <en>Portal user identifier.</en>
        /// </l>
        /// </param>
        /// <param name="take">
        /// <l>
        ///   <zh-CN>最多返回条数。</zh-CN>
        ///   <en>Maximum number of rows to return.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求列表。</zh-CN>
        ///   <en>Request list.</en>
        /// </l>
        /// </returns>
        public IList<EmployeeProfileCorrectionRequestInfo> GetRecentRequestsForUser(int userId, int take)
        {
            if (userId <= 0 || !IsSchemaAvailable())
            {
                return new List<EmployeeProfileCorrectionRequestInfo>();
            }

            int safeTake = NormalizeTake(take, 10);
            try
            {
                // <lang>
                //   <zh-CN>用户侧最近请求只按当前用户过滤，避免员工多人绑定或历史绑定时串看到其他账号的申请。</zh-CN>
                //   <en>User-facing recent requests are filtered by current user to avoid leaking another account's requests when employees have multiple or historical bindings.</en>
                // </lang>
                return context.Database.SqlQuery<EmployeeProfileCorrectionRequestInfo>(
                    @"
SELECT TOP (@Take)
    [Request].[RequestId],
    [Request].[EmployeeId],
    [Employee].[EmployeeCode],
    [Employee].[DisplayName] AS [EmployeeDisplayName],
    [Request].[UserId],
    [User].[Name] AS [UserName],
    [Request].[BindingId],
    [Request].[SubmittedUtc],
    [Request].[SubmittedBy],
    [Request].[FieldName],
    [Request].[CurrentValueSnapshot],
    [Request].[ProposedValue],
    [Request].[RequestNote],
    [Request].[RequestStatus],
    [Request].[ReviewedUtc],
    [Request].[ReviewedBy],
    [Request].[ReviewNote]
FROM [dbo].[PortalBiz_EmployeeProfileCorrectionRequests] AS [Request]
INNER JOIN [dbo].[PortalBiz_Employees] AS [Employee]
    ON [Employee].[EmployeeId] = [Request].[EmployeeId]
INNER JOIN [dbo].[Portal_Users] AS [User]
    ON [User].[UserID] = [Request].[UserId]
WHERE [Request].[UserId] = @UserId
ORDER BY [Request].[SubmittedUtc] DESC, [Request].[RequestId] DESC;",
                    new SqlParameter("@Take", safeTake),
                    new SqlParameter("@UserId", userId)).ToList();
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>历史请求列表读取失败时返回空集合，页面保持可用；提交动作仍会单独校验并返回明确失败。</zh-CN>
                //   <en>When recent-request reading fails, return an empty list so the page remains usable; submission still validates independently and returns explicit failures.</en>
                // </lang>
                return new List<EmployeeProfileCorrectionRequestInfo>();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交一条员工资料更正请求。</zh-CN>
        ///   <en>Submits one employee-profile correction request.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>提交请求。</zh-CN>
        ///   <en>Submission request.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>写入结果。</zh-CN>
        ///   <en>Write result.</en>
        /// </l>
        /// </returns>
        public EmployeeProfileCorrectionRequestResult SubmitRequest(EmployeeProfileCorrectionSubmitRequest request)
        {
            // <lang>
            //   <zh-CN>提交入口先统一归一化，保证后面的白名单、必填和 SQL 参数都基于裁剪后的稳定值。</zh-CN>
            //   <en>The submission entry normalizes first so allow-list checks, required checks and SQL parameters all use trimmed stable values.</en>
            // </lang>
            EmployeeProfileCorrectionSubmitRequest normalized = NormalizeSubmitRequest(request);
            if (normalized.UserId <= 0 || normalized.EmployeeId <= 0 || normalized.BindingId <= 0)
            {
                return new EmployeeProfileCorrectionRequestResult(false, 0, "A signed-in user with an active employee binding is required.");
            }

            if (!IsAllowedFieldName(normalized.FieldName))
            {
                return new EmployeeProfileCorrectionRequestResult(false, 0, "Select a supported profile field.");
            }

            if (string.IsNullOrWhiteSpace(normalized.ProposedValue))
            {
                return new EmployeeProfileCorrectionRequestResult(false, 0, "Proposed value is required.");
            }

            if (!IsSchemaAvailable())
            {
                return new EmployeeProfileCorrectionRequestResult(false, 0, "Employee profile correction schema is unavailable.");
            }

            try
            {
                // <lang>
                //   <zh-CN>插入时重新联查 Active 绑定和 Active 员工，并在 SQL 中截取当前值快照，避免页面提交过期或伪造的当前值。</zh-CN>
                //   <en>The insert re-joins the active binding and active employee, and captures the current value snapshot in SQL so stale or forged page values are not trusted.</en>
                // </lang>
                List<long> rows = context.Database.SqlQuery<long>(
                    @"
DECLARE @Inserted TABLE
(
    [RequestId] BIGINT NOT NULL
);

INSERT INTO [dbo].[PortalBiz_EmployeeProfileCorrectionRequests]
    ([EmployeeId],
     [UserId],
     [BindingId],
     [SubmittedUtc],
     [SubmittedBy],
     [FieldName],
     [CurrentValueSnapshot],
     [ProposedValue],
     [RequestNote],
     [RequestStatus])
OUTPUT INSERTED.[RequestId] INTO @Inserted
SELECT TOP (1)
    [Employee].[EmployeeId],
    [Binding].[UserId],
    [Binding].[BindingId],
    @SubmittedUtc,
    @SubmittedBy,
    @FieldName,
    CASE @FieldName
        WHEN N'DisplayName' THEN [Employee].[DisplayName]
        WHEN N'PreferredName' THEN [Employee].[PreferredName]
        WHEN N'WorkEmail' THEN [Employee].[WorkEmail]
        WHEN N'OrganizationDisplayName' THEN [Organization].[DisplayName]
        ELSE NULL
    END,
    @ProposedValue,
    @RequestNote,
    N'Submitted'
FROM [dbo].[PortalBiz_UserEmployeeBindings] AS [Binding]
INNER JOIN [dbo].[PortalBiz_Employees] AS [Employee]
    ON [Employee].[EmployeeId] = [Binding].[EmployeeId]
LEFT JOIN [dbo].[PortalBiz_OrganizationUnits] AS [Organization]
    ON [Organization].[OrganizationUnitId] = [Employee].[OrganizationUnitId]
WHERE [Binding].[UserId] = @UserId
  AND [Employee].[EmployeeId] = @EmployeeId
  AND [Binding].[BindingId] = @BindingId
  AND [Binding].[BindingStatus] = N'Active'
  AND [Employee].[EmploymentStatus] = N'Active';

SELECT [RequestId] FROM @Inserted;",
                    new SqlParameter("@UserId", normalized.UserId),
                    new SqlParameter("@EmployeeId", normalized.EmployeeId),
                    new SqlParameter("@BindingId", normalized.BindingId),
                    new SqlParameter("@SubmittedUtc", normalized.SubmittedUtc.Value),
                    new SqlParameter("@SubmittedBy", normalized.SubmittedBy),
                    new SqlParameter("@FieldName", normalized.FieldName),
                    new SqlParameter("@ProposedValue", normalized.ProposedValue),
                    CreateNullableStringParameter("@RequestNote", normalized.RequestNote)).ToList();

                long requestId = rows.Count == 0 ? 0 : rows[0];
                if (requestId <= 0)
                {
                    return new EmployeeProfileCorrectionRequestResult(false, 0, "No active employee profile is available for correction.");
                }

                return new EmployeeProfileCorrectionRequestResult(true, requestId, "Employee profile correction request submitted.");
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>写入异常返回统一低敏失败消息；调用页面可结合操作上下文写 `PortalDiagnostics` 事件编号。</zh-CN>
                //   <en>Write exceptions return one low-sensitivity failure message; the calling page may log a `PortalDiagnostics` event id with operation context.</en>
                // </lang>
                return new EmployeeProfileCorrectionRequestResult(false, 0, "Employee profile correction request failed.");
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按状态读取后台处理列表。</zh-CN>
        ///   <en>Reads an administrator review list by status.</en>
        /// </lang>
        /// </summary>
        /// <param name="status">
        /// <l>
        ///   <zh-CN>状态筛选；空值表示全部。</zh-CN>
        ///   <en>Status filter; empty means all statuses.</en>
        /// </l>
        /// </param>
        /// <param name="take">
        /// <l>
        ///   <zh-CN>最多返回条数。</zh-CN>
        ///   <en>Maximum number of rows to return.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求列表。</zh-CN>
        ///   <en>Request list.</en>
        /// </l>
        /// </returns>
        public IList<EmployeeProfileCorrectionRequestInfo> GetAdminRequests(string status, int take)
        {
            if (!IsSchemaAvailable())
            {
                return new List<EmployeeProfileCorrectionRequestInfo>();
            }

            string normalizedStatus = NormalizeStatusFilter(status);
            int safeTake = NormalizeTake(take, 50);
            try
            {
                // <lang>
                //   <zh-CN>后台列表按状态可选过滤，默认只取有限条数，避免旧 WebForms 页面一次绑定过多记录。</zh-CN>
                //   <en>The admin list optionally filters by status and always limits rows so the legacy WebForms page does not bind too many records at once.</en>
                // </lang>
                return context.Database.SqlQuery<EmployeeProfileCorrectionRequestInfo>(
                    @"
SELECT TOP (@Take)
    [Request].[RequestId],
    [Request].[EmployeeId],
    [Employee].[EmployeeCode],
    [Employee].[DisplayName] AS [EmployeeDisplayName],
    [Request].[UserId],
    [User].[Name] AS [UserName],
    [Request].[BindingId],
    [Request].[SubmittedUtc],
    [Request].[SubmittedBy],
    [Request].[FieldName],
    [Request].[CurrentValueSnapshot],
    [Request].[ProposedValue],
    [Request].[RequestNote],
    [Request].[RequestStatus],
    [Request].[ReviewedUtc],
    [Request].[ReviewedBy],
    [Request].[ReviewNote]
FROM [dbo].[PortalBiz_EmployeeProfileCorrectionRequests] AS [Request]
INNER JOIN [dbo].[PortalBiz_Employees] AS [Employee]
    ON [Employee].[EmployeeId] = [Request].[EmployeeId]
INNER JOIN [dbo].[Portal_Users] AS [User]
    ON [User].[UserID] = [Request].[UserId]
WHERE (@Status = N'' OR [Request].[RequestStatus] = @Status)
ORDER BY [Request].[SubmittedUtc] DESC, [Request].[RequestId] DESC;",
                    new SqlParameter("@Take", safeTake),
                    new SqlParameter("@Status", normalizedStatus)).ToList();
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>后台查询失败时返回空集合，由页面级提示和诊断日志承接，不把 SQL 细节直接暴露到浏览器。</zh-CN>
                //   <en>When admin querying fails, return an empty list and let page-level messaging and diagnostics handle it without exposing SQL details to the browser.</en>
                // </lang>
                return new List<EmployeeProfileCorrectionRequestInfo>();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>撤回本人提出的、尚在处理中的更正请求。</zh-CN>
        ///   <en>Withdraws a still-pending correction request submitted by the requesting user.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>请求表状态受检查约束限定为 <c>Submitted</c>/<c>Reviewed</c>/<c>Closed</c>/<c>Rejected</c>，且非 <c>Submitted</c> 时必须同时具备审核时间与审核人。因此撤回复用 <c>Closed</c> 并把提交人本人记为操作人，在备注中留痕：<b>无需任何架构迁移</b>，也不引入新的终态语义。归属不符、状态不允许或请求不存在一律失败且不改变任何行。</zh-CN>
        ///   <en>The request table constrains status to <c>Submitted</c>/<c>Reviewed</c>/<c>Closed</c>/<c>Rejected</c> and requires a review time and reviewer for any non-<c>Submitted</c> row. Withdrawal therefore reuses <c>Closed</c>, records the requester as the actor, and leaves a note: <b>no schema migration is needed</b> and no new terminal semantics are introduced. Ownership mismatch, a disallowed status, or a missing request all fail without changing any row.</en>
        /// </lang>
        /// </remarks>
        /// <param name="userId"><l><zh-CN>当前门户用户标识；非正值返回失败。</zh-CN><en>The current Portal user identifier; a non-positive value fails.</en></l></param>
        /// <param name="requestId"><l><zh-CN>更正请求标识；非正值返回失败。</zh-CN><en>The correction request identifier; a non-positive value fails.</en></l></param>
        /// <param name="actorName"><l><zh-CN>操作人账号名或系统标识；空白时使用 <c>system</c>。</zh-CN><en>Actor account name or system identifier; blank becomes <c>system</c>.</en></l></param>
        /// <returns><l><zh-CN>撤回结果；失败时包含低敏说明。</zh-CN><en>The withdrawal result; failures carry a low-sensitivity explanation.</en></l></returns>
        public EmployeeProfileCorrectionRequestResult CancelOwnRequest(int userId, long requestId, string actorName)
        {
            if (userId <= 0 || requestId <= 0)
            {
                return new EmployeeProfileCorrectionRequestResult(false, 0, "A valid user and correction request are required.");
            }

            if (!IsSchemaAvailable())
            {
                return new EmployeeProfileCorrectionRequestResult(false, 0, "Employee profile correction schema is unavailable.");
            }

            EmployeeProfileCorrectionRequestInfo pending = LoadRequestForReview(requestId);
            if (pending == null)
            {
                return new EmployeeProfileCorrectionRequestResult(false, 0, "Correction request was not found.");
            }

            // <lang>
            //   <zh-CN>归属校验必须严格相等：撤回权来自"这是本人提交的请求"，不匹配即拒绝，且不回显他人请求细节。</zh-CN>
            //   <en>Ownership must match exactly: the right to withdraw comes from "this is my own request"; a mismatch is rejected and no details of another user's request are echoed.</en>
            // </lang>
            if (pending.UserId != userId)
            {
                return new EmployeeProfileCorrectionRequestResult(false, 0, "Only the submitter can withdraw this correction request.");
            }

            // <lang>
            //   <zh-CN>只允许撤回仍在处理中的请求：已进入审核结论的终态不应被提交人改写。</zh-CN>
            //   <en>Only still-pending requests may be withdrawn: terminal review outcomes must not be rewritten by the submitter.</en>
            // </lang>
            if (!string.Equals(pending.RequestStatus, EmployeeProfileCorrectionRequestStatuses.Submitted, StringComparison.Ordinal))
            {
                return new EmployeeProfileCorrectionRequestResult(false, 0, "Only pending correction requests can be withdrawn.");
            }

            string actor = string.IsNullOrWhiteSpace(actorName) ? "system" : actorName.Trim();
            DateTime withdrawnUtc = DateTime.UtcNow;

            try
            {
                int affectedRows = context.Database.ExecuteSqlCommand(
                    @"
UPDATE [dbo].[PortalBiz_EmployeeProfileCorrectionRequests]
SET [RequestStatus] = @RequestStatus,
    [ReviewedUtc] = @ReviewedUtc,
    [ReviewedBy] = @ReviewedBy,
    [ReviewNote] = @ReviewNote
WHERE [RequestId] = @RequestId
  AND [UserId] = @UserId
  AND [RequestStatus] = @PendingStatus;",
                    new SqlParameter("@RequestStatus", EmployeeProfileCorrectionRequestStatuses.Closed),
                    new SqlParameter("@ReviewedUtc", withdrawnUtc),
                    new SqlParameter("@ReviewedBy", actor),
                    new SqlParameter("@ReviewNote", "Withdrawn by the requester."),
                    new SqlParameter("@RequestId", requestId),
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@PendingStatus", EmployeeProfileCorrectionRequestStatuses.Submitted));

                if (affectedRows <= 0)
                {
                    return new EmployeeProfileCorrectionRequestResult(false, 0, "The correction request could not be withdrawn.");
                }

                return new EmployeeProfileCorrectionRequestResult(true, requestId, "Correction request withdrawn.");
            }
            catch (Exception)
            {
                return new EmployeeProfileCorrectionRequestResult(false, 0, "The correction request could not be withdrawn.");
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取审核回写所需的请求要素（员工标识、字段名、建议值）。</zh-CN>
        ///   <en>Reads the request elements needed for review write-back (employee identifier, field name, proposed value).</en>
        /// </lang>
        /// </summary>
        /// <param name="requestId"><l><zh-CN>更正请求标识；非正数返回 <c>null</c>。</zh-CN><en>Correction request identifier; a non-positive value returns <c>null</c>.</en></l></param>
        /// <returns>
        /// <l>
        ///   <zh-CN>请求投影；不存在或读取失败时返回 <c>null</c>，调用方按失败处理。</zh-CN>
        ///   <en>The request projection; <c>null</c> when it does not exist or reading fails, which the caller treats as a failure.</en>
        /// </l>
        /// </returns>
        private EmployeeProfileCorrectionRequestInfo LoadRequestForReview(long requestId)
        {
            if (requestId <= 0)
            {
                return null;
            }

            try
            {
                // <lang>
                //   <zh-CN>只取回写判定所需的四列，避免把业务正文带入审核路径；复用公开 DTO 承接，不新增映射类型。</zh-CN>
                //   <en>Only the four columns needed for the write-back decision are read, keeping domain content out of the review path; the public DTO is reused so no new mapping type is added.</en>
                // </lang>
                return context.Database.SqlQuery<EmployeeProfileCorrectionRequestInfo>(
                    @"
SELECT TOP (1)
    [RequestId],
    [EmployeeId],
    [UserId],
    [FieldName],
    [ProposedValue],
    [RequestStatus]
FROM [dbo].[PortalBiz_EmployeeProfileCorrectionRequests]
WHERE [RequestId] = @RequestId;",
                    new SqlParameter("@RequestId", requestId)).FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>管理员更新更正请求处理状态。</zh-CN>
        ///   <en>Updates the administrator review status of a correction request.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>W69 起，审核通过（<c>Reviewed</c>）会先按服务端白名单把建议值回写到员工主数据，成功后再置位；回写失败、字段需人工处理或字段不受支持时一律不置位，请求保持原状态。组织显示名因需映射为组织标识且存在重名歧义，不自动回写。</zh-CN>
        ///   <en>From W69 onward, an approval (<c>Reviewed</c>) first writes the proposed value into employee master data according to the server-side allow list and only then sets the status; a failed write, a manually handled field, or an unsupported field all leave the status unchanged. The organization display name is never written back automatically because it must map to an organization identifier and display names may be ambiguous.</en>
        /// </lang>
        /// </remarks>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>处理请求。</zh-CN>
        ///   <en>Review request.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>处理结果。</zh-CN>
        ///   <en>Review result.</en>
        /// </l>
        /// </returns>
        public EmployeeProfileCorrectionRequestResult ReviewRequest(EmployeeProfileCorrectionReviewRequest request)
        {
            // <lang>
            //   <zh-CN>审核入口只改变请求状态和审核备注；当前不在此处直接写员工主数据，避免把审批和资料维护混成一个不可审计动作。</zh-CN>
            //   <en>The review entry changes only request state and review notes; it does not write employee master data here, keeping approval and profile maintenance as auditable separate actions.</en>
            // </lang>
            EmployeeProfileCorrectionReviewRequest normalized = NormalizeReviewRequest(request);
            if (normalized.RequestId <= 0)
            {
                return new EmployeeProfileCorrectionRequestResult(false, 0, "Correction request id is required.");
            }

            if (!IsReviewStatus(normalized.RequestStatus))
            {
                return new EmployeeProfileCorrectionRequestResult(false, 0, "Select a supported review status.");
            }

            if (!IsSchemaAvailable())
            {
                return new EmployeeProfileCorrectionRequestResult(false, 0, "Employee profile correction schema is unavailable.");
            }

            // <lang>
            //   <zh-CN>W69：审核通过（Reviewed）必须先产生业务结果——把建议值回写到员工主数据，成功后再置位；任何失败都不置位，杜绝"已批准但未生效"的静默不一致。</zh-CN>
            //   <en>W69: an approval (Reviewed) must first produce the business result by writing the proposed value into employee master data, and only then set the status; any failure leaves the status unchanged, eliminating the silent inconsistency of "approved but not applied".</en>
            // </lang>
            if (string.Equals(normalized.RequestStatus, EmployeeProfileCorrectionRequestStatuses.Reviewed, StringComparison.Ordinal))
            {
                EmployeeProfileCorrectionRequestInfo pending = LoadRequestForReview(normalized.RequestId);
                if (pending == null)
                {
                    return new EmployeeProfileCorrectionRequestResult(false, 0, "Correction request was not found.");
                }

                // <lang>
                //   <zh-CN>字段名必须由服务端再次校验：前端白名单可被绕过，未知字段一律拒绝。</zh-CN>
                //   <en>The field name must be re-validated server-side: the front-end allow list can be bypassed, so unknown fields are always rejected.</en>
                // </lang>
                if (EmployeeProfileCorrectionWriteBackPolicy.RequiresManualHandling(pending.FieldName))
                {
                    return new EmployeeProfileCorrectionRequestResult(false, 0, "This field requires manual maintenance and was not written back automatically.");
                }

                if (!EmployeeProfileCorrectionWriteBackPolicy.CanAutoApply(pending.FieldName))
                {
                    return new EmployeeProfileCorrectionRequestResult(false, 0, "This correction field is not supported.");
                }

                if (employeeAdminDb == null)
                {
                    return new EmployeeProfileCorrectionRequestResult(false, 0, "Employee master data administration is unavailable.");
                }

                IEmployeeInfo employee = employeeAdminDb.GetEmployeeById(pending.EmployeeId);
                if (employee == null)
                {
                    return new EmployeeProfileCorrectionRequestResult(false, 0, "The employee for this correction request was not found.");
                }

                // <lang>
                //   <zh-CN>回写是全量更新且带乐观并发条件，因此必须带齐现有值与加载时的更新时间戳；否则既会覆盖其它字段，也会因缺少时间戳失败。</zh-CN>
                //   <en>The write-back is a full update guarded by optimistic concurrency, so it must carry the current values and the update timestamp from load time; otherwise it would overwrite other fields and fail the timestamp check.</en>
                // </lang>
                EmployeeSaveRequest saveRequest = new EmployeeSaveRequest
                {
                    EmployeeId = employee.EmployeeId,
                    EmployeeCode = employee.EmployeeCode,
                    DisplayName = employee.DisplayName,
                    PreferredName = employee.PreferredName,
                    WorkEmail = employee.WorkEmail,
                    OrganizationUnitId = employee.OrganizationUnitId,
                    EmploymentStatus = employee.EmploymentStatus,
                    JoinedUtc = employee.JoinedUtc,
                    LeftUtc = employee.LeftUtc,
                    SourceSystem = employee.SourceSystem,
                    OriginalUpdatedUtc = employee.UpdatedUtc,
                    ActorName = normalized.ReviewedBy
                };

                if (!EmployeeProfileCorrectionWriteBackPolicy.TryApply(saveRequest, pending.FieldName, pending.ProposedValue))
                {
                    return new EmployeeProfileCorrectionRequestResult(false, 0, "The proposed value could not be applied to employee master data.");
                }

                EmployeeDirectoryWriteResult writeResult = employeeAdminDb.SaveEmployee(saveRequest);
                if (!writeResult.Succeeded)
                {
                    return new EmployeeProfileCorrectionRequestResult(false, 0, "The approved correction could not be written to employee master data.");
                }
            }

            try
            {
                List<long> rows = context.Database.SqlQuery<long>(
                    @"
DECLARE @Updated TABLE
(
    [RequestId] BIGINT NOT NULL
);

UPDATE [dbo].[PortalBiz_EmployeeProfileCorrectionRequests]
SET [RequestStatus] = @RequestStatus,
    [ReviewedUtc] = @ReviewedUtc,
    [ReviewedBy] = @ReviewedBy,
    [ReviewNote] = @ReviewNote
OUTPUT INSERTED.[RequestId] INTO @Updated
WHERE [RequestId] = @RequestId;

SELECT [RequestId] FROM @Updated;",
                    new SqlParameter("@RequestId", normalized.RequestId),
                    new SqlParameter("@RequestStatus", normalized.RequestStatus),
                    new SqlParameter("@ReviewedUtc", normalized.ReviewedUtc.Value),
                    new SqlParameter("@ReviewedBy", normalized.ReviewedBy),
                    CreateNullableStringParameter("@ReviewNote", normalized.ReviewNote)).ToList();

                long requestId = rows.Count == 0 ? 0 : rows[0];
                if (requestId <= 0)
                {
                    return new EmployeeProfileCorrectionRequestResult(false, 0, "Correction request was not found.");
                }

                return new EmployeeProfileCorrectionRequestResult(true, requestId, "Correction request review state updated.");
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>审核写入异常同样返回低敏失败，由调用方记录事件编号并提示管理员复核。</zh-CN>
                //   <en>Review write exceptions also return a low-sensitivity failure; callers log an event id and ask administrators to review.</en>
                // </lang>
                return new EmployeeProfileCorrectionRequestResult(false, 0, "Correction request review failed.");
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查指定业务表是否存在。</zh-CN>
        ///   <en>Checks whether the specified business table exists.</en>
        /// </lang>
        /// </summary>
        /// <param name="tableName">
        /// <l>
        ///   <zh-CN>受控表名常量，不接受用户输入。</zh-CN>
        ///   <en>Controlled table-name constant; user input is not accepted.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>表存在时返回 <c>true</c>；查询异常时返回 <c>false</c>。</zh-CN>
        ///   <en><c>true</c> when the table exists; <c>false</c> when probing fails.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>这里使用受控常量拼接 `OBJECT_ID`，不是任意 SQL 拼接入口。</zh-CN>
        ///   <en>This uses controlled constants in `OBJECT_ID` and is not an arbitrary SQL concatenation entry.</en>
        /// </lang>
        /// </remarks>
        private bool HasTable(string tableName)
        {
            try
            {
                string sql = string.Format(
                    "SELECT CASE WHEN OBJECT_ID(N'[dbo].[{0}]', N'U') IS NULL THEN 0 ELSE 1 END",
                    tableName);
                return context.Database.SqlQuery<int>(sql).Single() == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化列表读取数量。</zh-CN>
        ///   <en>Normalizes the list read size.</en>
        /// </lang>
        /// </summary>
        /// <param name="take">
        /// <l>
        ///   <zh-CN>调用方请求的记录数。</zh-CN>
        ///   <en>Record count requested by the caller.</en>
        /// </l>
        /// </param>
        /// <param name="defaultValue">
        /// <l>
        ///   <zh-CN>无效输入时使用的默认记录数。</zh-CN>
        ///   <en>Default record count used for invalid input.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>`1..200` 范围内的安全记录数。</zh-CN>
        ///   <en>A safe record count in the `1..200` range.</en>
        /// </l>
        /// </returns>
        private static int NormalizeTake(int take, int defaultValue)
        {
            if (take <= 0)
            {
                return defaultValue;
            }

            return Math.Min(take, 200);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化员工资料更正提交请求。</zh-CN>
        ///   <en>Normalizes an employee-profile correction submission request.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>调用方提交的请求；为空时按空请求处理。</zh-CN>
        ///   <en>Request supplied by the caller; null is treated as an empty request.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>字段名、文本长度、UTC 时间和提交者均已稳定化的新请求。</zh-CN>
        ///   <en>A new request with stable field name, text lengths, UTC timestamp and submitter.</en>
        /// </l>
        /// </returns>
        private static EmployeeProfileCorrectionSubmitRequest NormalizeSubmitRequest(EmployeeProfileCorrectionSubmitRequest request)
        {
            request = request ?? new EmployeeProfileCorrectionSubmitRequest();
            return new EmployeeProfileCorrectionSubmitRequest
            {
                UserId = request.UserId,
                EmployeeId = request.EmployeeId,
                BindingId = request.BindingId,
                FieldName = NormalizeFieldName(request.FieldName),
                ProposedValue = NormalizeText(request.ProposedValue, 512),
                RequestNote = NormalizeOptionalText(request.RequestNote, 1000),
                SubmittedUtc = request.SubmittedUtc ?? DateTime.UtcNow,
                SubmittedBy = string.IsNullOrWhiteSpace(request.SubmittedBy)
                    ? "system"
                    : NormalizeText(request.SubmittedBy, 100)
            };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化管理员审核请求。</zh-CN>
        ///   <en>Normalizes an administrator review request.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>调用方提交的审核请求；为空时按空请求处理。</zh-CN>
        ///   <en>Review request supplied by the caller; null is treated as an empty request.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>状态、备注、UTC 时间和审核人均已稳定化的新请求。</zh-CN>
        ///   <en>A new request with stable status, note, UTC timestamp and reviewer.</en>
        /// </l>
        /// </returns>
        private static EmployeeProfileCorrectionReviewRequest NormalizeReviewRequest(EmployeeProfileCorrectionReviewRequest request)
        {
            request = request ?? new EmployeeProfileCorrectionReviewRequest();
            return new EmployeeProfileCorrectionReviewRequest
            {
                RequestId = request.RequestId,
                RequestStatus = NormalizeStatusFilter(request.RequestStatus),
                ReviewNote = NormalizeOptionalText(request.ReviewNote, 1000),
                ReviewedUtc = request.ReviewedUtc ?? DateTime.UtcNow,
                ReviewedBy = string.IsNullOrWhiteSpace(request.ReviewedBy)
                    ? "system"
                    : NormalizeText(request.ReviewedBy, 100)
            };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化更正字段名。</zh-CN>
        ///   <en>Normalizes a correction field name.</en>
        /// </lang>
        /// </summary>
        /// <param name="fieldName">
        /// <l>
        ///   <zh-CN>原始字段名。</zh-CN>
        ///   <en>Raw field name.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>裁剪后的字段名；空值返回空字符串。</zh-CN>
        ///   <en>Trimmed field name, or an empty string for blank input.</en>
        /// </l>
        /// </returns>
        private static string NormalizeFieldName(string fieldName)
        {
            return string.IsNullOrWhiteSpace(fieldName) ? string.Empty : fieldName.Trim();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化审核状态过滤值。</zh-CN>
        ///   <en>Normalizes a review-status filter value.</en>
        /// </lang>
        /// </summary>
        /// <param name="status">
        /// <l>
        ///   <zh-CN>原始状态值。</zh-CN>
        ///   <en>Raw status value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>裁剪后的状态值；空值表示不过滤。</zh-CN>
        ///   <en>Trimmed status value; empty means no filtering.</en>
        /// </l>
        /// </returns>
        private static string NormalizeStatusFilter(string status)
        {
            return string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>裁剪文本并限制最大长度。</zh-CN>
        ///   <en>Trims text and applies a maximum length.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>原始文本。</zh-CN>
        ///   <en>Raw text.</en>
        /// </l>
        /// </param>
        /// <param name="maxLength">
        /// <l>
        ///   <zh-CN>允许保存的最大长度。</zh-CN>
        ///   <en>Maximum length allowed for persistence.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>裁剪并按最大长度截断后的文本。</zh-CN>
        ///   <en>Trimmed text truncated to the maximum length.</en>
        /// </l>
        /// </returns>
        private static string NormalizeText(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>裁剪可选文本并把空值转换为数据库空值。</zh-CN>
        ///   <en>Trims optional text and converts empty values to database nulls.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>原始文本。</zh-CN>
        ///   <en>Raw text.</en>
        /// </l>
        /// </param>
        /// <param name="maxLength">
        /// <l>
        ///   <zh-CN>允许保存的最大长度。</zh-CN>
        ///   <en>Maximum length allowed for persistence.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>非空文本或 <c>null</c>。</zh-CN>
        ///   <en>Non-empty text or <c>null</c>.</en>
        /// </l>
        /// </returns>
        private static string NormalizeOptionalText(string value, int maxLength)
        {
            string normalized = NormalizeText(value, maxLength);
            return normalized.Length == 0 ? null : normalized;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断请求字段是否属于当前员工资料更正白名单。</zh-CN>
        ///   <en>Determines whether a requested field belongs to the current profile-correction allow-list.</en>
        /// </lang>
        /// </summary>
        /// <param name="fieldName">
        /// <l>
        ///   <zh-CN>已归一化的字段名。</zh-CN>
        ///   <en>Normalized field name.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>字段允许提交更正时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the field may be submitted for correction.</en>
        /// </l>
        /// </returns>
        private static bool IsAllowedFieldName(string fieldName)
        {
            return string.Equals(fieldName, "DisplayName", StringComparison.Ordinal) ||
                   string.Equals(fieldName, "PreferredName", StringComparison.Ordinal) ||
                   string.Equals(fieldName, "WorkEmail", StringComparison.Ordinal) ||
                   string.Equals(fieldName, "OrganizationDisplayName", StringComparison.Ordinal);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断管理员审核状态是否允许写入。</zh-CN>
        ///   <en>Determines whether an administrator review status may be written.</en>
        /// </lang>
        /// </summary>
        /// <param name="status">
        /// <l>
        ///   <zh-CN>已归一化的审核状态。</zh-CN>
        ///   <en>Normalized review status.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>状态属于已审核、已关闭或已拒绝时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> for reviewed, closed or rejected states.</en>
        /// </l>
        /// </returns>
        private static bool IsReviewStatus(string status)
        {
            return string.Equals(status, EmployeeProfileCorrectionRequestStatuses.Reviewed, StringComparison.Ordinal) ||
                   string.Equals(status, EmployeeProfileCorrectionRequestStatuses.Closed, StringComparison.Ordinal) ||
                   string.Equals(status, EmployeeProfileCorrectionRequestStatuses.Rejected, StringComparison.Ordinal);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建可为空字符串 SQL 参数。</zh-CN>
        ///   <en>Creates a nullable string SQL parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>参数名称。</zh-CN>
        ///   <en>Parameter name.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>参数文本值。</zh-CN>
        ///   <en>Parameter text value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>空字符串映射为 <see cref="DBNull.Value"/> 的 SQL 参数。</zh-CN>
        ///   <en>SQL parameter whose empty string is mapped to <see cref="DBNull.Value"/>.</en>
        /// </l>
        /// </returns>
        private static SqlParameter CreateNullableStringParameter(string name, string value)
        {
            return new SqlParameter(name, string.IsNullOrEmpty(value) ? (object)DBNull.Value : value);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工资料更正页面所需的当前资料投影。</zh-CN>
        ///   <en>Current-profile projection used by the employee-profile correction page.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该内部类型只承接 SQL 查询列名，不参与业务校验或授权判断。</zh-CN>
        ///   <en>This internal type only receives SQL query columns and does not participate in business validation or authorization decisions.</en>
        /// </lang>
        /// </remarks>
        private sealed class ProfileProjection
        {
            /// <summary>
            /// <lang>
            ///   <zh-CN>当前登录账号绑定到的员工主键。</zh-CN>
            ///   <en>Employee primary key bound to the current signed-in account.</en>
            /// </lang>
            /// </summary>
            public int EmployeeId { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工工号，用于用户可识别的身份展示和业务核对。</zh-CN>
            ///   <en>Employee code used for user-recognizable identity display and business verification.</en>
            /// </lang>
            /// </summary>
            public string EmployeeCode { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工正式显示姓名。</zh-CN>
            ///   <en>Employee formal display name.</en>
            /// </lang>
            /// </summary>
            public string DisplayName { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工偏好姓名或简称。</zh-CN>
            ///   <en>Employee preferred name or short name.</en>
            /// </lang>
            /// </summary>
            public string PreferredName { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工工作邮箱。</zh-CN>
            ///   <en>Employee work email address.</en>
            /// </lang>
            /// </summary>
            public string WorkEmail { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工所属组织的显示名称。</zh-CN>
            ///   <en>Display name of the employee's organization unit.</en>
            /// </lang>
            /// </summary>
            public string OrganizationDisplayName { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工状态快照，查询已在 SQL 中限定为 Active。</zh-CN>
            ///   <en>Employee status snapshot; the SQL query already limits it to Active.</en>
            /// </lang>
            /// </summary>
            public string EmploymentStatus { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>当前用户与员工的绑定记录主键。</zh-CN>
            ///   <en>Primary key of the binding between the current user and employee.</en>
            /// </lang>
            /// </summary>
            public int BindingId { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>绑定建立时间，用于在多条绑定记录中选择最新有效记录。</zh-CN>
            ///   <en>Binding creation time used to choose the latest active binding among multiple rows.</en>
            /// </lang>
            /// </summary>
            public DateTime BoundUtc { get; set; }
        }
    }
}
