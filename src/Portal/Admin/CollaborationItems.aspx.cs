using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>企业协同事项后台页。</zh-CN>
    ///   <en>Administration page for enterprise collaboration items.</en>
    /// </lang>
    /// </summary>
    public partial class CollaborationItems : PortalPage<CollaborationItems>
    {
        private const int PageSize = 50;

        /// <summary>
        /// <lang>
        ///   <zh-CN>企业协同事项数据服务。</zh-CN>
        ///   <en>Enterprise collaboration-item data service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public ICollaborationItemDb CollaborationItemDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>受治理参考数据目录读取服务，用于后台创建表单的类型和优先级选择。</zh-CN>
        ///   <en>Governed reference-data catalog reader for type and priority selectors on the administration create form.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IReferenceDataDb ReferenceDataDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>轻量待办数据服务，用于把事项提交和处理结果投影到后台待办。</zh-CN>
        ///   <en>Lightweight work-item data service used to project item submissions and handling results into the administration work queue.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IPortalWorkItemDb WorkItemDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>用户数据访问服务，用于解析当前管理员用户标识。</zh-CN>
        ///   <en>User data service used to resolve the current administrator user identifier.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IUsersDb UsersDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化后台页并绑定筛选条件和协同事项列表。</zh-CN>
        ///   <en>Initializes the administration page and binds filters plus the collaboration-item list.</en>
        /// </lang>
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>先执行查看权限门禁，再仅在首次请求绑定筛选项和列表，避免回发覆盖管理员输入。</zh-CN>
            //   <en>Apply the view gate first, then bind filters and the list only on the first request so postbacks do not overwrite administrator input.</en>
            // </lang>
            if (!EnsureCanViewItems())
            {
                return;
            }

            if (!IsPostBack)
            {
                BindReferenceDataLists();
                BindStatusFilter();
                BindItems();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建并提交一条低敏企业协同事项。</zh-CN>
        ///   <en>Creates and submits one low-sensitivity enterprise collaboration item.</en>
        /// </lang>
        /// </summary>
        protected void CreateButton_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>创建流程按创建权限、期限格式和数据服务顺序推进；门禁失败保持页面提示，不进入持久化。</zh-CN>
            //   <en>The create flow proceeds through create permission, due-date format, and data-service gates; failures stay as page messages and never reach persistence.</en>
            // </lang>
            if (!EnsureCanCreateItems())
            {
                return;
            }

            // <lang>
            //   <zh-CN>后台表单期限统一解释为 UTC；空值表示未设置，非法格式不会写入事项。</zh-CN>
            //   <en>Interpret administration-form due dates as UTC; blank means unset and invalid formats never write an item.</en>
            // </lang>
            DateTime? dueUtc;
            if (!TryParseDueUtc(DueUtcTextBox.Text, out dueUtc))
            {
                MessageLabel.Text = "Due UTC must be empty or use yyyy-MM-dd HH:mm:ss.";
                return;
            }

            // <lang>
            //   <zh-CN>创建请求保留后台指定的处理角色和当前管理员身份，服务层负责事实与状态写入。</zh-CN>
            //   <en>The create request preserves the administration-selected handling role and current administrator identity while the service writes the fact and state.</en>
            // </lang>
            CollaborationItemResult result = CollaborationItemDb.CreateSubmittedItem(
                new CollaborationItemCreateRequest
                {
                    ItemTypeKey = ItemTypeList.SelectedValue,
                    Title = TitleTextBox.Text,
                    Summary = SummaryTextBox.Text,
                    Description = DescriptionTextBox.Text,
                    InitiatorUserId = GetCurrentUserId(),
                    OwnerRoleKey = OwnerRoleKeyTextBox.Text,
                    PriorityKey = PriorityList.SelectedValue,
                    DueUtc = dueUtc,
                    SubmittedUtc = DateTime.UtcNow,
                    SubmittedBy = GetCurrentUserName()
                });

            if (!result.Succeeded)
            {
                MessageLabel.Text = result.Message;
                BindItems();
                return;
            }

            // <lang>
            //   <zh-CN>事项创建成功后记录审计，再补建待办投影；待办失败不回滚事项事实。</zh-CN>
            //   <en>Record audit after item creation succeeds, then ensure the work-item projection; projection failure does not roll back the item fact.</en>
            // </lang>
            PortalOperationAudit.Record(
                PortalOperationAuditEvents.BusinessModuleCategory,
                PortalOperationAuditEvents.CollaborationItemSubmitted,
                PortalOperationAuditEvents.CollaborationItemTargetType,
                result.ItemId.ToString(CultureInfo.InvariantCulture),
                "Collaboration item submitted. ItemCode=" + result.ItemCode,
                Context);

            TryEnsureWorkItem(result.ItemId, result.ItemCode, TitleTextBox.Text, SummaryTextBox.Text, OwnerRoleKeyTextBox.Text, dueUtc);
            ClearCreateForm();
            MessageLabel.Text = "Collaboration item submitted.";
            BindItems();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按当前筛选条件重新绑定协同事项列表。</zh-CN>
        ///   <en>Rebinds collaboration items using the current filter.</en>
        /// </lang>
        /// </summary>
        protected void SearchButton_Click(object sender, EventArgs e)
        {
            if (!EnsureCanViewItems())
            {
                return;
            }

            BindItems();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理列表中的协同事项动作。</zh-CN>
        ///   <en>Handles collaboration-item actions from the list.</en>
        /// </lang>
        /// </summary>
        protected void ItemsRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            // <lang>
            //   <zh-CN>列表动作先校验动作参数，再区分评论与状态动作，最后由处理权限门禁保护领域动作。</zh-CN>
            //   <en>Validate the list action first, separate comments from state actions, and protect domain actions with the handling-permission gate.</en>
            // </lang>
            string actionKey = Convert.ToString(e.CommandName, CultureInfo.InvariantCulture);
            long itemId;
            if (!long.TryParse(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture), out itemId) || itemId <= 0)
            {
                MessageLabel.Text = "Invalid collaboration item id.";
                return;
            }

            // <lang>
            //   <zh-CN>评论文本来自当前列表行控件；后续服务和审计只使用归一化长度边界。</zh-CN>
            //   <en>Read the comment from the current list-row control; the service and audit later use the normalized length boundary.</en>
            // </lang>
            TextBox commentBox = e.Item.FindControl("ActionCommentTextBox") as TextBox;
            string actionComment = commentBox == null ? string.Empty : commentBox.Text;
            if (IsCommentCommand(actionKey))
            {
                // <lang>
                //   <zh-CN>评论动作不改变事项状态，但会写入对应可见范围并刷新列表。</zh-CN>
                //   <en>Comment actions do not change item state; they write the corresponding visibility scope and refresh the list.</en>
                // </lang>
                TryAddComment(itemId, actionKey, actionComment);
                BindItems();
                return;
            }

            if (!IsSupportedAction(actionKey))
            {
                MessageLabel.Text = "Unsupported collaboration action.";
                BindItems();
                return;
            }

            if (!EnsureCanHandleItems())
            {
                return;
            }

            // <lang>
            //   <zh-CN>状态动作由协同事项服务执行，动作键和评论长度都沿用固定契约。</zh-CN>
            //   <en>State actions are executed by the collaboration-item service using the fixed action-key and comment-length contracts.</en>
            // </lang>
            CollaborationItemResult result = CollaborationItemDb.ApplyAction(
                new CollaborationItemActionRequest
                {
                    ItemId = itemId,
                    ActionKey = actionKey,
                    Comment = NormalizeInput(actionComment, 1000),
                    ActorUserId = GetCurrentUserId(),
                    ActorName = GetCurrentUserName(),
                    OccurredUtc = DateTime.UtcNow
                });

            if (!result.Succeeded)
            {
                MessageLabel.Text = result.Message;
                BindItems();
                return;
            }

            // <lang>
            //   <zh-CN>动作事实成功后记录审计，并按重提或其它动作分别维护待办投影。</zh-CN>
            //   <en>After the action fact succeeds, record audit and maintain the work-item projection differently for resubmission versus other actions.</en>
            // </lang>
            TryRecordOperationAudit(result);
            if (string.Equals(actionKey, PortalCollaborationItemActions.Resubmit, StringComparison.Ordinal))
            {
                TryEnsureResubmittedWorkItem(result.ItemId);
            }
            else
            {
                TryCompleteWorkItem(result.ItemId, actionKey, actionComment);
            }

            MessageLabel.Text = "Collaboration item state updated.";
            BindItems();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定后台允许筛选的协同事项状态，并默认定位到已提交状态。</zh-CN>
        ///   <en>Binds administration-allowed collaboration-item statuses and defaults to submitted items.</en>
        /// </lang>
        /// </summary>
        private void BindStatusFilter()
        {
            // <lang>
            //   <zh-CN>状态值使用固定契约键，显示文本保持后台既有英文兼容值。</zh-CN>
            //   <en>Use fixed contract keys for status values while preserving the administration page's existing display values.</en>
            // </lang>
            StatusFilterList.Items.Clear();
            StatusFilterList.Items.Add(new ListItem("All", string.Empty));
            StatusFilterList.Items.Add(new ListItem(PortalCollaborationItemStatuses.Submitted, PortalCollaborationItemStatuses.Submitted));
            StatusFilterList.Items.Add(new ListItem(PortalCollaborationItemStatuses.InProgress, PortalCollaborationItemStatuses.InProgress));
            StatusFilterList.Items.Add(new ListItem(PortalCollaborationItemStatuses.Returned, PortalCollaborationItemStatuses.Returned));
            StatusFilterList.Items.Add(new ListItem(PortalCollaborationItemStatuses.Completed, PortalCollaborationItemStatuses.Completed));
            StatusFilterList.Items.Add(new ListItem(PortalCollaborationItemStatuses.Rejected, PortalCollaborationItemStatuses.Rejected));
            StatusFilterList.Items.Add(new ListItem(PortalCollaborationItemStatuses.Cancelled, PortalCollaborationItemStatuses.Cancelled));
            StatusFilterList.Items.Add(new ListItem(PortalCollaborationItemStatuses.Closed, PortalCollaborationItemStatuses.Closed));
            StatusFilterList.SelectedValue = PortalCollaborationItemStatuses.Submitted;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定创建表单的事项类型和优先级参考数据。</zh-CN>
        ///   <en>Binds item-type and priority reference data for the create form.</en>
        /// </lang>
        /// </summary>
        private void BindReferenceDataLists()
        {
            BindReferenceDataList(ItemTypeList, PortalReferenceDataSets.CollaborationItemType);
            BindReferenceDataList(PriorityList, PortalReferenceDataSets.CollaborationPriority);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>优先读取活动参考数据，服务不可用时使用固定治理回退项。</zh-CN>
        ///   <en>Prefers active reference data and uses governed fixed fallback entries when the service is unavailable.</en>
        /// </lang>
        /// </summary>
        private void BindReferenceDataList(DropDownList list, string referenceSetKey)
        {
            // <lang>
            //   <zh-CN>绑定前清空旧项，避免后台回发或服务切换造成重复选项。</zh-CN>
            //   <en>Clear old entries before binding so postbacks or service switches cannot duplicate options.</en>
            // </lang>
            list.Items.Clear();
            IList<ReferenceDataItem> items;
            if (ReferenceDataDb == null || !ReferenceDataDb.TryGetActiveItems(referenceSetKey, out items))
            {
                items = PortalReferenceDataSets.GetFallbackItems(referenceSetKey);
            }

            // <lang>
            //   <zh-CN>下拉项保存稳定值键，显示名称仅来自参考数据目录。</zh-CN>
            //   <en>Store stable value keys in the drop-down while taking display names only from the reference catalog.</en>
            // </lang>
            foreach (ReferenceDataItem item in items)
            {
                list.Items.Add(new ListItem(item.DisplayName, item.ValueKey));
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按筛选状态读取后台事项和管理员可见事件并绑定展示行。</zh-CN>
        ///   <en>Loads administration items and administrator-visible events by status and binds display rows.</en>
        /// </lang>
        /// </summary>
        private void BindItems()
        {
            // <lang>
            //   <zh-CN>数据服务缺失或 Schema 不可用时使用统一空列表回退，不继续读取业务数据。</zh-CN>
            //   <en>Use the common empty-list fallback when the data service or schema is unavailable and do not continue reading business data.</en>
            // </lang>
            if (CollaborationItemDb == null)
            {
                ShowUnavailable("Collaboration item data service is not registered.");
                return;
            }

            if (!CollaborationItemDb.IsSchemaAvailable())
            {
                ShowUnavailable("Collaboration item schema is unavailable. Run the P21.3 item migrations and P23.6 PortalBiz_CollaborationItemCommentWorkflow.sql.");
                return;
            }

            // <lang>
            //   <zh-CN>后台查询固定分页上限；事件可见性由服务按当前管理员身份裁剪。</zh-CN>
            //   <en>Use the fixed administration page-size limit; the service scopes visible events to the current administrator.</en>
            // </lang>
            IList<CollaborationItemInfo> items = CollaborationItemDb.GetAdminItems(
                StatusFilterList.SelectedValue,
                PageSize);
            int currentUserId = GetCurrentUserId();
            ItemsRepeater.DataSource = items.Select(item => new CollaborationItemAdminRow(
                item,
                CollaborationItemDb.GetVisibleEvents(item.ItemId, currentUserId))).ToList();
            ItemsRepeater.DataBind();

            ResultLabel.Text = "Showing up to " + PageSize.ToString(CultureInfo.InvariantCulture) +
                               " collaboration items; count: " + items.Count.ToString(CultureInfo.InvariantCulture) + ".";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>显示后台能力不可用提示并绑定空展示集合。</zh-CN>
        ///   <en>Displays an unavailable-capability message and binds an empty display collection.</en>
        /// </lang>
        /// </summary>
        private void ShowUnavailable(string message)
        {
            // <lang>
            //   <zh-CN>不可用路径不暴露数据服务内部异常，也不保留旧列表内容。</zh-CN>
            //   <en>The unavailable path does not expose data-service internals and does not retain stale list content.</en>
            // </lang>
            MessageLabel.Text = message ?? string.Empty;
            ResultLabel.Text = string.Empty;
            ItemsRepeater.DataSource = Enumerable.Empty<CollaborationItemAdminRow>();
            ItemsRepeater.DataBind();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查查看全部事项或协同管理员权限。</zh-CN>
        ///   <en>Checks the permission to view all items or act as collaboration administrator.</en>
        /// </lang>
        /// </summary>
        private bool EnsureCanViewItems()
        {
            return PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.BusinessCollaborationViewAll,
                PortalPermissionKeys.BusinessCollaborationAdmin);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查创建事项或协同管理员权限。</zh-CN>
        ///   <en>Checks the permission to create items or act as collaboration administrator.</en>
        /// </lang>
        /// </summary>
        private bool EnsureCanCreateItems()
        {
            return PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.BusinessCollaborationCreate,
                PortalPermissionKeys.BusinessCollaborationAdmin);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查处理事项或协同管理员权限。</zh-CN>
        ///   <en>Checks the permission to handle items or act as collaboration administrator.</en>
        /// </lang>
        /// </summary>
        private bool EnsureCanHandleItems()
        {
            return PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.BusinessCollaborationHandle,
                PortalPermissionKeys.BusinessCollaborationAdmin);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将当前认证用户名解析为门户用户标识；缺少身份或服务时返回零。</zh-CN>
        ///   <en>Resolves the current authenticated name to a Portal user identifier and returns zero when identity or service is unavailable.</en>
        /// </lang>
        /// </summary>
        private int GetCurrentUserId()
        {
            // <lang>
            //   <zh-CN>用户标识只通过用户服务解析，不从请求参数或列表命令推断。</zh-CN>
            //   <en>Resolve the user identifier only through the user service; never infer it from request parameters or list commands.</en>
            // </lang>
            string userName = GetCurrentUserName();
            if (string.IsNullOrWhiteSpace(userName) || UsersDb == null)
            {
                return 0;
            }

            IUserItem user = UsersDb.GetSingleUser(userName);
            return user == null ? 0 : user.UserId;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前认证用户名；后台无认证上下文时使用 system 兼容回退。</zh-CN>
        ///   <en>Reads the current authenticated name and uses the existing system fallback when no authenticated context exists.</en>
        /// </lang>
        /// </summary>
        private string GetCurrentUserName()
        {
            return Context != null &&
                   Context.User != null &&
                   Context.User.Identity != null &&
                   Context.User.Identity.IsAuthenticated
                ? Context.User.Identity.Name
                : "system";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为新建事项确保后台待办投影；待办失败不回滚事项及事件事实。</zh-CN>
        ///   <en>Ensures an administration work-item projection for a new item; work-item failure does not roll back item or event facts.</en>
        /// </lang>
        /// </summary>
        private void TryEnsureWorkItem(long itemId, string itemCode, string title, string summary, string ownerRoleKey, DateTime? dueUtc)
        {
            // <lang>
            //   <zh-CN>待办是后台入口投影，不是协同事项事实；待办创建失败只影响入口便利性，不回滚已写入的事项和事项事件。</zh-CN>
            //   <en>The work item is an administration-entry projection, not the collaboration-item fact; creation failures affect entry convenience only and do not roll back the item or its event.</en>
            // </lang>
            if (WorkItemDb == null || itemId <= 0)
            {
                return;
            }

            WorkItemDb.EnsureWorkItem(
                new PortalWorkItemCreateRequest
                {
                    BusinessKind = PortalWorkItemBusinessKinds.CollaborationItem,
                    BusinessId = itemId.ToString(CultureInfo.InvariantCulture),
                    Title = NormalizeInput(title, 200),
                    Summary = "Collaboration item " + itemCode + ": " + NormalizeInput(summary, 400),
                    AssignedRoleKey = NormalizeInput(ownerRoleKey, 120),
                    CreatedUtc = DateTime.UtcNow,
                    CreatedBy = GetCurrentUserName(),
                    DueUtc = dueUtc
                });
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按处理动作完成或取消当前待办投影；Start 只写事项事件，不修改待办。</zh-CN>
        ///   <en>Completes or cancels the current work-item projection by handling action; Start writes only the item event and does not mutate the work item.</en>
        /// </lang>
        /// </summary>
        private void TryCompleteWorkItem(long itemId, string actionKey, string actionComment)
        {
            // <lang>
            //   <zh-CN>`Start` 当前只写协同事项事件，不改变待办投影；终态和退回动作则关闭当前处理待办，避免列表留下陈旧入口。</zh-CN>
            //   <en>`Start` currently writes only a collaboration-item event and does not mutate the work-item projection; terminal and return actions close the current handling work item so the list does not keep a stale entry.</en>
            // </lang>
            if (WorkItemDb == null ||
                itemId <= 0 ||
                string.Equals(actionKey, PortalCollaborationItemActions.Start, StringComparison.Ordinal))
            {
                return;
            }

            WorkItemDb.CompleteBusinessWorkItem(
                new PortalWorkItemCompletionRequest
                {
                    BusinessKind = PortalWorkItemBusinessKinds.CollaborationItem,
                    BusinessId = itemId.ToString(CultureInfo.InvariantCulture),
                    EventType = MapWorkItemEventType(actionKey),
                    TargetStatus = MapWorkItemTargetStatus(actionKey),
                    ActorUserId = GetCurrentUserId(),
                    ActorName = GetCurrentUserName(),
                    Comment = NormalizeInput(actionComment, 1000),
                    OccurredUtc = DateTime.UtcNow
                });
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>重新读取重提事项并恢复后台待办投影。</zh-CN>
        ///   <en>Reloads a resubmitted item and restores its administration work-item projection.</en>
        /// </lang>
        /// </summary>
        private void TryEnsureResubmittedWorkItem(long itemId)
        {
            // <lang>
            //   <zh-CN>重提查询仅用于最小待办载荷，不把列表结果当作新的授权来源。</zh-CN>
            //   <en>The resubmission lookup supplies only the minimal work-item payload and is not treated as a new authorization source.</en>
            // </lang>
            if (CollaborationItemDb == null || itemId <= 0)
            {
                return;
            }

            CollaborationItemInfo item = CollaborationItemDb.GetAdminItems(string.Empty, 200)
                .FirstOrDefault(candidate => candidate.ItemId == itemId);
            if (item == null)
            {
                return;
            }

            TryEnsureWorkItem(item.ItemId, item.ItemCode, item.Title, item.Summary, item.OwnerRoleKey, item.DueUtc);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>写入参与者或管理员范围评论并记录不含正文的审计元数据。</zh-CN>
        ///   <en>Writes a participant- or administrator-scope comment and records audit metadata without the comment body.</en>
        /// </lang>
        /// </summary>
        private void TryAddComment(long itemId, string commandName, string comment)
        {
            // <lang>
            //   <zh-CN>可见范围由受支持的命令名映射，不接受请求方自定义范围键。</zh-CN>
            //   <en>Map visibility scope from supported command names and do not accept a caller-defined scope key.</en>
            // </lang>
            string visibilityScope = string.Equals(commandName, "AddAdministratorComment", StringComparison.Ordinal)
                ? PortalCollaborationItemVisibilityScopes.Administrators
                : PortalCollaborationItemVisibilityScopes.ItemParticipants;
            CollaborationItemCommentResult result = CollaborationItemDb.AddComment(
                new CollaborationItemCommentCreateRequest
                {
                    ItemId = itemId,
                    Comment = comment,
                    VisibilityScope = visibilityScope,
                    ActorUserId = GetCurrentUserId(),
                    ActorName = GetCurrentUserName(),
                    OccurredUtc = DateTime.UtcNow
                });
            if (!result.Succeeded)
            {
                MessageLabel.Text = result.Message;
                return;
            }

            // <lang>
            //   <zh-CN>评论审计只保留事项、事件、范围和长度，降低评论正文泄露风险。</zh-CN>
            //   <en>Comment audit retains only item, event, scope, and length metadata to reduce comment-body exposure.</en>
            // </lang>
            PortalOperationAudit.Record(
                PortalOperationAuditEvents.BusinessModuleCategory,
                PortalOperationAuditEvents.CollaborationItemCommentAdded,
                PortalOperationAuditEvents.CollaborationItemTargetType,
                result.ItemId.ToString(CultureInfo.InvariantCulture),
                "Collaboration item comment added. EventId=" + result.EventId.ToString(CultureInfo.InvariantCulture) +
                "; VisibilityScope=" + visibilityScope +
                "; Length=" + NormalizeInput(comment, 1000).Length.ToString(CultureInfo.InvariantCulture),
                Context);
            MessageLabel.Text = "Collaboration item comment added.";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将支持的事项处理动作映射为操作审计事件。</zh-CN>
        ///   <en>Maps supported item-handling actions to operation-audit events.</en>
        /// </lang>
        /// </summary>
        private void TryRecordOperationAudit(CollaborationItemResult result)
        {
            // <lang>
            //   <zh-CN>未知或未映射动作不写审计，避免制造无法解释的事件。</zh-CN>
            //   <en>Skip unknown or unmapped actions so the audit log does not contain unexplained events.</en>
            // </lang>
            string eventKey = MapAuditEvent(result.ActionKey);
            if (string.IsNullOrEmpty(eventKey))
            {
                return;
            }

            PortalOperationAudit.Record(
                PortalOperationAuditEvents.BusinessModuleCategory,
                eventKey,
                PortalOperationAuditEvents.CollaborationItemTargetType,
                result.ItemId.ToString(CultureInfo.InvariantCulture),
                "Collaboration item handled. ItemCode=" + result.ItemCode + "; ActionKey=" + result.ActionKey,
                Context);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>成功创建后清理后台创建表单并恢复受治理默认值。</zh-CN>
        ///   <en>Clears the administration create form after success and restores governed default values.</en>
        /// </lang>
        /// </summary>
        private void ClearCreateForm()
        {
            // <lang>
            //   <zh-CN>清空用户输入，处理角色恢复为固定权限键而不是沿用任意旧值。</zh-CN>
            //   <en>Clear user input and restore the handling role to a fixed permission key rather than retaining an arbitrary old value.</en>
            // </lang>
            TitleTextBox.Text = string.Empty;
            SummaryTextBox.Text = string.Empty;
            DescriptionTextBox.Text = string.Empty;
            DueUtcTextBox.Text = string.Empty;
            SelectReferenceValue(PriorityList, PortalReferenceDataSets.NormalPriority);
            SelectReferenceValue(ItemTypeList, PortalReferenceDataSets.GeneralItemType);
            OwnerRoleKeyTextBox.Text = PortalPermissionKeys.BusinessCollaborationHandle;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在目标参考值存在时安全地选中下拉项。</zh-CN>
        ///   <en>Safely selects a drop-down entry when the target reference value exists.</en>
        /// </lang>
        /// </summary>
        private static void SelectReferenceValue(DropDownList list, string valueKey)
        {
            if (list == null)
            {
                return;
            }

            ListItem item = list.Items.FindByValue(valueKey);
            if (item != null)
            {
                list.ClearSelection();
                item.Selected = true;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将协同事项动作映射为待办事件类型。</zh-CN>
        ///   <en>Maps a collaboration-item action to a work-item event type.</en>
        /// </lang>
        /// </summary>
        private static string MapWorkItemEventType(string actionKey)
        {
            // <lang>
            //   <zh-CN>拒绝、取消、关闭和退回保持既有待办事件语义，其余支持动作回退为完成事件。</zh-CN>
            //   <en>Preserve existing work-item semantics for reject, cancel, close, and return; supported actions otherwise fall back to completed.</en>
            // </lang>
            if (string.Equals(actionKey, PortalCollaborationItemActions.Reject, StringComparison.Ordinal))
            {
                return PortalWorkItemEventTypes.Rejected;
            }

            if (string.Equals(actionKey, PortalCollaborationItemActions.Cancel, StringComparison.Ordinal) ||
                string.Equals(actionKey, PortalCollaborationItemActions.Close, StringComparison.Ordinal))
            {
                return PortalWorkItemEventTypes.Cancelled;
            }

            if (string.Equals(actionKey, PortalCollaborationItemActions.Return, StringComparison.Ordinal))
            {
                return PortalWorkItemEventTypes.Commented;
            }

            return PortalWorkItemEventTypes.Completed;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将取消或关闭动作映射为待办取消状态，其余动作映射为完成状态。</zh-CN>
        ///   <en>Maps cancel or close actions to the cancelled work-item status and all other actions to completed.</en>
        /// </lang>
        /// </summary>
        private static string MapWorkItemTargetStatus(string actionKey)
        {
            return string.Equals(actionKey, PortalCollaborationItemActions.Cancel, StringComparison.Ordinal) ||
                   string.Equals(actionKey, PortalCollaborationItemActions.Close, StringComparison.Ordinal)
                ? PortalWorkItemStatuses.Cancelled
                : PortalWorkItemStatuses.Completed;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将固定协同事项动作键映射为固定操作审计事件键。</zh-CN>
        ///   <en>Maps fixed collaboration-item action keys to fixed operation-audit event keys.</en>
        /// </lang>
        /// </summary>
        private static string MapAuditEvent(string actionKey)
        {
            // <lang>
            //   <zh-CN>映射表只覆盖当前契约动作，未知键返回空字符串并由调用方跳过审计。</zh-CN>
            //   <en>The map covers only contract actions; unknown keys return an empty string for the caller to skip auditing.</en>
            // </lang>
            if (string.Equals(actionKey, PortalCollaborationItemActions.Start, StringComparison.Ordinal))
            {
                return PortalOperationAuditEvents.CollaborationItemStarted;
            }

            if (string.Equals(actionKey, PortalCollaborationItemActions.Complete, StringComparison.Ordinal))
            {
                return PortalOperationAuditEvents.CollaborationItemCompleted;
            }

            if (string.Equals(actionKey, PortalCollaborationItemActions.Return, StringComparison.Ordinal))
            {
                return PortalOperationAuditEvents.CollaborationItemReturned;
            }

            if (string.Equals(actionKey, PortalCollaborationItemActions.Resubmit, StringComparison.Ordinal))
            {
                return PortalOperationAuditEvents.CollaborationItemResubmitted;
            }

            if (string.Equals(actionKey, PortalCollaborationItemActions.Reject, StringComparison.Ordinal))
            {
                return PortalOperationAuditEvents.CollaborationItemRejected;
            }

            if (string.Equals(actionKey, PortalCollaborationItemActions.Cancel, StringComparison.Ordinal))
            {
                return PortalOperationAuditEvents.CollaborationItemCancelled;
            }

            if (string.Equals(actionKey, PortalCollaborationItemActions.Close, StringComparison.Ordinal))
            {
                return PortalOperationAuditEvents.CollaborationItemClosed;
            }

            return string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断列表动作是否属于协同事项状态动作白名单。</zh-CN>
        ///   <en>Determines whether a list action belongs to the collaboration-item state-action allowlist.</en>
        /// </lang>
        /// </summary>
        private static bool IsSupportedAction(string actionKey)
        {
            return string.Equals(actionKey, PortalCollaborationItemActions.Start, StringComparison.Ordinal) ||
                   string.Equals(actionKey, PortalCollaborationItemActions.Complete, StringComparison.Ordinal) ||
                   string.Equals(actionKey, PortalCollaborationItemActions.Return, StringComparison.Ordinal) ||
                   string.Equals(actionKey, PortalCollaborationItemActions.Resubmit, StringComparison.Ordinal) ||
                   string.Equals(actionKey, PortalCollaborationItemActions.Reject, StringComparison.Ordinal) ||
                   string.Equals(actionKey, PortalCollaborationItemActions.Cancel, StringComparison.Ordinal) ||
                   string.Equals(actionKey, PortalCollaborationItemActions.Close, StringComparison.Ordinal);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>判断列表命令是否属于参与者或管理员评论命令。</zh-CN>
        ///   <en>Determines whether a list command is a participant or administrator comment command.</en>
        /// </lang>
        /// </summary>
        private static bool IsCommentCommand(string commandName)
        {
            return string.Equals(commandName, "AddParticipantComment", StringComparison.Ordinal) ||
                   string.Equals(commandName, "AddAdministratorComment", StringComparison.Ordinal);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按允许格式解析 UTC 期限，空白输入表示未设置。</zh-CN>
        ///   <en>Parses a UTC due date using the allowed formats; blank input means unset.</en>
        /// </lang>
        /// </summary>
        private static bool TryParseDueUtc(string value, out DateTime? dueUtc)
        {
            // <lang>
            //   <zh-CN>先把输出初始化为 null，保证空值和失败路径不会复用旧解析结果。</zh-CN>
            //   <en>Initialize the output to null so blank and failure paths cannot reuse a previous parse result.</en>
            // </lang>
            dueUtc = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            DateTime parsed;
            if (!DateTime.TryParseExact(
                value.Trim(),
                new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out parsed))
            {
                return false;
            }

            dueUtc = parsed;
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>裁剪并限制后台输入长度，null 按空字符串处理。</zh-CN>
        ///   <en>Trims and limits administration input length, treating null as an empty string.</en>
        /// </lang>
        /// </summary>
        private static string NormalizeInput(string value, int maxLength)
        {
            // <lang>
            //   <zh-CN>该 helper 只做边界归一化，不承担权限、必填或持久化职责。</zh-CN>
            //   <en>This helper performs boundary normalization only; authorization, required-field checks, and persistence remain elsewhere.</en>
            // </lang>
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>企业协同事项后台展示行。</zh-CN>
    ///   <en>Administration display row for an enterprise collaboration item.</en>
    /// </lang>
    /// </summary>
    public sealed class CollaborationItemAdminRow
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>把协同事项和管理员可见事件转换为只读后台展示模型。</zh-CN>
        ///   <en>Converts a collaboration item and administrator-visible events into a read-only administration display model.</en>
        /// </lang>
        /// </summary>
        internal CollaborationItemAdminRow(CollaborationItemInfo item, IList<CollaborationItemEventInfo> visibleEvents)
        {
            // <lang>
            //   <zh-CN>展示行保留稳定主键和必要低敏字段，并将空值统一为占位文本。</zh-CN>
            //   <en>The display row keeps the stable key and required low-sensitivity fields while normalizing empty values to placeholders.</en>
            // </lang>
            ItemId = item.ItemId;
            ItemCode = item.ItemCode;
            ItemTypeKey = EmptyToNone(item.ItemTypeKey);
            Title = EmptyToNone(item.Title);
            Summary = EmptyToNone(item.Summary);
            Description = EmptyToNone(item.Description);
            ItemStatus = item.IsOverdue ? item.ItemStatus + " / Overdue" : item.ItemStatus;
            PriorityKey = EmptyToNone(item.PriorityKey);
            OwnerText = GetOwnerText(item);
            LastActionUtcText = item.LastActionUtc.HasValue
                ? item.LastActionUtc.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)
                : "(none)";
            LastActionComment = EmptyToNone(item.LastActionComment);
            // <lang>
            //   <zh-CN>最新评论按时间和事件号稳定选择，事件集合已由数据服务按管理员可见范围裁剪。</zh-CN>
            //   <en>Select the latest comment deterministically by time and event identifier; the data service already scopes events to administrator visibility.</en>
            // </lang>
            CollaborationItemEventInfo latestComment = (visibleEvents ?? new List<CollaborationItemEventInfo>())
                .Where(itemEvent => string.Equals(itemEvent.EventType, PortalCollaborationItemEventTypes.Comment, StringComparison.Ordinal))
                .OrderByDescending(itemEvent => itemEvent.OccurredUtc)
                .ThenByDescending(itemEvent => itemEvent.EventId)
                .FirstOrDefault();
            LatestVisibleComment = latestComment == null ? "(none)" : EmptyToNone(latestComment.Comment);
        }

        /// <summary><lang><zh-CN>协同事项主键。</zh-CN><en>Collaboration-item primary key.</en></lang></summary>
        public long ItemId { get; private set; }

        /// <summary><lang><zh-CN>协同事项编号。</zh-CN><en>Collaboration-item code.</en></lang></summary>
        public string ItemCode { get; private set; }

        /// <summary><lang><zh-CN>事项类型键。</zh-CN><en>Item type key.</en></lang></summary>
        public string ItemTypeKey { get; private set; }

        /// <summary><lang><zh-CN>事项标题。</zh-CN><en>Item title.</en></lang></summary>
        public string Title { get; private set; }

        /// <summary><lang><zh-CN>低敏摘要。</zh-CN><en>Low-sensitivity summary.</en></lang></summary>
        public string Summary { get; private set; }

        /// <summary><lang><zh-CN>事项说明。</zh-CN><en>Item description.</en></lang></summary>
        public string Description { get; private set; }

        /// <summary><lang><zh-CN>事项状态。</zh-CN><en>Item status.</en></lang></summary>
        public string ItemStatus { get; private set; }

        /// <summary><lang><zh-CN>优先级键。</zh-CN><en>Priority key.</en></lang></summary>
        public string PriorityKey { get; private set; }

        /// <summary><lang><zh-CN>负责人展示文本。</zh-CN><en>Owner display text.</en></lang></summary>
        public string OwnerText { get; private set; }

        /// <summary><lang><zh-CN>最近动作 UTC 展示文本。</zh-CN><en>Latest action UTC display text.</en></lang></summary>
        public string LastActionUtcText { get; private set; }

        /// <summary><lang><zh-CN>最近办理意见。</zh-CN><en>Latest handling comment.</en></lang></summary>
        public string LastActionComment { get; private set; }

        /// <summary><lang><zh-CN>当前管理员可见的最新评论。</zh-CN><en>Latest comment visible to the current administrator.</en></lang></summary>
        public string LatestVisibleComment { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将负责人用户或角色信息转换为后台展示文本。</zh-CN>
        ///   <en>Converts owner user or role information into administration display text.</en>
        /// </lang>
        /// </summary>
        private static string GetOwnerText(CollaborationItemInfo item)
        {
            // <lang>
            //   <zh-CN>优先显示负责人用户标识和名称，否则回退到负责人角色键。</zh-CN>
            //   <en>Prefer the owner user identifier and name, falling back to the owner role key otherwise.</en>
            // </lang>
            if (item.OwnerUserId.HasValue)
            {
                return item.OwnerUserId.Value.ToString(CultureInfo.InvariantCulture) + " / " + EmptyToNone(item.OwnerUserName);
            }

            return EmptyToNone(item.OwnerRoleKey);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把空白后台展示字段转换为统一占位文本。</zh-CN>
        ///   <en>Converts blank administration display fields to a consistent placeholder.</en>
        /// </lang>
        /// </summary>
        private static string EmptyToNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }
    }
}
