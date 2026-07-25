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
        public EmployeeProfileCorrectionRequestDb(PortalBizDbContext context)
        {
            this.context = context;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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
            public int EmployeeId { get; set; }

            public string EmployeeCode { get; set; }

            public string DisplayName { get; set; }

            public string PreferredName { get; set; }

            public string WorkEmail { get; set; }

            public string OrganizationDisplayName { get; set; }

            public string EmploymentStatus { get; set; }

            public int BindingId { get; set; }

            public DateTime BoundUtc { get; set; }
        }
    }
}
