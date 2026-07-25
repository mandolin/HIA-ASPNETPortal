using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>基于 <see cref="PortalBizDbContext"/> 的员工资料确认模块数据访问实现。</zh-CN>
    ///   <en>Employee-profile confirmation module data-access implementation backed by <see cref="PortalBizDbContext"/>.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>此实现只接受当前 Active 员工和 Active 账号绑定。确认写入会保存一份低敏资料快照，方便审计和人工核对；它不更新员工主数据，也不变更账号绑定。</zh-CN>
    ///   <en>This implementation accepts only a current active employee and an active user binding. Confirmation writes store a low-sensitivity profile snapshot for later audit and manual review; they do not update employee master data or change account bindings.</en>
    /// </lang>
    /// </remarks>
    public sealed class EmployeeProfileConfirmationDb : IEmployeeProfileConfirmationDb
    {
        private const string ConfirmationTableName = "PortalBiz_EmployeeProfileConfirmations";
        private const string EmployeeTableName = "PortalBiz_Employees";
        private const string BindingTableName = "PortalBiz_UserEmployeeBindings";
        private readonly PortalBizDbContext context;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化员工资料确认模块数据访问实现。</zh-CN>
        ///   <en>Initializes the employee-profile confirmation module data-access implementation.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>企业业务基础数据上下文。</zh-CN>
        ///   <en>Enterprise business foundation data context.</en>
        /// </l>
        /// </param>
        public EmployeeProfileConfirmationDb(PortalBizDbContext context)
        {
            this.context = context;
        }

        /// <inheritdoc />
        public bool IsSchemaAvailable()
        {
            // <lang>
            //   <zh-CN>资料确认模块横跨员工、绑定和确认快照三张表；任一表缺失时页面应安全降级，而不是局部执行写入。</zh-CN>
            //   <en>The profile-confirmation module spans employee, binding and confirmation snapshot tables; if any table is missing, the page should degrade safely instead of partially writing data.</en>
            // </lang>
            return HasTable(ConfirmationTableName) &&
                   HasTable(EmployeeTableName) &&
                   HasTable(BindingTableName);
        }

        /// <inheritdoc />
        public EmployeeProfileConfirmationView GetCurrentProfileForUser(int userId)
        {
            if (userId <= 0 || !IsSchemaAvailable())
            {
                return null;
            }

            try
            {
                // <lang>
                //   <zh-CN>只取当前用户最新 Active 绑定对应的 Active 员工，同时带出最近一次确认记录，供前台显示“已确认”状态。</zh-CN>
                //   <en>Only the current user's latest active binding to an active employee is loaded, with the latest confirmation record included for the front-end confirmation state.</en>
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
    [Binding].[BoundUtc],
    [Latest].[ConfirmationId] AS [LastConfirmationId],
    [Latest].[ConfirmedUtc] AS [LastConfirmedUtc]
FROM [dbo].[PortalBiz_UserEmployeeBindings] AS [Binding]
INNER JOIN [dbo].[PortalBiz_Employees] AS [Employee]
    ON [Employee].[EmployeeId] = [Binding].[EmployeeId]
LEFT JOIN [dbo].[PortalBiz_OrganizationUnits] AS [Organization]
    ON [Organization].[OrganizationUnitId] = [Employee].[OrganizationUnitId]
OUTER APPLY
(
    SELECT TOP (1)
        [Confirmation].[ConfirmationId],
        [Confirmation].[ConfirmedUtc]
    FROM [dbo].[PortalBiz_EmployeeProfileConfirmations] AS [Confirmation]
    WHERE [Confirmation].[EmployeeId] = [Employee].[EmployeeId]
      AND [Confirmation].[UserId] = [Binding].[UserId]
    ORDER BY [Confirmation].[ConfirmedUtc] DESC, [Confirmation].[ConfirmationId] DESC
) AS [Latest]
WHERE [Binding].[UserId] = @p0
  AND [Binding].[BindingStatus] = N'Active'
  AND [Employee].[EmploymentStatus] = N'Active'
ORDER BY [Binding].[BoundUtc] DESC, [Binding].[BindingId] DESC;",
                    userId).SingleOrDefault();

                return row == null
                    ? null
                    : new EmployeeProfileConfirmationView(
                        row.EmployeeId,
                        row.EmployeeCode,
                        row.DisplayName,
                        row.PreferredName,
                        row.WorkEmail,
                        row.OrganizationDisplayName,
                        row.EmploymentStatus,
                        row.BindingId,
                        row.BoundUtc,
                        row.LastConfirmationId,
                        row.LastConfirmedUtc);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <inheritdoc />
        public EmployeeProfileConfirmationResult ConfirmProfile(EmployeeProfileConfirmationRequest request)
        {
            EmployeeProfileConfirmationRequest normalized = NormalizeRequest(request);
            if (normalized.UserId <= 0 || normalized.EmployeeId <= 0)
            {
                return new EmployeeProfileConfirmationResult(false, 0, "A signed-in user with an active employee binding is required.");
            }

            if (!IsSchemaAvailable())
            {
                return new EmployeeProfileConfirmationResult(false, 0, "Employee profile confirmation schema is unavailable.");
            }

            try
            {
                // <lang>
                //   <zh-CN>确认写入通过 `INSERT ... SELECT TOP (1)` 再次校验 Active 绑定和 Active 员工，避免页面加载后绑定状态变化造成越权确认。</zh-CN>
                //   <en>The confirmation write uses `INSERT ... SELECT TOP (1)` to re-check active binding and active employee state, preventing unauthorized confirmation if binding state changes after page load.</en>
                // </lang>
                var rows = context.Database.SqlQuery<long>(
                    @"
DECLARE @Inserted TABLE
(
    [ConfirmationId] BIGINT NOT NULL
);

INSERT INTO [dbo].[PortalBiz_EmployeeProfileConfirmations]
    ([EmployeeId],
     [UserId],
     [BindingId],
     [ConfirmedUtc],
     [ConfirmedBy],
     [SnapshotEmployeeCode],
     [SnapshotDisplayName],
     [SnapshotPreferredName],
     [SnapshotWorkEmail],
     [SnapshotOrganizationDisplayName])
OUTPUT INSERTED.[ConfirmationId] INTO @Inserted
SELECT TOP (1)
    [Employee].[EmployeeId],
    [Binding].[UserId],
    [Binding].[BindingId],
    @ConfirmedUtc,
    @ConfirmedBy,
    [Employee].[EmployeeCode],
    [Employee].[DisplayName],
    [Employee].[PreferredName],
    [Employee].[WorkEmail],
    [Organization].[DisplayName]
FROM [dbo].[PortalBiz_UserEmployeeBindings] AS [Binding]
INNER JOIN [dbo].[PortalBiz_Employees] AS [Employee]
    ON [Employee].[EmployeeId] = [Binding].[EmployeeId]
LEFT JOIN [dbo].[PortalBiz_OrganizationUnits] AS [Organization]
    ON [Organization].[OrganizationUnitId] = [Employee].[OrganizationUnitId]
WHERE [Binding].[UserId] = @UserId
  AND [Employee].[EmployeeId] = @EmployeeId
  AND [Binding].[BindingStatus] = N'Active'
  AND [Employee].[EmploymentStatus] = N'Active'
ORDER BY [Binding].[BoundUtc] DESC, [Binding].[BindingId] DESC;

SELECT [ConfirmationId] FROM @Inserted;",
                    new SqlParameter("@UserId", normalized.UserId),
                    new SqlParameter("@EmployeeId", normalized.EmployeeId),
                    new SqlParameter("@ConfirmedUtc", normalized.ConfirmedUtc.Value),
                    new SqlParameter("@ConfirmedBy", normalized.ConfirmedBy)).ToList();

                long confirmationId = rows.Count == 0 ? 0 : rows[0];
                if (confirmationId <= 0)
                {
                    return new EmployeeProfileConfirmationResult(false, 0, "No active employee profile is available for confirmation.");
                }

                return new EmployeeProfileConfirmationResult(true, confirmationId, "Employee profile confirmed.");
            }
            catch (Exception)
            {
                return new EmployeeProfileConfirmationResult(false, 0, "Employee profile confirmation failed.");
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查业务表是否存在。</zh-CN>
        ///   <en>Checks whether a business table exists.</en>
        /// </lang>
        /// </summary>
        /// <param name="tableName">
        /// <l>
        ///   <zh-CN>不带 schema 的表名常量。</zh-CN>
        ///   <en>Table-name constant without schema.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>表存在时为 <c>true</c>；无法探测时返回 <c>false</c> 以触发安全降级。</zh-CN>
        ///   <en><c>true</c> when the table exists; <c>false</c> when probing fails so the caller can degrade safely.</en>
        /// </l>
        /// </returns>
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
        ///   <zh-CN>归一化员工资料确认请求。</zh-CN>
        ///   <en>Normalizes an employee-profile confirmation request.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>调用方提交的原始请求；可为 <c>null</c>。</zh-CN>
        ///   <en>Raw request submitted by the caller; may be <c>null</c>.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>带 UTC 时间和非空操作者标识的请求副本。</zh-CN>
        ///   <en>Request copy with UTC timestamp and non-empty actor identifier.</en>
        /// </l>
        /// </returns>
        private static EmployeeProfileConfirmationRequest NormalizeRequest(EmployeeProfileConfirmationRequest request)
        {
            request = request ?? new EmployeeProfileConfirmationRequest();
            return new EmployeeProfileConfirmationRequest
            {
                UserId = request.UserId,
                EmployeeId = request.EmployeeId,
                ConfirmedUtc = request.ConfirmedUtc ?? DateTime.UtcNow,
                ConfirmedBy = string.IsNullOrWhiteSpace(request.ConfirmedBy)
                    ? "system"
                    : request.ConfirmedBy.Trim()
            };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>员工资料确认页面使用的 SQL 查询投影。</zh-CN>
        ///   <en>SQL query projection used by the employee-profile confirmation page.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>此类型只承接当前页面需要展示和判断的低敏字段，不应扩展为完整员工资料对象。</zh-CN>
        ///   <en>This type carries only the low-sensitivity fields needed for this page's display and decisions; it should not grow into a full employee profile object.</en>
        /// </lang>
        /// </remarks>
        private sealed class ProfileProjection
        {
            /// <summary>
            /// <lang>
            ///   <zh-CN>员工主数据标识。</zh-CN>
            ///   <en>Employee master-data identifier.</en>
            /// </lang>
            /// </summary>
            public int EmployeeId { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工工号。</zh-CN>
            ///   <en>Employee code.</en>
            /// </lang>
            /// </summary>
            public string EmployeeCode { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工显示姓名。</zh-CN>
            ///   <en>Employee display name.</en>
            /// </lang>
            /// </summary>
            public string DisplayName { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工偏好姓名。</zh-CN>
            ///   <en>Employee preferred name.</en>
            /// </lang>
            /// </summary>
            public string PreferredName { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工工作邮箱。</zh-CN>
            ///   <en>Employee work email.</en>
            /// </lang>
            /// </summary>
            public string WorkEmail { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>所属组织显示名称。</zh-CN>
            ///   <en>Display name of the owning organization.</en>
            /// </lang>
            /// </summary>
            public string OrganizationDisplayName { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>员工当前状态。</zh-CN>
            ///   <en>Current employee status.</en>
            /// </lang>
            /// </summary>
            public string EmploymentStatus { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>账号员工绑定标识。</zh-CN>
            ///   <en>User-employee binding identifier.</en>
            /// </lang>
            /// </summary>
            public int BindingId { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>绑定创建 UTC 时间。</zh-CN>
            ///   <en>Binding creation UTC time.</en>
            /// </lang>
            /// </summary>
            public DateTime BoundUtc { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>最近一次确认记录标识。</zh-CN>
            ///   <en>Identifier of the latest confirmation record.</en>
            /// </lang>
            /// </summary>
            public long? LastConfirmationId { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>最近一次确认 UTC 时间。</zh-CN>
            ///   <en>Latest confirmation UTC time.</en>
            /// </lang>
            /// </summary>
            public DateTime? LastConfirmedUtc { get; set; }
        }
    }
}
