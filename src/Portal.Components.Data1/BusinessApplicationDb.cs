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
    ///   <zh-CN>P19.4 第一版只处理低敏纯文本申请、单层审核状态和流程事件事实；待办投影与运营审计由页面层在业务事实写入成功后旁路记录。本类负责受控 SQL 数据访问，不负责当前用户授权，也不把申请事实、待办投影和运营审计扩展为一个跨门面原子事务。</zh-CN>
    ///   <en>The first P19.4 version handles only low-sensitivity plain-text applications, one-level review states, and workflow-event facts; page code records work-item projections and operation audits after business facts succeed. This class performs controlled SQL data access, but it does not authorize the current user or expand application facts, work-item projections, and operation audits into one cross-facade atomic transaction.</en>
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
        /// <param name="context">
        /// <l>
        ///   <zh-CN>当前数据源上下文；本类只保存引用，不负责创建、释放或切换上下文。</zh-CN>
        ///   <en>The current data-source context. This class stores the reference and does not create, dispose, or switch the context.</en>
        /// </l>
        /// </param>
        public BusinessApplicationDb(PortalBizDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查申请表和流程事件表是否同时可访问。</zh-CN>
        ///   <en>Checks whether both the application and workflow-event tables are accessible.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>这是一次可用性探测，不证明后续写入成功，也不执行业务授权；表缺失、元数据查询失败和连接异常均按不可用处理。</zh-CN>
        ///   <en>This is an availability probe. It guarantees neither a later successful write nor business authorization; missing tables, metadata-query failures, and connection errors are treated as unavailable.</en>
        /// </lang>
        /// </remarks>
        /// <returns>
        /// <l>
        ///   <zh-CN>两个受控表均可访问时为 <c>true</c>，否则为 <c>false</c>。</zh-CN>
        ///   <en><c>true</c> when both controlled tables are accessible; otherwise <c>false</c>.</en>
        /// </l>
        /// </returns>
        public bool IsSchemaAvailable()
        {
            return HasTable(ApplicationTableName) && HasTable(WorkflowEventTableName);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>归一化并提交一条申请事实，同时写入 Submit 流程事件。</zh-CN>
        ///   <en>Normalizes and submits one application fact together with its Submit workflow event.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>空请求、必填字段缺失、架构不可用和 SQL 异常均返回低敏失败结果。申请与 Submit 事件在同一 SQL 批次写入，但后续待办/审计旁路不属于本方法事务；本方法也不替调用方证明当前用户身份或提交权限。</zh-CN>
        ///   <en>Null requests, missing required fields, unavailable schema, and SQL exceptions return low-sensitivity failure results. The application and Submit event are written in one SQL batch, but later work-item/audit sidecars are outside this method's transaction; the method also does not prove the caller's identity or submission authorization.</en>
        /// </lang>
        /// </remarks>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>提交参数；实现会复制、裁剪并补默认值，不修改调用方实例。</zh-CN>
        ///   <en>Submission parameters. The implementation copies, trims, and defaults the values without mutating the caller instance.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>成功时包含申请标识和编号；失败时只提供可展示的低敏说明。</zh-CN>
        ///   <en>The application identifier and code on success, or only a display-safe low-sensitivity explanation on failure.</en>
        /// </l>
        /// </returns>
        public BusinessApplicationResult SubmitApplication(BusinessApplicationSubmitRequest request)
        {
            // <lang>
            //   <zh-CN>先生成不可变于调用方的归一化快照；后续必填校验和 SQL 参数都只使用该快照，避免原请求在流程中改变。</zh-CN>
            //   <en>Build a normalized snapshot independent of caller mutations first; required-field checks and SQL parameters use only that snapshot.</en>
            // </lang>
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
                // <lang>
                //   <zh-CN>申请和 Submit 事件的批次失败时统一返回低敏结果，不把 SQL、连接或表结构细节暴露给页面。</zh-CN>
                //   <en>When the application-and-Submit batch fails, return one low-sensitivity result without exposing SQL, connection, or schema details to the page.</en>
                // </lang>
                return new BusinessApplicationResult(false, 0, string.Empty, "Business application submission failed.");
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取指定门户用户的最新申请查询快照。</zh-CN>
        ///   <en>Reads a newest-first application query snapshot for a specified Portal user.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>非正用户标识、架构不可用或查询异常返回空列表；空列表不能区分无申请与读取失败。方法只按标识过滤，不负责证明当前请求用户确实拥有该标识。</zh-CN>
        ///   <en>Non-positive user identifiers, unavailable schema, and query failures return an empty list; an empty list cannot distinguish no applications from a read failure. The method filters by identifier only and does not prove that the current request user owns that identifier.</en>
        /// </lang>
        /// </remarks>
        /// <param name="applicantUserId">
        /// <l>
        ///   <zh-CN>正数申请人门户用户标识；非正数直接返回空列表。</zh-CN>
        ///   <en>Positive applicant Portal user identifier; non-positive values return an empty list.</en>
        /// </l>
        /// </param>
        /// <param name="take">
        /// <l>
        ///   <zh-CN>期望最大条数；实现对非正数使用 10，并把上限限制为 200。</zh-CN>
        ///   <en>Requested maximum count; the implementation uses 10 for non-positive values and caps the count at 200.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>按提交时间和主键倒序的可变投影列表。</zh-CN>
        ///   <en>A mutable projection list ordered by submission time and then primary key descending.</en>
        /// </l>
        /// </returns>
        public IList<BusinessApplicationInfo> GetRecentApplicationsForUser(int applicantUserId, int take)
        {
            if (applicantUserId <= 0 || !IsSchemaAvailable())
            {
                return new List<BusinessApplicationInfo>();
            }

            try
            {
                // <lang>
                //   <zh-CN>用户列表路径固定按申请人标识过滤，并把条数限制交给共享 helper；它不从页面输入推导额外 where 片段。</zh-CN>
                //   <en>The user-list path filters only by the applicant identifier and delegates the row bound to the shared helper; it does not derive extra where fragments from page input.</en>
                // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取后台申请列表查询快照。</zh-CN>
        ///   <en>Reads an administration application-list query snapshot.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>本方法不执行管理员授权；调用方必须先完成页面访问控制。空状态表示不过滤，状态文本只裁剪不替换为稳定常量；架构不可用或查询异常返回空列表。</zh-CN>
        ///   <en>This method does not authorize administrators; callers must enforce page access control first. An empty status means no filter, and status text is only trimmed rather than replaced with a stable constant. Unavailable schema or query failures return an empty list.</en>
        /// </lang>
        /// </remarks>
        /// <param name="status">
        /// <l>
        ///   <zh-CN>可选精确状态筛选。</zh-CN>
        ///   <en>Optional exact status filter.</en>
        /// </l>
        /// </param>
        /// <param name="take">
        /// <l>
        ///   <zh-CN>期望最大条数；实现对非正数使用 50，并把上限限制为 200。</zh-CN>
        ///   <en>Requested maximum count; the implementation uses 50 for non-positive values and caps the count at 200.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>最新提交优先的可变投影列表。</zh-CN>
        ///   <en>A mutable list of projections ordered by newest submission first.</en>
        /// </l>
        /// </returns>
        public IList<BusinessApplicationInfo> GetAdminApplications(string status, int take)
        {
            if (!IsSchemaAvailable())
            {
                return new List<BusinessApplicationInfo>();
            }

            string normalizedStatus = NormalizeStatusFilter(status);
            try
            {
                // <lang>
                //   <zh-CN>后台路径先把状态和条数归一化，再选择固定的无筛选或参数化状态 where 片段；不拼接用户文本到 SQL。</zh-CN>
                //   <en>Normalize status and count before choosing a fixed no-filter or parameterized-status where fragment; user text is never concatenated into SQL.</en>
                // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>在仍可审核的申请上执行受控审核动作，并记录 WorkflowEvent。</zh-CN>
        ///   <en>Applies a controlled review action to a still-reviewable application and records a WorkflowEvent.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>动作键只映射到固定状态；更新条件限制为 Submitted/InReview。空请求、非法动作、未找到可审核申请、架构不可用和 SQL 异常均返回低敏结果；审核授权必须由调用方在进入本方法前完成。</zh-CN>
        ///   <en>Action keys map only to fixed statuses, and the update is limited to Submitted/InReview. Null requests, unsupported actions, no reviewable application, unavailable schema, and SQL exceptions return low-sensitivity results; callers must authorize the review before entering this method.</en>
        /// </lang>
        /// </remarks>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>审核参数；实现会复制、裁剪并补默认值，不修改调用方实例。</zh-CN>
        ///   <en>Review parameters. The implementation copies, trims, and defaults the values without mutating the caller instance.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>成功时包含申请标识和编号；失败时只提供可展示的低敏说明。</zh-CN>
        ///   <en>The application identifier and code on success, or only a display-safe low-sensitivity explanation on failure.</en>
        /// </l>
        /// </returns>
        public BusinessApplicationResult ReviewApplication(BusinessApplicationReviewRequest request)
        {
            // <lang>
            //   <zh-CN>审核路径先生成独立归一化快照，再把动作键映射为固定目标状态；后续 SQL 不读取原始请求。</zh-CN>
            //   <en>The review path first creates an independent normalized snapshot and maps the action to a fixed target status; later SQL reads no raw request.</en>
            // </lang>
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
                // <lang>
                //   <zh-CN>审核批次失败只返回与申请标识关联的低敏结果，不将 SQL 或异常细节传到页面。</zh-CN>
                //   <en>Return only a low-sensitivity result associated with the application identifier; do not send SQL or exception details to the page.</en>
                // </lang>
                return new BusinessApplicationResult(false, normalized.ApplicationId, string.Empty, "Business application review failed.");
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>使用受控 where 片段和参数查询申请投影。</zh-CN>
        ///   <en>Queries application projections with a controlled where fragment and parameters.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>调用方只能传入本类固定的 where 片段；用户值必须通过参数传入。查询只返回列表/详情投影，不执行授权或输出编码。</zh-CN>
        ///   <en>Callers may pass only where fragments fixed by this class; user values must be parameters. The query returns list/detail projections only and performs neither authorization nor output encoding.</en>
        /// </lang>
        /// </remarks>
        /// <param name="whereClause">
        /// <l>
        ///   <zh-CN>受控 SQL 条件片段，可为空；不得来自用户输入。</zh-CN>
        ///   <en>A controlled SQL condition fragment, possibly empty; it must not come from user input.</en>
        /// </l>
        /// </param>
        /// <param name="take">
        /// <l>
        ///   <zh-CN>已归一化的最大条数。</zh-CN>
        ///   <en>The already-normalized maximum row count.</en>
        /// </l>
        /// </param>
        /// <param name="parameters">
        /// <l>
        ///   <zh-CN>与条件片段对应的参数，可为空。</zh-CN>
        ///   <en>Parameters corresponding to the condition fragment, possibly null.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>最新提交优先的可变申请投影列表。</zh-CN>
        ///   <en>A mutable list of application projections ordered by newest submission first.</en>
        /// </l>
        /// </returns>
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

            // <lang>
            //   <zh-CN>条数始终作为参数传入；固定 where 片段之外的筛选值由调用方提供参数，避免把用户文本拼入 SQL。</zh-CN>
            //   <en>Always pass the row count as a parameter; callers provide parameters for values outside the fixed where fragment so user text is never concatenated into SQL.</en>
            // </lang>
            var sqlParameters = new List<SqlParameter> { new SqlParameter("@Take", take) };
            if (parameters != null)
            {
                sqlParameters.AddRange(parameters);
            }

            return context.Database.SqlQuery<BusinessApplicationInfo>(sql, sqlParameters.ToArray()).ToList();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查受控 SQL Server 表是否存在。</zh-CN>
        ///   <en>Checks whether a controlled SQL Server table exists.</en>
        /// </lang>
        /// </summary>
        /// <param name="tableName">
        /// <l>
        ///   <zh-CN>仅允许来自本类常量的表名。</zh-CN>
        ///   <en>A table name that must come only from constants in this class.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>表可访问时为 <c>true</c>；任何异常按不可用处理。</zh-CN>
        ///   <en><c>true</c> when the table is accessible; any exception is treated as unavailable.</en>
        /// </l>
        /// </returns>
        private bool HasTable(string tableName)
        {
            try
            {
                // <lang>
                //   <zh-CN>表名来自固定常量，OBJECT_ID 查询不接收用户输入；元数据失败时采用安全的不可用回退。</zh-CN>
                //   <en>Table names come from fixed constants, so the OBJECT_ID query accepts no user input; metadata failures use a safe unavailable fallback.</en>
                // </lang>
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
        ///   <zh-CN>复制并归一化申请提交参数。</zh-CN>
        ///   <en>Copies and normalizes application-submission parameters.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>原始请求，可为空。</zh-CN>
        ///   <en>The raw request, possibly null.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>长度、可选值、审核键、UTC 时间和提交人已归一化的新实例。</zh-CN>
        ///   <en>A new instance with lengths, optional values, review key, UTC time, and submitter normalized.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>复制并归一化审核请求。</zh-CN>
        ///   <en>Copies and normalizes a review request.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>原始请求，可为空。</zh-CN>
        ///   <en>The raw request, possibly null.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>动作、意见、审核人和 UTC 时间已归一化的新实例。</zh-CN>
        ///   <en>A new instance with action, comment, reviewer, and UTC time normalized.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>把受控流程动作映射为稳定申请状态。</zh-CN>
        ///   <en>Maps a controlled workflow action to a stable application status.</en>
        /// </lang>
        /// </summary>
        /// <param name="actionKey">
        /// <l>
        ///   <zh-CN>归一化动作键。</zh-CN>
        ///   <en>The normalized action key.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>受支持时返回稳定状态，否则返回空字符串。</zh-CN>
        ///   <en>The stable status when supported; otherwise an empty string.</en>
        /// </l>
        /// </returns>
        private static string MapActionToStatus(string actionKey)
        {
            if (string.Equals(actionKey, PortalWorkflowActions.Approve, StringComparison.Ordinal))
            {
                // <lang>
                //   <zh-CN>动作到状态映射只接受固定常量，未知动作由方法末尾返回空字符串。</zh-CN>
                //   <en>Action-to-status mapping accepts only fixed constants; unknown actions return an empty string at the end.</en>
                // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>根据提交 UTC 时间和随机后缀生成展示用申请编号。</zh-CN>
        ///   <en>Creates a display-oriented application code from submission UTC time and a random suffix.</en>
        /// </lang>
        /// </summary>
        /// <param name="submittedUtc">
        /// <l>
        ///   <zh-CN>已归一化的提交 UTC 时间。</zh-CN>
        ///   <en>The normalized submission UTC time.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>不承载授权或保密信息的申请编号。</zh-CN>
        ///   <en>An application code that carries neither authorization nor secret information.</en>
        /// </l>
        /// </returns>
        private static string CreateApplicationCode(DateTime submittedUtc)
        {
            return "BA-" + submittedUtc.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "-" +
                   Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>裁剪必填或默认文本并限制最大长度。</zh-CN>
        ///   <en>Trims required or default text and limits its maximum length.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>原始文本值。</zh-CN>
        ///   <en>The raw text value.</en>
        /// </l>
        /// </param>
        /// <param name="maxLength">
        /// <l>
        ///   <zh-CN>允许进入参数的最大字符数。</zh-CN>
        ///   <en>The maximum character count allowed into parameters.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>裁剪并截断后的非 null 文本。</zh-CN>
        ///   <en>A non-null trimmed and truncated text value.</en>
        /// </l>
        /// </returns>
        private static string NormalizeText(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>裁剪可选文本，并把空白值转换为 null。</zh-CN>
        ///   <en>Trims optional text and converts blank values to null.</en>
        /// </lang>
        /// </summary>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>原始可选文本。</zh-CN>
        ///   <en>The raw optional text.</en>
        /// </l>
        /// </param>
        /// <param name="maxLength">
        /// <l>
        ///   <zh-CN>允许进入参数的最大字符数。</zh-CN>
        ///   <en>The maximum character count allowed into parameters.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>非空裁剪文本或 null。</zh-CN>
        ///   <en>A non-empty trimmed text value or null.</en>
        /// </l>
        /// </returns>
        private static string NormalizeOptionalText(string value, int maxLength)
        {
            string normalized = NormalizeText(value, maxLength);
            return normalized.Length == 0 ? null : normalized;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>裁剪后台状态筛选，并以空字符串表达不筛选。</zh-CN>
        ///   <en>Trims an administration status filter and represents no filter as an empty string.</en>
        /// </lang>
        /// </summary>
        /// <param name="status">
        /// <l>
        ///   <zh-CN>页面提供的状态文本。</zh-CN>
        ///   <en>The status text supplied by the page.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>裁剪后的状态文本或空字符串。</zh-CN>
        ///   <en>The trimmed status text or an empty string.</en>
        /// </l>
        /// </returns>
        private static string NormalizeStatusFilter(string status)
        {
            return string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把列表条数限制在默认值到 200 的安全范围内。</zh-CN>
        ///   <en>Constrains a list row count to a safe range using a default and a maximum of 200.</en>
        /// </lang>
        /// </summary>
        /// <param name="take">
        /// <l>
        ///   <zh-CN>调用方期望条数。</zh-CN>
        ///   <en>The caller-requested row count.</en>
        /// </l>
        /// </param>
        /// <param name="defaultValue">
        /// <l>
        ///   <zh-CN>非正数输入的默认条数。</zh-CN>
        ///   <en>The default count for non-positive input.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>不超过 200 的正数条数。</zh-CN>
        ///   <en>A positive row count no greater than 200.</en>
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
        ///   <zh-CN>创建把空字符串映射为数据库 NULL 的字符串参数。</zh-CN>
        ///   <en>Creates a string parameter that maps empty text to database NULL.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>受控 SQL 参数名。</zh-CN>
        ///   <en>The controlled SQL parameter name.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>可选字符串值。</zh-CN>
        ///   <en>The optional string value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>对应参数实例。</zh-CN>
        ///   <en>The corresponding parameter instance.</en>
        /// </l>
        /// </returns>
        private static SqlParameter CreateNullableStringParameter(string name, string value)
        {
            return new SqlParameter(name, string.IsNullOrEmpty(value) ? (object)DBNull.Value : value);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建把空整数映射为数据库 NULL 的整数参数。</zh-CN>
        ///   <en>Creates an integer parameter that maps an absent value to database NULL.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>受控 SQL 参数名。</zh-CN>
        ///   <en>The controlled SQL parameter name.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>可选整数值。</zh-CN>
        ///   <en>The optional integer value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>对应参数实例。</zh-CN>
        ///   <en>The corresponding parameter instance.</en>
        /// </l>
        /// </returns>
        private static SqlParameter CreateNullableIntParameter(string name, int? value)
        {
            return new SqlParameter(name, value.HasValue ? (object)value.Value : DBNull.Value);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>承接审核 SQL OUTPUT 结果的内部行模型。</zh-CN>
        ///   <en>Internal row model for the review SQL OUTPUT result.</en>
        /// </lang>
        /// </summary>
        private sealed class BusinessApplicationReviewWriteRow
        {
            /// <summary>
            /// <lang>
            ///   <zh-CN>被更新申请的主键。</zh-CN>
            ///   <en>Primary key of the updated application.</en>
            /// </lang>
            /// </summary>
            public long ApplicationId { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>被更新申请的稳定编号。</zh-CN>
            ///   <en>Stable code of the updated application.</en>
            /// </lang>
            /// </summary>
            public string ApplicationCode { get; set; }
        }
    }
}
