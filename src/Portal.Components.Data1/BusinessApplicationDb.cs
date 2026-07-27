using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>基于 <see cref="PortalBizDbContext"/> 的抽象业务申请数据访问实现。</zh-CN>
    ///   <en>Abstract business-application data-access implementation backed by <see cref="PortalBizDbContext"/>.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P19.4 第一版只处理低敏纯文本申请、单层审核状态和流程事件事实；待办投影与运营审计由页面层在业务事实写入成功后旁路记录。</zh-CN>
    ///   <en>The first P19.4 version handles only low-sensitivity plain-text applications, one-level review states, and workflow-event facts; work-item projections and operation audits are recorded by page code after the business facts are written.</en>
    /// </lang>
    /// </remarks>
    public sealed class BusinessApplicationDb : IBusinessApplicationDb
    {
        private const string ApplicationTableName = "PortalBiz_BusinessApplications";
        private const string WorkflowEventTableName = "PortalBiz_WorkflowEvents";
        private readonly PortalBizDbContext context;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化抽象业务申请数据访问实现。</zh-CN>
        ///   <en>Initializes the abstract business-application data-access implementation.</en>
        /// </lang>
        /// </summary>
        public BusinessApplicationDb(PortalBizDbContext context)
        {
            this.context = context;
        }

        /// <inheritdoc />
        public bool IsSchemaAvailable()
        {
            return HasTable(ApplicationTableName) && HasTable(WorkflowEventTableName);
        }

        /// <inheritdoc />
        public BusinessApplicationResult SubmitApplication(BusinessApplicationSubmitRequest request)
        {
            BusinessApplicationSubmitRequest normalized = NormalizeSubmitRequest(request);
            if (normalized.ApplicantUserId <= 0)
            {
                return new BusinessApplicationResult(false, 0, string.Empty, "A signed-in portal user is required.");
            }

            if (string.IsNullOrWhiteSpace(normalized.Title))
            {
                return new BusinessApplicationResult(false, 0, string.Empty, "Application title is required.");
            }

            if (string.IsNullOrWhiteSpace(normalized.Summary) && string.IsNullOrWhiteSpace(normalized.Body))
            {
                return new BusinessApplicationResult(false, 0, string.Empty, "Application summary or body is required.");
            }

            if (!IsSchemaAvailable())
            {
                return new BusinessApplicationResult(false, 0, string.Empty, "Business application schema is unavailable.");
            }

            string applicationCode = CreateApplicationCode(normalized.SubmittedUtc.Value);
            try
            {
                // <lang>
                //   <zh-CN>申请事实和 Submit 流程事件必须同批写入，避免申请已经存在但流程流水缺失；待办投影稍后由页面层旁路补充。</zh-CN>
                //   <en>The application fact and Submit workflow event are written in the same batch to avoid an application without event history; the page layer later adds the work-item projection as a sidecar.</en>
                // </lang>
                List<long> rows = context.Database.SqlQuery<long>(
                    @"
DECLARE @ApplicationId BIGINT;

INSERT INTO [dbo].[PortalBiz_BusinessApplications]
    ([ApplicationCode],
     [Title],
     [CategoryKey],
     [Summary],
     [Body],
     [ApplicantUserId],
     [ApplicantEmployeeId],
     [OrganizationUnitId],
     [ReviewRoleKey],
     [ApplicationStatus],
     [SubmittedUtc],
     [CreatedUtc],
     [CreatedBy],
     [UpdatedUtc],
     [UpdatedBy])
VALUES
    (@ApplicationCode,
     @Title,
     @CategoryKey,
     @Summary,
     @Body,
     @ApplicantUserId,
     @ApplicantEmployeeId,
     @OrganizationUnitId,
     @ReviewRoleKey,
     N'Submitted',
     @SubmittedUtc,
     @SubmittedUtc,
     @SubmittedBy,
     @SubmittedUtc,
     @SubmittedBy);

SET @ApplicationId = CONVERT(BIGINT, SCOPE_IDENTITY());

INSERT INTO [dbo].[PortalBiz_WorkflowEvents]
    ([BusinessKind], [BusinessId], [OccurredUtc], [ActionKey], [ActorUserId], [ActorName], [FromStatus], [ToStatus], [Comment], [EventDataJson])
VALUES
    (N'BusinessApplication',
     CONVERT(NVARCHAR(80), @ApplicationId),
     @SubmittedUtc,
     N'Submit',
     @ApplicantUserId,
     @SubmittedBy,
     NULL,
     N'Submitted',
     @Summary,
     NULL);

SELECT @ApplicationId;",
                    new SqlParameter("@ApplicationCode", applicationCode),
                    new SqlParameter("@Title", normalized.Title),
                    CreateNullableStringParameter("@CategoryKey", normalized.CategoryKey),
                    CreateNullableStringParameter("@Summary", normalized.Summary),
                    CreateNullableStringParameter("@Body", normalized.Body),
                    new SqlParameter("@ApplicantUserId", normalized.ApplicantUserId),
                    CreateNullableIntParameter("@ApplicantEmployeeId", normalized.ApplicantEmployeeId),
                    CreateNullableIntParameter("@OrganizationUnitId", normalized.OrganizationUnitId),
                    new SqlParameter("@ReviewRoleKey", normalized.ReviewRoleKey),
                    new SqlParameter("@SubmittedUtc", normalized.SubmittedUtc.Value),
                    new SqlParameter("@SubmittedBy", normalized.SubmittedBy)).ToList();

                long applicationId = rows.Count == 0 ? 0 : rows[0];
                return applicationId <= 0
                    ? new BusinessApplicationResult(false, 0, string.Empty, "Business application was not created.")
                    : new BusinessApplicationResult(true, applicationId, applicationCode, "Business application submitted.");
            }
            catch (Exception)
            {
                return new BusinessApplicationResult(false, 0, string.Empty, "Business application submission failed.");
            }
        }

        /// <inheritdoc />
        public IList<BusinessApplicationInfo> GetRecentApplicationsForUser(int applicantUserId, int take)
        {
            if (applicantUserId <= 0 || !IsSchemaAvailable())
            {
                return new List<BusinessApplicationInfo>();
            }

            try
            {
                return QueryApplications(
                    @"
WHERE [Application].[ApplicantUserId] = @ApplicantUserId",
                    NormalizeTake(take, 10),
                    new SqlParameter("@ApplicantUserId", applicantUserId));
            }
            catch (Exception)
            {
                return new List<BusinessApplicationInfo>();
            }
        }

        /// <inheritdoc />
        public IList<BusinessApplicationInfo> GetAdminApplications(string status, int take)
        {
            if (!IsSchemaAvailable())
            {
                return new List<BusinessApplicationInfo>();
            }

            string normalizedStatus = NormalizeStatusFilter(status);
            try
            {
                return string.IsNullOrEmpty(normalizedStatus)
                    ? QueryApplications(string.Empty, NormalizeTake(take, 50))
                    : QueryApplications(
                        @"
WHERE [Application].[ApplicationStatus] = @ApplicationStatus",
                        NormalizeTake(take, 50),
                        new SqlParameter("@ApplicationStatus", normalizedStatus));
            }
            catch (Exception)
            {
                return new List<BusinessApplicationInfo>();
            }
        }

        /// <inheritdoc />
        public BusinessApplicationResult ReviewApplication(BusinessApplicationReviewRequest request)
        {
            BusinessApplicationReviewRequest normalized = NormalizeReviewRequest(request);
            if (normalized.ApplicationId <= 0)
            {
                return new BusinessApplicationResult(false, 0, string.Empty, "Application id is required.");
            }

            string targetStatus = MapActionToStatus(normalized.ActionKey);
            if (string.IsNullOrEmpty(targetStatus))
            {
                return new BusinessApplicationResult(false, 0, string.Empty, "Unsupported workflow action.");
            }

            if (!IsSchemaAvailable())
            {
                return new BusinessApplicationResult(false, 0, string.Empty, "Business application schema is unavailable.");
            }

            try
            {
                // <lang>
                //   <zh-CN>审核动作只允许处理仍处于待审核窗口的申请；用表变量捕获旧状态和编号，再写入 WorkflowEvent 事实。</zh-CN>
                //   <en>Review actions process only applications still inside the review window; a table variable captures the previous state and code before the WorkflowEvent fact is written.</en>
                // </lang>
                List<BusinessApplicationReviewWriteRow> rows = context.Database.SqlQuery<BusinessApplicationReviewWriteRow>(
                    @"
DECLARE @Updated TABLE
(
    [ApplicationId] BIGINT NOT NULL,
    [ApplicationCode] NVARCHAR(40) NOT NULL,
    [FromStatus] NVARCHAR(20) NOT NULL
);

UPDATE [dbo].[PortalBiz_BusinessApplications]
SET [ApplicationStatus] = @TargetStatus,
    [ReviewedUtc] = @ReviewedUtc,
    [ReviewedByUserId] = @ReviewedByUserId,
    [ReviewComment] = @ReviewComment,
    [UpdatedUtc] = @ReviewedUtc,
    [UpdatedBy] = @ReviewedBy
OUTPUT INSERTED.[ApplicationId], INSERTED.[ApplicationCode], DELETED.[ApplicationStatus]
INTO @Updated ([ApplicationId], [ApplicationCode], [FromStatus])
WHERE [ApplicationId] = @ApplicationId
  AND [ApplicationStatus] IN (N'Submitted', N'InReview');

DECLARE @ApplicationCode NVARCHAR(40);
DECLARE @FromStatus NVARCHAR(20);

SELECT TOP (1)
    @ApplicationCode = [ApplicationCode],
    @FromStatus = [FromStatus]
FROM @Updated;

IF @ApplicationCode IS NOT NULL
BEGIN
    INSERT INTO [dbo].[PortalBiz_WorkflowEvents]
        ([BusinessKind], [BusinessId], [OccurredUtc], [ActionKey], [ActorUserId], [ActorName], [FromStatus], [ToStatus], [Comment], [EventDataJson])
    VALUES
        (N'BusinessApplication',
         CONVERT(NVARCHAR(80), @ApplicationId),
         @ReviewedUtc,
         @ActionKey,
         @ReviewedByUserId,
         @ReviewedBy,
         @FromStatus,
         @TargetStatus,
         @ReviewComment,
         NULL);
END

SELECT
    @ApplicationId AS [ApplicationId],
    ISNULL(@ApplicationCode, N'') AS [ApplicationCode];",
                    new SqlParameter("@ApplicationId", normalized.ApplicationId),
                    new SqlParameter("@TargetStatus", targetStatus),
                    new SqlParameter("@ReviewedUtc", normalized.ReviewedUtc.Value),
                    CreateNullableIntParameter("@ReviewedByUserId", normalized.ReviewedByUserId),
                    new SqlParameter("@ReviewedBy", normalized.ReviewedBy),
                    new SqlParameter("@ActionKey", normalized.ActionKey),
                    CreateNullableStringParameter("@ReviewComment", normalized.ReviewComment)).ToList();

                BusinessApplicationReviewWriteRow row = rows.Count == 0 ? null : rows[0];
                return row == null || string.IsNullOrWhiteSpace(row.ApplicationCode)
                    ? new BusinessApplicationResult(false, normalized.ApplicationId, string.Empty, "Application was not found or is no longer reviewable.")
                    : new BusinessApplicationResult(true, row.ApplicationId, row.ApplicationCode, "Business application review state updated.");
            }
            catch (Exception)
            {
                return new BusinessApplicationResult(false, normalized.ApplicationId, string.Empty, "Business application review failed.");
            }
        }

        private IList<BusinessApplicationInfo> QueryApplications(string whereClause, int take, params SqlParameter[] parameters)
        {
            string sql = @"
SELECT TOP (@Take)
    [Application].[ApplicationId],
    [Application].[ApplicationCode],
    [Application].[Title],
    [Application].[CategoryKey],
    [Application].[Summary],
    [Application].[Body],
    [Application].[ApplicantUserId],
    [User].[Name] AS [ApplicantUserName],
    [Application].[ApplicantEmployeeId],
    [Application].[OrganizationUnitId],
    [Application].[ReviewRoleKey],
    [Application].[ApplicationStatus],
    [Application].[SubmittedUtc],
    [Application].[ReviewedUtc],
    [Application].[ReviewedByUserId],
    [Application].[ReviewComment]
FROM [dbo].[PortalBiz_BusinessApplications] AS [Application]
LEFT JOIN [dbo].[Portal_Users] AS [User]
    ON [User].[UserID] = [Application].[ApplicantUserId]" +
                whereClause +
                @"
ORDER BY [Application].[SubmittedUtc] DESC, [Application].[ApplicationId] DESC;";

            var sqlParameters = new List<SqlParameter> { new SqlParameter("@Take", take) };
            if (parameters != null)
            {
                sqlParameters.AddRange(parameters);
            }

            return context.Database.SqlQuery<BusinessApplicationInfo>(sql, sqlParameters.ToArray()).ToList();
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

        private static BusinessApplicationSubmitRequest NormalizeSubmitRequest(BusinessApplicationSubmitRequest request)
        {
            request = request ?? new BusinessApplicationSubmitRequest();
            DateTime submittedUtc = request.SubmittedUtc ?? DateTime.UtcNow;
            return new BusinessApplicationSubmitRequest
            {
                Title = NormalizeText(request.Title, 200),
                CategoryKey = NormalizeOptionalText(request.CategoryKey, 80),
                Summary = NormalizeOptionalText(request.Summary, 500),
                Body = NormalizeOptionalText(request.Body, 4000),
                ApplicantUserId = request.ApplicantUserId,
                ApplicantEmployeeId = request.ApplicantEmployeeId.HasValue && request.ApplicantEmployeeId.Value > 0 ? request.ApplicantEmployeeId : null,
                OrganizationUnitId = request.OrganizationUnitId.HasValue && request.OrganizationUnitId.Value > 0 ? request.OrganizationUnitId : null,
                ReviewRoleKey = string.IsNullOrWhiteSpace(request.ReviewRoleKey)
                    ? PortalPermissionKeys.BusinessApplicationReview
                    : NormalizeText(request.ReviewRoleKey, 120),
                SubmittedUtc = submittedUtc,
                SubmittedBy = string.IsNullOrWhiteSpace(request.SubmittedBy) ? "system" : NormalizeText(request.SubmittedBy, 100)
            };
        }

        private static BusinessApplicationReviewRequest NormalizeReviewRequest(BusinessApplicationReviewRequest request)
        {
            request = request ?? new BusinessApplicationReviewRequest();
            return new BusinessApplicationReviewRequest
            {
                ApplicationId = request.ApplicationId,
                ActionKey = NormalizeText(request.ActionKey, 40),
                ReviewComment = NormalizeOptionalText(request.ReviewComment, 1000),
                ReviewedByUserId = request.ReviewedByUserId.HasValue && request.ReviewedByUserId.Value > 0 ? request.ReviewedByUserId : null,
                ReviewedBy = string.IsNullOrWhiteSpace(request.ReviewedBy) ? "system" : NormalizeText(request.ReviewedBy, 100),
                ReviewedUtc = request.ReviewedUtc ?? DateTime.UtcNow
            };
        }

        private static string MapActionToStatus(string actionKey)
        {
            if (string.Equals(actionKey, PortalWorkflowActions.Approve, StringComparison.Ordinal))
            {
                return PortalBusinessApplicationStatuses.Approved;
            }

            if (string.Equals(actionKey, PortalWorkflowActions.Return, StringComparison.Ordinal))
            {
                return PortalBusinessApplicationStatuses.Returned;
            }

            if (string.Equals(actionKey, PortalWorkflowActions.Reject, StringComparison.Ordinal))
            {
                return PortalBusinessApplicationStatuses.Rejected;
            }

            if (string.Equals(actionKey, PortalWorkflowActions.Close, StringComparison.Ordinal))
            {
                return PortalBusinessApplicationStatuses.Closed;
            }

            return string.Empty;
        }

        private static string CreateApplicationCode(DateTime submittedUtc)
        {
            return "BA-" + submittedUtc.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "-" +
                   Guid.NewGuid().ToString("N").Substring(0, 8);
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

        private static string NormalizeStatusFilter(string status)
        {
            return string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim();
        }

        private static int NormalizeTake(int take, int defaultValue)
        {
            if (take <= 0)
            {
                return defaultValue;
            }

            return Math.Min(take, 200);
        }

        private static SqlParameter CreateNullableStringParameter(string name, string value)
        {
            return new SqlParameter(name, string.IsNullOrEmpty(value) ? (object)DBNull.Value : value);
        }

        private static SqlParameter CreateNullableIntParameter(string name, int? value)
        {
            return new SqlParameter(name, value.HasValue ? (object)value.Value : DBNull.Value);
        }

        private sealed class BusinessApplicationReviewWriteRow
        {
            public long ApplicationId { get; set; }

            public string ApplicationCode { get; set; }
        }
    }
}
