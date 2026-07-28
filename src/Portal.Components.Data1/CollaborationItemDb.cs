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
        private readonly IReferenceDataDb referenceDataDb;
        private readonly IUsersDb usersDb;
        private readonly IRolesDb rolesDb;

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
        /// <param name="referenceDataDb">
        /// <l>
        ///   <zh-CN>受治理参考数据目录读取服务，用于在写入前复核类型和优先级稳定键。</zh-CN>
        ///   <en>Governed reference-data catalog reader used to revalidate type and priority stable keys before writing.</en>
        /// </l>
        /// </param>
        /// <param name="usersDb">
        /// <l>
        ///   <zh-CN>门户用户服务，用于在状态和评论写入时重新确认动作人。</zh-CN>
        ///   <en>Portal-user service used to re-confirm the actor during workflow and comment writes.</en>
        /// </l>
        /// </param>
        /// <param name="rolesDb">
        /// <l>
        ///   <zh-CN>角色权限服务，用于按当前映射检查负责人和管理员权限。</zh-CN>
        ///   <en>Role-permission service used to check current handler and administrator permissions.</en>
        /// </l>
        /// </param>
        public CollaborationItemDb(PortalBizDbContext context, IReferenceDataDb referenceDataDb, IUsersDb usersDb, IRolesDb rolesDb)
        {
            this.context = context;
            this.referenceDataDb = referenceDataDb;
            this.usersDb = usersDb;
            this.rolesDb = rolesDb;
        }

        /// <inheritdoc />
        public bool IsSchemaAvailable()
        {
            return HasTable(ItemTableName) &&
                   HasTable(EventTableName) &&
                   HasColumn(EventTableName, "EventType") &&
                   HasColumn(EventTableName, "VisibilityScope");
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

            string itemTypeKey;
            if (!TryResolveActiveReferenceValue(PortalReferenceDataSets.CollaborationItemType, normalized.ItemTypeKey, out itemTypeKey))
            {
                return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "The collaboration item type is not allowed.");
            }

            string priorityKey;
            if (!TryResolveActiveReferenceValue(PortalReferenceDataSets.CollaborationPriority, normalized.PriorityKey, out priorityKey))
            {
                return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "The collaboration item priority is not allowed.");
            }

            normalized.ItemTypeKey = itemTypeKey;
            normalized.PriorityKey = priorityKey;

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
    ([ItemId], [OccurredUtc], [EventType], [ActionKey], [VisibilityScope], [ActorUserId], [ActorName], [FromStatus], [ToStatus], [Comment], [EventDataJson])
VALUES
    (@ItemId, @SubmittedUtc, N'WorkflowAction', N'Submit', N'ItemParticipants', @InitiatorUserId, @SubmittedBy, NULL, N'Submitted', @Summary, NULL);

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
        public IList<CollaborationItemInfo> GetRecentItemsForUser(int userId, int take)
        {
            if (userId <= 0 || !IsSchemaAvailable())
            {
                return new List<CollaborationItemInfo>();
            }

            try
            {
                return QueryItems(
                    @"
WHERE [Item].[InitiatorUserId] = @UserId
   OR [Item].[OwnerUserId] = @UserId",
                    NormalizeTake(take, 20),
                    new SqlParameter("@UserId", userId));
            }
            catch (Exception)
            {
                return new List<CollaborationItemInfo>();
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
        public IList<CollaborationItemEventInfo> GetVisibleEvents(long itemId, int actorUserId)
        {
            if (itemId <= 0 || !IsSchemaAvailable())
            {
                return new List<CollaborationItemEventInfo>();
            }

            CollaborationItemInfo item = FindItem(itemId);
            CollaborationItemActorAuthorization actor;
            if (item == null || !TryGetActorAuthorization(actorUserId, out actor) || !CanParticipate(item, actor))
            {
                return new List<CollaborationItemEventInfo>();
            }

            try
            {
                string visibilityClause = actor.IsAdministrator
                    ? string.Empty
                    : @"
  AND ([Event].[EventType] = N'WorkflowAction' OR [Event].[VisibilityScope] = N'ItemParticipants')";
                return context.Database.SqlQuery<CollaborationItemEventInfo>(
                    @"
SELECT
    [Event].[EventId],
    [Event].[ItemId],
    [Event].[EventType],
    [Event].[ActionKey],
    [Event].[VisibilityScope],
    [Event].[ActorUserId],
    [Event].[ActorName],
    [Event].[OccurredUtc],
    [Event].[FromStatus],
    [Event].[ToStatus],
    [Event].[Comment]
FROM [dbo].[PortalBiz_CollaborationItemEvents] AS [Event]
WHERE [Event].[ItemId] = @ItemId" + visibilityClause + @"
ORDER BY [Event].[OccurredUtc] ASC, [Event].[EventId] ASC;",
                    new SqlParameter("@ItemId", itemId)).ToList();
            }
            catch (Exception)
            {
                return new List<CollaborationItemEventInfo>();
            }
        }

        /// <inheritdoc />
        public CollaborationItemCommentResult AddComment(CollaborationItemCommentCreateRequest request)
        {
            request = request ?? new CollaborationItemCommentCreateRequest();
            if (request.ItemId <= 0)
            {
                return new CollaborationItemCommentResult(false, 0, 0, "Collaboration item id is required.");
            }

            if (!IsSchemaAvailable())
            {
                return new CollaborationItemCommentResult(false, request.ItemId, 0, "Collaboration item schema is unavailable.");
            }

            string comment = NormalizeOptionalText(request.Comment, 1000);
            if (string.IsNullOrWhiteSpace(comment))
            {
                return new CollaborationItemCommentResult(false, request.ItemId, 0, "A plain-text comment is required.");
            }

            if ((request.Comment ?? string.Empty).Trim().Length > 1000)
            {
                return new CollaborationItemCommentResult(false, request.ItemId, 0, "The plain-text comment cannot exceed 1000 characters.");
            }

            string visibilityScope = NormalizeText(request.VisibilityScope, 30);
            if (string.IsNullOrWhiteSpace(visibilityScope))
            {
                visibilityScope = PortalCollaborationItemVisibilityScopes.ItemParticipants;
            }

            if (!IsKnownVisibilityScope(visibilityScope))
            {
                return new CollaborationItemCommentResult(false, request.ItemId, 0, "The requested comment visibility scope is not supported.");
            }

            CollaborationItemInfo item = FindItem(request.ItemId);
            CollaborationItemActorAuthorization actor;
            if (item == null || !TryGetActorAuthorization(request.ActorUserId, out actor))
            {
                return new CollaborationItemCommentResult(false, request.ItemId, 0, "A signed-in portal user is required to add a comment.");
            }

            if (!CanParticipate(item, actor))
            {
                return new CollaborationItemCommentResult(false, request.ItemId, 0, "The current user is not allowed to comment on this item.");
            }

            if (string.Equals(visibilityScope, PortalCollaborationItemVisibilityScopes.Administrators, StringComparison.Ordinal) && !actor.IsAdministrator)
            {
                return new CollaborationItemCommentResult(false, request.ItemId, 0, "Only collaboration-item administrators can add administrator-visible comments.");
            }

            DateTime occurredUtc = request.OccurredUtc ?? DateTime.UtcNow;
            try
            {
                List<long> eventIds = context.Database.SqlQuery<long>(
                    @"
INSERT INTO [dbo].[PortalBiz_CollaborationItemEvents]
    ([ItemId], [OccurredUtc], [EventType], [ActionKey], [VisibilityScope], [ActorUserId], [ActorName], [FromStatus], [ToStatus], [Comment], [EventDataJson])
VALUES
    (@ItemId, @OccurredUtc, N'Comment', NULL, @VisibilityScope, @ActorUserId, @ActorName, NULL, NULL, @Comment, NULL);

SELECT CONVERT(BIGINT, SCOPE_IDENTITY());",
                    new SqlParameter("@ItemId", item.ItemId),
                    new SqlParameter("@OccurredUtc", occurredUtc),
                    new SqlParameter("@VisibilityScope", visibilityScope),
                    new SqlParameter("@ActorUserId", actor.ActorUserId),
                    new SqlParameter("@ActorName", actor.ActorName),
                    new SqlParameter("@Comment", comment)).ToList();
                long eventId = eventIds.Count == 0 ? 0 : eventIds[0];
                return eventId <= 0
                    ? new CollaborationItemCommentResult(false, item.ItemId, 0, "The collaboration-item comment was not created.")
                    : new CollaborationItemCommentResult(true, item.ItemId, eventId, "The collaboration-item comment was added.");
            }
            catch (Exception)
            {
                return new CollaborationItemCommentResult(false, item.ItemId, 0, "The collaboration-item comment could not be added.");
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

            CollaborationItemInfo item = FindItem(normalized.ItemId);
            if (item == null)
            {
                return new CollaborationItemResult(false, normalized.ItemId, string.Empty, normalized.ActionKey, "Collaboration item was not found or cannot accept this action.");
            }

            CollaborationItemActorAuthorization actor;
            if (!TryGetActorAuthorization(normalized.ActorUserId, out actor))
            {
                return new CollaborationItemResult(false, normalized.ItemId, item.ItemCode, normalized.ActionKey, "A signed-in portal user is required to apply this action.");
            }

            normalized.ActorName = actor.ActorName;
            if (!CanApplyAction(item, normalized.ActionKey, actor))
            {
                return new CollaborationItemResult(false, normalized.ItemId, item.ItemCode, normalized.ActionKey, "The current user is not allowed to apply this action.");
            }

            if (ActionRequiresComment(normalized.ActionKey) && string.IsNullOrWhiteSpace(normalized.Comment))
            {
                return new CollaborationItemResult(false, normalized.ItemId, item.ItemCode, normalized.ActionKey, "A plain-text handling comment is required for this action.");
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
        WHEN @TargetStatus IN (N'Completed', N'Rejected', N'Cancelled') THEN @OccurredUtc
        WHEN @TargetStatus = N'Closed' THEN [CompletedUtc]
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
        (@ActionKey = N'Submit' AND [ItemStatus] = N'Draft')
        OR
        (@ActionKey = N'Start' AND [ItemStatus] = N'Submitted')
        OR
        (@ActionKey = N'Complete' AND [ItemStatus] IN (N'Submitted', N'InProgress'))
        OR
        (@ActionKey = N'Return' AND [ItemStatus] IN (N'Submitted', N'InProgress'))
        OR
        (@ActionKey = N'Resubmit' AND [ItemStatus] = N'Returned')
        OR
        (@ActionKey = N'Reject' AND [ItemStatus] IN (N'Submitted', N'InProgress'))
        OR
        (@ActionKey = N'Cancel' AND [ItemStatus] IN (N'Draft', N'Submitted', N'Returned'))
        OR
        (@ActionKey = N'Close' AND [ItemStatus] IN (N'Completed', N'Rejected', N'Cancelled'))
      );

INSERT INTO [dbo].[PortalBiz_CollaborationItemEvents]
    ([ItemId], [OccurredUtc], [EventType], [ActionKey], [VisibilityScope], [ActorUserId], [ActorName], [FromStatus], [ToStatus], [Comment], [EventDataJson])
SELECT
    [ItemId],
    @OccurredUtc,
    N'WorkflowAction',
    @ActionKey,
    N'ItemParticipants',
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
    CAST(CASE
        WHEN [Item].[ItemStatus] IN (N'Submitted', N'InProgress', N'Returned')
         AND [Item].[DueUtc] IS NOT NULL
         AND [Item].[DueUtc] < SYSUTCDATETIME()
        THEN 1
        ELSE 0
    END AS BIT) AS [IsOverdue],
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

        private bool HasColumn(string tableName, string columnName)
        {
            try
            {
                return context.Database.SqlQuery<int>(
                    "SELECT CASE WHEN COL_LENGTH(N'[dbo].[" + tableName + "]', @ColumnName) IS NULL THEN 0 ELSE 1 END",
                    new SqlParameter("@ColumnName", columnName)).Single() == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private CollaborationItemInfo FindItem(long itemId)
        {
            try
            {
                return QueryItems(
                    @"
WHERE [Item].[ItemId] = @ItemId",
                    1,
                    new SqlParameter("@ItemId", itemId)).FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool TryGetActorAuthorization(int? actorUserId, out CollaborationItemActorAuthorization authorization)
        {
            authorization = null;
            if (!actorUserId.HasValue || actorUserId.Value <= 0 || usersDb == null)
            {
                return false;
            }

            try
            {
                IUserItem actor = usersDb.FindUserById(actorUserId.Value);
                if (actor == null || string.IsNullOrWhiteSpace(actor.Name))
                {
                    return false;
                }

                string[] permissionKeys = rolesDb == null
                    ? new string[0]
                    : (rolesDb.GetPermissionKeysByUserName(actor.Name) ?? Enumerable.Empty<string>())
                        .Where(key => !string.IsNullOrWhiteSpace(key))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                string[] roleNames = (usersDb.GetRoleNamesByUser(actor.Name) ?? Enumerable.Empty<string>())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToArray();
                bool isAdministrator = roleNames.Any(name => string.Equals(name, PortalRoleNames.Administrators, StringComparison.OrdinalIgnoreCase)) ||
                                       permissionKeys.Any(key => string.Equals(key, PortalPermissionKeys.BusinessCollaborationAdmin, StringComparison.OrdinalIgnoreCase));
                authorization = new CollaborationItemActorAuthorization(actor.UserId, NormalizeText(actor.Name, 100), permissionKeys, isAdministrator);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool CanParticipate(CollaborationItemInfo item, CollaborationItemActorAuthorization actor)
        {
            return item != null && actor != null &&
                   (actor.IsAdministrator ||
                    item.InitiatorUserId == actor.ActorUserId ||
                    (item.OwnerUserId.HasValue && item.OwnerUserId.Value == actor.ActorUserId) ||
                    HasOwnerRolePermission(item, actor));
        }

        private static bool CanApplyAction(CollaborationItemInfo item, string actionKey, CollaborationItemActorAuthorization actor)
        {
            if (item == null || actor == null)
            {
                return false;
            }

            bool isInitiator = item.InitiatorUserId == actor.ActorUserId;
            bool isHandler = (item.OwnerUserId.HasValue && item.OwnerUserId.Value == actor.ActorUserId) || HasOwnerRolePermission(item, actor);
            if (string.Equals(actionKey, PortalCollaborationItemActions.Start, StringComparison.Ordinal) ||
                string.Equals(actionKey, PortalCollaborationItemActions.Complete, StringComparison.Ordinal) ||
                string.Equals(actionKey, PortalCollaborationItemActions.Return, StringComparison.Ordinal) ||
                string.Equals(actionKey, PortalCollaborationItemActions.Reject, StringComparison.Ordinal))
            {
                return actor.IsAdministrator || isHandler;
            }

            if (string.Equals(actionKey, PortalCollaborationItemActions.Submit, StringComparison.Ordinal) ||
                string.Equals(actionKey, PortalCollaborationItemActions.Resubmit, StringComparison.Ordinal) ||
                string.Equals(actionKey, PortalCollaborationItemActions.Cancel, StringComparison.Ordinal))
            {
                return actor.IsAdministrator || isInitiator;
            }

            return string.Equals(actionKey, PortalCollaborationItemActions.Close, StringComparison.Ordinal) && actor.IsAdministrator;
        }

        private static bool HasOwnerRolePermission(CollaborationItemInfo item, CollaborationItemActorAuthorization actor)
        {
            return item != null &&
                   actor != null &&
                   !string.IsNullOrWhiteSpace(item.OwnerRoleKey) &&
                   actor.PermissionKeys.Any(key => string.Equals(key, item.OwnerRoleKey, StringComparison.OrdinalIgnoreCase));
        }

        private static bool ActionRequiresComment(string actionKey)
        {
            return string.Equals(actionKey, PortalCollaborationItemActions.Return, StringComparison.Ordinal) ||
                   string.Equals(actionKey, PortalCollaborationItemActions.Reject, StringComparison.Ordinal);
        }

        private static bool IsKnownVisibilityScope(string visibilityScope)
        {
            return string.Equals(visibilityScope, PortalCollaborationItemVisibilityScopes.ItemParticipants, StringComparison.Ordinal) ||
                   string.Equals(visibilityScope, PortalCollaborationItemVisibilityScopes.Administrators, StringComparison.Ordinal);
        }

        private static CollaborationItemCreateRequest NormalizeCreateRequest(CollaborationItemCreateRequest request)
        {
            request = request ?? new CollaborationItemCreateRequest();
            DateTime submittedUtc = request.SubmittedUtc ?? DateTime.UtcNow;
            return new CollaborationItemCreateRequest
            {
                ItemTypeKey = string.IsNullOrWhiteSpace(request.ItemTypeKey) ? PortalReferenceDataSets.GeneralItemType : NormalizeText(request.ItemTypeKey, 80),
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
            if (string.Equals(actionKey, PortalCollaborationItemActions.Submit, StringComparison.Ordinal))
            {
                return PortalCollaborationItemStatuses.Submitted;
            }

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

            if (string.Equals(actionKey, PortalCollaborationItemActions.Resubmit, StringComparison.Ordinal))
            {
                return PortalCollaborationItemStatuses.Submitted;
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
            return string.IsNullOrEmpty(normalized) ? PortalReferenceDataSets.NormalPriority : normalized;
        }

        private bool TryResolveActiveReferenceValue(string referenceSetKey, string candidateValueKey, out string canonicalValueKey)
        {
            canonicalValueKey = string.Empty;
            IList<ReferenceDataItem> activeItems;
            if (referenceDataDb != null && referenceDataDb.TryGetActiveItems(referenceSetKey, out activeItems))
            {
                foreach (ReferenceDataItem item in activeItems)
                {
                    if (string.Equals(item.ValueKey, candidateValueKey, StringComparison.OrdinalIgnoreCase))
                    {
                        canonicalValueKey = item.ValueKey;
                        return true;
                    }
                }

                return false;
            }

            return PortalReferenceDataSets.TryResolveFallbackValue(referenceSetKey, candidateValueKey, out canonicalValueKey);
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>一次服务端身份复核后用于协同事项授权的最小动作人快照。</zh-CN>
        ///   <en>Minimal actor snapshot used for collaboration-item authorization after a server-side identity recheck.</en>
        /// </lang>
        /// </summary>
        private sealed class CollaborationItemActorAuthorization
        {
            public CollaborationItemActorAuthorization(int actorUserId, string actorName, string[] permissionKeys, bool isAdministrator)
            {
                ActorUserId = actorUserId;
                ActorName = actorName;
                PermissionKeys = permissionKeys ?? new string[0];
                IsAdministrator = isAdministrator;
            }

            public int ActorUserId { get; private set; }

            public string ActorName { get; private set; }

            public string[] PermissionKeys { get; private set; }

            public bool IsAdministrator { get; private set; }
        }
    }
}
