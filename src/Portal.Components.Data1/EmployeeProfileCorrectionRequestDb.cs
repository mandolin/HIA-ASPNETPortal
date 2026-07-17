using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：基于 <see cref="PortalBizDbContext"/> 的员工资料更正请求数据访问实现。
    ///
    /// English: Employee-profile correction-request data-access implementation backed by <see cref="PortalBizDbContext"/>.
    /// </summary>
    /// <remarks>
    /// 中文：此实现只写入请求和管理员处理状态，不直接修改员工主数据。员工主数据修改仍应通过员工目录后台或后续审批机制完成。
    ///
    /// English: This implementation writes only requests and administrator review states. Employee master-data changes
    /// must still be performed through the employee-directory administration area or a future approval workflow.
    /// </remarks>
    public sealed class EmployeeProfileCorrectionRequestDb : IEmployeeProfileCorrectionRequestDb
    {
        private const string RequestTableName = "PortalBiz_EmployeeProfileCorrectionRequests";
        private const string EmployeeTableName = "PortalBiz_Employees";
        private const string BindingTableName = "PortalBiz_UserEmployeeBindings";
        private const string UserTableName = "Portal_Users";
        private readonly PortalBizDbContext context;

        /// <summary>
        /// 中文：初始化员工资料更正请求数据访问实现。
        ///
        /// English: Initializes the employee-profile correction-request data-access implementation.
        /// </summary>
        /// <param name="context">中文：企业业务基础数据上下文。English: Enterprise business foundation data context.</param>
        public EmployeeProfileCorrectionRequestDb(PortalBizDbContext context)
        {
            this.context = context;
        }

        /// <inheritdoc />
        public bool IsSchemaAvailable()
        {
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
                return new List<EmployeeProfileCorrectionRequestInfo>();
            }
        }

        /// <inheritdoc />
        public EmployeeProfileCorrectionRequestResult SubmitRequest(EmployeeProfileCorrectionSubmitRequest request)
        {
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
                return new List<EmployeeProfileCorrectionRequestInfo>();
            }
        }

        /// <inheritdoc />
        public EmployeeProfileCorrectionRequestResult ReviewRequest(EmployeeProfileCorrectionReviewRequest request)
        {
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
                return new EmployeeProfileCorrectionRequestResult(false, 0, "Correction request review failed.");
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

        private static int NormalizeTake(int take, int defaultValue)
        {
            if (take <= 0)
            {
                return defaultValue;
            }

            return Math.Min(take, 200);
        }

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

        private static string NormalizeFieldName(string fieldName)
        {
            return string.IsNullOrWhiteSpace(fieldName) ? string.Empty : fieldName.Trim();
        }

        private static string NormalizeStatusFilter(string status)
        {
            return string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim();
        }

        private static string NormalizeText(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }

        private static string NormalizeOptionalText(string value, int maxLength)
        {
            string normalized = NormalizeText(value, maxLength);
            return normalized.Length == 0 ? null : normalized;
        }

        private static bool IsAllowedFieldName(string fieldName)
        {
            return string.Equals(fieldName, "DisplayName", StringComparison.Ordinal) ||
                   string.Equals(fieldName, "PreferredName", StringComparison.Ordinal) ||
                   string.Equals(fieldName, "WorkEmail", StringComparison.Ordinal) ||
                   string.Equals(fieldName, "OrganizationDisplayName", StringComparison.Ordinal);
        }

        private static bool IsReviewStatus(string status)
        {
            return string.Equals(status, EmployeeProfileCorrectionRequestStatuses.Reviewed, StringComparison.Ordinal) ||
                   string.Equals(status, EmployeeProfileCorrectionRequestStatuses.Closed, StringComparison.Ordinal) ||
                   string.Equals(status, EmployeeProfileCorrectionRequestStatuses.Rejected, StringComparison.Ordinal);
        }

        private static SqlParameter CreateNullableStringParameter(string name, string value)
        {
            return new SqlParameter(name, string.IsNullOrEmpty(value) ? (object)DBNull.Value : value);
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
        }
    }
}
