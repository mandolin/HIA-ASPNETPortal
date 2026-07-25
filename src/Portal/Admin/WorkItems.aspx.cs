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
    ///   <zh-CN>轻量待办后台只读列表页。</zh-CN>
    ///   <en>Read-only administration list page for lightweight work items.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P12.3 第一版只提供集中查看和状态筛选，不执行转办、加签、会签或通知发送。</zh-CN>
    ///   <en>The first P12.3 version provides centralized viewing and status filtering only. It does not perform delegation, countersigning, co-signing, or notification delivery.</en>
    /// </lang>
    /// </remarks>
    public partial class WorkItems : PortalPage<WorkItems>
    {
        private const int PageSize = 50;

        /// <summary>
        /// <lang>
        ///   <zh-CN>轻量待办数据服务。</zh-CN>
        ///   <en>Lightweight work-item data service.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IPortalWorkItemDb WorkItemDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化待办后台页。</zh-CN>
        ///   <en>Initializes the work-item administration page.</en>
        /// </lang>
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
        /// <lang>
        ///   <zh-CN>按当前筛选条件重新读取待办列表。</zh-CN>
        ///   <en>Reloads work items using the current filter.</en>
        /// </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化待办状态筛选项。</zh-CN>
        ///   <en>Initializes work-item status filter options.</en>
        /// </lang>
        /// </summary>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>按当前状态筛选读取后台待办列表。</zh-CN>
        ///   <en>Reads the administration work-item list using the current status filter.</en>
        /// </lang>
        /// </summary>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>显示待办数据不可用提示，并清空列表。</zh-CN>
        ///   <en>Displays work-item data unavailable messages and clears the list.</en>
        /// </lang>
        /// </summary>
        private void ShowUnavailable(string message)
        {
            MessageLabel.Text = message ?? string.Empty;
            ResultLabel.Text = string.Empty;
            WorkItemsRepeater.DataSource = Enumerable.Empty<PortalWorkItemAdminRow>();
            WorkItemsRepeater.DataBind();
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>待办后台展示行。</zh-CN>
    ///   <en>Administration display row for a work item.</en>
    /// </lang>
    /// </summary>
    public sealed class PortalWorkItemAdminRow
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>从待办数据对象创建后台展示行。</zh-CN>
        ///   <en>Creates an administration display row from a work-item data object.</en>
        /// </lang>
        /// </summary>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>待办标识。</zh-CN>
        ///   <en>Work-item identifier.</en>
        /// </lang>
        /// </summary>
        public long WorkItemId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>待办状态。</zh-CN>
        ///   <en>Work-item status.</en>
        /// </lang>
        /// </summary>
        public string WorkItemStatus { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>业务对象类型。</zh-CN>
        ///   <en>Business-object kind.</en>
        /// </lang>
        /// </summary>
        public string BusinessKind { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>业务对象标识。</zh-CN>
        ///   <en>Business-object identifier.</en>
        /// </lang>
        /// </summary>
        public string BusinessId { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>业务入口链接。</zh-CN>
        ///   <en>Business entry URL.</en>
        /// </lang>
        /// </summary>
        public string BusinessUrl { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>标题。</zh-CN>
        ///   <en>Title.</en>
        /// </lang>
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>摘要。</zh-CN>
        ///   <en>Summary.</en>
        /// </lang>
        /// </summary>
        public string Summary { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>分派信息。</zh-CN>
        ///   <en>Assignment text.</en>
        /// </lang>
        /// </summary>
        public string AssignedText { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>创建 UTC 文本。</zh-CN>
        ///   <en>Creation UTC text.</en>
        /// </lang>
        /// </summary>
        public string CreatedUtcText { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>完成 UTC 文本。</zh-CN>
        ///   <en>Completion UTC text.</en>
        /// </lang>
        /// </summary>
        public string CompletedUtcText { get; private set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>生成用户或角色分派的展示文本。</zh-CN>
        ///   <en>Builds display text for user or role assignment.</en>
        /// </lang>
        /// </summary>
        private static string GetAssignedText(PortalWorkItemInfo item)
        {
            if (item.AssignedUserId.HasValue)
            {
                return "User " + item.AssignedUserId.Value.ToString(CultureInfo.InvariantCulture) +
                       " / " + EmptyToNone(item.AssignedUserName);
            }

            return "Role " + EmptyToNone(item.AssignedRoleKey);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>根据业务类型生成后台查看入口。</zh-CN>
        ///   <en>Builds the administration viewing entry URL from the business kind.</en>
        /// </lang>
        /// </summary>
        private static string GetBusinessUrl(string businessKind)
        {
            if (string.Equals(businessKind, PortalWorkItemBusinessKinds.EmployeeProfileCorrectionRequest, StringComparison.Ordinal))
            {
                return "EmployeeProfileCorrectionRequests.aspx";
            }

            return "NotImplemented.aspx";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将空展示值统一转换为页面占位文本。</zh-CN>
        ///   <en>Converts empty display values to the shared page placeholder text.</en>
        /// </lang>
        /// </summary>
        private static string EmptyToNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }
    }
}
