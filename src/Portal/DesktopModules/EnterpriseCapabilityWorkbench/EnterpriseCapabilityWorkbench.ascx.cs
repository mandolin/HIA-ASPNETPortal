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

            DateTime? dueUtc;
            if (!TryParseDueUtc(DueUtcTextBox.Text, out dueUtc))
            {
                ShowMessage("期限 UTC 必须为空，或使用 yyyy-MM-dd / yyyy-MM-dd HH:mm:ss。");
                return;
            }

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

        private void BindModule()
        {
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

        private void BindItemTypeList()
        {
            BindReferenceDataList(ItemTypeList, PortalReferenceDataSets.CollaborationItemType);
        }

        private void BindPriorityList()
        {
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

        private void BindRecentItems(int userId)
        {
            IList<CollaborationItemInfo> items = CollaborationItemDb == null || userId <= 0
                ? new List<CollaborationItemInfo>()
                : CollaborationItemDb.GetRecentItemsForUser(userId, RecentItemLimit);
            RecentItemsRepeater.DataSource = items.Select(item => new EnterpriseCapabilityWorkbenchItemRow(item)).ToList();
            RecentItemsRepeater.DataBind();
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

        private bool IsCurrentUserAuthenticated()
        {
            return Context != null &&
                   Context.User != null &&
                   Context.User.Identity != null &&
                   Context.User.Identity.IsAuthenticated;
        }

        private string GetCurrentUserName()
        {
            return IsCurrentUserAuthenticated() ? Context.User.Identity.Name : string.Empty;
        }

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

        private void ClearSubmitForm()
        {
            TitleTextBox.Text = string.Empty;
            SummaryTextBox.Text = string.Empty;
            DescriptionTextBox.Text = string.Empty;
            DueUtcTextBox.Text = string.Empty;
            SelectReferenceValue(ItemTypeList, PortalReferenceDataSets.GeneralItemType);
            SelectReferenceValue(PriorityList, PortalReferenceDataSets.NormalPriority);
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

        private void ShowMessage(string message)
        {
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
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
    ///   <zh-CN>企业能力工作台本人事项展示行。</zh-CN>
    ///   <en>Own-item display row for the enterprise-capability workbench.</en>
    /// </lang>
    /// </summary>
    public sealed class EnterpriseCapabilityWorkbenchItemRow
    {
        internal EnterpriseCapabilityWorkbenchItemRow(CollaborationItemInfo item)
        {
            ItemCode = EmptyToNone(item.ItemCode);
            Title = EmptyToNone(item.Title);
            ItemStatus = EmptyToNone(item.ItemStatus);
            PriorityKey = EmptyToNone(item.PriorityKey);
            LastActionUtcText = item.LastActionUtc.HasValue
                ? item.LastActionUtc.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)
                : "(none)";
            LastActionComment = EmptyToNone(item.LastActionComment);
        }

        /// <summary><lang><zh-CN>事项编号。</zh-CN><en>Item code.</en></lang></summary>
        public string ItemCode { get; private set; }

        /// <summary><lang><zh-CN>事项标题。</zh-CN><en>Item title.</en></lang></summary>
        public string Title { get; private set; }

        /// <summary><lang><zh-CN>事项状态。</zh-CN><en>Item status.</en></lang></summary>
        public string ItemStatus { get; private set; }

        /// <summary><lang><zh-CN>优先级键。</zh-CN><en>Priority key.</en></lang></summary>
        public string PriorityKey { get; private set; }

        /// <summary><lang><zh-CN>最近动作 UTC 展示文本。</zh-CN><en>Latest action UTC display text.</en></lang></summary>
        public string LastActionUtcText { get; private set; }

        /// <summary><lang><zh-CN>最近办理意见。</zh-CN><en>Latest handling comment.</en></lang></summary>
        public string LastActionComment { get; private set; }

        private static string EmptyToNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }
    }
}
