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
        /// <remarks>
        /// <lang>
        ///   <zh-CN>页面允许具备待办查看权限或历史兼容管理权限的用户进入。拒绝请求在读取筛选、访问待办服务或绑定页面前结束；首次加载才初始化筛选和列表，postback 保留用户提交的筛选状态。</zh-CN>
        ///   <en>The page allows users holding the work-item view permission or the legacy-compatible administration permission. A denied request ends before reading filters, accessing the work-item service, or binding the page; only the initial load initializes the filter and list, while postback preserves the user-submitted filter state.</en>
        /// </lang>
        /// </remarks>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>查看权限是最小访问门槛，管理权限作为历史兼容的替代门槛；任一权限均可进入只读后台列表。</zh-CN>
            //   <en>The view permission is the minimum access gate, while the administration permission is a legacy-compatible alternative; either permission may enter the read-only administration list.</en>
            // </lang>
            if (!PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.BusinessWorkItemsView,
                PortalPermissionKeys.BusinessWorkItemsAdmin))
            {
                // <lang>
                //   <zh-CN>授权组件已处理拒绝响应；此处立即结束，确保未授权请求不会触及筛选控件、服务或绑定结果。</zh-CN>
                //   <en>The authorization component has handled the denial response; end immediately so an unauthorized request cannot touch filters, services, or bound results.</en>
                // </lang>
                return;
            }

            // <lang>
            //   <zh-CN>仅在首次请求建立默认状态筛选并加载列表；postback 保留已提交筛选，交由搜索事件重新读取。</zh-CN>
            //   <en>Establish the default status filter and load the list only on the initial request; postback retains the submitted filter for the search event to reload.</en>
            // </lang>
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
        /// <remarks>
        /// <lang>
        ///   <zh-CN>postback 回调在重新读取列表前重复执行与首次加载相同的查看/管理兼容权限门禁；拒绝时不访问数据服务。筛选控件的已提交值由 Web Forms 生命周期保留，随后交由 <see cref="BindWorkItems"/> 处理。</zh-CN>
        ///   <en>The postback callback repeats the same view-or-administration compatibility permission gate as the initial load before reloading the list; it does not access the data service when denied. The submitted filter value is retained by the Web Forms lifecycle and is then handled by <see cref="BindWorkItems"/>.</en>
        /// </lang>
        /// </remarks>
        protected void SearchButton_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>事件回调不能仅依赖首次加载的授权结果；每次提交均重新确认查看权限或兼容管理权限。</zh-CN>
            //   <en>An event callback cannot rely only on authorization from the initial load; re-confirm the view permission or compatible administration permission on every submission.</en>
            // </lang>
            if (!PortalAuthorization.EnsureAnyPermission(
                Context,
                PortalPermissionKeys.BusinessWorkItemsView,
                PortalPermissionKeys.BusinessWorkItemsAdmin))
            {
                // <lang>
                //   <zh-CN>授权组件已处理拒绝响应；立即返回，避免未授权 postback 使用其提交的筛选值触发任何数据绑定。</zh-CN>
                //   <en>The authorization component has handled the denial response; return immediately so an unauthorized postback cannot use its submitted filter value to trigger any data binding.</en>
                // </lang>
                return;
            }

            // <lang>
            //   <zh-CN>复用当前控件状态刷新列表，不重置筛选默认值；默认值初始化仍仅属于首次加载路径。</zh-CN>
            //   <en>Refresh the list from current control state without resetting the filter default; default initialization remains solely on the initial-load path.</en>
            // </lang>
            BindWorkItems();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>初始化待办状态筛选项。</zh-CN>
        ///   <en>Initializes work-item status filter options.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>筛选值为 <see cref="PortalWorkItemStatuses"/> 定义的固定契约，并保留空值的“全部”选项；首次加载时默认选中 <c>Open</c>，后续回发不调用此方法，以免覆盖用户已提交的筛选条件。</zh-CN>
        ///   <en>The filter values form a fixed contract defined by <see cref="PortalWorkItemStatuses"/>, with an empty-value “All” option retained. Initial load selects <c>Open</c>; subsequent postbacks do not call this method so a user-submitted filter is not overwritten.</en>
        /// </lang>
        /// </remarks>
        private void BindStatusFilter()
        {
            // <lang>
            //   <zh-CN>先清空控件项，保证初始化路径可重入且不会因生命周期重复执行累积重复筛选项。</zh-CN>
            //   <en>Clear existing items first so the initialization path is re-entrant and repeated lifecycle execution cannot accumulate duplicate filters.</en>
            // </lang>
            StatusFilterList.Items.Clear();

            // <lang>
            //   <zh-CN>仅暴露稳定状态契约及空值“全部”选项；列表顺序和值同时构成当前 UI 与后端筛选的兼容边界。</zh-CN>
            //   <en>Expose only the stable status contract and the empty-value “All” option; their order and values together form the current UI-to-backend filtering compatibility boundary.</en>
            // </lang>
            StatusFilterList.Items.Add(new ListItem("All", string.Empty));
            StatusFilterList.Items.Add(new ListItem(PortalWorkItemStatuses.Open, PortalWorkItemStatuses.Open));
            StatusFilterList.Items.Add(new ListItem(PortalWorkItemStatuses.InProgress, PortalWorkItemStatuses.InProgress));
            StatusFilterList.Items.Add(new ListItem(PortalWorkItemStatuses.Completed, PortalWorkItemStatuses.Completed));
            StatusFilterList.Items.Add(new ListItem(PortalWorkItemStatuses.Cancelled, PortalWorkItemStatuses.Cancelled));
            StatusFilterList.Items.Add(new ListItem(PortalWorkItemStatuses.Expired, PortalWorkItemStatuses.Expired));

            // <lang>
            //   <zh-CN>此默认值仅经首次加载路径设置；搜索回发保留控件当前选择，避免静默回退到待处理状态。</zh-CN>
            //   <en>This default is set only through the initial-load path; search postbacks retain the control's current selection instead of silently reverting to Open.</en>
            // </lang>
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
        /// <remarks>
        /// <lang>
        ///   <zh-CN>此方法只负责受控的页面呈现，不重新授权或访问数据服务。调用方只能传入已审查的操作提示，不能传入异常、连接或其他敏感实现细节；无论消息是否为空，均会清理旧摘要和列表，避免不可用状态下保留陈旧管理数据。</zh-CN>
        ///   <en>This method only performs controlled page presentation; it neither reauthorizes nor accesses the data service. Callers may supply only reviewed operational guidance, not exception, connection, or other sensitive implementation details; whether the message is empty or not, it clears the old summary and list so unavailable state cannot retain stale administration data.</en>
        /// </lang>
        /// </remarks>
        private void ShowUnavailable(string message)
        {
            // <lang>
            //   <zh-CN>将空消息规范为空文本；调用方提供的是已审查的操作提示，而非直接暴露的底层异常内容。</zh-CN>
            //   <en>Normalize a null message to empty text; callers supply reviewed operational guidance rather than directly exposed lower-level exception content.</en>
            // </lang>
            MessageLabel.Text = message ?? string.Empty;

            // <lang>
            //   <zh-CN>清除成功路径的统计摘要，使不可用提示不与先前查询结果并存。</zh-CN>
            //   <en>Clear the successful-path summary so the unavailable notice cannot coexist with a previous query result.</en>
            // </lang>
            ResultLabel.Text = string.Empty;

            // <lang>
            //   <zh-CN>绑定固定的空展示行集合以撤销旧列表；不在故障呈现路径重新查询或写入待办数据。</zh-CN>
            //   <en>Bind a fixed empty display-row collection to remove the old list; the failure-presentation path does not requery or write work-item data.</en>
            // </lang>
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
        /// <remarks>
        /// <lang>
        ///   <zh-CN>此构造器仅执行已授权查询结果到页面专用模型的受控投影，不重新查询、写入或授权。它保留模板所需的标识和状态字段，将业务入口限制为固定 URL helper，使用既有空值占位和不随服务器区域变化的 UTC 文本格式；调用方须提供有效的待办对象。</zh-CN>
        ///   <en>This constructor only performs a controlled projection of an already-authorized query result into a page-specific model; it neither requeries, writes, nor authorizes. It retains the identifiers and status required by the template, limits the business entry to the fixed-URL helper, and uses the established empty-value placeholders and server-locale-independent UTC text format; callers must provide a valid work-item object.</en>
        /// </lang>
        /// </remarks>
        internal PortalWorkItemAdminRow(PortalWorkItemInfo item)
        {
            // <lang>
            //   <zh-CN>保留模板和受控 URL 映射所需的原始标识、状态与业务类型；BusinessId 仅保留在展示模型中，当前模板不直接输出它。</zh-CN>
            //   <en>Retain the raw identifier, status, and business kind needed by the template and controlled URL mapping; BusinessId remains in the display model but the current template does not render it directly.</en>
            // </lang>
            WorkItemId = item.WorkItemId;
            WorkItemStatus = item.WorkItemStatus;
            BusinessKind = item.BusinessKind;
            BusinessId = item.BusinessId;

            // <lang>
            //   <zh-CN>业务入口只由固定白名单 helper 生成，不能将业务标识或其他数据值拼接成 URL。</zh-CN>
            //   <en>Generate the business entry only through the fixed allowlist helper; do not concatenate the business identifier or other data values into a URL.</en>
            // </lang>
            BusinessUrl = GetBusinessUrl(item.BusinessKind);

            // <lang>
            //   <zh-CN>保留标题供标记层的编码绑定使用；此投影不改变标题内容或添加 HTML。</zh-CN>
            //   <en>Retain the title for encoded markup binding; this projection neither changes its content nor adds HTML.</en>
            // </lang>
            Title = item.Title;

            // <lang>
            //   <zh-CN>摘要使用既有空值占位，避免后台列表以空单元格掩盖缺失的业务摘要。</zh-CN>
            //   <en>Apply the established empty-value placeholder to the summary so the administration list does not conceal a missing business summary with a blank cell.</en>
            // </lang>
            Summary = EmptyToNone(item.Summary);

            // <lang>
            //   <zh-CN>将用户或角色分派转换为单一展示文本；helper 保持两种分派模式的既有优先级和占位规则。</zh-CN>
            //   <en>Convert user or role assignment to one display text; the helper preserves the established precedence and placeholder rules for both assignment modes.</en>
            // </lang>
            AssignedText = GetAssignedText(item);

            // <lang>
            //   <zh-CN>创建时间以不随服务器区域变化的 UTC 格式投影，保证后台列表和审计时间语义可比较。</zh-CN>
            //   <en>Project the creation time in a server-locale-independent UTC format so administration-list and audit-time semantics remain comparable.</en>
            // </lang>
            CreatedUtcText = item.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);

            // <lang>
            //   <zh-CN>完成事实存在时显示 UTC 时间和完成人空值占位；未完成事项保留既有的“(open)”状态文本。</zh-CN>
            //   <en>When completion fact exists, show its UTC time and a placeholder for a missing completer; incomplete items retain the established “(open)” status text.</en>
            // </lang>
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
        /// <remarks>
        /// <lang>
        ///   <zh-CN>直接用户分派优先于角色分派；当用户标识存在时，角色键不参与展示。用户标识使用文化不变格式，用户名称和角色键均沿用空值占位，保证管理列表在部分分派数据下仍保持稳定列结构。此 helper 只格式化已投影的数据，不查询、写入或授权。</zh-CN>
        ///   <en>Direct user assignment takes precedence over role assignment; when a user identifier exists, the role key does not participate in display. The user identifier uses culture-invariant formatting, while the user name and role key retain the established empty-value placeholder so the administration list keeps a stable column structure with partial assignment data. This helper only formats projected data and neither queries, writes, nor authorizes.</en>
        /// </lang>
        /// </remarks>
        private static string GetAssignedText(PortalWorkItemInfo item)
        {
            // <lang>
            //   <zh-CN>存在直接用户分派时优先显示它；即使同时存在角色键，也不能改变现有用户优先的管理语义。</zh-CN>
            //   <en>Prefer a direct user assignment when it exists; even if a role key also exists, it must not change the established user-first administration semantics.</en>
            // </lang>
            if (item.AssignedUserId.HasValue)
            {
                // <lang>
                //   <zh-CN>保留既有“User {标识} / {名称}”文本契约；数值标识不受服务器区域影响，缺失名称使用统一占位。</zh-CN>
                //   <en>Preserve the established “User {identifier} / {name}” text contract; the numeric identifier is server-locale-independent and a missing name uses the shared placeholder.</en>
                // </lang>
                return "User " + item.AssignedUserId.Value.ToString(CultureInfo.InvariantCulture) +
                       " / " + EmptyToNone(item.AssignedUserName);
            }

            // <lang>
            //   <zh-CN>没有直接用户分派时才回退到角色文本；空角色键仍使用统一占位而不返回空单元格。</zh-CN>
            //   <en>Fall back to role text only when no direct user assignment exists; an empty role key still uses the shared placeholder rather than returning a blank cell.</en>
            // </lang>
            return "Role " + EmptyToNone(item.AssignedRoleKey);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>根据业务类型生成后台查看入口。</zh-CN>
        ///   <en>Builds the administration viewing entry URL from the business kind.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>此映射只返回本地、固定的后台页路径，不拼接待办中的业务标识或其他数据值。未知类型统一落到占位页，避免把未受支持的类型解释为可导航 URL。</zh-CN>
        ///   <en>This mapping returns only fixed local administration-page paths and never concatenates a work item's business identifier or other data value. Unknown kinds consistently fall back to a placeholder page so unsupported kinds cannot be interpreted as navigable URLs.</en>
        /// </lang>
        /// </remarks>
        private static string GetBusinessUrl(string businessKind)
        {
            // <lang>
            //   <zh-CN>工号资料更正待办使用固定的后台查看页；按序号比较稳定类型键，不把来源值直接用于 URL。</zh-CN>
            //   <en>Employee-profile correction work items use a fixed administration viewing page; compare the stable kind key ordinally and never use the source value directly in a URL.</en>
            // </lang>
            if (string.Equals(businessKind, PortalWorkItemBusinessKinds.EmployeeProfileCorrectionRequest, StringComparison.Ordinal))
            {
                return "EmployeeProfileCorrectionRequests.aspx";
            }

            // <lang>
            //   <zh-CN>业务申请待办映射到固定申请后台页，保留页面级既有授权与筛选边界。</zh-CN>
            //   <en>Business-application work items map to the fixed application administration page, preserving its existing page-level authorization and filtering boundary.</en>
            // </lang>
            if (string.Equals(businessKind, PortalWorkItemBusinessKinds.BusinessApplication, StringComparison.Ordinal))
            {
                return "BusinessApplications.aspx";
            }

            // <lang>
            //   <zh-CN>协同事项待办映射到固定协同事项后台页，不在列表页构造带数据的深链接。</zh-CN>
            //   <en>Collaboration-item work items map to the fixed collaboration administration page; the list page does not construct data-bearing deep links.</en>
            // </lang>
            if (string.Equals(businessKind, PortalWorkItemBusinessKinds.CollaborationItem, StringComparison.Ordinal))
            {
                return "CollaborationItems.aspx";
            }

            // <lang>
            //   <zh-CN>未知或未来业务类型进入显式占位页，使新增类型必须先完成受审查的导航注册而非隐式放行。</zh-CN>
            //   <en>Unknown or future business kinds enter an explicit placeholder page so a new kind must complete reviewed navigation registration instead of being implicitly allowed.</en>
            // </lang>
            return "NotImplemented.aspx";
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将空展示值统一转换为页面占位文本。</zh-CN>
        ///   <en>Converts empty display values to the shared page placeholder text.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>仅对 <see langword="null"/>、空字符串或纯空白值返回固定的 <c>(none)</c> 占位；非空白输入按原样返回，不修剪、不编码也不改写领域数据。此 helper 只服务展示投影，不查询、写入或授权。</zh-CN>
        ///   <en>Returns the fixed <c>(none)</c> placeholder only for <see langword="null"/>, empty, or whitespace-only values; non-whitespace input is returned unchanged, without trimming, encoding, or rewriting domain data. This helper serves display projection only and neither queries, writes, nor authorizes.</en>
        /// </lang>
        /// </remarks>
        private static string EmptyToNone(string value)
        {
            // <lang>
            //   <zh-CN>将无展示内容的三种等价输入归一为固定占位；含实际字符的文本必须保留原值，交由标记层按既有规则编码。</zh-CN>
            //   <en>Normalize the three equivalent no-display-content inputs to the fixed placeholder; text containing actual characters must retain its original value for markup to encode under its established rules.</en>
            // </lang>
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }
    }
}
