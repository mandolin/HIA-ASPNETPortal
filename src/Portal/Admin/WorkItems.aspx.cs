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
        /// <remarks>
        /// <lang>
        ///   <zh-CN>此方法假定调用入口已通过待办查看或管理权限门禁。数据服务或 schema 不可用时清空旧展示并给出部署提示；成功时只绑定受限页大小的展示模型，不执行待办状态写入。</zh-CN>
        ///   <en>This method assumes its caller has passed the work-item view or administration permission gate. When the data service or schema is unavailable, it clears stale display and gives a deployment hint; on success, it binds only a page-size-limited display model and performs no work-item state write.</en>
        /// </lang>
        /// </remarks>
        private void BindWorkItems()
        {
            // <lang>
            //   <zh-CN>依赖注入失败时不能继续访问数据层；清空此前绑定内容以避免页面展示过期管理信息。</zh-CN>
            //   <en>Do not continue to the data layer when dependency injection fails; clear previously bound content so the page cannot display stale administration information.</en>
            // </lang>
            if (WorkItemDb == null)
            {
                ShowUnavailable("Portal work-item data service is not registered.");
                return;
            }

            // <lang>
            //   <zh-CN>待办和事件 schema 是列表投影的最小前置条件；缺失时返回可操作的迁移提示而不执行部分查询。</zh-CN>
            //   <en>The work-item and event schema is the minimum prerequisite for list projection; when missing, return an actionable migration hint rather than attempting a partial query.</en>
            // </lang>
            if (!WorkItemDb.IsSchemaAvailable())
            {
                ShowUnavailable("P12.3 work-item schema is unavailable. Run PortalBiz_WorkItems.sql and PortalBiz_WorkItemEvents.sql.");
                return;
            }

            // <lang>
            //   <zh-CN>使用由筛选控件提供的状态和固定页大小读取后台视图；服务层负责保持状态筛选的既有受控契约。</zh-CN>
            //   <en>Read the administration view with the status supplied by the filter control and a fixed page size; the service layer preserves the established controlled status-filter contract.</en>
            // </lang>
            IList<PortalWorkItemInfo> workItems = WorkItemDb.GetAdminWorkItems(
                StatusFilterList.SelectedValue,
                PageSize);

            // <lang>
            //   <zh-CN>将数据对象映射为页面专用展示行，使业务入口 URL、空值占位、分派文字和 UTC 时间格式在受控投影中生成。</zh-CN>
            //   <en>Map data objects to page-specific display rows so business-entry URLs, empty-value placeholders, assignment text, and UTC formatting are produced in a controlled projection.</en>
            // </lang>
            WorkItemsRepeater.DataSource = workItems.Select(item => new PortalWorkItemAdminRow(item)).ToList();
            WorkItemsRepeater.DataBind();

            // <lang>
            //   <zh-CN>用不随服务器区域设置变化的数字格式报告固定上限与实际计数，避免管理页摘要产生文化相关歧义。</zh-CN>
            //   <en>Report the fixed limit and actual count with culture-invariant number formatting so the administration summary has no server-locale ambiguity.</en>
            // </lang>
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

            if (string.Equals(businessKind, PortalWorkItemBusinessKinds.BusinessApplication, StringComparison.Ordinal))
            {
                return "BusinessApplications.aspx";
            }

            if (string.Equals(businessKind, PortalWorkItemBusinessKinds.CollaborationItem, StringComparison.Ordinal))
            {
                return "CollaborationItems.aspx";
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
