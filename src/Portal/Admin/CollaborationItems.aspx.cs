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
            if (!EnsureCanCreateItems())
            {
                return;
            }

            DateTime? dueUtc;
            if (!TryParseDueUtc(DueUtcTextBox.Text, out dueUtc))
            {
                MessageLabel.Text = "Due UTC must be empty or use yyyy-MM-dd HH:mm:ss.";
                return;
            }

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
            string actionKey = Convert.ToString(e.CommandName, CultureInfo.InvariantCulture);
            long itemId;
            if (!long.TryParse(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture), out itemId) || itemId <= 0)
            {
                MessageLabel.Text = "Invalid collaboration item id.";
                return;
            }

            TextBox commentBox = e.Item.FindControl("ActionCommentTextBox") as TextBox;
            string actionComment = commentBox == null ? string.Empty : commentBox.Text;
            if (IsCommentCommand(actionKey))
            {
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

        private void BindStatusFilter()
        {
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

        private void BindReferenceDataLists()
        {
            BindReferenceDataList(ItemTypeList, PortalReferenceDataSets.CollaborationItemType);
            BindReferenceDataList(PriorityList, PortalReferenceDataSets.CollaborationPriority);
        }

        private void BindReferenceDataList(DropDownList list, string referenceSetKey)
        {
            list.Items.Clear();
            IList<ReferenceDataItem> items;
            if (ReferenceDataDb == null || !ReferenceDataDb.TryGetActiveItems(referenceSetKey, out items))
            {
                items = PortalReferenceDataSets.GetFallbackItems(referenceSetKey);
            }

            foreach (ReferenceDataItem item in items)
            {
                list.Items.Add(new ListItem(item.DisplayName, item.ValueKey));
            }
        }

        private void BindItems()
        {
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

        private void ShowUnavailable(string message)
        {
            MessageLabel.Text = message ?? string.Empty;
            ResultLabel.Text = string.Empty;
            ItemsRepeater.DataSource = Enumerable.Empty<CollaborationItemAdminRow>();
            ItemsRepeater.DataBind();
        }

        private bool EnsureCanViewItems()
        {
            return PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.BusinessCollaborationViewAll,
                PortalPermissionKeys.BusinessCollaborationAdmin);
        }

        private bool EnsureCanCreateItems()
        {
            return PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.BusinessCollaborationCreate,
                PortalPermissionKeys.BusinessCollaborationAdmin);
        }

        private bool EnsureCanHandleItems()
        {
            return PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.BusinessCollaborationHandle,
                PortalPermissionKeys.BusinessCollaborationAdmin);
        }

        private int GetCurrentUserId()
        {
            string userName = GetCurrentUserName();
            if (string.IsNullOrWhiteSpace(userName) || UsersDb == null)
            {
                return 0;
            }

            IUserItem user = UsersDb.GetSingleUser(userName);
            return user == null ? 0 : user.UserId;
        }

        private string GetCurrentUserName()
        {
            return Context != null &&
                   Context.User != null &&
                   Context.User.Identity != null &&
                   Context.User.Identity.IsAuthenticated
                ? Context.User.Identity.Name
                : "system";
        }

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

        private void TryEnsureResubmittedWorkItem(long itemId)
        {
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

        private void TryAddComment(long itemId, string commandName, string comment)
        {
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

        private void TryRecordOperationAudit(CollaborationItemResult result)
        {
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

        private void ClearCreateForm()
        {
            TitleTextBox.Text = string.Empty;
            SummaryTextBox.Text = string.Empty;
            DescriptionTextBox.Text = string.Empty;
            DueUtcTextBox.Text = string.Empty;
            SelectReferenceValue(PriorityList, PortalReferenceDataSets.NormalPriority);
            SelectReferenceValue(ItemTypeList, PortalReferenceDataSets.GeneralItemType);
            OwnerRoleKeyTextBox.Text = PortalPermissionKeys.BusinessCollaborationHandle;
        }

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

        private static string MapWorkItemEventType(string actionKey)
        {
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

        private static string MapWorkItemTargetStatus(string actionKey)
        {
            return string.Equals(actionKey, PortalCollaborationItemActions.Cancel, StringComparison.Ordinal) ||
                   string.Equals(actionKey, PortalCollaborationItemActions.Close, StringComparison.Ordinal)
                ? PortalWorkItemStatuses.Cancelled
                : PortalWorkItemStatuses.Completed;
        }

        private static string MapAuditEvent(string actionKey)
        {
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

        private static bool IsCommentCommand(string commandName)
        {
            return string.Equals(commandName, "AddParticipantComment", StringComparison.Ordinal) ||
                   string.Equals(commandName, "AddAdministratorComment", StringComparison.Ordinal);
        }

        private static bool TryParseDueUtc(string value, out DateTime? dueUtc)
        {
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

        private static string NormalizeInput(string value, int maxLength)
        {
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
        internal CollaborationItemAdminRow(CollaborationItemInfo item, IList<CollaborationItemEventInfo> visibleEvents)
        {
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

        private static string GetOwnerText(CollaborationItemInfo item)
        {
            if (item.OwnerUserId.HasValue)
            {
                return item.OwnerUserId.Value.ToString(CultureInfo.InvariantCulture) + " / " + EmptyToNone(item.OwnerUserName);
            }

            return EmptyToNone(item.OwnerRoleKey);
        }

        private static string EmptyToNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }
    }
}
