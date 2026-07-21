using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：基于 <see cref="PortalBizDbContext"/> 的轻量审批/待办数据访问实现。
    ///
    /// English: Lightweight approval/work-item data-access implementation backed by <see cref="PortalBizDbContext"/>.
    /// </summary>
    /// <remarks>
    /// 中文：P12.3 第一版只保存待办当前态和事件流水。调用方应把它视为业务流程补充能力；
    /// 表缺失或写入失败不得阻断已经成功的原业务动作。
    ///
    /// English: The first P12.3 version stores only current work-item state and event history. Callers should treat it
    /// as a supplemental business-flow capability; missing tables or write failures must not block already-successful
    /// domain operations.
    /// </remarks>
    public sealed class PortalWorkItemDb : IPortalWorkItemDb
    {
        private const string WorkItemTableName = "PortalBiz_WorkItems";
        private const string WorkItemEventTableName = "PortalBiz_WorkItemEvents";
        private readonly PortalBizDbContext context;

        /// <summary>
        /// 中文：初始化轻量待办数据访问实现。
        ///
        /// English: Initializes the lightweight work-item data-access implementation.
        /// </summary>
        /// <param name="context">中文：企业业务基础数据上下文。English: Enterprise business foundation data context.</param>
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

        private static int NormalizeTake(int take, int defaultValue)
        {
            if (take <= 0)
            {
                return defaultValue;
            }

            return Math.Min(take, 200);
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

        private static SqlParameter CreateNullableIntParameter(string name, int? value)
        {
            return new SqlParameter(name, value.HasValue ? (object)value.Value : DBNull.Value);
        }

        private static SqlParameter CreateNullableDateTimeParameter(string name, DateTime? value)
        {
            return new SqlParameter(name, value.HasValue ? (object)value.Value : DBNull.Value);
        }

        private static SqlParameter CreateNullableStringParameter(string name, string value)
        {
            return new SqlParameter(name, string.IsNullOrEmpty(value) ? (object)DBNull.Value : value);
        }
    }
}
