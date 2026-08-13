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

        /// <summary>
        /// <lang>
        ///   <zh-CN>规范化并校验创建请求后，创建一条处于 <c>Submitted</c> 状态的协同事项及其 Submit 事件。</zh-CN>
        ///   <en>Normalizes and validates a create request, then creates a collaboration item in <c>Submitted</c> status with its Submit event.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>由已认证页面层提供的事项输入；发起人、标题和至少一个负责人目标为必填项。</zh-CN>
        ///   <en>Item input supplied by an authenticated page layer; initiator, title, and at least one owner target are required.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>成功时包含新事项标识和事项编码；校验、参考数据或数据库失败时返回不含内部异常详情的失败结果。</zh-CN>
        ///   <en>On success, contains the new item identifier and item code; validation, reference-data, or database failures return a result without internal exception detail.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>本方法仅写入事项事实和 Submit 事件，两者通过同一参数化数据库命令批次提交；待办投影和运营审计由调用页面在业务事实成功后作为旁路处理。</zh-CN>
        ///   <en>This method writes only the item fact and Submit event through one parameterized database-command batch; the calling page handles work-item projection and operational audit as sidecars after the business fact succeeds.</en>
        /// </lang>
        /// </remarks>
        public CollaborationItemResult CreateSubmittedItem(CollaborationItemCreateRequest request)
        {
            // <lang>
            //   <zh-CN>先复制并规范化不受信任的页面输入，固定默认值、长度和可空值语义，避免后续校验与写入使用不同表示。</zh-CN>
            //   <en>First copy and normalize untrusted page input so defaults, length limits, and nullable semantics remain consistent for validation and persistence.</en>
            // </lang>
            CollaborationItemCreateRequest normalized = NormalizeCreateRequest(request);

            // <lang>
            //   <zh-CN>事项提交必须可追溯到已登录的发起人；缺失身份时在触及数据库前失败。</zh-CN>
            //   <en>Submission must be attributable to a signed-in initiator; fail before database access when the identity is missing.</en>
            // </lang>
            if (normalized.InitiatorUserId <= 0)
            {
                return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "A signed-in portal user is required.");
            }

            // <lang>
            //   <zh-CN>标题是事项的最小可识别业务内容，空白标题不能进入事件时间线。</zh-CN>
            //   <en>The title is the minimum identifiable business content of an item; a blank title cannot enter the event timeline.</en>
            // </lang>
            if (string.IsNullOrWhiteSpace(normalized.Title))
            {
                return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "Collaboration item title is required.");
            }

            // <lang>
            //   <zh-CN>提交状态必须有可解析的个人或角色负责人，避免产生没有处理目标的事项。</zh-CN>
            //   <en>A submitted item needs a resolvable individual or role owner so no item is created without a handling target.</en>
            // </lang>
            if (!normalized.OwnerUserId.HasValue && string.IsNullOrWhiteSpace(normalized.OwnerRoleKey))
            {
                return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "An owner user or owner role is required.");
            }

            // <lang>
            //   <zh-CN>先确认事项和事件表的最小结构可用，防止部分部署环境写入不完整业务事实。</zh-CN>
            //   <en>Confirm that the minimum item and event schema is available before writing, preventing incomplete business facts in partially deployed environments.</en>
            // </lang>
            if (!IsSchemaAvailable())
            {
                return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "Collaboration item schema is unavailable.");
            }

            // <lang>
            //   <zh-CN>将请求类型复核为当前启用的参考数据稳定键；调用方不能借由自由文本绕过目录治理。</zh-CN>
            //   <en>Revalidate the requested type as an active reference-data stable key so callers cannot bypass catalog governance with free text.</en>
            // </lang>
            string itemTypeKey;
            if (!TryResolveActiveReferenceValue(PortalReferenceDataSets.CollaborationItemType, normalized.ItemTypeKey, out itemTypeKey))
            {
                return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "The collaboration item type is not allowed.");
            }

            // <lang>
            //   <zh-CN>同样复核优先级稳定键，确保写入值仍属于当前启用的优先级目录。</zh-CN>
            //   <en>Likewise revalidate the priority stable key, ensuring the persisted value remains in the currently active priority catalog.</en>
            // </lang>
            string priorityKey;
            if (!TryResolveActiveReferenceValue(PortalReferenceDataSets.CollaborationPriority, normalized.PriorityKey, out priorityKey))
            {
                return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "The collaboration item priority is not allowed.");
            }

            // <lang>
            //   <zh-CN>用目录返回的规范稳定键替换输入值，使主记录和后续筛选均使用同一受治理标识。</zh-CN>
            //   <en>Replace input values with catalog-returned canonical stable keys so the record and later filtering use the same governed identifiers.</en>
            // </lang>
            normalized.ItemTypeKey = itemTypeKey;
            normalized.PriorityKey = priorityKey;

            // <lang>
            //   <zh-CN>事项编码以已规范化的提交时间生成，供页面、待办投影和运营追踪使用，而非暴露数据库主键。</zh-CN>
            //   <en>Generate the item code from the normalized submission time for pages, work-item projection, and operations tracing without exposing the database key.</en>
            // </lang>
            string itemCode = CreateItemCode(normalized.SubmittedUtc.Value);
            try
            {
                // <lang>
                //   <zh-CN>以下参数化命令批次依次写入事项事实和 Submit 事件并返回新标识；待办投影由页面层在成功后旁路创建。</zh-CN>
                //   <en>The parameterized command batch below writes the item fact and Submit event in order and returns the new identifier; the page layer creates the work-item projection as a sidecar after success.</en>
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

                // <lang>
                //   <zh-CN>仅接受数据库批次明确返回的首个标识；空结果按创建失败处理，避免将未知写入状态报告为成功。</zh-CN>
                //   <en>Accept only the first identifier explicitly returned by the database batch; treat an empty result as creation failure rather than reporting an unknown write state as success.</en>
                // </lang>
                long itemId = rows.Count == 0 ? 0 : rows[0];
                return itemId <= 0
                    ? new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "Collaboration item was not created.")
                    : new CollaborationItemResult(true, itemId, itemCode, PortalCollaborationItemActions.Submit, "Collaboration item submitted.");
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>不向页面返回数据库异常细节；上层可按既有日志和运营审计策略记录上下文。</zh-CN>
                //   <en>Do not return database exception detail to the page; upper layers may record context through their established logging and operational-audit policy.</en>
                // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回当前动作人可见的协同事项事件时间线。</zh-CN>
        ///   <en>Returns the collaboration-item event timeline visible to the current actor.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemId">
        /// <l>
        ///   <zh-CN>待读取的协同事项数据库标识；必须为正值。</zh-CN>
        ///   <en>Database identifier of the collaboration item to read; must be positive.</en>
        /// </l>
        /// </param>
        /// <param name="actorUserId">
        /// <l>
        ///   <zh-CN>由已认证调用方传入并在服务端重新解析授权的门户用户标识。</zh-CN>
        ///   <en>Portal-user identifier supplied by the authenticated caller and re-resolved for authorization on the server.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>按发生时间和事件标识升序排列的可见事件；无效输入、无参与权、schema 不可用或读取异常时返回空集合且不泄露内部原因。</zh-CN>
        ///   <en>Visible events ordered by occurrence time and event identifier; invalid input, missing participation, unavailable schema, or read failure returns an empty collection without exposing the internal reason.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>管理员读取所有事件；普通参与者只能读取工作流动作和参与者可见事件。查询只拼接固定受控 SQL 片段，事项标识始终作为参数传入。</zh-CN>
        ///   <en>Administrators read all events; ordinary participants read only workflow actions and participant-visible events. The query concatenates only fixed controlled SQL fragments, while the item identifier is always passed as a parameter.</en>
        /// </lang>
        /// </remarks>
        public IList<CollaborationItemEventInfo> GetVisibleEvents(long itemId, int actorUserId)
        {
            // <lang>
            //   <zh-CN>无效标识或不完整 schema 直接返回空集合，避免在无法安全读取事件时继续触及数据访问层。</zh-CN>
            //   <en>Return an empty collection for an invalid identifier or incomplete schema rather than reaching the data layer when events cannot be read safely.</en>
            // </lang>
            if (itemId <= 0 || !IsSchemaAvailable())
            {
                return new List<CollaborationItemEventInfo>();
            }

            // <lang>
            //   <zh-CN>先取得事项事实，再按当前用户重新计算动作人授权；事项不存在、身份无效和无参与权共用空结果，避免向调用方区分这些内部状态。</zh-CN>
            //   <en>Load the item fact first, then recompute actor authorization for the current user; item absence, invalid identity, and missing participation share an empty result so callers cannot distinguish those internal states.</en>
            // </lang>
            CollaborationItemInfo item = FindItem(itemId);
            CollaborationItemActorAuthorization actor;
            if (item == null || !TryGetActorAuthorization(actorUserId, out actor) || !CanParticipate(item, actor))
            {
                return new List<CollaborationItemEventInfo>();
            }

            try
            {
                // <lang>
                //   <zh-CN>仅管理员可省略额外可见性条件；普通参与者仍可看到工作流动作和参与者范围事件。拼接片段为内部固定文本，不接受请求值。</zh-CN>
                //   <en>Only administrators may omit the additional visibility predicate; ordinary participants still see workflow actions and participant-scope events. The concatenated fragment is fixed internal text and accepts no request value.</en>
                // </lang>
                string visibilityClause = actor.IsAdministrator
                    ? string.Empty
                    : @"
  AND ([Event].[EventType] = N'WorkflowAction' OR [Event].[VisibilityScope] = N'ItemParticipants')";

                // <lang>
                //   <zh-CN>只读取时间线显示所需字段，并将事项标识作为 SQL 参数；排序同时使用 UTC 发生时间和稳定事件标识，确保相同时间的顺序可预测。</zh-CN>
                //   <en>Read only fields needed for timeline display and pass the item identifier as a SQL parameter; order by UTC occurrence time and stable event identifier so equal-time events remain predictable.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>读取失败时维持不可见的空结果，不向页面泄露 SQL、连接或异常细节。</zh-CN>
                //   <en>Keep the non-disclosing empty result on read failure and do not expose SQL, connection, or exception detail to the page.</en>
                // </lang>
                return new List<CollaborationItemEventInfo>();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为当前有参与权的动作人创建一条不改变事项状态的纯文本评论事件。</zh-CN>
        ///   <en>Creates a plain-text comment event for a current actor with participation rights without changing item status.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>评论输入，包含事项、动作人、纯文本内容、可见性范围和可选发生时间；服务端重新校验所有授权相关字段。</zh-CN>
        ///   <en>Comment input containing the item, actor, plain-text content, visibility scope, and optional occurrence time; the server revalidates every authorization-relevant field.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>成功时包含事项和新事件标识；校验、授权、schema 或数据库失败时包含可呈现但不泄露内部异常的失败原因。</zh-CN>
        ///   <en>On success, contains the item and new event identifiers; validation, authorization, schema, or database failures contain a displayable reason without internal exception detail.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>评论以独立事件写入，不更新事项事实、当前状态或最近工作流评论投影。管理员可写管理员范围评论；参与者只能写参与者范围评论。</zh-CN>
        ///   <en>The comment is written as an independent event and does not update the item fact, current status, or latest workflow-comment projection. Administrators may write administrator-scope comments; participants may write only participant-scope comments.</en>
        /// </lang>
        /// </remarks>
        public CollaborationItemCommentResult AddComment(CollaborationItemCommentCreateRequest request)
        {
            // <lang>
            //   <zh-CN>将空请求收敛为本地对象，使后续校验返回稳定的业务失败结果而非空引用异常。</zh-CN>
            //   <en>Collapse a null request to a local object so later checks return a stable business failure instead of a null-reference exception.</en>
            // </lang>
            request = request ?? new CollaborationItemCommentCreateRequest();

            // <lang>
            //   <zh-CN>评论必须绑定已有事项标识；无效标识在数据库访问前被拒绝。</zh-CN>
            //   <en>A comment must be bound to an existing item identifier; reject an invalid identifier before database access.</en>
            // </lang>
            if (request.ItemId <= 0)
            {
                return new CollaborationItemCommentResult(false, 0, 0, "Collaboration item id is required.");
            }

            // <lang>
            //   <zh-CN>先确认事件表所需 schema 可用，防止降级部署把评论路径误报为成功。</zh-CN>
            //   <en>Confirm that the schema needed by the event table is available first, preventing a downgraded deployment from reporting the comment path as successful.</en>
            // </lang>
            if (!IsSchemaAvailable())
            {
                return new CollaborationItemCommentResult(false, request.ItemId, 0, "Collaboration item schema is unavailable.");
            }

            // <lang>
            //   <zh-CN>规范化用于持久化的评论值，但仍须结合原始输入检查长度，避免静默截断超长用户内容。</zh-CN>
            //   <en>Normalize the comment value for persistence, while retaining a raw-input length check so overlong user content is rejected rather than silently truncated.</en>
            // </lang>
            string comment = NormalizeOptionalText(request.Comment, 1000);
            if (string.IsNullOrWhiteSpace(comment))
            {
                return new CollaborationItemCommentResult(false, request.ItemId, 0, "A plain-text comment is required.");
            }

            // <lang>
            //   <zh-CN>长度限制按去除首尾空白后的原始请求执行，与事件列容量和页面提示保持一致。</zh-CN>
            //   <en>Apply the length limit to the trimmed original request so it remains aligned with event-column capacity and the page message.</en>
            // </lang>
            if ((request.Comment ?? string.Empty).Trim().Length > 1000)
            {
                return new CollaborationItemCommentResult(false, request.ItemId, 0, "The plain-text comment cannot exceed 1000 characters.");
            }

            // <lang>
            //   <zh-CN>规范化可见性范围；省略时采用参与者范围这一最小共享默认值。</zh-CN>
            //   <en>Normalize the visibility scope and use participant scope as the minimum shared default when it is omitted.</en>
            // </lang>
            string visibilityScope = NormalizeText(request.VisibilityScope, 30);
            if (string.IsNullOrWhiteSpace(visibilityScope))
            {
                visibilityScope = PortalCollaborationItemVisibilityScopes.ItemParticipants;
            }

            // <lang>
            //   <zh-CN>范围必须属于封闭白名单，禁止将任意文本持久化为潜在的新可见性语义。</zh-CN>
            //   <en>The scope must be in the closed allowlist so arbitrary text cannot be persisted as a potential new visibility semantic.</en>
            // </lang>
            if (!IsKnownVisibilityScope(visibilityScope))
            {
                return new CollaborationItemCommentResult(false, request.ItemId, 0, "The requested comment visibility scope is not supported.");
            }

            // <lang>
            //   <zh-CN>读取事项并重新解析动作人授权；不可用身份不会仅凭客户端传入的用户标识获得评论资格。</zh-CN>
            //   <en>Load the item and re-resolve actor authorization; an unavailable identity cannot gain comment eligibility from a client-supplied user identifier alone.</en>
            // </lang>
            CollaborationItemInfo item = FindItem(request.ItemId);
            CollaborationItemActorAuthorization actor;
            if (item == null || !TryGetActorAuthorization(request.ActorUserId, out actor))
            {
                return new CollaborationItemCommentResult(false, request.ItemId, 0, "A signed-in portal user is required to add a comment.");
            }

            // <lang>
            //   <zh-CN>即使身份有效，也必须是当前事项参与者或管理员，避免已认证但无关用户写入事件时间线。</zh-CN>
            //   <en>Even a valid identity must be a current item participant or administrator, preventing authenticated but unrelated users from writing to the event timeline.</en>
            // </lang>
            if (!CanParticipate(item, actor))
            {
                return new CollaborationItemCommentResult(false, request.ItemId, 0, "The current user is not allowed to comment on this item.");
            }

            // <lang>
            //   <zh-CN>管理员范围评论可能对普通参与者隐藏，因此仅保留给已重新确认的管理员。</zh-CN>
            //   <en>Administrator-scope comments can be hidden from ordinary participants, so reserve them for actors re-confirmed as administrators.</en>
            // </lang>
            if (string.Equals(visibilityScope, PortalCollaborationItemVisibilityScopes.Administrators, StringComparison.Ordinal) && !actor.IsAdministrator)
            {
                return new CollaborationItemCommentResult(false, request.ItemId, 0, "Only collaboration-item administrators can add administrator-visible comments.");
            }

            // <lang>
            //   <zh-CN>以调用方提供的 UTC 时刻或当前 UTC 记录事件发生时间，避免本地时区参与时间线排序。</zh-CN>
            //   <en>Record the event occurrence with a caller-supplied UTC time or current UTC, keeping local time zones out of timeline ordering.</en>
            // </lang>
            DateTime occurredUtc = request.OccurredUtc ?? DateTime.UtcNow;
            try
            {
                // <lang>
                //   <zh-CN>评论只写入独立 Comment 事件；事项状态和最近工作流评论字段保持不变，所有可变值均通过显式参数传递。</zh-CN>
                //   <en>Write the comment only as an independent Comment event; item status and latest workflow-comment fields remain unchanged, and every variable value is passed through an explicit parameter.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>只有数据库明确返回的新事件标识才报告成功；空结果按未创建处理，避免未知写入状态进入页面反馈。</zh-CN>
                //   <en>Report success only when the database explicitly returns a new event identifier; treat an empty result as not created so an unknown write state cannot reach page feedback.</en>
                // </lang>
                long eventId = eventIds.Count == 0 ? 0 : eventIds[0];
                return eventId <= 0
                    ? new CollaborationItemCommentResult(false, item.ItemId, 0, "The collaboration-item comment was not created.")
                    : new CollaborationItemCommentResult(true, item.ItemId, eventId, "The collaboration-item comment was added.");
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>异常只映射为既有通用失败结果，不向页面泄露数据库、SQL 或异常细节。</zh-CN>
                //   <en>Map exceptions only to the established generic failure result and do not expose database, SQL, or exception detail to the page.</en>
                // </lang>
                return new CollaborationItemCommentResult(false, item.ItemId, 0, "The collaboration-item comment could not be added.");
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在服务端重新授权和当前状态谓词均满足时，对协同事项执行一个受支持的工作流动作。</zh-CN>
        ///   <en>Applies a supported workflow action to a collaboration item when both server-side reauthorization and the current-status predicate are satisfied.</en>
        /// </lang>
        /// </summary>
        /// <param name="request">
        /// <l>
        ///   <zh-CN>状态动作输入，包含事项、动作、动作人、可选处理意见和发生时间；动作人和状态前置条件不会信任客户端表示。</zh-CN>
        ///   <en>State-action input containing the item, action, actor, optional handling comment, and occurrence time; actor and state preconditions do not trust the client representation.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>成功时包含动作后事项标识和编码；不支持的动作、授权/状态前置条件、schema 或数据库失败时返回不泄露内部异常的失败结果。</zh-CN>
        ///   <en>On success, contains the post-action item identifier and code; unsupported actions, authorization or state-precondition failures, schema failures, and database failures return a result without internal exception detail.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>更新使用动作与当前状态的 SQL 谓词，并仅在实际更新的事项集合上写入 WorkflowAction 事件，避免过期读取产生孤立事件。评论会投影为最近工作流动作评论；独立评论继续由 <see cref="AddComment"/> 处理。</zh-CN>
        ///   <en>The update uses an action-and-current-status SQL predicate and writes a WorkflowAction event only for actually updated items, preventing stale reads from producing orphan events. Its comment projects as the latest workflow-action comment; independent comments remain handled by <see cref="AddComment"/>.</en>
        /// </lang>
        /// </remarks>
        public CollaborationItemResult ApplyAction(CollaborationItemActionRequest request)
        {
            // <lang>
            //   <zh-CN>先复制并规范化不受信任的页面动作输入，固定动作键、可选评论、动作人和 UTC 发生时间的表示。</zh-CN>
            //   <en>First copy and normalize untrusted page action input so action key, optional comment, actor, and UTC occurrence time have stable representations.</en>
            // </lang>
            CollaborationItemActionRequest normalized = NormalizeActionRequest(request);

            // <lang>
            //   <zh-CN>状态动作必须指向正数事项标识；缺失标识时不进入状态映射或数据访问。</zh-CN>
            //   <en>A state action must target a positive item identifier; do not enter state mapping or data access when it is missing.</en>
            // </lang>
            if (normalized.ItemId <= 0)
            {
                return new CollaborationItemResult(false, 0, string.Empty, normalized.ActionKey, "Collaboration item id is required.");
            }

            // <lang>
            //   <zh-CN>将受控动作键映射为唯一目标状态；没有映射的动作不能绕过有限状态机进入写入路径。</zh-CN>
            //   <en>Map the controlled action key to its single target status; an unmapped action cannot bypass the finite-state machine into the write path.</en>
            // </lang>
            string targetStatus = MapActionToStatus(normalized.ActionKey);
            if (string.IsNullOrEmpty(targetStatus))
            {
                return new CollaborationItemResult(false, normalized.ItemId, string.Empty, normalized.ActionKey, "Unsupported collaboration action.");
            }

            // <lang>
            //   <zh-CN>写入事项事实和工作流事件前确认最小 schema，避免只更新其中一侧的降级路径。</zh-CN>
            //   <en>Confirm the minimum schema before writing item facts and workflow events, avoiding a downgraded path that can update only one side.</en>
            // </lang>
            if (!IsSchemaAvailable())
            {
                return new CollaborationItemResult(false, normalized.ItemId, string.Empty, normalized.ActionKey, "Collaboration item schema is unavailable.");
            }

            // <lang>
            //   <zh-CN>读取当前事项用于服务端授权和处理结果编码；不存在的事项不披露更多存储细节。</zh-CN>
            //   <en>Load the current item for server-side authorization and the result code; an absent item does not disclose further storage detail.</en>
            // </lang>
            CollaborationItemInfo item = FindItem(normalized.ItemId);
            if (item == null)
            {
                return new CollaborationItemResult(false, normalized.ItemId, string.Empty, normalized.ActionKey, "Collaboration item was not found or cannot accept this action.");
            }

            // <lang>
            //   <zh-CN>重新解析当前动作人授权；客户端提供的用户标识不会直接决定状态动作权限。</zh-CN>
            //   <en>Re-resolve current actor authorization; a client-supplied user identifier does not directly determine state-action permission.</en>
            // </lang>
            CollaborationItemActorAuthorization actor;
            if (!TryGetActorAuthorization(normalized.ActorUserId, out actor))
            {
                return new CollaborationItemResult(false, normalized.ItemId, item.ItemCode, normalized.ActionKey, "A signed-in portal user is required to apply this action.");
            }

            // <lang>
            //   <zh-CN>用服务端确认的显示名替换输入值，确保事件动作人文字与授权身份一致。</zh-CN>
            //   <en>Replace the input value with the server-confirmed display name so event actor text remains aligned with the authorized identity.</en>
            // </lang>
            normalized.ActorName = actor.ActorName;

            // <lang>
            //   <zh-CN>在写入前按事项、动作和当前授权复核处理权；SQL 仍会在更新时再次验证当前状态，防止预读后的陈旧状态推进。</zh-CN>
            //   <en>Recheck handling permission from the item, action, and current authorization before writing; SQL still verifies current status during update to prevent a stale pre-read from advancing state.</en>
            // </lang>
            if (!CanApplyAction(item, normalized.ActionKey, actor))
            {
                return new CollaborationItemResult(false, normalized.ItemId, item.ItemCode, normalized.ActionKey, "The current user is not allowed to apply this action.");
            }

            // <lang>
            //   <zh-CN>退回和拒绝等需要处理意见的动作不能产生无说明的状态事件；其他动作保留可选评论契约。</zh-CN>
            //   <en>Actions such as return and reject that require a handling reason cannot create an unexplained state event; other actions retain the optional-comment contract.</en>
            // </lang>
            if (ActionRequiresComment(normalized.ActionKey) && string.IsNullOrWhiteSpace(normalized.Comment))
            {
                return new CollaborationItemResult(false, normalized.ItemId, item.ItemCode, normalized.ActionKey, "A plain-text handling comment is required for this action.");
            }

            try
            {
                // <lang>
                //   <zh-CN>单个参数化命令批次先以动作/当前状态谓词更新事项，再仅从实际更新集合写入 WorkflowAction 事件并返回事项事实；更新为零时不会生成孤立事件。</zh-CN>
                //   <en>The single parameterized command batch first updates the item through an action/current-status predicate, then writes a WorkflowAction event only from the actually updated set and returns item facts; a zero-row update produces no orphan event.</en>
                // </lang>
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

                // <lang>
                //   <zh-CN>只有批次返回事项编码时才确认状态已更新；空结果统一表示事项不存在或已不再接受该动作，避免泄露并发细节。</zh-CN>
                //   <en>Confirm the state update only when the batch returns an item code; an empty result consistently means the item is absent or no longer accepts the action, avoiding disclosure of concurrency detail.</en>
                // </lang>
                CollaborationItemWriteRow row = rows.Count == 0 ? null : rows[0];
                return row == null || string.IsNullOrWhiteSpace(row.ItemCode)
                    ? new CollaborationItemResult(false, normalized.ItemId, string.Empty, normalized.ActionKey, "Collaboration item was not found or cannot accept this action.")
                    : new CollaborationItemResult(true, row.ItemId, row.ItemCode, normalized.ActionKey, "Collaboration item state updated.");
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>状态动作失败只返回既有通用结果，不向页面泄露 SQL、连接或异常详情。</zh-CN>
                //   <en>Return only the established generic result when a state action fails and do not expose SQL, connection, or exception detail to the page.</en>
                // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>数据库状态动作批次返回的最小事项写入结果。</zh-CN>
        ///   <en>Minimal item-write result returned by the database state-action batch.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>该类型只承载批次明确返回的标识和编码，供调用方区分实际更新与零行并发失败；它不是新的授权来源，也不包含数据库异常详情。</zh-CN>
        ///   <en>This type carries only the identifier and code explicitly returned by the batch so callers can distinguish an actual update from a zero-row concurrency failure; it is not an authorization source and contains no database exception detail.</en>
        /// </lang>
        /// </remarks>
        private sealed class CollaborationItemWriteRow
        {
            /// <summary>
            /// <lang>
            ///   <zh-CN>批次实际更新的协同事项标识。</zh-CN>
            ///   <en>Identifier of the collaboration item actually updated by the batch.</en>
            /// </lang>
            /// </summary>
            public long ItemId { get; set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>批次实际更新的稳定事项编码。</zh-CN>
            ///   <en>Stable item code of the collaboration item actually updated by the batch.</en>
            /// </lang>
            /// </summary>
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
            /// <summary>
            /// <lang>
            ///   <zh-CN>创建一次服务端身份复核后的最小动作人授权快照。</zh-CN>
            ///   <en>Creates a minimal actor-authorization snapshot after server-side identity revalidation.</en>
            /// </lang>
            /// </summary>
            /// <param name="actorUserId">
            /// <l>
            ///   <zh-CN>服务端重新解析得到的门户用户标识。</zh-CN>
            ///   <en>Portal-user identifier re-resolved by the server.</en>
            /// </l>
            /// </param>
            /// <param name="actorName">
            /// <l>
            ///   <zh-CN>经长度限制的服务端显示名称。</zh-CN>
            ///   <en>Server-confirmed display name after length limiting.</en>
            /// </l>
            /// </param>
            /// <param name="permissionKeys">
            /// <l>
            ///   <zh-CN>当前用户的去空、去重权限键快照。</zh-CN>
            ///   <en>Current user's non-empty, de-duplicated permission-key snapshot.</en>
            /// </l>
            /// </param>
            /// <param name="isAdministrator">
            /// <l>
            ///   <zh-CN>服务端按角色或协同管理员权限计算出的管理员标记。</zh-CN>
            ///   <en>Administrator flag computed by the server from roles or collaboration-admin permission.</en>
            /// </l>
            /// </param>
            public CollaborationItemActorAuthorization(int actorUserId, string actorName, string[] permissionKeys, bool isAdministrator)
            {
                ActorUserId = actorUserId;
                ActorName = actorName;
                PermissionKeys = permissionKeys ?? new string[0];
                IsAdministrator = isAdministrator;
            }

            /// <summary>
            /// <lang>
            ///   <zh-CN>服务端确认的门户用户标识。</zh-CN>
            ///   <en>Server-confirmed portal-user identifier.</en>
            /// </lang>
            /// </summary>
            public int ActorUserId { get; private set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>用于事件和诊断低敏展示的服务端用户名称。</zh-CN>
            ///   <en>Server-confirmed user name for low-sensitivity event and diagnostic display.</en>
            /// </lang>
            /// </summary>
            public string ActorName { get; private set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>本次授权复核得到的权限键快照。</zh-CN>
            ///   <en>Permission-key snapshot produced by this authorization recheck.</en>
            /// </lang>
            /// </summary>
            public string[] PermissionKeys { get; private set; }

            /// <summary>
            /// <lang>
            ///   <zh-CN>服务端计算的协同事项管理员标记。</zh-CN>
            ///   <en>Server-computed collaboration-item administrator flag.</en>
            /// </lang>
            /// </summary>
            public bool IsAdministrator { get; private set; }
        }
    }
}
