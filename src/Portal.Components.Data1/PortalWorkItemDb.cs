using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>基于 <see cref="PortalBizDbContext"/> 的轻量审批工作项数据访问实现。</zh-CN>
    ///   <en>Lightweight approval/work-item data-access implementation backed by <see cref="PortalBizDbContext"/>.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P12.3 第一版只保存审批工作项当前态和事件流水。调用方应把它视为业务流程补充能力，而不是业务授权入口；表缺失或写入失败不得阻断已经成功的原业务动作。</zh-CN>
    ///   <en>The first P12.3 version stores only current work-item state and event history. Callers should treat it as supplemental business-flow capability rather than a business authorization entry; missing tables or write failures must not block already-successful domain operations.</en>
    /// </lang>
    /// </remarks>
    public sealed class PortalWorkItemDb : IPortalWorkItemDb
    {
        private const string WorkItemTableName = "PortalBiz_WorkItems";
        private const string WorkItemEventTableName = "PortalBiz_WorkItemEvents";
        private readonly PortalBizDbContext context;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化轻量审批工作项数据访问实现。</zh-CN>
        ///   <en>Initializes the lightweight work-item data-access implementation.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>企业业务基础数据上下文。</zh-CN>
        ///   <en>Enterprise business foundation data context.</en>
        /// </l>
        /// </param>
        public PortalWorkItemDb(PortalBizDbContext context)
        {
            this.context = context;
        }

        /// <inheritdoc />
        public bool IsSchemaAvailable()
        {
            return HasTable(WorkItemTableName) && HasTable(WorkItemEventTableName);
        }

        /// <inheritdoc />
        public PortalWorkItemResult EnsureWorkItem(PortalWorkItemCreateRequest request)
        {
            PortalWorkItemCreateRequest normalized = NormalizeCreateRequest(request);
            if (string.IsNullOrWhiteSpace(normalized.BusinessKind) ||
                string.IsNullOrWhiteSpace(normalized.BusinessId) ||
                string.IsNullOrWhiteSpace(normalized.Title))
            {
                return new PortalWorkItemResult(false, 0, "Business kind, business id, and title are required.");
            }

            if (!normalized.AssignedUserId.HasValue && string.IsNullOrWhiteSpace(normalized.AssignedRoleKey))
            {
                return new PortalWorkItemResult(false, 0, "A work item requires an assigned user or role key.");
            }

            if (!IsSchemaAvailable())
            {
                return new PortalWorkItemResult(false, 0, "Portal work-item schema is unavailable.");
            }

            try
            {
                // <lang>
                //   <zh-CN>同一业务对象若已存在未完成审批工作项，则复用最新一条；否则在同一批 SQL 中创建工作项并记录 Created 事件。</zh-CN>
                //   <en>If the same business object already has an unfinished work item, reuse the newest one; otherwise create the work item and its Created event in the same SQL batch.</en>
                // </lang>
                List<long> rows = context.Database.SqlQuery<long>(
                    @"
DECLARE @WorkItemId BIGINT;

SELECT TOP (1)
    @WorkItemId = [WorkItemId]
FROM [dbo].[PortalBiz_WorkItems]
WHERE [BusinessKind] = @BusinessKind
  AND [BusinessId] = @BusinessId
  AND [WorkItemStatus] IN (N'Open', N'InProgress')
ORDER BY [CreatedUtc] DESC, [WorkItemId] DESC;

IF @WorkItemId IS NULL
BEGIN
    INSERT INTO [dbo].[PortalBiz_WorkItems]
        ([BusinessKind],
         [BusinessId],
         [Title],
         [Summary],
         [WorkItemStatus],
         [AssignedUserId],
         [AssignedRoleKey],
         [CreatedUtc],
         [CreatedBy],
         [DueUtc])
    VALUES
        (@BusinessKind,
         @BusinessId,
         @Title,
         @Summary,
         N'Open',
         @AssignedUserId,
         @AssignedRoleKey,
         @CreatedUtc,
         @CreatedBy,
         @DueUtc);

    SET @WorkItemId = CONVERT(BIGINT, SCOPE_IDENTITY());

    INSERT INTO [dbo].[PortalBiz_WorkItemEvents]
        ([WorkItemId], [OccurredUtc], [EventType], [ActorUserId], [ActorName], [FromStatus], [ToStatus], [Comment])
    VALUES
        (@WorkItemId, @CreatedUtc, N'Created', NULL, @CreatedBy, NULL, N'Open', @Summary);
END

SELECT @WorkItemId;",
                    new SqlParameter("@BusinessKind", normalized.BusinessKind),
                    new SqlParameter("@BusinessId", normalized.BusinessId),
                    new SqlParameter("@Title", normalized.Title),
                    CreateNullableStringParameter("@Summary", normalized.Summary),
                    CreateNullableIntParameter("@AssignedUserId", normalized.AssignedUserId),
                    CreateNullableStringParameter("@AssignedRoleKey", normalized.AssignedRoleKey),
                    new SqlParameter("@CreatedUtc", normalized.CreatedUtc.Value),
                    new SqlParameter("@CreatedBy", normalized.CreatedBy),
                    CreateNullableDateTimeParameter("@DueUtc", normalized.DueUtc)).ToList();

                long workItemId = rows.Count == 0 ? 0 : rows[0];
                return workItemId <= 0
                    ? new PortalWorkItemResult(false, 0, "Work item was not created.")
                    : new PortalWorkItemResult(true, workItemId, "Work item is available.");
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>审批工作项只是业务动作之后的补充记录，创建失败时返回低敏失败结果，避免把异常细节抛给页面或中断已完成业务。</zh-CN>
                //   <en>Work items are supplemental records after the domain action, so creation failures return low-sensitivity failure results instead of exposing exception details or interrupting completed business work.</en>
                // </lang>
                return new PortalWorkItemResult(false, 0, "Work item creation failed.");
            }
        }

        /// <inheritdoc />
        public PortalWorkItemResult CompleteBusinessWorkItem(PortalWorkItemCompletionRequest request)
        {
            PortalWorkItemCompletionRequest normalized = NormalizeCompletionRequest(request);
            if (string.IsNullOrWhiteSpace(normalized.BusinessKind) ||
                string.IsNullOrWhiteSpace(normalized.BusinessId) ||
                string.IsNullOrWhiteSpace(normalized.TargetStatus) ||
                string.IsNullOrWhiteSpace(normalized.EventType))
            {
                return new PortalWorkItemResult(false, 0, "Business kind, business id, target status, and event type are required.");
            }

            if (!IsSchemaAvailable())
            {
                return new PortalWorkItemResult(false, 0, "Portal work-item schema is unavailable.");
            }

            try
            {
                // <lang>
                //   <zh-CN>通过 OUTPUT 捕获被更新的审批工作项标识和旧状态，再写入状态事件；没有未完成工作项时返回 0，由调用方决定是否需要提示。</zh-CN>
                //   <en>Use OUTPUT to capture the updated work-item identity and previous status, then write a status event; when no unfinished item exists, return 0 and let the caller decide whether to show guidance.</en>
                // </lang>
                List<long> rows = context.Database.SqlQuery<long>(
                    @"
DECLARE @Updated TABLE
(
    [WorkItemId] BIGINT NOT NULL,
    [FromStatus] NVARCHAR(20) NOT NULL
);

UPDATE [dbo].[PortalBiz_WorkItems]
SET [WorkItemStatus] = @TargetStatus,
    [CompletedUtc] = @OccurredUtc,
    [CompletedBy] = @ActorName
OUTPUT INSERTED.[WorkItemId], DELETED.[WorkItemStatus]
INTO @Updated ([WorkItemId], [FromStatus])
WHERE [BusinessKind] = @BusinessKind
  AND [BusinessId] = @BusinessId
  AND [WorkItemStatus] IN (N'Open', N'InProgress');

DECLARE @WorkItemId BIGINT;
DECLARE @FromStatus NVARCHAR(20);

SELECT TOP (1)
    @WorkItemId = [WorkItemId],
    @FromStatus = [FromStatus]
FROM @Updated;

IF @WorkItemId IS NOT NULL
BEGIN
    INSERT INTO [dbo].[PortalBiz_WorkItemEvents]
        ([WorkItemId], [OccurredUtc], [EventType], [ActorUserId], [ActorName], [FromStatus], [ToStatus], [Comment])
    VALUES
        (@WorkItemId, @OccurredUtc, @EventType, @ActorUserId, @ActorName, @FromStatus, @TargetStatus, @Comment);
END

SELECT ISNULL(@WorkItemId, 0);",
                    new SqlParameter("@BusinessKind", normalized.BusinessKind),
                    new SqlParameter("@BusinessId", normalized.BusinessId),
                    new SqlParameter("@TargetStatus", normalized.TargetStatus),
                    new SqlParameter("@OccurredUtc", normalized.OccurredUtc.Value),
                    new SqlParameter("@EventType", normalized.EventType),
                    CreateNullableIntParameter("@ActorUserId", normalized.ActorUserId),
                    new SqlParameter("@ActorName", normalized.ActorName),
                    CreateNullableStringParameter("@Comment", normalized.Comment)).ToList();

                long workItemId = rows.Count == 0 ? 0 : rows[0];
                return workItemId <= 0
                    ? new PortalWorkItemResult(false, 0, "No unfinished work item was found.")
                    : new PortalWorkItemResult(true, workItemId, "Work item state updated.");
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>状态回写失败不应暴露 SQL 或表结构细节；调用方只接收可展示的低敏失败说明。</zh-CN>
                //   <en>Status write-back failures should not expose SQL or schema details; callers receive only display-safe failure text.</en>
                // </lang>
                return new PortalWorkItemResult(false, 0, "Work item state update failed.");
            }
        }

        /// <inheritdoc />
        public IList<PortalWorkItemInfo> GetAdminWorkItems(string status, int take)
        {
            if (!IsSchemaAvailable())
            {
                return new List<PortalWorkItemInfo>();
            }

            string normalizedStatus = NormalizeStatusFilter(status);
            int safeTake = NormalizeTake(take, 50);
            try
            {
                // <lang>
                //   <zh-CN>管理员列表只读取审批工作项摘要和被分配用户显示名；条数和状态过滤在进入 SQL 前已标准化，避免异常输入扩大查询范围。</zh-CN>
                //   <en>The administration list reads only work-item summaries and assigned-user display names; row count and status filters are normalized before entering SQL so unusual input cannot expand the query scope.</en>
                // </lang>
                return context.Database.SqlQuery<PortalWorkItemInfo>(
                    @"
SELECT TOP (@Take)
    [WorkItem].[WorkItemId],
    [WorkItem].[BusinessKind],
    [WorkItem].[BusinessId],
    [WorkItem].[Title],
    [WorkItem].[Summary],
    [WorkItem].[WorkItemStatus],
    [WorkItem].[AssignedUserId],
    [User].[Name] AS [AssignedUserName],
    [WorkItem].[AssignedRoleKey],
    [WorkItem].[CreatedUtc],
    [WorkItem].[CreatedBy],
    [WorkItem].[DueUtc],
    [WorkItem].[CompletedUtc],
    [WorkItem].[CompletedBy]
FROM [dbo].[PortalBiz_WorkItems] AS [WorkItem]
LEFT JOIN [dbo].[Portal_Users] AS [User]
    ON [User].[UserID] = [WorkItem].[AssignedUserId]
WHERE (@Status = N'' OR [WorkItem].[WorkItemStatus] = @Status)
ORDER BY [WorkItem].[CreatedUtc] DESC, [WorkItem].[WorkItemId] DESC;",
                    new SqlParameter("@Take", safeTake),
                    new SqlParameter("@Status", normalizedStatus)).ToList();
            }
            catch (Exception)
            {
                return new List<PortalWorkItemInfo>();
            }
        }

        /// <inheritdoc />
        public IList<PortalWorkItemEventInfo> GetWorkItemEvents(long workItemId, int take)
        {
            if (workItemId <= 0 || !IsSchemaAvailable())
            {
                return new List<PortalWorkItemEventInfo>();
            }

            int safeTake = NormalizeTake(take, 20);
            try
            {
                // <lang>
                //   <zh-CN>事件流水按单个审批工作项标识读取，主要用于管理员追踪状态变化，不承担业务授权判断。</zh-CN>
                //   <en>Event history is read for one work-item identifier and is mainly used by administrators to trace status changes; it does not perform business authorization decisions.</en>
                // </lang>
                return context.Database.SqlQuery<PortalWorkItemEventInfo>(
                    @"
SELECT TOP (@Take)
    [EventId],
    [WorkItemId],
    [OccurredUtc],
    [EventType],
    [ActorUserId],
    [ActorName],
    [FromStatus],
    [ToStatus],
    [Comment]
FROM [dbo].[PortalBiz_WorkItemEvents]
WHERE [WorkItemId] = @WorkItemId
ORDER BY [OccurredUtc] DESC, [EventId] DESC;",
                    new SqlParameter("@Take", safeTake),
                    new SqlParameter("@WorkItemId", workItemId)).ToList();
            }
            catch (Exception)
            {
                return new List<PortalWorkItemEventInfo>();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查指定 SQL Server 表是否存在。</zh-CN>
        ///   <en>Checks whether the specified SQL Server table exists.</en>
        /// </lang>
        /// </summary>
        /// <param name="tableName">
        /// <l>
        ///   <zh-CN>受控常量表名；不得来自用户输入。</zh-CN>
        ///   <en>A controlled constant table name; it must not come from user input.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>表存在且当前连接可查询元数据时返回 <c>true</c>。</zh-CN>
        ///   <en>Returns <c>true</c> when the table exists and the current connection can query metadata.</en>
        /// </l>
        /// </returns>
        private bool HasTable(string tableName)
        {
            try
            {
                // <lang>
                //   <zh-CN>表名仅来自本类常量，拼接 OBJECT_ID 查询不会接受外部输入；任何异常都按架构不可用处理。</zh-CN>
                //   <en>Table names come only from constants in this class, so the OBJECT_ID query does not accept external input; any exception is treated as unavailable schema.</en>
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
        ///   <zh-CN>标准化审批工作项创建请求。</zh-CN>
        ///   <en>Normalizes a work-item creation request.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>调用方提交的原始创建请求，可为空。</zh-CN>
        ///   <en>The raw creation request from the caller, which may be null.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>裁剪长度、补齐默认 UTC 时间和系统操作者后的创建请求。</zh-CN>
        ///   <en>The creation request after trimming lengths and filling default UTC time and system actor.</en>
        /// </l>
        /// </returns>
        private static PortalWorkItemCreateRequest NormalizeCreateRequest(PortalWorkItemCreateRequest request)
        {
            request = request ?? new PortalWorkItemCreateRequest();
            return new PortalWorkItemCreateRequest
            {
                BusinessKind = NormalizeText(request.BusinessKind, 80),
                BusinessId = NormalizeText(request.BusinessId, 80),
                Title = NormalizeText(request.Title, 200),
                Summary = NormalizeOptionalText(request.Summary, 500),
                AssignedUserId = request.AssignedUserId.HasValue && request.AssignedUserId.Value > 0 ? request.AssignedUserId : null,
                AssignedRoleKey = NormalizeOptionalText(request.AssignedRoleKey, 120),
                CreatedUtc = request.CreatedUtc ?? DateTime.UtcNow,
                CreatedBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "system" : NormalizeText(request.CreatedBy, 100),
                DueUtc = request.DueUtc
            };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>标准化审批工作项完成请求。</zh-CN>
        ///   <en>Normalizes a work-item completion request.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>调用方提交的原始完成请求，可为空。</zh-CN>
        ///   <en>The raw completion request from the caller, which may be null.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>裁剪长度、补齐默认 UTC 时间和系统操作者后的完成请求。</zh-CN>
        ///   <en>The completion request after trimming lengths and filling default UTC time and system actor.</en>
        /// </l>
        /// </returns>
        private static PortalWorkItemCompletionRequest NormalizeCompletionRequest(PortalWorkItemCompletionRequest request)
        {
            request = request ?? new PortalWorkItemCompletionRequest();
            return new PortalWorkItemCompletionRequest
            {
                BusinessKind = NormalizeText(request.BusinessKind, 80),
                BusinessId = NormalizeText(request.BusinessId, 80),
                EventType = NormalizeText(request.EventType, 40),
                TargetStatus = NormalizeText(request.TargetStatus, 20),
                ActorUserId = request.ActorUserId.HasValue && request.ActorUserId.Value > 0 ? request.ActorUserId : null,
                ActorName = string.IsNullOrWhiteSpace(request.ActorName) ? "system" : NormalizeText(request.ActorName, 100),
                Comment = NormalizeOptionalText(request.Comment, 1000),
                OccurredUtc = request.OccurredUtc ?? DateTime.UtcNow
            };
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把列表读取条数限制在安全范围内。</zh-CN>
        ///   <en>Constrains a list-read count to a safe range.</en>
        /// </lang>
        /// </summary>
        /// <param name="take">
        /// <l>
        ///   <zh-CN>调用方期望读取的条数。</zh-CN>
        ///   <en>The caller-requested number of rows.</en>
        /// </l>
        /// </param>
        /// <param name="defaultValue">
        /// <l>
        ///   <zh-CN>输入无效时使用的默认条数。</zh-CN>
        ///   <en>The default row count used when the input is invalid.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>介于 1 到 200 之间的安全条数。</zh-CN>
        ///   <en>A safe row count between 1 and 200.</en>
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
        ///   <zh-CN>标准化管理员列表状态过滤值。</zh-CN>
        ///   <en>Normalizes the administration-list status filter.</en>
        /// </lang>
        /// </summary>
        /// <param name="status">
        /// <l>
        ///   <zh-CN>页面传入的状态过滤值。</zh-CN>
        ///   <en>The status filter supplied by the page.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>裁剪后的状态值；空字符串表示不过滤。</zh-CN>
        ///   <en>The trimmed status value; an empty string means no filtering.</en>
        /// </l>
        /// </returns>
        private static string NormalizeStatusFilter(string status)
        {
            return string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>裁剪必填文本并限制最大长度。</zh-CN>
        ///   <en>Trims required text and limits its maximum length.</en>
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
        ///   <zh-CN>允许进入 SQL 参数的最大字符数。</zh-CN>
        ///   <en>The maximum number of characters allowed into SQL parameters.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>裁剪并截断后的非空字符串；原始空值会变为空字符串。</zh-CN>
        ///   <en>The trimmed and truncated non-null string; original null values become an empty string.</en>
        /// </l>
        /// </returns>
        private static string NormalizeText(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>裁剪可选文本并把空值转换为数据库空值语义。</zh-CN>
        ///   <en>Trims optional text and converts empty text to database-null semantics.</en>
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
        ///   <zh-CN>允许进入 SQL 参数的最大字符数。</zh-CN>
        ///   <en>The maximum number of characters allowed into SQL parameters.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>裁剪并截断后的字符串；空文本返回 <c>null</c>。</zh-CN>
        ///   <en>The trimmed and truncated string; empty text returns <c>null</c>.</en>
        /// </l>
        /// </returns>
        private static string NormalizeOptionalText(string value, int maxLength)
        {
            string normalized = NormalizeText(value, maxLength);
            return normalized.Length == 0 ? null : normalized;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建可为空的整数 SQL 参数。</zh-CN>
        ///   <en>Creates a nullable integer SQL parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>SQL 参数名称。</zh-CN>
        ///   <en>The SQL parameter name.</en>
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
        ///   <zh-CN>值为空时写入 <see cref="DBNull.Value"/> 的 SQL 参数。</zh-CN>
        ///   <en>A SQL parameter that writes <see cref="DBNull.Value"/> when the value is absent.</en>
        /// </l>
        /// </returns>
        private static SqlParameter CreateNullableIntParameter(string name, int? value)
        {
            return new SqlParameter(name, value.HasValue ? (object)value.Value : DBNull.Value);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建可为空的时间 SQL 参数。</zh-CN>
        ///   <en>Creates a nullable date-time SQL parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>SQL 参数名称。</zh-CN>
        ///   <en>The SQL parameter name.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>可选 UTC 时间值。</zh-CN>
        ///   <en>The optional UTC date-time value.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>值为空时写入 <see cref="DBNull.Value"/> 的 SQL 参数。</zh-CN>
        ///   <en>A SQL parameter that writes <see cref="DBNull.Value"/> when the value is absent.</en>
        /// </l>
        /// </returns>
        private static SqlParameter CreateNullableDateTimeParameter(string name, DateTime? value)
        {
            return new SqlParameter(name, value.HasValue ? (object)value.Value : DBNull.Value);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建可为空的字符串 SQL 参数。</zh-CN>
        ///   <en>Creates a nullable string SQL parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="name">
        /// <l>
        ///   <zh-CN>SQL 参数名称。</zh-CN>
        ///   <en>The SQL parameter name.</en>
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
        ///   <zh-CN>空字符串或空引用会写入 <see cref="DBNull.Value"/> 的 SQL 参数。</zh-CN>
        ///   <en>A SQL parameter that writes <see cref="DBNull.Value"/> for empty or null strings.</en>
        /// </l>
        /// </returns>
        private static SqlParameter CreateNullableStringParameter(string name, string value)
        {
            return new SqlParameter(name, string.IsNullOrEmpty(value) ? (object)DBNull.Value : value);
        }
    }
}
