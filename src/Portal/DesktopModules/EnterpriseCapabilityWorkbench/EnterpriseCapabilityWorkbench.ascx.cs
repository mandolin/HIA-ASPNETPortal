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
    ///   <zh-CN>企业能力工作台前台模块。</zh-CN>
    ///   <en>Front-end module for the enterprise-capability workbench.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>
    ///     P22.4 首版只复用 P21 企业协同事项、待办和运营审计链路，提供普通用户提交与本人查看入口。
    ///     它不引入动态表单、附件、在线脚本或领域专有字段，以便先验证 Profile gate 下的最小前后台闭环。
    ///   </zh-CN>
    ///   <en>
    ///     The first P22.4 version reuses the P21 collaboration-item, work-item, and operation-audit loop only,
    ///     giving ordinary users a submit-and-own-list entry. It does not introduce dynamic forms, attachments,
    ///     online scripts, or domain-specific fields, so the first proof can focus on the minimum front/back loop
    ///     under the Profile gate.
    ///   </en>
    /// </lang>
    /// </remarks>
    public partial class EnterpriseCapabilityWorkbench : PortalModuleControl<EnterpriseCapabilityWorkbench>
    {
        private const int RecentItemLimit = 10;

        /// <summary>
        /// <lang>
        ///   <zh-CN>用户数据访问服务，用于把当前登录名解析为门户用户标识。</zh-CN>
        ///   <en>User data service used to resolve the current sign-in name to a Portal user identifier.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IUsersDb UsersDb { private get; set; }

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
        ///   <zh-CN>受治理参考数据目录读取服务，用于填充类型和优先级。</zh-CN>
        ///   <en>Governed reference-data catalog reader used to populate types and priorities.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IReferenceDataDb ReferenceDataDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>轻量待办数据服务，用于把用户提交投影到后台处理入口。</zh-CN>
        ///   <en>Lightweight work-item data service used to project user submissions into the administration handling entry.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IPortalWorkItemDb WorkItemDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化工作台并绑定固定选项和本人最近事项。</zh-CN>
        ///   <en>Initializes the workbench and binds fixed options plus recent own items.</en>
        /// </lang>
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>仅在首次请求绑定固定参考数据和本人列表，避免回发时覆盖用户输入或控件选择。</zh-CN>
            //   <en>Bind fixed reference data and the own-item list only on the first request so postbacks do not overwrite user input or selections.</en>
            // </lang>
            if (!IsPostBack)
            {
                BindItemTypeList();
                BindPriorityList();
                BindModule();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交当前用户填写的企业协同事项。</zh-CN>
        ///   <en>Submits the enterprise collaboration item entered by the current user.</en>
        /// </lang>
        /// </summary>
        protected void SubmitButton_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>先解析当前身份，再执行权限、期限、内容和数据服务门禁；任何门禁失败都只显示提示并保持页面闭环。</zh-CN>
            //   <en>Resolve the current identity before permission, due-date, content, and data-service gates; every failed gate stays within the page flow and only reports a message.</en>
            // </lang>
            int userId = GetCurrentUserId();
            if (userId <= 0)
            {
                ShowMessage("请先登录后再提交协同事项。");
                BindModule();
                return;
            }

            if (!PortalAuthorization.HasAnyPermission(
                PortalPermissionKeys.BusinessCollaborationCreate,
                PortalPermissionKeys.BusinessCollaborationAdmin))
            {
                ShowMessage("当前账号没有提交企业协同事项的权限。");
                BindModule();
                return;
            }

            // <lang>
            //   <zh-CN>期限按 UTC 解析；空值代表未设置，格式错误不会进入持久化。</zh-CN>
            //   <en>Parse the due date as UTC; an empty value means unset, while an invalid format never reaches persistence.</en>
            // </lang>
            DateTime? dueUtc;
            if (!TryParseDueUtc(DueUtcTextBox.Text, out dueUtc))
            {
                ShowMessage("期限 UTC 必须为空，或使用 yyyy-MM-dd / yyyy-MM-dd HH:mm:ss。");
                return;
            }

            // <lang>
            //   <zh-CN>在服务调用前统一裁剪文本，保持 UI 输入与领域字段长度契约一致。</zh-CN>
            //   <en>Normalize text before the service call so UI input remains within the domain field-length contract.</en>
            // </lang>
            string title = NormalizeInput(TitleTextBox.Text, 200);
            string summary = NormalizeInput(SummaryTextBox.Text, 500);
            string description = NormalizeInput(DescriptionTextBox.Text, 4000);
            if (string.IsNullOrWhiteSpace(title))
            {
                ShowMessage("请填写事项标题。");
                return;
            }

            if (string.IsNullOrWhiteSpace(summary) && string.IsNullOrWhiteSpace(description))
            {
                ShowMessage("请填写摘要或事项说明。");
                return;
            }

            // <lang>
            //   <zh-CN>协同事项服务写入提交事实和状态；提交人和处理角色采用当前上下文的稳定身份键。</zh-CN>
            //   <en>The collaboration-item service writes the submission fact and state; submitter and handling role use stable identity keys from the current context.</en>
            // </lang>
            CollaborationItemResult result = CollaborationItemDb.CreateSubmittedItem(
                new CollaborationItemCreateRequest
                {
                    ItemTypeKey = ItemTypeList.SelectedValue,
                    Title = title,
                    Summary = summary,
                    Description = description,
                    InitiatorUserId = userId,
                    OwnerRoleKey = PortalPermissionKeys.BusinessCollaborationHandle,
                    PriorityKey = PriorityList.SelectedValue,
                    DueUtc = dueUtc,
                    SubmittedUtc = DateTime.UtcNow,
                    SubmittedBy = GetCurrentUserName()
                });

            if (!result.Succeeded)
            {
                ShowMessage(result.Message);
                BindModule();
                return;
            }

            // <lang>
            //   <zh-CN>领域事实成功后记录操作审计；后续待办投影失败不回滚已提交事项。</zh-CN>
            //   <en>Record operation audit after the domain fact succeeds; a later work-item projection failure must not roll back the submitted item.</en>
            // </lang>
            PortalOperationAudit.Record(
                PortalOperationAuditEvents.BusinessModuleCategory,
                PortalOperationAuditEvents.CollaborationItemSubmitted,
                PortalOperationAuditEvents.CollaborationItemTargetType,
                result.ItemId.ToString(CultureInfo.InvariantCulture),
                "Collaboration item submitted from EnterpriseCapabilityWorkbench. ItemCode=" + result.ItemCode,
                Context);

            TryEnsureWorkItem(result.ItemId, result.ItemCode, title, summary, dueUtc);
            ClearSubmitForm();
            ShowMessage("企业协同事项已提交，编号：" + result.ItemCode);
            BindModule();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理本人事项列表中的评论和退回后重新提交请求。</zh-CN>
        ///   <en>Handles comment and post-return resubmission requests from the own-item list.</en>
        /// </lang>
        /// </summary>
        protected void RecentItemsRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            // <lang>
            //   <zh-CN>列表命令重新解析当前身份并校验命令参数，防止匿名或无效事项标识进入动作服务。</zh-CN>
            //   <en>Re-resolve identity and validate the command argument before list actions so anonymous or invalid item identifiers never reach the action service.</en>
            // </lang>
            int userId = GetCurrentUserId();
            if (userId <= 0 || !IsCurrentUserAuthenticated())
            {
                ShowMessage("请先登录后再操作协同事项。");
                BindModule();
                return;
            }

            long itemId;
            if (!long.TryParse(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture), out itemId) || itemId <= 0)
            {
                ShowMessage("协同事项标识无效。");
                BindModule();
                return;
            }

            // <lang>
            //   <zh-CN>仅允许显式支持的评论和重提动作；未知动作走统一提示并重新绑定列表。</zh-CN>
            //   <en>Only explicit comment and resubmission actions are supported; unknown actions use the common message and rebind the list.</en>
            // </lang>
            string commandName = Convert.ToString(e.CommandName, CultureInfo.InvariantCulture);
            if (string.Equals(commandName, "AddParticipantComment", StringComparison.Ordinal))
            {
                // <lang>
                //   <zh-CN>评论限定为事项参与者可见范围，并保留服务层返回的失败信息。</zh-CN>
                //   <en>Comments are written to the item-participant visibility scope while preserving the service failure message.</en>
                // </lang>
                TextBox commentBox = e.Item.FindControl("ParticipantCommentTextBox") as TextBox;
                string comment = commentBox == null ? string.Empty : commentBox.Text;
                CollaborationItemCommentResult commentResult = CollaborationItemDb.AddComment(
                    new CollaborationItemCommentCreateRequest
                    {
                        ItemId = itemId,
                        Comment = comment,
                        VisibilityScope = PortalCollaborationItemVisibilityScopes.ItemParticipants,
                        ActorUserId = userId,
                        ActorName = GetCurrentUserName(),
                        OccurredUtc = DateTime.UtcNow
                    });
                if (!commentResult.Succeeded)
                {
                    ShowMessage(commentResult.Message);
                    BindModule();
                    return;
                }

                RecordCommentAudit(commentResult, PortalCollaborationItemVisibilityScopes.ItemParticipants, comment);
                ShowMessage("已添加参与者范围评论。");
                BindModule();
                return;
            }

            if (string.Equals(commandName, PortalCollaborationItemActions.Resubmit, StringComparison.Ordinal))
            {
                // <lang>
                //   <zh-CN>重提动作成功后重新读取本人事项，再补建后台待办并记录动作审计。</zh-CN>
                //   <en>After resubmission succeeds, reload the user's item, recreate the handling work item, and record the action audit.</en>
                // </lang>
                CollaborationItemResult result = CollaborationItemDb.ApplyAction(
                    new CollaborationItemActionRequest
                    {
                        ItemId = itemId,
                        ActionKey = PortalCollaborationItemActions.Resubmit,
                        ActorUserId = userId,
                        ActorName = GetCurrentUserName(),
                        OccurredUtc = DateTime.UtcNow
                    });
                if (!result.Succeeded)
                {
                    ShowMessage(result.Message);
                    BindModule();
                    return;
                }

                CollaborationItemInfo item = CollaborationItemDb.GetRecentItemsForUser(userId, RecentItemLimit)
                    .FirstOrDefault(candidate => candidate.ItemId == result.ItemId);
                if (item != null)
                {
                    TryEnsureWorkItem(item.ItemId, item.ItemCode, item.Title, item.Summary, item.DueUtc);
                }

                RecordActionAudit(result);
                ShowMessage("协同事项已重新提交。");
                BindModule();
                return;
            }

            ShowMessage("不支持的协同事项操作。");
            BindModule();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按身份、Schema 和权限门禁控制工作台可见性，并绑定当前用户最近事项。</zh-CN>
        ///   <en>Applies identity, schema, and permission gates to workbench visibility and binds the current user's recent items.</en>
        /// </lang>
        /// </summary>
        private void BindModule()
        {
            // <lang>
            //   <zh-CN>模块绑定前先做认证和数据库能力检查，失败时隐藏工作区并绑定空列表。</zh-CN>
            //   <en>Check authentication and database capability before binding; failures hide the work area and bind an empty list.</en>
            // </lang>
            int userId = GetCurrentUserId();
            if (!IsCurrentUserAuthenticated())
            {
                WorkbenchPanel.Visible = false;
                BindRecentItems(0);
                ShowMessage("请先登录后再使用企业能力工作台。");
                return;
            }

            if (CollaborationItemDb == null || !CollaborationItemDb.IsSchemaAvailable())
            {
                WorkbenchPanel.Visible = false;
                BindRecentItems(0);
                ShowMessage("企业协同事项模块尚未完成数据库初始化。");
                return;
            }

            // <lang>
            //   <zh-CN>创建、查看本人和管理员权限共同决定前台是否可用；此处不推断角色继承关系。</zh-CN>
            //   <en>Create, own-view, and administrator permissions jointly determine front-end availability; no role inheritance is inferred here.</en>
            // </lang>
            bool canUseWorkbench = PortalAuthorization.HasAnyPermission(
                PortalPermissionKeys.BusinessCollaborationCreate,
                PortalPermissionKeys.BusinessCollaborationViewOwn,
                PortalPermissionKeys.BusinessCollaborationAdmin);
            WorkbenchPanel.Visible = canUseWorkbench;
            if (!canUseWorkbench)
            {
                BindRecentItems(0);
                ShowMessage("当前账号没有使用企业能力工作台的权限。");
                return;
            }

            BindRecentItems(userId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定协同事项类型参考数据。</zh-CN>
        ///   <en>Binds collaboration-item type reference data.</en>
        /// </lang>
        /// </summary>
        private void BindItemTypeList()
        {
            BindReferenceDataList(ItemTypeList, PortalReferenceDataSets.CollaborationItemType);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>绑定协同事项优先级参考数据。</zh-CN>
        ///   <en>Binds collaboration-item priority reference data.</en>
        /// </lang>
        /// </summary>
        private void BindPriorityList()
        {
            BindReferenceDataList(PriorityList, PortalReferenceDataSets.CollaborationPriority);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>优先读取活动参考数据，读取不可用时使用受治理的固定回退项。</zh-CN>
        ///   <en>Prefers active reference data and uses governed fixed fallback items when the reader is unavailable.</en>
        /// </lang>
        /// </summary>
        private void BindReferenceDataList(DropDownList list, string referenceSetKey)
        {
            // <lang>
            //   <zh-CN>每次绑定先清空旧项，避免回发或服务切换造成重复选项。</zh-CN>
            //   <en>Clear old entries before each bind so postbacks or service switches cannot create duplicate options.</en>
            // </lang>
            list.Items.Clear();
            IList<ReferenceDataItem> items;
            if (ReferenceDataDb == null || !ReferenceDataDb.TryGetActiveItems(referenceSetKey, out items))
            {
                items = PortalReferenceDataSets.GetFallbackItems(referenceSetKey);
            }

            // <lang>
            //   <zh-CN>下拉框保存稳定值键，显示名称只来自参考数据目录。</zh-CN>
            //   <en>Store stable value keys in the drop-down while taking display names only from the reference catalog.</en>
            // </lang>
            foreach (ReferenceDataItem item in items)
            {
                list.Items.Add(new ListItem(item.DisplayName, item.ValueKey));
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前用户最近事项及其可见事件并转换为展示行。</zh-CN>
        ///   <en>Loads the current user's recent items and visible events and converts them to display rows.</en>
        /// </lang>
        /// </summary>
        private void BindRecentItems(int userId)
        {
            // <lang>
            //   <zh-CN>未认证或服务不可用时使用空集合，保持模板绑定安全且不暴露跨用户数据。</zh-CN>
            //   <en>Use an empty collection when unauthenticated or unavailable so template binding stays safe and cannot expose another user's data.</en>
            // </lang>
            IList<CollaborationItemInfo> items = CollaborationItemDb == null || userId <= 0
                ? new List<CollaborationItemInfo>()
                : CollaborationItemDb.GetRecentItemsForUser(userId, RecentItemLimit);
            // <lang>
            //   <zh-CN>可见事件由数据服务按当前用户裁剪，展示行只做排序和低敏文本转换。</zh-CN>
            //   <en>The data service scopes visible events to the current user; the display row only sorts and converts low-sensitivity text.</en>
            // </lang>
            RecentItemsRepeater.DataSource = items.Select(item => new EnterpriseCapabilityWorkbenchItemRow(
                item,
                CollaborationItemDb.GetVisibleEvents(item.ItemId, userId))).ToList();
            RecentItemsRepeater.DataBind();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为成功的协同事项重提动作写入操作审计。</zh-CN>
        ///   <en>Records operation audit for a successful collaboration-item resubmission.</en>
        /// </lang>
        /// </summary>
        private void RecordActionAudit(CollaborationItemResult result)
        {
            // <lang>
            //   <zh-CN>只审计成功且动作键明确为重提的结果，避免重复或误报。</zh-CN>
            //   <en>Audit only successful results whose action key is explicitly resubmission to avoid duplicates or false reports.</en>
            // </lang>
            if (result == null || !result.Succeeded ||
                !string.Equals(result.ActionKey, PortalCollaborationItemActions.Resubmit, StringComparison.Ordinal))
            {
                return;
            }

            PortalOperationAudit.Record(
                PortalOperationAuditEvents.BusinessModuleCategory,
                PortalOperationAuditEvents.CollaborationItemResubmitted,
                PortalOperationAuditEvents.CollaborationItemTargetType,
                result.ItemId.ToString(CultureInfo.InvariantCulture),
                "Collaboration item resubmitted from EnterpriseCapabilityWorkbench. ItemCode=" + result.ItemCode,
                Context);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>记录评论成功事件的审计元数据，不写入评论正文。</zh-CN>
        ///   <en>Records audit metadata for a successful comment event without writing the comment body.</en>
        /// </lang>
        /// </summary>
        private void RecordCommentAudit(CollaborationItemCommentResult result, string visibilityScope, string comment)
        {
            // <lang>
            //   <zh-CN>审计仅保留可追踪的事项、事件、可见范围和长度，降低正文泄露风险。</zh-CN>
            //   <en>Audit retains only traceable item, event, scope, and length metadata to reduce comment-body exposure.</en>
            // </lang>
            if (result == null || !result.Succeeded)
            {
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
            //   <zh-CN>用户标识只通过用户服务解析，不从页面参数或控件值推断。</zh-CN>
            //   <en>Resolve the user identifier only through the user service; never infer it from page parameters or control values.</en>
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
        ///   <zh-CN>判断当前 HTTP 上下文是否存在已认证身份。</zh-CN>
        ///   <en>Determines whether the current HTTP context contains an authenticated identity.</en>
        /// </lang>
        /// </summary>
        private bool IsCurrentUserAuthenticated()
        {
            return Context != null &&
                   Context.User != null &&
                   Context.User.Identity != null &&
                   Context.User.Identity.IsAuthenticated;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取当前认证用户名；未认证时返回空字符串。</zh-CN>
        ///   <en>Reads the current authenticated name and returns an empty string when unauthenticated.</en>
        /// </lang>
        /// </summary>
        private string GetCurrentUserName()
        {
            return IsCurrentUserAuthenticated() ? Context.User.Identity.Name : string.Empty;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为已提交或重提事项补建后台待办投影；待办写入失败不回滚领域事实。</zh-CN>
        ///   <en>Ensures an administration work-item projection for a submitted or resubmitted item; work-item failure does not roll back the domain fact.</en>
        /// </lang>
        /// </summary>
        private void TryEnsureWorkItem(long itemId, string itemCode, string title, string summary, DateTime? dueUtc)
        {
            // <lang>
            //   <zh-CN>待办只是后台处理入口投影；提交事实和事件已经写入后，待办失败不应回滚用户提交。</zh-CN>
            //   <en>The work item is only an administration handling-entry projection; after the submission fact and event are written, work-item failure must not roll back the user's submission.</en>
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
                    Title = "Collaboration item " + itemCode + ": " + NormalizeInput(title, 160),
                    Summary = NormalizeInput(summary, 500),
                    AssignedRoleKey = PortalPermissionKeys.BusinessCollaborationHandle,
                    CreatedUtc = DateTime.UtcNow,
                    CreatedBy = GetCurrentUserName(),
                    DueUtc = dueUtc
                });
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>清理提交控件并恢复受治理的默认参考值。</zh-CN>
        ///   <en>Clears submission controls and restores governed default reference values.</en>
        /// </lang>
        /// </summary>
        private void ClearSubmitForm()
        {
            // <lang>
            //   <zh-CN>成功提交后清空用户输入，默认值仅通过稳定参考数据键选择。</zh-CN>
            //   <en>Clear user input after a successful submission and select defaults only by stable reference-data keys.</en>
            // </lang>
            TitleTextBox.Text = string.Empty;
            SummaryTextBox.Text = string.Empty;
            DescriptionTextBox.Text = string.Empty;
            DueUtcTextBox.Text = string.Empty;
            SelectReferenceValue(ItemTypeList, PortalReferenceDataSets.GeneralItemType);
            SelectReferenceValue(PriorityList, PortalReferenceDataSets.NormalPriority);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在下拉列表存在目标值时安全地选中该参考数据项。</zh-CN>
        ///   <en>Safely selects a reference-data entry when the target value exists in the drop-down.</en>
        /// </lang>
        /// </summary>
        private static void SelectReferenceValue(DropDownList list, string valueKey)
        {
            // <lang>
            //   <zh-CN>列表或目标值缺失时保持当前状态，不人为插入不存在的选项。</zh-CN>
            //   <en>Leave the current state unchanged when the list or target value is missing; do not invent an absent option.</en>
            // </lang>
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
        ///   <zh-CN>将页面提示进行 HTML 编码后写入消息标签。</zh-CN>
        ///   <en>HTML-encodes a page message before writing it to the message label.</en>
        /// </lang>
        /// </summary>
        private void ShowMessage(string message)
        {
            // <lang>
            //   <zh-CN>提示内容按不可信文本处理，避免服务返回信息直接形成标记。</zh-CN>
            //   <en>Treat message content as untrusted text so service-returned text cannot become markup.</en>
            // </lang>
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按允许的 UTC 日期格式解析期限，空白输入表示未设置。</zh-CN>
        ///   <en>Parses a due date using the allowed UTC formats; blank input means unset.</en>
        /// </lang>
        /// </summary>
        private static bool TryParseDueUtc(string value, out DateTime? dueUtc)
        {
            // <lang>
            //   <zh-CN>先初始化输出为 null，保证失败和空值路径都不会泄漏上次解析结果。</zh-CN>
            //   <en>Initialize the output to null so failed and blank paths cannot leak a previous parse result.</en>
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
        ///   <zh-CN>裁剪并限制用户输入长度，null 按空字符串处理。</zh-CN>
        ///   <en>Trims and limits user input length, treating null as an empty string.</en>
        /// </lang>
        /// </summary>
        private static string NormalizeInput(string value, int maxLength)
        {
            // <lang>
            //   <zh-CN>该 helper 只做边界归一化，不承担业务必填校验或持久化。</zh-CN>
            //   <en>This helper performs boundary normalization only; required-field validation and persistence remain elsewhere.</en>
            // </lang>
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>企业能力工作台本人事项展示行。</zh-CN>
    ///   <en>Own-item display row for the enterprise-capability workbench.</en>
    /// </lang>
    /// </summary>
    public sealed class EnterpriseCapabilityWorkbenchItemRow
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>把领域事项和当前用户可见事件转换为只读展示模型。</zh-CN>
        ///   <en>Converts a domain item and events visible to the current user into a read-only display model.</en>
        /// </lang>
        /// </summary>
        internal EnterpriseCapabilityWorkbenchItemRow(CollaborationItemInfo item, IList<CollaborationItemEventInfo> visibleEvents)
        {
            // <lang>
            //   <zh-CN>展示模型保留稳定主键和低敏字段，空值统一回退为占位文本。</zh-CN>
            //   <en>The display model keeps the stable key and low-sensitivity fields while normalizing empty values to placeholders.</en>
            // </lang>
            ItemId = item.ItemId;
            ItemCode = EmptyToNone(item.ItemCode);
            Title = EmptyToNone(item.Title);
            ItemStatus = EmptyToNone(item.ItemStatus);
            StatusText = item.IsOverdue ? ItemStatus + " / Overdue" : ItemStatus;
            PriorityKey = EmptyToNone(item.PriorityKey);
            LastActionUtcText = item.LastActionUtc.HasValue
                ? item.LastActionUtc.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)
                : "(none)";
            LastActionComment = EmptyToNone(item.LastActionComment);
            // <lang>
            //   <zh-CN>最新评论只从当前用户可见且属于参与者范围的评论事件中按时间和事件号稳定选取。</zh-CN>
            //   <en>Select the latest comment deterministically from visible participant-scope events by time and event identifier.</en>
            // </lang>
            CollaborationItemEventInfo latestComment = (visibleEvents ?? new List<CollaborationItemEventInfo>())
                .Where(itemEvent => string.Equals(itemEvent.EventType, PortalCollaborationItemEventTypes.Comment, StringComparison.Ordinal) &&
                                    string.Equals(itemEvent.VisibilityScope, PortalCollaborationItemVisibilityScopes.ItemParticipants, StringComparison.Ordinal))
                .OrderByDescending(itemEvent => itemEvent.OccurredUtc)
                .ThenByDescending(itemEvent => itemEvent.EventId)
                .FirstOrDefault();
            LatestParticipantComment = latestComment == null ? "(none)" : EmptyToNone(latestComment.Comment);
        }

        /// <summary><lang><zh-CN>协同事项主键。</zh-CN><en>Collaboration-item primary key.</en></lang></summary>
        public long ItemId { get; private set; }

        /// <summary><lang><zh-CN>事项编号。</zh-CN><en>Item code.</en></lang></summary>
        public string ItemCode { get; private set; }

        /// <summary><lang><zh-CN>事项标题。</zh-CN><en>Item title.</en></lang></summary>
        public string Title { get; private set; }

        /// <summary><lang><zh-CN>事项状态。</zh-CN><en>Item status.</en></lang></summary>
        public string ItemStatus { get; private set; }

        /// <summary><lang><zh-CN>包含只读超期标记的事项状态。</zh-CN><en>Item status including the read-only overdue indicator.</en></lang></summary>
        public string StatusText { get; private set; }

        /// <summary><lang><zh-CN>优先级键。</zh-CN><en>Priority key.</en></lang></summary>
        public string PriorityKey { get; private set; }

        /// <summary><lang><zh-CN>最近动作 UTC 展示文本。</zh-CN><en>Latest action UTC display text.</en></lang></summary>
        public string LastActionUtcText { get; private set; }

        /// <summary><lang><zh-CN>最近办理意见。</zh-CN><en>Latest handling comment.</en></lang></summary>
        public string LastActionComment { get; private set; }

        /// <summary><lang><zh-CN>当前用户可见的最新评论。</zh-CN><en>Latest comment visible to the current user.</en></lang></summary>
        public string LatestParticipantComment { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将空白展示字段转换为统一的占位文本。</zh-CN>
        ///   <en>Converts blank display fields to a consistent placeholder.</en>
        /// </lang>
        /// </summary>
        private static string EmptyToNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }
    }
}
