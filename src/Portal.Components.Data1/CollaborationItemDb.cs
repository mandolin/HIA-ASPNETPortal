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

        // <lang>
        //   <zh-CN>父子层级最大深度（顶层为 1）。超出该深度的新子项会被拒绝。</zh-CN>
        //   <en>Maximum parent-child hierarchy depth (top level is 1). A new child beyond this depth is rejected.</en>
        // </lang>
        private const int MaxHierarchyDepth = 5;
        private readonly PortalBizDbContext context;
        private readonly IReferenceDataDb referenceDataDb;
        private readonly IUsersDb usersDb;
        private readonly IRolesDb rolesDb;

        // <lang>
        //   <zh-CN>可选员工组织目录读取服务，用于解析当前动作人的组织范围；为空时组织维度不授予可见性（fail-closed）。</zh-CN>
        //   <en>Optional employee-directory reader used to resolve the current actor's organization scope; when null the organization dimension grants no visibility (fail-closed).</en>
        // </lang>
        private readonly IEmployeeDirectoryDb employeeDirectoryDb;

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
        /// <param name="employeeDirectoryDb">
        /// <l>
        ///   <zh-CN>可选员工组织目录读取服务，用于解析当前动作人所属组织单元；为空引用或读取失败时组织维度不授予可见性。</zh-CN>
        ///   <en>Optional employee-directory reader used to resolve the current actor's organization unit; a null reference or a read failure leaves the organization dimension granting no visibility.</en>
        /// </l>
        /// </param>
        public CollaborationItemDb(PortalBizDbContext context, IReferenceDataDb referenceDataDb, IUsersDb usersDb, IRolesDb rolesDb, IEmployeeDirectoryDb employeeDirectoryDb)
        {
            this.context = context;
            this.referenceDataDb = referenceDataDb;
            this.usersDb = usersDb;
            this.rolesDb = rolesDb;
            this.employeeDirectoryDb = employeeDirectoryDb;
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
            //   <zh-CN>父项校验：父项列必须已迁移，父项必须存在且非终态，且新子项深度不得超出层级上限。新建事项不可能成为既有事项的祖先，因此创建路径无需成环检查。</zh-CN>
            //   <en>Parent validation: the parent column must be migrated, the parent must exist and be non-terminal, and the new child must not exceed the depth limit. A new item cannot become an ancestor of any existing item, so the create path needs no cycle check.</en>
            // </lang>
            if (normalized.ParentItemId.HasValue)
            {
                if (!HasColumn(ItemTableName, "ParentItemId"))
                {
                    return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "Collaboration hierarchy is not available in this deployment.");
                }

                CollaborationItemInfo parent = FindItem(normalized.ParentItemId.Value);
                if (parent == null)
                {
                    return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "Parent item was not found.");
                }

                if (IsTerminalStatus(parent.ItemStatus))
                {
                    return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "Parent item is closed and cannot accept child items.");
                }

                if (GetItemDepth(parent.ItemId) >= MaxHierarchyDepth)
                {
                    return new CollaborationItemResult(false, 0, string.Empty, PortalCollaborationItemActions.Submit, "Collaboration hierarchy depth limit reached.");
                }
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
     [UpdatedBy],
     [ParentItemId])
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
     @SubmittedBy,
     @ParentItemId);

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
                    new SqlParameter("@SubmittedBy", normalized.SubmittedBy),
                    CreateNullableLongParameter("@ParentItemId", normalized.ParentItemId)).ToList();

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

            // <lang>
            //   <zh-CN>列表读取前先在服务端重新解析动作人授权；身份无法确认时不返回任何事项，避免用客户端传入的用户标识直接换取数据。</zh-CN>
            //   <en>Re-resolve actor authorization on the server before the list read; when the identity cannot be confirmed, return no items so a client-supplied user identifier cannot by itself fetch data.</en>
            // </lang>
            CollaborationItemActorAuthorization actor;
            if (!TryGetActorAuthorization(userId, out actor))
            {
                return new List<CollaborationItemInfo>();
            }

            try
            {
                // <lang>
                //   <zh-CN>SQL 只做粗筛，返回前统一用数据范围判定裁剪；列表与详情共用同一判定，避免两条路径出现不同的可见性口径。</zh-CN>
                //   <en>The SQL only pre-filters; every row is trimmed by the data-scope decision before returning, so list and detail share one decision instead of two visibility rules.</en>
                // </lang>
                return QueryItems(
                    @"
WHERE [Item].[InitiatorUserId] = @UserId
   OR [Item].[OwnerUserId] = @UserId",
                    NormalizeTake(take, 20),
                    new SqlParameter("@UserId", userId))
                    .Where(item => CanView(item, actor))
                    .ToList();
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
        ///   <zh-CN>按发生时间和事件标识升序排列的可见事件；无效输入、无参与权、不在数据范围内、schema 不可用或读取异常时返回空集合且不泄露内部原因。</zh-CN>
        ///   <en>Visible events ordered by occurrence time and event identifier; invalid input, missing participation, out-of-scope data, unavailable schema, or read failure returns an empty collection without exposing the internal reason.</en>
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
            if (item == null || !TryGetActorAuthorization(actorUserId, out actor) || !CanParticipate(item, actor) || !CanView(item, actor))
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
            //   <zh-CN>显式状态机门禁：从当前状态发起该动作不合法时直接拒绝（fail-closed），在数据库守卫之外提供可单测的服务端判定；未知动作已由 MapActionToStatus 先行拦截。</zh-CN>
            //   <en>Explicit state-machine gate: deny directly when starting the action from the current status is not a legal transition (fail-closed), adding a testable server-side decision on top of the database guard; unknown actions were already intercepted by MapActionToStatus.</en>
            // </lang>
            if (!PortalCollaborationItemTransitions.IsLegalTransition(item.ItemStatus, normalized.ActionKey))
            {
                return new CollaborationItemResult(false, normalized.ItemId, item.ItemCode, normalized.ActionKey, "This action is not allowed from the current item status.");
            }

            // <lang>
            //   <zh-CN>退回和拒绝等需要处理意见的动作不能产生无说明的状态事件；其他动作保留可选评论契约。</zh-CN>
            //   <en>Actions such as return and reject that require a handling reason cannot create an unexplained state event; other actions retain the optional-comment contract.</en>
            // </lang>
            if (ActionRequiresComment(normalized.ActionKey) && string.IsNullOrWhiteSpace(normalized.Comment))
            {
                return new CollaborationItemResult(false, normalized.ItemId, item.ItemCode, normalized.ActionKey, "A plain-text handling comment is required for this action.");
            }

            // <lang>
            //   <zh-CN>父子状态约束：进入终态前必须无未终态后代，防止父项在子项仍进行时被关闭。</zh-CN>
            //   <en>Parent-child state constraint: an item must have no non-terminal descendant before it becomes terminal, preventing a parent from being closed while children are still in progress.</en>
            // </lang>
            if (IsTerminalStatus(targetStatus) && HasOpenDescendants(item.ItemId))
            {
                return new CollaborationItemResult(false, normalized.ItemId, item.ItemCode, normalized.ActionKey, "Item has unfinished child items and cannot be closed.");
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
  AND (" + PortalCollaborationItemTransitions.BuildSqlStatusPredicate() + @"
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

        /// <inheritdoc />
        public IList<CollaborationItemParticipantInfo> GetParticipants(long itemId)
        {
            if (itemId <= 0 || !HasTable("PortalBiz_CollaborationItemParticipants"))
            {
                return new List<CollaborationItemParticipantInfo>();
            }

            try
            {
                return context.Database.SqlQuery<CollaborationItemParticipantInfo>(@"
SELECT
    [P].[ParticipantId],
    [P].[ItemId],
    [P].[UserId],
    [U].[Name] AS [UserName],
    [P].[ParticipantRoleKey],
    [P].[CreatedUtc]
FROM [dbo].[PortalBiz_CollaborationItemParticipants] AS [P]
LEFT JOIN [dbo].[Portal_Users] AS [U]
    ON [U].[UserID] = [P].[UserId]
WHERE [P].[ItemId] = @ItemId
ORDER BY [P].[ParticipantId];",
                    new SqlParameter("@ItemId", itemId)).ToList();
            }
            catch (Exception)
            {
                return new List<CollaborationItemParticipantInfo>();
            }
        }

        /// <inheritdoc />
        public CollaborationItemParticipantResult AddParticipant(CollaborationItemParticipantCreateRequest request)
        {
            if (request == null)
            {
                return new CollaborationItemParticipantResult(false, "Participant request is missing.");
            }

            long itemId = request.ItemId;
            int userId = request.UserId;
            string roleKey = NormalizeOptionalText(request.ParticipantRoleKey, 20);

            if (itemId <= 0 || userId <= 0)
            {
                return new CollaborationItemParticipantResult(false, "A valid item and user are required.");
            }

            if (!string.Equals(roleKey, PortalCollaborationItemParticipantRoles.Collaborator, StringComparison.Ordinal) &&
                !string.Equals(roleKey, PortalCollaborationItemParticipantRoles.Watcher, StringComparison.Ordinal))
            {
                return new CollaborationItemParticipantResult(false, "Participant role is not allowed.");
            }

            if (!HasTable("PortalBiz_CollaborationItemParticipants"))
            {
                return new CollaborationItemParticipantResult(false, "Participant schema is unavailable.");
            }

            // <lang>
            //   <zh-CN>服务端重新校验操作者授权：发起人、负责人或协同事项管理员方可管理参与人。</zh-CN>
            //   <en>Revalidate actor authorization on the server: only the initiator, owner, or collaboration-item administrator may manage participants.</en>
            // </lang>
            CollaborationItemInfo item = FindItem(itemId);
            if (item == null)
            {
                return new CollaborationItemParticipantResult(false, "Collaboration item was not found.");
            }

            CollaborationItemActorAuthorization actor;
            if (!TryGetActorAuthorization(request.ActorUserId, out actor))
            {
                return new CollaborationItemParticipantResult(false, "A signed-in portal user is required.");
            }

            if (!actor.IsAdministrator &&
                item.InitiatorUserId != actor.ActorUserId &&
                !(item.OwnerUserId.HasValue && item.OwnerUserId.Value == actor.ActorUserId))
            {
                return new CollaborationItemParticipantResult(false, "The current user is not allowed to manage participants.");
            }

            try
            {
                // <lang>
                //   <zh-CN>参与人必须是真实用户；重复添加按既有语义拒绝，不覆盖角色。</zh-CN>
                //   <en>The participant must be a real user; a duplicate add is rejected per existing semantics and does not overwrite the role.</en>
                // </lang>
                if (usersDb == null || usersDb.FindUserById(userId) == null)
                {
                    return new CollaborationItemParticipantResult(false, "Participant user was not found.");
                }

                if (IsParticipant(itemId, userId))
                {
                    return new CollaborationItemParticipantResult(false, "The user is already a participant.");
                }

                context.Database.ExecuteSqlCommand(@"
INSERT INTO [dbo].[PortalBiz_CollaborationItemParticipants]
    ([ItemId], [UserId], [ParticipantRoleKey], [CreatedUtc], [CreatedBy])
VALUES
    (@ItemId, @UserId, @RoleKey, SYSUTCDATETIME(), @CreatedBy);",
                    new SqlParameter("@ItemId", itemId),
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@RoleKey", roleKey),
                    new SqlParameter("@CreatedBy", actor.ActorName ?? "system"));

                return new CollaborationItemParticipantResult(true, "Participant added.");
            }
            catch (Exception)
            {
                return new CollaborationItemParticipantResult(false, "Participant add failed.");
            }
        }

        /// <inheritdoc />
        public CollaborationItemParticipantResult RemoveParticipant(long itemId, int userId, int actorUserId)
        {
            if (itemId <= 0 || userId <= 0)
            {
                return new CollaborationItemParticipantResult(false, "A valid item and user are required.");
            }

            if (!HasTable("PortalBiz_CollaborationItemParticipants"))
            {
                return new CollaborationItemParticipantResult(false, "Participant schema is unavailable.");
            }

            CollaborationItemInfo item = FindItem(itemId);
            if (item == null)
            {
                return new CollaborationItemParticipantResult(false, "Collaboration item was not found.");
            }

            CollaborationItemActorAuthorization actor;
            if (!TryGetActorAuthorization(actorUserId, out actor))
            {
                return new CollaborationItemParticipantResult(false, "A signed-in portal user is required.");
            }

            if (!actor.IsAdministrator &&
                item.InitiatorUserId != actor.ActorUserId &&
                !(item.OwnerUserId.HasValue && item.OwnerUserId.Value == actor.ActorUserId))
            {
                return new CollaborationItemParticipantResult(false, "The current user is not allowed to manage participants.");
            }

            try
            {
                context.Database.ExecuteSqlCommand(@"
DELETE FROM [dbo].[PortalBiz_CollaborationItemParticipants]
WHERE [ItemId] = @ItemId AND [UserId] = @UserId;",
                    new SqlParameter("@ItemId", itemId),
                    new SqlParameter("@UserId", userId));

                return new CollaborationItemParticipantResult(true, "Participant removed.");
            }
            catch (Exception)
            {
                return new CollaborationItemParticipantResult(false, "Participant remove failed.");
            }
        }

        private IList<CollaborationItemInfo> QueryItems(string whereClause, int take, params SqlParameter[] parameters)
        {
            // <lang>
            //   <zh-CN>父项列是 P47.1 的部署级迁移；迁移未执行时列可能不存在，因此用元数据探测选择实际列或 NULL 投影，避免旧库上的读取失败。</zh-CN>
            //   <en>The parent column is a P47.1 deployment-level migration; the column may be absent before migration runs, so probe metadata to select either the real column or a NULL projection and avoid read failures on old databases.</en>
            // </lang>
            string parentColumn = HasColumn(ItemTableName, "ParentItemId")
                ? "    [Item].[ParentItemId]\n"
                : "    CAST(NULL AS BIGINT) AS [ParentItemId]\n";

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
    [Item].[LastActionComment],
" + parentColumn + @"FROM [dbo].[PortalBiz_CollaborationItems] AS [Item]
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

        private bool CanParticipate(CollaborationItemInfo item, CollaborationItemActorAuthorization actor)
        {
            if (item == null || actor == null)
            {
                return false;
            }

            if (actor.IsAdministrator ||
                item.InitiatorUserId == actor.ActorUserId ||
                (item.OwnerUserId.HasValue && item.OwnerUserId.Value == actor.ActorUserId) ||
                HasOwnerRolePermission(item, actor))
            {
                return true;
            }

            // <lang>
            //   <zh-CN>参与人集合成员（协办/关注）也可参与；发起人与负责人已在上面短路，其余角色查询参与人表。</zh-CN>
            //   <en>Participant-set members (Collaborator/Watcher) may also participate; the initiator and owner short-circuit above, while other roles check the participant table.</en>
            // </lang>
            return IsParticipant(item.ItemId, actor.ActorUserId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按数据范围契约判定当前动作人是否可见指定协同事项，列表与详情共用本判定。</zh-CN>
        ///   <en>Decides whether the current actor may view the specified collaboration item under the data-scope contract; list and detail reads share this decision.</en>
        /// </lang>
        /// </summary>
        /// <param name="item">
        /// <l>
        ///   <zh-CN>待判定的协同事项投影；为空引用时判定失败。</zh-CN>
        ///   <en>Collaboration-item projection to evaluate; a null reference fails the decision.</en>
        /// </l>
        /// </param>
        /// <param name="actor">
        /// <l>
        ///   <zh-CN>服务端重新解析出的动作人授权快照；为空引用时判定失败。</zh-CN>
        ///   <en>Actor-authorization snapshot re-resolved by the server; a null reference fails the decision.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>归属、参与人或组织任一维度成立时为 <c>true</c>；证据不足时一律为 <c>false</c>（fail-closed）。</zh-CN>
        ///   <en><c>true</c> when the ownership, participant, or organization dimension holds; always <c>false</c> when evidence is insufficient (fail-closed).</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>本方法只做可见性判定，不改变 <c>CanParticipate</c> 的既有结果：写动作与参与资格仍由 <c>CanParticipate</c> 负责，本方法是在其之上追加的数据范围层。判定逻辑集中在 <see cref="CollaborationItemDataScopePolicy"/>，本方法只负责收集服务端证据。</zh-CN>
        ///   <en>This method only decides visibility and never changes the existing result of <c>CanParticipate</c>: write actions and participation eligibility remain owned by <c>CanParticipate</c>, while this method adds the data-scope layer on top. The decision logic lives in <see cref="CollaborationItemDataScopePolicy"/>; this method only collects server-side evidence.</en>
        /// </lang>
        /// </remarks>
        private bool CanView(CollaborationItemInfo item, CollaborationItemActorAuthorization actor)
        {
            // <lang>
            //   <zh-CN>事项或动作人快照缺失时无法安全判定，直接拒绝而不是放行。</zh-CN>
            //   <en>Deny directly rather than allow when the item or the actor snapshot is missing and no safe decision is possible.</en>
            // </lang>
            if (item == null || actor == null)
            {
                return false;
            }

            // <lang>
            //   <zh-CN>参与人与组织范围都在服务端现查，避免调用方自行声明范围边界；查询失败时对应维度退化为拒绝。</zh-CN>
            //   <en>Resolve the participant flag and the organization scope on the server so callers cannot declare their own scope; a failed query degrades the related dimension to deny.</en>
            // </lang>
            CollaborationItemDataScope scope = new CollaborationItemDataScope(
                actor.ActorUserId,
                actor.IsAdministrator,
                IsParticipant(item.ItemId, actor.ActorUserId),
                ResolveVisibleOrganizationUnitIds(actor.ActorUserId));

            return CollaborationItemDataScopePolicy.CanView(item, scope);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析当前动作人可见的组织单元范围；范围未知时返回空集合，使组织维度不授予可见性。</zh-CN>
        ///   <en>Resolves the organization-unit scope visible to the current actor; returns an empty collection when the scope is unknown so the organization dimension grants no visibility.</en>
        /// </lang>
        /// </summary>
        /// <param name="actorUserId">
        /// <l>
        ///   <zh-CN>服务端重新解析得到的门户用户标识；非正值直接返回空集合。</zh-CN>
        ///   <en>Portal-user identifier re-resolved by the server; a non-positive value returns an empty collection directly.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可见组织单元标识集合；当前只解析动作人所属组织单元本身，子树展开留作后续深化项。</zh-CN>
        ///   <en>Visible organization-unit identifiers; currently only the actor's own organization unit is resolved, while subtree expansion remains a later deepening item.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>组织范围来自账号与员工的有效绑定及员工主数据；缺目录服务、缺绑定、缺员工号或读取异常一律返回空集合，绝不因组织维度解析失败而放宽可见性。</zh-CN>
        ///   <en>The organization scope comes from the active user-to-employee binding and employee master data; a missing directory service, missing binding, missing employee code, or read failure all return an empty collection, and the organization dimension is never widened by a resolution failure.</en>
        /// </lang>
        /// </remarks>
        private IList<int> ResolveVisibleOrganizationUnitIds(int actorUserId)
        {
            if (employeeDirectoryDb == null || actorUserId <= 0)
            {
                return new int[0];
            }

            try
            {
                // <lang>
                //   <zh-CN>没有有效绑定的账号没有组织身份，组织维度不授予任何可见性。</zh-CN>
                //   <en>An account without an active binding has no organization identity, so the organization dimension grants nothing.</en>
                // </lang>
                IUserEmployeeBindingInfo binding = employeeDirectoryDb.GetActiveBindingByUserId(actorUserId);
                if (binding == null || binding.EmployeeId <= 0 || string.IsNullOrWhiteSpace(binding.EmployeeCode))
                {
                    return new int[0];
                }

                // <lang>
                //   <zh-CN>用员工号作为关键字回查员工主数据，并按员工标识精确取回，避免模糊匹配把其他员工带进组织范围。</zh-CN>
                //   <en>Look the employee master data back up by employee code and select strictly by employee identifier so fuzzy matching cannot pull another employee into the organization scope.</en>
                // </lang>
                IEmployeeInfo employee = employeeDirectoryDb
                    .GetEmployees(new EmployeeDirectoryQuery { Keyword = binding.EmployeeCode, Take = 20 })
                    .FirstOrDefault(candidate => candidate.EmployeeId == binding.EmployeeId);

                if (employee == null || !employee.OrganizationUnitId.HasValue || employee.OrganizationUnitId.Value <= 0)
                {
                    return new int[0];
                }

                return new[] { employee.OrganizationUnitId.Value };
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>目录读取失败按范围未知处理；此处不记录异常细节，避免低敏组织信息之外的内容进入调用方反馈。</zh-CN>
                //   <en>Treat a directory read failure as unknown scope; no exception detail is recorded here so nothing beyond low-sensitivity organization data can reach caller feedback.</en>
                // </lang>
                return new int[0];
            }
        }

        // <lang>
        //   <zh-CN>判断用户是否属于事项的参与人集合（协办/关注）。</zh-CN>
        //   <en>Determines whether a user belongs to the item participant set (Collaborator/Watcher).</en>
        // </lang>
        private bool IsParticipant(long itemId, int userId)
        {
            if (itemId <= 0 || userId <= 0 || !HasTable("PortalBiz_CollaborationItemParticipants"))
            {
                return false;
            }

            try
            {
                return context.Database.SqlQuery<int>(@"
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM [dbo].[PortalBiz_CollaborationItemParticipants]
    WHERE [ItemId] = @ItemId AND [UserId] = @UserId
) THEN 1 ELSE 0 END;",
                    new SqlParameter("@ItemId", itemId),
                    new SqlParameter("@UserId", userId)).Single() == 1;
            }
            catch (Exception)
            {
                return false;
            }
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

        // <lang>
        //   <zh-CN>判断事项状态是否为终态；终态事项不得再作为父项，也不得作为子项遗留。</zh-CN>
        //   <en>Determines whether an item status is terminal; a terminal item can no longer be a parent nor remain as a child.</en>
        // </lang>
        private static bool IsTerminalStatus(string status)
        {
            return string.Equals(status, PortalCollaborationItemStatuses.Completed, StringComparison.Ordinal) ||
                   string.Equals(status, PortalCollaborationItemStatuses.Rejected, StringComparison.Ordinal) ||
                   string.Equals(status, PortalCollaborationItemStatuses.Cancelled, StringComparison.Ordinal) ||
                   string.Equals(status, PortalCollaborationItemStatuses.Closed, StringComparison.Ordinal);
        }

        // <lang>
        //   <zh-CN>沿父链递归计算事项深度（顶层为 1）；迁移未执行或读取失败时保守返回 1，不阻断单层创建。</zh-CN>
        //   <en>Recursively computes the item depth along the parent chain (top level is 1); conservatively returns 1 when the migration is absent or the read fails, so single-level creation is not blocked.</en>
        // </lang>
        private int GetItemDepth(long itemId)
        {
            if (itemId <= 0 || !HasColumn(ItemTableName, "ParentItemId"))
            {
                return 1;
            }

            try
            {
                return context.Database.SqlQuery<int>(@"
;WITH [ParentChain] AS
(
    SELECT [ItemId], [ParentItemId], 1 AS [Depth]
    FROM [dbo].[PortalBiz_CollaborationItems]
    WHERE [ItemId] = @ItemId
    UNION ALL
    SELECT [Child].[ItemId], [Child].[ParentItemId], [ParentChain].[Depth] + 1
    FROM [dbo].[PortalBiz_CollaborationItems] AS [Child]
    INNER JOIN [ParentChain] ON [Child].[ItemId] = [ParentChain].[ParentItemId]
    WHERE [ParentChain].[Depth] < @MaxDepth
)
SELECT MAX([Depth]) FROM [ParentChain];",
                    new SqlParameter("@ItemId", itemId),
                    new SqlParameter("@MaxDepth", MaxHierarchyDepth)).Single();
            }
            catch (Exception)
            {
                return 1;
            }
        }

        // <lang>
        //   <zh-CN>判断事项是否仍有未终态的后代；父项进入终态前必须无未终态后代。</zh-CN>
        //   <en>Determines whether an item still has non-terminal descendants; an item must have no non-terminal descendant before it becomes terminal.</en>
        // </lang>
        private bool HasOpenDescendants(long itemId)
        {
            if (itemId <= 0 || !HasColumn(ItemTableName, "ParentItemId"))
            {
                return false;
            }

            try
            {
                return context.Database.SqlQuery<int>(@"
;WITH [Descendants] AS
(
    SELECT [ItemId], [ParentItemId], [ItemStatus]
    FROM [dbo].[PortalBiz_CollaborationItems]
    WHERE [ParentItemId] = @ItemId
    UNION ALL
    SELECT [Child].[ItemId], [Child].[ParentItemId], [Child].[ItemStatus]
    FROM [dbo].[PortalBiz_CollaborationItems] AS [Child]
    INNER JOIN [Descendants] ON [Child].[ParentItemId] = [Descendants].[ItemId]
)
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM [Descendants]
    WHERE [ItemStatus] NOT IN (N'Completed', N'Rejected', N'Cancelled', N'Closed')
) THEN 1 ELSE 0 END;",
                    new SqlParameter("@ItemId", itemId)).Single() == 1;
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
                SubmittedBy = string.IsNullOrWhiteSpace(request.SubmittedBy) ? "system" : NormalizeText(request.SubmittedBy, 100),
                ParentItemId = request.ParentItemId.HasValue && request.ParentItemId.Value > 0 ? request.ParentItemId : null
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

        // <lang>
        //   <zh-CN>动作到目标状态的映射由显式迁移表派生（当前设计中同一动作目标唯一）；未知动作返回空串沿用既有"不支持动作"语义。</zh-CN>
        //   <en>The action-to-target mapping derives from the explicit transition table (each action has a unique target in the current design); an unknown action returns an empty string, preserving the established "unsupported action" behavior.</en>
        // </lang>
        private static string MapActionToStatus(string actionKey)
        {
            CollaborationItemTransition transition = PortalCollaborationItemTransitions.All
                .FirstOrDefault(candidate => string.Equals(candidate.ActionKey, actionKey, StringComparison.Ordinal));
            return transition.ToStatus ?? string.Empty;
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

        private static SqlParameter CreateNullableLongParameter(string name, long? value)
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
