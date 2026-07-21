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
    /// 中文：轻量待办后台只读列表页。
    ///
    /// English: Read-only administration list page for lightweight work items.
    /// </summary>
    /// <remarks>
    /// 中文：P12.3 第一版只提供集中查看和状态筛选，不执行转办、加签、会签或通知发送。
    ///
    /// English: The first P12.3 version provides centralized viewing and status filtering only. It does not perform
    /// delegation, countersigning, co-signing, or notification delivery.
    /// </remarks>
    public partial class WorkItems : PortalPage<WorkItems>
    {
        private const int PageSize = 50;

        /// <summary>
        /// 中文：轻量待办数据服务。
        ///
        /// English: Lightweight work-item data service.
        /// </summary>
        [Dependency]
        public IPortalWorkItemDb WorkItemDb { private get; set; }

        /// <summary>
        /// 中文：初始化待办后台页。
        ///
        /// English: Initializes the work-item administration page.
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.BusinessWorkItemsView,
                PortalPermissionKeys.BusinessWorkItemsAdmin))
            {
                return;
            }

            if (!IsPostBack)
            {
                BindStatusFilter();
                BindWorkItems();
            }
        }

        /// <summary>
        /// 中文：按当前筛选条件重新读取待办列表。
        ///
        /// English: Reloads work items using the current filter.
        /// </summary>
        protected void SearchButton_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.BusinessWorkItemsView,
                PortalPermissionKeys.BusinessWorkItemsAdmin))
            {
                return;
            }

            BindWorkItems();
        }

        private void BindStatusFilter()
        {
            StatusFilterList.Items.Clear();
            StatusFilterList.Items.Add(new ListItem("All", string.Empty));
            StatusFilterList.Items.Add(new ListItem(PortalWorkItemStatuses.Open, PortalWorkItemStatuses.Open));
            StatusFilterList.Items.Add(new ListItem(PortalWorkItemStatuses.InProgress, PortalWorkItemStatuses.InProgress));
            StatusFilterList.Items.Add(new ListItem(PortalWorkItemStatuses.Completed, PortalWorkItemStatuses.Completed));
            StatusFilterList.Items.Add(new ListItem(PortalWorkItemStatuses.Cancelled, PortalWorkItemStatuses.Cancelled));
            StatusFilterList.Items.Add(new ListItem(PortalWorkItemStatuses.Expired, PortalWorkItemStatuses.Expired));
            StatusFilterList.SelectedValue = PortalWorkItemStatuses.Open;
        }

        private void BindWorkItems()
        {
            if (WorkItemDb == null)
            {
                ShowUnavailable("Portal work-item data service is not registered.");
                return;
            }

            if (!WorkItemDb.IsSchemaAvailable())
            {
                ShowUnavailable("P12.3 work-item schema is unavailable. Run PortalBiz_WorkItems.sql and PortalBiz_WorkItemEvents.sql.");
                return;
            }

            IList<PortalWorkItemInfo> workItems = WorkItemDb.GetAdminWorkItems(
                StatusFilterList.SelectedValue,
                PageSize);
            WorkItemsRepeater.DataSource = workItems.Select(item => new PortalWorkItemAdminRow(item)).ToList();
            WorkItemsRepeater.DataBind();

            ResultLabel.Text = "Showing up to " + PageSize.ToString(CultureInfo.InvariantCulture) +
                               " work items; count: " + workItems.Count.ToString(CultureInfo.InvariantCulture) + ".";
        }

        private void ShowUnavailable(string message)
        {
            MessageLabel.Text = message ?? string.Empty;
            ResultLabel.Text = string.Empty;
            WorkItemsRepeater.DataSource = Enumerable.Empty<PortalWorkItemAdminRow>();
            WorkItemsRepeater.DataBind();
        }
    }

    /// <summary>
    /// 中文：待办后台展示行。
    ///
    /// English: Administration display row for a work item.
    /// </summary>
    public sealed class PortalWorkItemAdminRow
    {
        internal PortalWorkItemAdminRow(PortalWorkItemInfo item)
        {
            WorkItemId = item.WorkItemId;
            WorkItemStatus = item.WorkItemStatus;
            BusinessKind = item.BusinessKind;
            BusinessId = item.BusinessId;
            BusinessUrl = GetBusinessUrl(item.BusinessKind);
            Title = item.Title;
            Summary = EmptyToNone(item.Summary);
            AssignedText = GetAssignedText(item);
            CreatedUtcText = item.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
            CompletedUtcText = item.CompletedUtc.HasValue
                ? item.CompletedUtc.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture) + " / " + EmptyToNone(item.CompletedBy)
                : "(open)";
        }

        /// <summary>中文：待办标识。English: Work-item identifier.</summary>
        public long WorkItemId { get; private set; }

        /// <summary>中文：待办状态。English: Work-item status.</summary>
        public string WorkItemStatus { get; private set; }

        /// <summary>中文：业务对象类型。English: Business-object kind.</summary>
        public string BusinessKind { get; private set; }

        /// <summary>中文：业务对象标识。English: Business-object identifier.</summary>
        public string BusinessId { get; private set; }

        /// <summary>中文：业务入口链接。English: Business entry URL.</summary>
        public string BusinessUrl { get; private set; }

        /// <summary>中文：标题。English: Title.</summary>
        public string Title { get; private set; }

        /// <summary>中文：摘要。English: Summary.</summary>
        public string Summary { get; private set; }

        /// <summary>中文：分派信息。English: Assignment text.</summary>
        public string AssignedText { get; private set; }

        /// <summary>中文：创建 UTC 文本。English: Creation UTC text.</summary>
        public string CreatedUtcText { get; private set; }

        /// <summary>中文：完成 UTC 文本。English: Completion UTC text.</summary>
        public string CompletedUtcText { get; private set; }

        private static string GetAssignedText(PortalWorkItemInfo item)
        {
            if (item.AssignedUserId.HasValue)
            {
                return "User " + item.AssignedUserId.Value.ToString(CultureInfo.InvariantCulture) +
                       " / " + EmptyToNone(item.AssignedUserName);
            }

            return "Role " + EmptyToNone(item.AssignedRoleKey);
        }

        private static string GetBusinessUrl(string businessKind)
        {
            if (string.Equals(businessKind, PortalWorkItemBusinessKinds.EmployeeProfileCorrectionRequest, StringComparison.Ordinal))
            {
                return "EmployeeProfileCorrectionRequests.aspx";
            }

            return "NotImplemented.aspx";
        }

        private static string EmptyToNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }
    }
}
