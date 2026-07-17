using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：基于 <see cref="PortalBizDbContext"/> 的员工资料确认模块数据访问实现。
    ///
    /// English: Employee-profile confirmation module data-access implementation backed by <see cref="PortalBizDbContext"/>.
    /// </summary>
    /// <remarks>
    /// 中文：此实现只接受当前 Active 员工和 Active 账号绑定。确认写入会保存一份低敏资料快照，方便后续审计
    /// 和人工核对；它不更新员工主数据，也不变更账号绑定。
    ///
    /// English: This implementation accepts only a current active employee and an active user binding. Confirmation
    /// writes store a low-sensitivity profile snapshot for later audit and manual review; they do not update employee
    /// master data or change account bindings.
    /// </remarks>
    public sealed class EmployeeProfileConfirmationDb : IEmployeeProfileConfirmationDb
    {
        private const string ConfirmationTableName = "PortalBiz_EmployeeProfileConfirmations";
        private const string EmployeeTableName = "PortalBiz_Employees";
        private const string BindingTableName = "PortalBiz_UserEmployeeBindings";
        private readonly PortalBizDbContext context;

        /// <summary>
        /// 中文：初始化员工资料确认模块数据访问实现。
        ///
        /// English: Initializes the employee-profile confirmation module data-access implementation.
        /// </summary>
        /// <param name="context">中文：企业业务基础数据上下文。English: Enterprise business foundation data context.</param>
        public EmployeeProfileConfirmationDb(PortalBizDbContext context)
        {
            this.context = context;
        }

        /// <inheritdoc />
        public bool IsSchemaAvailable()
        {
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

            public long? LastConfirmationId { get; set; }

            public DateTime? LastConfirmedUtc { get; set; }
        }
    }
}
