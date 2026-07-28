using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>基于 <see cref="PortalBizDbContext"/> 的企业协同事项数据访问实现。</zh-CN>
    ///   <en>Enterprise collaboration-item data-access implementation backed by <see cref="PortalBizDbContext"/>.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P21.3 第一版只处理低敏事项主数据、有限状态动作和事项事件事实；待办投影与运营审计由页面层在业务事实写入成功后旁路记录。</zh-CN>
    ///   <en>The first P21.3 version handles only low-sensitivity item facts, finite state actions, and item-event facts; work-item projections and operation audits are recorded by page code after the business facts are written.</en>
    /// </lang>
    /// </remarks>
    public sealed class CollaborationItemDb : ICollaborationItemDb
    {
        private const string ItemTableName = "PortalBiz_CollaborationItems";
        private const string EventTableName = "PortalBiz_CollaborationItemEvents";
        private readonly PortalBizDbContext context;

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化企业协同事项数据访问实现。</zh-CN>
        ///   <en>Initializes the enterprise collaboration-item data-access implementation.</en>
        /// </lang>
        /// </summary>
        /// <param name="context">
        /// <l>
        ///   <zh-CN>企业业务基础数据上下文。</zh-CN>
        ///   <en>Enterprise business foundation data context.</en>
        /// </l>
        /// </param>
        public CollaborationItemDb(PortalBizDbContext context)
        {
            this.context = context;
        }

        /// <inheritdoc />
        public bool IsSchemaAvailable()
        {
            return HasTable(ItemTableName) && HasTable(EventTableName);
        }

        /// <inheritdoc />
        public CollaborationItemResult CreateSubmittedItem(CollaborationItemCreateRequest request)
        {
            CollaborationItemCreateRequest normalized = NormalizeCreateRequest(request);
            if (normalized.InitiatorUserId <= 0)
            {
                return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "A signed-in portal user is required.");
            }

            if (string.IsNullOrWhiteSpace(normalized.Title))
            {
                return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "Collaboration item title is required.");
            }

            if (!normalized.OwnerUserId.HasValue && string.IsNullOrWhiteSpace(normalized.OwnerRoleKey))
            {
                return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "An owner user or owner role is required.");
            }

            if (!IsSchemaAvailable())
            {
                return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "Collaboration item schema is unavailable.");
            }

            string itemCode = CreateItemCode(normalized.SubmittedUtc.Value);
            try
            {
                // <lang>
                //   <zh-CN>协同事项事实和 Submit 事件必须同批写入，保证主状态与事件时间线不会分裂；待办投影稍后由页面层旁路创建。</zh-CN>
                //   <en>The collaboration-item fact and Submit event are written in the same batch so the current state and event timeline cannot split; the page layer later creates the work-item projection as a sidecar.</en>
                // </lang>
                List<long> rows = context.Database.SqlQuery<long>(
                    @"
DECLARE @ItemId BIGINT;

INSERT INTO [dbo].[PortalBiz_CollaborationItems]
    ([ItemCode],
     [ItemTypeKey],
     [Title],
     [Summary],
     [Description],
     [ItemStatus],
     [InitiatorUserId],
     [InitiatorEmployeeId],
     [OwnerUserId],
     [OwnerRoleKey],
     [OrganizationUnitId],
     [PriorityKey],
     [DueUtc],
     [SubmittedUtc],
     [LastActionUtc],
     [LastActionByUserId],
     [LastActionComment],
     [CreatedUtc],
     [CreatedBy],
     [UpdatedUtc],
     [UpdatedBy])
VALUES
    (@ItemCode,
     @ItemTypeKey,
     @Title,
     @Summary,
     @Description,
     N'Submitted',
     @InitiatorUserId,
     @InitiatorEmployeeId,
     @OwnerUserId,
     @OwnerRoleKey,
     @OrganizationUnitId,
     @PriorityKey,
     @DueUtc,
     @SubmittedUtc,
     @SubmittedUtc,
     @InitiatorUserId,
     @Summary,
     @SubmittedUtc,
     @SubmittedBy,
     @SubmittedUtc,
     @SubmittedBy);

SET @ItemId = CONVERT(BIGINT, SCOPE_IDENTITY());

INSERT INTO [dbo].[PortalBiz_CollaborationItemEvents]
    ([ItemId], [OccurredUtc], [ActionKey], [ActorUserId], [ActorName], [FromStatus], [ToStatus], [Comment], [EventDataJson])
VALUES
    (@ItemId, @SubmittedUtc, N'Submit', @InitiatorUserId, @SubmittedBy, NULL, N'Submitted', @Summary, NULL);

SELECT @ItemId;",
                    new SqlParameter("@ItemCode", itemCode),
                    new SqlParameter("@ItemTypeKey", normalized.ItemTypeKey),
                    new SqlParameter("@Title", normalized.Title),
                    CreateNullableStringParameter("@Summary", normalized.Summary),
                    CreateNullableStringParameter("@Description", normalized.Description),
                    new SqlParameter("@InitiatorUserId", normalized.InitiatorUserId),
                    CreateNullableIntParameter("@InitiatorEmployeeId", normalized.InitiatorEmployeeId),
                    CreateNullableIntParameter("@OwnerUserId", normalized.OwnerUserId),
                    CreateNullableStringParameter("@OwnerRoleKey", normalized.OwnerRoleKey),
                    CreateNullableIntParameter("@OrganizationUnitId", normalized.OrganizationUnitId),
                    CreateNullableStringParameter("@PriorityKey", normalized.PriorityKey),
                    CreateNullableDateTimeParameter("@DueUtc", normalized.DueUtc),
                    new SqlParameter("@SubmittedUtc", normalized.SubmittedUtc.Value),
                    new SqlParameter("@SubmittedBy", normalized.SubmittedBy)).ToList();

                long itemId = rows.Count == 0 ? 0 : rows[0];
                return itemId <= 0
                    ? new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "Collaboration item was not created.")
                    : new CollaborationItemResult(true, itemId, itemCode, PortalCollaborationItemActions.Submit, "Collaboration item submitted.");
            }
            catch (Exception)
            {
                return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "Collaboration item submission failed.");
            }
        }

        /// <inheritdoc />
        public IList<CollaborationItemInfo> GetAdminItems(string status, int take)
        {
            if (!IsSchemaAvailable())
            {
                return new List<CollaborationItemInfo>();
            }

            string normalizedStatus = NormalizeStatusFilter(status);
            try
            {
                return string.IsNullOrEmpty(normalizedStatus)
                    ? QueryItems(string.Empty, NormalizeTake(take, 50))
                    : QueryItems(
                        @"
WHERE [Item].[ItemStatus] = @ItemStatus",
                        NormalizeTake(take, 50),
                        new SqlParameter("@ItemStatus", normalizedStatus));
            }
            catch (Exception)
            {
                return new List<CollaborationItemInfo>();
            }
        }

        /// <inheritdoc />
        public CollaborationItemResult ApplyAction(CollaborationItemActionRequest request)
        {
            CollaborationItemActionRequest normalized = NormalizeActionRequest(request);
            if (normalized.ItemId <= 0)
            {
                return new CollaborationItemResult(false, 0, string.Empty, normalized.ActionKey, "Collaboration item id is required.");
            }

            string targetStatus = MapActionToStatus(normalized.ActionKey);
            if (string.IsNullOrEmpty(targetStatus))
            {
                return new CollaborationItemResult(false, normalized.ItemId, string.Empty, normalized.ActionKey, "Unsupported collaboration action.");
            }

            if (!IsSchemaAvailable())
            {
                return new CollaborationItemResult(false, normalized.ItemId, string.Empty, normalized.ActionKey, "Collaboration item schema is unavailable.");
            }

            try
            {
                List<CollaborationItemWriteRow> rows = context.Database.SqlQuery<CollaborationItemWriteRow>(
                    @"
DECLARE @Updated TABLE
(
    [ItemId] BIGINT NOT NULL,
    [ItemCode] NVARCHAR(40) NOT NULL,
    [FromStatus] NVARCHAR(20) NOT NULL
);

UPDATE [dbo].[PortalBiz_CollaborationItems]
SET [ItemStatus] = @TargetStatus,
    [CompletedUtc] = CASE
        WHEN @TargetStatus IN (N'Completed', N'Rejected', N'Cancelled', N'Closed') THEN @OccurredUtc
        ELSE NULL
    END,
    [ClosedUtc] = CASE
        WHEN @TargetStatus = N'Closed' THEN @OccurredUtc
        ELSE NULL
    END,
    [LastActionUtc] = @OccurredUtc,
    [LastActionByUserId] = @ActorUserId,
    [LastActionComment] = @Comment,
    [UpdatedUtc] = @OccurredUtc,
    [UpdatedBy] = @ActorName
OUTPUT inserted.[ItemId], inserted.[ItemCode], deleted.[ItemStatus]
INTO @Updated ([ItemId], [ItemCode], [FromStatus])
WHERE [ItemId] = @ItemId
  AND (
        (@ActionKey = N'Start' AND [ItemStatus] = N'Submitted')
        OR
        (@ActionKey = N'Complete' AND [ItemStatus] IN (N'Submitted', N'InProgress'))
        OR
        (@ActionKey = N'Return' AND [ItemStatus] IN (N'Submitted', N'InProgress'))
        OR
        (@ActionKey = N'Reject' AND [ItemStatus] IN (N'Submitted', N'InProgress'))
        OR
        (@ActionKey = N'Cancel' AND [ItemStatus] IN (N'Draft', N'Submitted', N'Returned'))
        OR
        (@ActionKey = N'Close' AND [ItemStatus] IN (N'Completed', N'Rejected', N'Cancelled'))
      );

INSERT INTO [dbo].[PortalBiz_CollaborationItemEvents]
    ([ItemId], [OccurredUtc], [ActionKey], [ActorUserId], [ActorName], [FromStatus], [ToStatus], [Comment], [EventDataJson])
SELECT
    [ItemId],
    @OccurredUtc,
    @ActionKey,
    @ActorUserId,
    @ActorName,
    [FromStatus],
    @TargetStatus,
    @Comment,
    NULL
FROM @Updated;

SELECT TOP (1)
    [ItemId],
    [ItemCode]
FROM @Updated;",
                    new SqlParameter("@ItemId", normalized.ItemId),
                    new SqlParameter("@ActionKey", normalized.ActionKey),
                    new SqlParameter("@TargetStatus", targetStatus),
                    new SqlParameter("@OccurredUtc", normalized.OccurredUtc.Value),
                    CreateNullableIntParameter("@ActorUserId", normalized.ActorUserId),
                    new SqlParameter("@ActorName", normalized.ActorName),
                    CreateNullableStringParameter("@Comment", normalized.Comment)).ToList();

                CollaborationItemWriteRow row = rows.Count == 0 ? null : rows[0];
                return row == null || string.IsNullOrWhiteSpace(row.ItemCode)
                    ? new CollaborationItemResult(false, normalized.ItemId, string.Empty, normalized.ActionKey, "Collaboration item was not found or cannot accept this action.")
                    : new CollaborationItemResult(true, row.ItemId, row.ItemCode, normalized.ActionKey, "Collaboration item state updated.");
            }
            catch (Exception)
            {
                return new CollaborationItemResult(false, normalized.ItemId, string.Empty, normalized.ActionKey, "Collaboration item action failed.");
            }
        }

        private IList<CollaborationItemInfo> QueryItems(string whereClause, int take, params SqlParameter[] parameters)
        {
            string sql = @"
SELECT TOP (@Take)
    [Item].[ItemId],
    [Item].[ItemCode],
    [Item].[ItemTypeKey],
    [Item].[Title],
    [Item].[Summary],
    [Item].[Description],
    [Item].[ItemStatus],
    [Item].[InitiatorUserId],
    [Initiator].[Name] AS [InitiatorUserName],
    [Item].[InitiatorEmployeeId],
    [Item].[OwnerUserId],
    [Owner].[Name] AS [OwnerUserName],
    [Item].[OwnerRoleKey],
    [Item].[OrganizationUnitId],
    [Item].[PriorityKey],
    [Item].[DueUtc],
    [Item].[SubmittedUtc],
    [Item].[CompletedUtc],
    [Item].[ClosedUtc],
    [Item].[LastActionUtc],
    [Item].[LastActionByUserId],
    [Item].[LastActionComment]
FROM [dbo].[PortalBiz_CollaborationItems] AS [Item]
LEFT JOIN [dbo].[Portal_Users] AS [Initiator]
    ON [Initiator].[UserID] = [Item].[InitiatorUserId]
LEFT JOIN [dbo].[Portal_Users] AS [Owner]
    ON [Owner].[UserID] = [Item].[OwnerUserId]" +
                whereClause +
                @"
ORDER BY ISNULL([Item].[LastActionUtc], [Item].[CreatedUtc]) DESC, [Item].[ItemId] DESC;";

            var sqlParameters = new List<SqlParameter> { new SqlParameter("@Take", take) };
            if (parameters != null)
            {
                sqlParameters.AddRange(parameters);
            }

            return context.Database.SqlQuery<CollaborationItemInfo>(sql, sqlParameters.ToArray()).ToList();
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

        private static CollaborationItemCreateRequest NormalizeCreateRequest(CollaborationItemCreateRequest request)
        {
            request = request ?? new CollaborationItemCreateRequest();
            DateTime submittedUtc = request.SubmittedUtc ?? DateTime.UtcNow;
            return new CollaborationItemCreateRequest
            {
                ItemTypeKey = string.IsNullOrWhiteSpace(request.ItemTypeKey) ? "General" : NormalizeText(request.ItemTypeKey, 80),
                Title = NormalizeText(request.Title, 200),
                Summary = NormalizeOptionalText(request.Summary, 500),
                Description = NormalizeOptionalText(request.Description, 4000),
                InitiatorUserId = request.InitiatorUserId,
                InitiatorEmployeeId = request.InitiatorEmployeeId.HasValue && request.InitiatorEmployeeId.Value > 0 ? request.InitiatorEmployeeId : null,
                OwnerUserId = request.OwnerUserId.HasValue && request.OwnerUserId.Value > 0 ? request.OwnerUserId : null,
                OwnerRoleKey = NormalizeOptionalText(request.OwnerRoleKey, 120),
                OrganizationUnitId = request.OrganizationUnitId.HasValue && request.OrganizationUnitId.Value > 0 ? request.OrganizationUnitId : null,
                PriorityKey = NormalizePriority(request.PriorityKey),
                DueUtc = request.DueUtc,
                SubmittedUtc = submittedUtc,
                SubmittedBy = string.IsNullOrWhiteSpace(request.SubmittedBy) ? "system" : NormalizeText(request.SubmittedBy, 100)
            };
        }

        private static CollaborationItemActionRequest NormalizeActionRequest(CollaborationItemActionRequest request)
        {
            request = request ?? new CollaborationItemActionRequest();
            return new CollaborationItemActionRequest
            {
                ItemId = request.ItemId,
                ActionKey = NormalizeText(request.ActionKey, 40),
                Comment = NormalizeOptionalText(request.Comment, 1000),
                ActorUserId = request.ActorUserId.HasValue && request.ActorUserId.Value > 0 ? request.ActorUserId : null,
                ActorName = string.IsNullOrWhiteSpace(request.ActorName) ? "system" : NormalizeText(request.ActorName, 100),
                OccurredUtc = request.OccurredUtc ?? DateTime.UtcNow
            };
        }

        private static string MapActionToStatus(string actionKey)
        {
            if (string.Equals(actionKey, PortalCollaborationItemActions.Start, StringComparison.Ordinal))
            {
                return PortalCollaborationItemStatuses.InProgress;
            }

            if (string.Equals(actionKey, PortalCollaborationItemActions.Complete, StringComparison.Ordinal))
            {
                return PortalCollaborationItemStatuses.Completed;
            }

            if (string.Equals(actionKey, PortalCollaborationItemActions.Return, StringComparison.Ordinal))
            {
                return PortalCollaborationItemStatuses.Returned;
            }

            if (string.Equals(actionKey, PortalCollaborationItemActions.Reject, StringComparison.Ordinal))
            {
                return PortalCollaborationItemStatuses.Rejected;
            }

            if (string.Equals(actionKey, PortalCollaborationItemActions.Cancel, StringComparison.Ordinal))
            {
                return PortalCollaborationItemStatuses.Cancelled;
            }

            if (string.Equals(actionKey, PortalCollaborationItemActions.Close, StringComparison.Ordinal))
            {
                return PortalCollaborationItemStatuses.Closed;
            }

            return string.Empty;
        }

        private static string CreateItemCode(DateTime submittedUtc)
        {
            return "CI-" + submittedUtc.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "-" +
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

        private static string NormalizePriority(string value)
        {
            string normalized = NormalizeOptionalText(value, 20);
            return string.Equals(normalized, "Important", StringComparison.OrdinalIgnoreCase) ? "Important" : "Normal";
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

        private static SqlParameter CreateNullableDateTimeParameter(string name, DateTime? value)
        {
            return new SqlParameter(name, value.HasValue ? (object)value.Value : DBNull.Value);
        }

        private sealed class CollaborationItemWriteRow
        {
            public long ItemId { get; set; }

            public string ItemCode { get; set; }
        }
    }
}
