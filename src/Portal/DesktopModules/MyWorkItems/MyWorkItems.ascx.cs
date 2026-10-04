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
    ///   <zh-CN>前台「我的待办」聚合入口，只呈现当前用户本人或其持有角色键所指派的未完成待办。</zh-CN>
    ///   <en>Front-end "My To-Do Items" aggregation entry that lists only unfinished work items assigned to the current user or to role keys the user holds.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本模块不办理业务、不拥有流程语义，只把既有待办投影呈现给承接人；办理必须跳转到对应业务对象。可见性、指派归属与读取结果全部由服务端判定，标记层不接受客户端提供的用户标识或状态。</zh-CN>
    ///   <en>This module neither processes business nor owns process semantics; it only surfaces the existing work-item projection to the assignee. Processing must navigate to the associated business object. Visibility, assignment, and read outcome are all decided server-side; the markup accepts no client-supplied user identifier or status.</en>
    /// </lang>
    /// </remarks>
    public partial class MyWorkItems : PortalModuleControl<MyWorkItems>
    {
        /// <summary><lang><zh-CN>前台列表条数上限；沿用后台既有量级，避免首页承载过大结果集。</zh-CN><en>Front-end list row cap; it reuses the administration magnitude so the landing page never carries an oversized result set.</en></lang></summary>
        private const int WorkItemLimit = 20;

        /// <summary><lang><zh-CN>待办数据访问门面；由容器注入，缺失时模块按读取失败处理而非抛错。</zh-CN><en>Work-item data-access facade injected by the container; when it is missing the module reports a read failure instead of throwing.</en></lang></summary>
        [Dependency]
        public IPortalWorkItemDb WorkItemDb { private get; set; }

        /// <summary><lang><zh-CN>用户数据访问门面，用于把登录名解析为门户用户标识。</zh-CN><en>User data-access facade used to resolve the sign-in name to a portal user identifier.</en></lang></summary>
        [Dependency]
        public IUsersDb UsersDb { private get; set; }

        /// <summary><lang><zh-CN>角色数据访问门面，用于解析当前用户持有的受控权限键。</zh-CN><en>Role data-access facade used to resolve the controlled permission keys held by the current user.</en></lang></summary>
        [Dependency]
        public IRolesDb RolesDb { private get; set; }

        /// <summary><lang><zh-CN>页签数据访问门面，用于把模块实例归属的页签解析为可导航目标；缺失时一律降级为不可办理，不猜测地址。</zh-CN><en>Tab data-access facade used to resolve the tab owning a module instance into a navigable target; when it is missing every row degrades to non-navigable and no URL is guessed.</en></lang></summary>
        [Dependency]
        public ITabsDb TabsConfig { private get; set; }

        /// <summary><lang><zh-CN>模块定义数据访问门面，用于按已验证桌面入口定位模块定义；缺失时一律降级。</zh-CN><en>Module-definition data-access facade used to locate a definition by its validated desktop entry; when it is missing every row degrades.</en></lang></summary>
        [Dependency]
        public IModuleDefsDb ModuleDefinitionsConfig { private get; set; }

        /// <summary><lang><zh-CN>模块实例数据访问门面，用于按模块定义标识枚举实例并读回其承载页签。基类已注入同类型的私有副本（其 getter 为 private，派生类与嵌套类型都读不到），故此处保留本模块可读的注入点；两处注入的是同一容器注册的同一实现，不产生第二套数据源。</zh-CN><en>Module-instance data-access facade used to enumerate instances by definition identifier and read back their hosting tab. The base class already injects a private copy of the same type whose getter is private, so neither derived nor nested types can read it; this keeps an injection point this module can read. Both injections resolve the same container registration, so no second data source appears.</en></lang></summary>
        [Dependency]
        public IModulesDb ModuleInstancesConfig { private get; set; }

        /// <summary><lang><zh-CN>最近一次绑定中已解析出可导航目标的行数；仅用于本次查看的审计统计，不跨请求保留。</zh-CN><en>Rows whose navigable target resolved during the latest bind; used only for this view's audit counters and never kept across requests.</en></lang></summary>
        private int resolvedTargetCount;

        /// <summary><lang><zh-CN>最近一次绑定中降级（不可办理）的行数。</zh-CN><en>Rows that degraded to non-navigable during the latest bind.</en></lang></summary>
        private int degradedTargetCount;

        /// <summary><lang><zh-CN>降级原因计数，键为解析器原因码；用于审计正文的非敏感统计。</zh-CN><en>Degradation reason counts keyed by resolver reason code, used for the non-sensitive statistics in the audit text.</en></lang></summary>
        private readonly IDictionary<string, int> degradationReasonCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary><lang><zh-CN>本次绑定使用的反查数据端口；在绑定前创建，避免逐行重复读取部署清单。</zh-CN><en>Reverse-lookup data port used by the current bind; created before binding so the deployment manifest is not reread for every row.</en></lang></summary>
        private IPortalModuleTargetSource targetSource;

        /// <summary>
        /// <lang>
        ///   <zh-CN>指示当前是否处于"仅超期"筛选；保存在视图状态中，不取自查询字符串。</zh-CN>
        ///   <en>Indicates whether the "overdue only" filter is active; it is kept in view state and never taken from the query string.</en>
        /// </lang>
        /// </summary>
        private bool ShowOverdueOnly
        {
            get
            {
                object value = ViewState["ShowOverdueOnly"];
                return value is bool && (bool)value;
            }

            set
            {
                ViewState["ShowOverdueOnly"] = value;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>页面加载入口：先做认证与权限门禁，再绑定列表；门禁不通过时整块隐藏并留下授权失败审计。</zh-CN>
        ///   <en>Page-load entry: it applies the authentication and permission gate first, then binds the list; when the gate fails the whole block is hidden and an authorization-failure audit is recorded.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender"><l><zh-CN>事件源。</zh-CN><en>The event source.</en></l></param>
        /// <param name="e"><l><zh-CN>事件参数。</zh-CN><en>The event arguments.</en></l></param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>认证缺失直接整块隐藏：未登录用户不应看到模块骨架，也不产生授权失败事件（尚无主体可归因）。</zh-CN>
            //   <en>An unauthenticated viewer hides the entire block: signed-out users should not see the module skeleton, and no authorization-failure event is produced because there is no principal to attribute.</en>
            // </lang>
            if (!IsCurrentUserAuthenticated())
            {
                ContentPanel.Visible = false;
                FailurePanel.Visible = false;
                return;
            }

            // <lang>
            //   <zh-CN>权限门禁失败即拒绝并写审计：这是 W62 补齐的"授权失败必须可审计"能力在本模块的落点。</zh-CN>
            //   <en>A failed permission gate denies access and writes an audit: this is where the W62 "authorization failures must be auditable" capability lands in this module.</en>
            // </lang>
            // <lang>
            //   <zh-CN>门禁同时接受查看与管理两个权限键，与 PortalNavigationRegistry 中该模块的既有用法一致；只判 View 会把仅持有 Admin 键的管理员误拒。</zh-CN>
            //   <en>The gate accepts both the view and administration permission keys, matching the established usage for this area in PortalNavigationRegistry; checking only View would wrongly reject administrators who hold just the Admin key.</en>
            // </lang>
            if (!PortalAuthorization.HasAnyPermission(
                    PortalPermissionKeys.BusinessWorkItemsView,
                    PortalPermissionKeys.BusinessWorkItemsAdmin))
            {
                ContentPanel.Visible = false;
                FailurePanel.Visible = false;
                PortalOperationAudit.Record(
                    PortalOperationAuditEvents.BusinessModuleCategory,
                    PortalOperationAuditEvents.WorkItemsViewed,
                    PortalOperationAuditEvents.WorkItemTargetType,
                    GetCurrentUserId().ToString(CultureInfo.InvariantCulture),
                    "My work items view denied. RequiredAny=" + PortalPermissionKeys.BusinessWorkItemsView + "|" + PortalPermissionKeys.BusinessWorkItemsAdmin,
                    Context,
                    null,
                    "Failure");
                return;
            }

            if (!IsPostBack)
            {
                BindWorkItems();
            }
        }

        /// <summary><lang><zh-CN>切换到"全部"筛选并重新绑定列表。</zh-CN><en>Switches to the "all" filter and rebinds the list.</en></lang></summary>
        /// <param name="sender"><l><zh-CN>事件源。</zh-CN><en>The event source.</en></l></param>
        /// <param name="e"><l><zh-CN>事件参数。</zh-CN><en>The event arguments.</en></l></param>
        protected void AllFilterButton_Click(object sender, EventArgs e)
        {
            ShowOverdueOnly = false;
            BindWorkItems();
        }

        /// <summary><lang><zh-CN>切换到"仅超期"筛选并重新绑定列表。</zh-CN><en>Switches to the "overdue only" filter and rebinds the list.</en></lang></summary>
        /// <param name="sender"><l><zh-CN>事件源。</zh-CN><en>The event source.</en></l></param>
        /// <param name="e"><l><zh-CN>事件参数。</zh-CN><en>The event arguments.</en></l></param>
        protected void OverdueFilterButton_Click(object sender, EventArgs e)
        {
            ShowOverdueOnly = true;
            BindWorkItems();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按当前身份查询待办并呈现成功列表、空态或失败态三分支之一。</zh-CN>
        ///   <en>Queries work items for the current identity and renders one of three branches: a successful list, the empty state, or the failure state.</en>
        /// </lang>
        /// </summary>
        private void BindWorkItems()
        {
            string userName = GetCurrentUserName();
            int userId = GetCurrentUserId();

            // <lang>
            //   <zh-CN>权限键由角色门面按登录名解析后传入；数据层不解析成员关系，因此此处是归属判定的唯一来源。</zh-CN>
            //   <en>Permission keys are resolved by the role facade from the sign-in name and passed in; the data layer does not resolve membership, so this is the only source of the ownership decision.</en>
            // </lang>
            IList<string> roleKeys = ResolveHeldPermissionKeys(userName);

            PortalWorkItemQueryResult result = WorkItemDb == null
                ? null
                : WorkItemDb.GetWorkItemsForUser(userId, roleKeys, string.Empty, WorkItemLimit);

            // <lang>
            //   <zh-CN>门面缺失或查询失败都呈现失败态，而不是空态：空态会让人误以为确实没有待办。</zh-CN>
            //   <en>A missing facade or a failed query renders the failure state rather than the empty state, because the empty state would suggest there are genuinely no to-do items.</en>
            // </lang>
            if (result == null || !result.Succeeded)
            {
                ContentPanel.Visible = false;
                FailurePanel.Visible = true;
                PortalOperationAudit.Record(
                    PortalOperationAuditEvents.BusinessModuleCategory,
                    PortalOperationAuditEvents.WorkItemsViewed,
                    PortalOperationAuditEvents.WorkItemTargetType,
                    userId.ToString(CultureInfo.InvariantCulture),
                    "My work items read failed.",
                    Context,
                    null,
                    "Failure");
                return;
            }

            IList<PortalWorkItemInfo> items = ShowOverdueOnly ? FilterOverdue(result.Items) : result.Items;

            ContentPanel.Visible = true;
            FailurePanel.Visible = false;
            WorkItemRepeater.Visible = items.Count > 0;
            WorkItemRepeater.DataSource = items;

            // <lang>
            //   <zh-CN>绑定前重置解析统计并创建反查端口：行绑定期间累加，随后一次性进入审计，避免每行写一条审计记录。</zh-CN>
            //   <en>Reset the resolution counters and create the reverse-lookup port before binding: row binding accumulates them and they enter the audit once afterwards, instead of writing one audit record per row.</en>
            // </lang>
            resolvedTargetCount = 0;
            degradedTargetCount = 0;
            degradationReasonCounts.Clear();
            targetSource = new PortalModuleTargetSource(this);

            WorkItemRepeater.DataBind();
            EmptyPanel.Visible = items.Count == 0;
            EmptyLabel.Text = ShowOverdueOnly ? GetResource("MyWorkItems_EmptyNoMatch") : GetResource("MyWorkItems_EmptyNone");

            // <lang>
            //   <zh-CN>查看与筛选只记录条数、筛选态、解析成功/降级计数与降级原因分布；标题、业务正文与地址一律不写，避免审计承载业务数据或可被复用的跳转细节。目标解析统计是本模块"点击跳转待补审计"的落点：普通超链接的点击本身对服务器不可见，故以**本次呈现**解析到多少可达目标作为替代证据，并在文档中如实说明其边界。</zh-CN>
            //   <en>Viewing and filtering record only the row count, filter state, resolved/degraded counters, and the reason breakdown; titles, domain content, and URLs are never written so the audit carries neither business data nor reusable navigation detail. The resolution counters are where this module's pending "click-through audit" lands: a plain hyperlink click is invisible to the server, so how many navigable targets **this render** resolved stands in as the evidence, with its limits stated in the documents.</en>
            // </lang>
            PortalOperationAudit.Record(
                PortalOperationAuditEvents.BusinessModuleCategory,
                PortalOperationAuditEvents.WorkItemsViewed,
                PortalOperationAuditEvents.WorkItemTargetType,
                userId.ToString(CultureInfo.InvariantCulture),
                "My work items viewed. Count=" + items.Count.ToString(CultureInfo.InvariantCulture) +
                "; OverdueOnly=" + ShowOverdueOnly.ToString(CultureInfo.InvariantCulture) +
                "; ResolvedTargets=" + resolvedTargetCount.ToString(CultureInfo.InvariantCulture) +
                "; DegradedTargets=" + degradedTargetCount.ToString(CultureInfo.InvariantCulture) +
                "; DegradationReasons=" + BuildDegradationReasonSummary(),
                Context);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把降级原因计数汇总为稳定的非敏感文本；无降级时返回 <c>none</c>。键按序排序，使同一状态的审计文本可复现、可比较。</zh-CN>
        ///   <en>Summarizes the degradation reason counts into stable non-sensitive text, returning <c>none</c> when nothing degraded. Keys are sorted so the audit text for the same state is reproducible and comparable.</en>
        /// </lang>
        /// </summary>
        /// <returns><l><zh-CN>形如 <c>NoModuleDefinition:2,NoAccessibleTab:1</c> 的汇总文本。</zh-CN><en>Summary text such as <c>NoModuleDefinition:2,NoAccessibleTab:1</c>.</en></l></returns>
        private string BuildDegradationReasonSummary()
        {
            if (degradationReasonCounts.Count == 0)
            {
                return "none";
            }

            IList<string> parts = new List<string>();
            foreach (string reasonCode in degradationReasonCounts.Keys.OrderBy(key => key, StringComparer.Ordinal))
            {
                parts.Add(reasonCode + ":" + degradationReasonCounts[reasonCode].ToString(CultureInfo.InvariantCulture));
            }

            return string.Join(",", parts.ToArray());
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析当前用户持有的受控权限键；门面缺失或解析异常时返回空集合，由数据层按 fail-closed 处理。</zh-CN>
        ///   <en>Resolves the controlled permission keys held by the current user; a missing facade or a resolution failure yields an empty set, which the data layer handles as fail-closed.</en>
        /// </lang>
        /// </summary>
        /// <param name="userName"><l><zh-CN>当前登录名；空白时返回空集合。</zh-CN><en>The current sign-in name; blank values yield an empty set.</en></l></param>
        /// <returns><l><zh-CN>权限键列表，可能为空但不会为空引用。</zh-CN><en>The permission-key list, which may be empty but is never a null reference.</en></l></returns>
        private IList<string> ResolveHeldPermissionKeys(string userName)
        {
            IList<string> keys = new List<string>();
            if (string.IsNullOrWhiteSpace(userName) || RolesDb == null)
            {
                return keys;
            }

            try
            {
                foreach (string key in RolesDb.GetPermissionKeysByUserName(userName))
                {
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        keys.Add(key);
                    }
                }
            }
            catch (Exception)
            {
                // <lang>
                //   <zh-CN>解析失败时退化为仅按本人指派查询，不扩大可见范围；异常细节不回显给用户。</zh-CN>
                //   <en>On resolution failure the query degrades to user-assigned items only and never widens visibility; exception details are not echoed to the user.</en>
                // </lang>
                return new List<string>();
            }

            return keys;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>在已读取的结果中筛选已超期条目；超期以到期 UTC 时间早于当前 UTC 时间判定。</zh-CN>
        ///   <en>Filters overdue rows from an already-read result; a row is overdue when its due UTC time precedes the current UTC time.</en>
        /// </lang>
        /// </summary>
        /// <param name="items"><l><zh-CN>已读取的待办投影列表。</zh-CN><en>The already-read work-item projection list.</en></l></param>
        /// <returns><l><zh-CN>仅含超期条目的列表。</zh-CN><en>A list containing only overdue rows.</en></l></returns>
        private static IList<PortalWorkItemInfo> FilterOverdue(IList<PortalWorkItemInfo> items)
        {
            IList<PortalWorkItemInfo> overdue = new List<PortalWorkItemInfo>();
            DateTime nowUtc = DateTime.UtcNow;
            foreach (PortalWorkItemInfo item in items)
            {
                if (item != null && item.DueUtc.HasValue && item.DueUtc.Value < nowUtc)
                {
                    overdue.Add(item);
                }
            }

            return overdue;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>行绑定：本地化业务类型、计算超期文本，并按可达性决定标题呈现为链接还是纯文本。</zh-CN>
        ///   <en>Row binding: localizes the business kind, computes the overdue wording, and decides from reachability whether the title renders as a link or as plain text.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender"><l><zh-CN>事件源。</zh-CN><en>The event source.</en></l></param>
        /// <param name="e"><l><zh-CN>重复器行事件参数。</zh-CN><en>The repeater row event arguments.</en></l></param>
        protected void WorkItemRepeater_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            PortalWorkItemInfo item = e.Item.DataItem as PortalWorkItemInfo;
            if (item == null)
            {
                return;
            }

            Label kindLabel = e.Item.FindControl("KindLabel") as Label;
            Label titleLabel = e.Item.FindControl("ItemTitleLabel") as Label;
            HyperLink itemLink = e.Item.FindControl("ItemLink") as HyperLink;
            Label unavailableLabel = e.Item.FindControl("UnavailableLabel") as Label;
            Label dueLabel = e.Item.FindControl("DueLabel") as Label;
            Label overdueLabel = e.Item.FindControl("OverdueLabel") as Label;

            if (kindLabel != null)
            {
                kindLabel.Text = LocalizeBusinessKind(item.BusinessKind);
            }

            if (titleLabel != null)
            {
                titleLabel.Text = Server.HtmlEncode(item.Title ?? string.Empty);
            }

            if (dueLabel != null)
            {
                dueLabel.Text = item.DueUtc.HasValue
                    ? item.DueUtc.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : GetResource("Common_NonePlaceholder");
            }

            // <lang>
            //   <zh-CN>超期除样式外必须带文本标记，使状态不依赖颜色传达（可访问性约束）。</zh-CN>
            //   <en>Overdue state must carry a text marker in addition to styling so the state does not rely on color alone (accessibility constraint).</en>
            // </lang>
            if (overdueLabel != null && item.DueUtc.HasValue && item.DueUtc.Value < DateTime.UtcNow)
            {
                TimeSpan overdue = DateTime.UtcNow - item.DueUtc.Value;
                int days = overdue.Days < 1 ? 1 : overdue.Days;
                overdueLabel.Text = " " + string.Format(
                    CultureInfo.CurrentCulture,
                    GetResource("MyWorkItems_OverdueDays"),
                    days.ToString(CultureInfo.InvariantCulture));
                overdueLabel.Visible = true;
            }

            // <lang>
            //   <zh-CN>T3 已确证：三类业务对象在前台均没有详情页，办理入口是后台 Admin 页，普通用户跳转会被拒绝；因此不渲染链接，改为呈现"暂无在线办理入口"提示。</zh-CN>
            //   <en>T3 is confirmed: none of the three business kinds has a front-end detail page; processing happens on administration pages that a normal user cannot open. No link is rendered and the "no online processing entry" hint is shown instead.</en>
            // </lang>
            string targetUrl = ResolveItemUrl(item);
            if (itemLink != null)
            {
                itemLink.Visible = !string.IsNullOrEmpty(targetUrl);
                if (itemLink.Visible)
                {
                    itemLink.NavigateUrl = targetUrl;
                    itemLink.Text = Server.HtmlEncode(item.Title ?? string.Empty);
                }
            }

            if (titleLabel != null)
            {
                titleLabel.Visible = string.IsNullOrEmpty(targetUrl);
            }

            // <lang>
            //   <zh-CN>不可达时给出明确提示而不是静默无链接，避免用户以为界面缺失；提示文本不含内部路径或异常细节。</zh-CN>
            //   <en>When unreachable, an explicit hint is shown instead of a silent missing link so users do not think the UI is broken; the hint carries no internal path or exception detail.</en>
            // </lang>
            if (unavailableLabel != null)
            {
                unavailableLabel.Visible = string.IsNullOrEmpty(targetUrl);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析待办对应业务对象的前台可达地址；解析落空或当前用户无权访问承载页签时返回空串，由标记层改为降级提示。</zh-CN>
        ///   <en>Resolves the front-end reachable URL of the business object behind a work item; when resolution falls through or the current user may not access the hosting tab it returns an empty string and the markup shows the degraded hint instead.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>P77.3 取代原 T3 判定（2026-09-29 的「三类业务在前台恒不可达、故恒返回空串」）。原判定只看到「没有可静态解析的前台深链」，**漏掉了运行期反查**：模块实例归属的页签本就登记在库中，可按「业务类型 → 能力键 → 主责模块包 → 已验证桌面入口 → 模块定义 → 模块实例 → 承载页签」逐环反查，再用当次请求的角色检查确认可达性（见 <see cref="PortalWorkItemTargetResolver"/>）。命中即产出地址，任一环落空即降级为提示；后台 <c>Admin/WorkItems.aspx</c> 的 Admin 映射与这里的普通用户可达性判定互不替代。</zh-CN>
        ///   <en>P77.3 supersedes the original T3 verdict (2026-09-29: every kind is unreachable in the front end, so always return an empty string). That verdict saw only that no statically resolvable front-end deep link exists and **missed the runtime reverse lookup**: the tab owning a module instance is already registered in the database, so the chain from business kind to capability key to primary module package to validated desktop entry to module definition to module instance to hosting tab can be walked, and the current request's role check then confirms reachability (see <see cref="PortalWorkItemTargetResolver"/>). A full pass yields a URL while any failed link degrades to the hint. The Admin mapping on <c>Admin/WorkItems.aspx</c> and this ordinary-user reachability decision do not substitute for each other.</en>
        /// </lang>
        /// </remarks>
        /// <param name="item"><l><zh-CN>待办投影；为 <c>null</c> 或业务类型未知时按降级处理。</zh-CN><en>The work-item projection; a <c>null</c> value or an unknown business kind degrades.</en></l></param>
        /// <returns><l><zh-CN>可导航的应用相对地址；不可达时为空串。</zh-CN><en>A navigable application-relative URL, or an empty string when unreachable.</en></l></returns>
        private string ResolveItemUrl(PortalWorkItemInfo item)
        {
            // <lang>
            //   <zh-CN>解析故障不得让整块待办变成错误页：任何异常都按"读取注册事实失败"降级，行仍以纯文本加速提示呈现。</zh-CN>
            //   <en>A resolution fault must not turn the whole to-do block into an error page: every exception degrades as "registration facts unreadable" and the row still renders as plain text plus a hint.</en>
            // </lang>
            try
            {
                PortalWorkItemTargetResolution resolution = PortalWorkItemTargetResolver.Resolve(
                    item == null ? null : item.BusinessKind,
                    targetSource);

                if (!resolution.IsResolved)
                {
                    degradedTargetCount++;
                    IncrementDegradationReason(resolution.ReasonCode);
                    return string.Empty;
                }

                resolvedTargetCount++;

                // <lang>
                //   <zh-CN>地址形状走共享构造点；<c>tabindex</c> 只影响目标页的匿名登录注入启发式，授权由 <c>tabid</c> 决定（P77.2 裁定 D2），因此这里取门户桌面列表下标并在取不到时退化为 0，绝不生成负下标。</zh-CN>
                //   <en>The URL shape comes from the shared construction point; <c>tabindex</c> only affects the target page's anonymous-login injection heuristic while authorization is decided by <c>tabid</c> (decision D2 from P77.2), so this reads the portal desktop-list index and degrades it to 0 when absent, never producing a negative subscript.</en>
                // </lang>
                return PortalDesktopTabUrl.Build(
                    Global.GetApplicationPath(Request),
                    ResolveTabIndex(resolution.Tab.TabId),
                    resolution.Tab.TabId);
            }
            catch (Exception)
            {
                degradedTargetCount++;
                IncrementDegradationReason(PortalWorkItemTargetResolver.ReasonResolutionFailed);
                return string.Empty;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>累加一个降级原因码的计数；空白原因码归入"读取注册事实失败"，避免统计出现空键。</zh-CN>
        ///   <en>Increments the count of one degradation reason code, filing a blank code under "registration facts unreadable" so the counters never gain an empty key.</en>
        /// </lang>
        /// </summary>
        /// <param name="reasonCode"><l><zh-CN>解析器原因码。</zh-CN><en>A resolver reason code.</en></l></param>
        private void IncrementDegradationReason(string reasonCode)
        {
            string key = string.IsNullOrEmpty(reasonCode)
                ? PortalWorkItemTargetResolver.ReasonSourceUnavailable
                : reasonCode;

            degradationReasonCounts[key] = degradationReasonCounts.ContainsKey(key)
                ? degradationReasonCounts[key] + 1
                : 1;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按门户桌面列表取页签下标，供地址中的 <c>tabindex</c> 使用。</zh-CN>
        ///   <en>Reads the tab index from the portal desktop list for the URL's <c>tabindex</c>.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabId"><l><zh-CN>页签标识。</zh-CN><en>The tab identifier.</en></l></param>
        /// <returns><l><zh-CN>非负下标；列表缺失或未命中时为 0。</zh-CN><en>A non-negative index, or 0 when the list is missing or the tab is not found.</en></l></returns>
        private int ResolveTabIndex(int tabId)
        {
            PortalSettings settings = PortalContext.GetPortalSettings(Context);
            if (settings == null || settings.DesktopTabs == null)
            {
                return 0;
            }

            int index = settings.DesktopTabs.FindIndex(tab => tab != null && tab.TabId == tabId);

            // <lang>
            //   <zh-CN>未命中时退化为 0 而不是 -1：目标页的 tabindex 参数按非负解析，负数没有意义，而授权真源是 tabid。</zh-CN>
            //   <en>Degrade to 0 rather than -1 when not found: the target page parses the tabindex parameter as non-negative, a negative value is meaningless, and the authorization source of truth is tabid.</en>
            // </lang>
            return index < 0 ? 0 : index;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把本模块已注入的数据门面与角色判定适配为解析器所需的最小端口。</zh-CN>
        ///   <en>Adapts the data facades and role check already injected into this module to the minimal port the resolver needs.</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>本类不新增数据访问，只做转发与一次绑定内的缓存：门面缺失时返回空集合或 <c>null</c>，使解析器按"未注册/未挂载"降级而不是抛错。</zh-CN>
        ///   <en>This class adds no data access; it only forwards and caches within one bind. A missing facade yields an empty collection or <c>null</c> so the resolver degrades as "not registered or not mounted" instead of throwing.</en>
        /// </lang>
        /// </remarks>
        private sealed class PortalModuleTargetSource : IPortalModuleTargetSource
        {
            /// <summary><l><zh-CN>宿主模块，用于读取已注入的门面。</zh-CN><en>The hosting module, used to read the injected facades.</en></l></summary>
            private readonly MyWorkItems owner;

            /// <summary><l><zh-CN>一次绑定内复用的"包标识 → 桌面入口"索引；未构建时为 <c>null</c>。</zh-CN><en>The package-to-desktop-entry index reused within one bind, or <c>null</c> before it is built.</en></l></summary>
            private IDictionary<string, string> desktopEntries;

            /// <summary><l><zh-CN>创建适配器。</zh-CN><en>Creates the adapter.</en></l></summary>
            /// <param name="owner"><l><zh-CN>宿主模块。</zh-CN><en>The hosting module.</en></l></param>
            internal PortalModuleTargetSource(MyWorkItems owner)
            {
                this.owner = owner;
            }

            /// <summary><l><zh-CN>按包标识读取已验证桌面入口；包不存在时为空串。</zh-CN><en>Reads the validated desktop entry by package identifier, or an empty string when absent.</en></l></summary>
            /// <param name="packageId"><l><zh-CN>模块包标识。</zh-CN><en>The module package identifier.</en></l></param>
            /// <returns><l><zh-CN>桌面入口虚拟路径或空串。</zh-CN><en>The desktop entry virtual path or an empty string.</en></l></returns>
            public string GetDesktopEntry(string packageId)
            {
                if (string.IsNullOrEmpty(packageId))
                {
                    return string.Empty;
                }

                string entry;
                return GetDesktopEntries().TryGetValue(packageId, out entry) ? entry : string.Empty;
            }

            /// <summary><l><zh-CN>读取全部模块定义；门面缺失时为空集合。</zh-CN><en>Reads all module definitions, or an empty collection when the facade is missing.</en></l></summary>
            /// <returns><l><zh-CN>模块定义集合。</zh-CN><en>The module-definition collection.</en></l></returns>
            public IEnumerable<IModuleDefinitionItem> GetModuleDefinitions()
            {
                IModuleDefsDb definitions = owner.ModuleDefinitionsConfig;
                if (definitions == null)
                {
                    return Enumerable.Empty<IModuleDefinitionItem>();
                }

                return definitions.GetModuleDefinitions() ?? Enumerable.Empty<IModuleDefinitionItem>();
            }

            /// <summary><l><zh-CN>读取引用指定定义的模块实例标识；门面缺失时为空集合。</zh-CN><en>Reads the module-instance identifiers referencing the definition, or an empty collection when the facade is missing.</en></l></summary>
            /// <param name="definitionId"><l><zh-CN>模块定义标识。</zh-CN><en>The module-definition identifier.</en></l></param>
            /// <returns><l><zh-CN>模块实例标识集合。</zh-CN><en>The module-instance identifier collection.</en></l></returns>
            public IEnumerable<int> GetModuleIdsByDefinitionId(int definitionId)
            {
                IModulesDb modules = owner.ModuleInstancesConfig;
                if (modules == null)
                {
                    return Enumerable.Empty<int>();
                }

                return modules.GetModulesByModuleDefId(definitionId) ?? Enumerable.Empty<int>();
            }

            /// <summary><l><zh-CN>按标识查找模块实例；门面缺失时为 <c>null</c>。</zh-CN><en>Finds a module instance by identifier, or <c>null</c> when the facade is missing.</en></l></summary>
            /// <param name="moduleId"><l><zh-CN>模块实例标识。</zh-CN><en>The module-instance identifier.</en></l></param>
            /// <returns><l><zh-CN>模块实例或 <c>null</c>。</zh-CN><en>The module instance or <c>null</c>.</en></l></returns>
            public IModuleItem FindModuleById(int moduleId)
            {
                IModulesDb modules = owner.ModuleInstancesConfig;
                return modules == null ? null : modules.FindModuleById(moduleId);
            }

            /// <summary><l><zh-CN>按标识查找页签；门面缺失时为 <c>null</c>。</zh-CN><en>Finds a tab by identifier, or <c>null</c> when the facade is missing.</en></l></summary>
            /// <param name="tabId"><l><zh-CN>页签标识。</zh-CN><en>The tab identifier.</en></l></param>
            /// <returns><l><zh-CN>页签或 <c>null</c>。</zh-CN><en>The tab or <c>null</c>.</en></l></returns>
            public ITabItem FindTabById(int tabId)
            {
                ITabsDb tabs = owner.TabsConfig;
                return tabs == null ? null : tabs.FindTabById(tabId);
            }

            /// <summary><l><zh-CN>按既有安全策略判定当前请求是否满足角色串（无请求上下文时为 <c>false</c>）。</zh-CN><en>Decides with the established security policy whether the current request satisfies the role string (<c>false</c> with no request context).</en></l></summary>
            /// <param name="accessRoles"><l><zh-CN>页签访问角色串。</zh-CN><en>The tab access-role string.</en></l></param>
            /// <returns><l><zh-CN>满足时为 <c>true</c>。</zh-CN><en><c>true</c> when satisfied.</en></l></returns>
            public bool IsInRoles(string accessRoles)
            {
                return PortalSecurity.IsInRoles(accessRoles);
            }

            /// <summary>
            /// <lang>
            ///   <zh-CN>构建并缓存"包标识 → 桌面入口"索引：部署清单的读取与校验有成本，不宜逐行重复。清单读取异常按"无可用入口"降级，使一次部署损坏只影响提示而不影响页面渲染。</zh-CN>
            ///   <en>Builds and caches the package-to-desktop-entry index, because reading and validating deployment manifests is costly and must not repeat per row. A manifest read failure degrades to "no entry available" so one broken deployment affects the hint only, not page rendering.</en>
            /// </lang>
            /// </summary>
            /// <returns><l><zh-CN>包标识到桌面入口的索引；可能为空但不会为 <c>null</c>。</zh-CN><en>The package-to-desktop-entry index, which may be empty but is never <c>null</c>.</en></l></returns>
            private IDictionary<string, string> GetDesktopEntries()
            {
                if (desktopEntries != null)
                {
                    return desktopEntries;
                }

                desktopEntries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                IList<PortalModulePackage> packages;
                try
                {
                    packages = PortalModuleCatalog.GetTrustedPackages();
                }
                catch (Exception)
                {
                    return desktopEntries;
                }

                if (packages == null)
                {
                    return desktopEntries;
                }

                foreach (PortalModulePackage package in packages)
                {
                    if (package == null || string.IsNullOrEmpty(package.PackageId))
                    {
                        continue;
                    }

                    desktopEntries[package.PackageId] = package.DesktopEntry ?? string.Empty;
                }

                return desktopEntries;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>把业务类型键本地化为展示文本；未知类型回退为编码后的原始键，不臆造译文。</zh-CN>
        ///   <en>Localizes a business-kind key into display text; an unknown kind falls back to the encoded raw key rather than a fabricated translation.</en>
        /// </lang>
        /// </summary>
        /// <param name="businessKind"><l><zh-CN>稳定的业务对象类型键。</zh-CN><en>The stable business-object kind key.</en></l></param>
        /// <returns><l><zh-CN>本地化后的类型文本。</zh-CN><en>The localized kind text.</en></l></returns>
        private string LocalizeBusinessKind(string businessKind)
        {
            if (string.Equals(businessKind, PortalWorkItemBusinessKinds.CollaborationItem, StringComparison.Ordinal))
            {
                return GetResource("MyWorkItems_KindCollaborationItem");
            }

            if (string.Equals(businessKind, PortalWorkItemBusinessKinds.BusinessApplication, StringComparison.Ordinal))
            {
                return GetResource("MyWorkItems_KindBusinessApplication");
            }

            if (string.Equals(businessKind, PortalWorkItemBusinessKinds.EmployeeProfileCorrectionRequest, StringComparison.Ordinal))
            {
                return GetResource("MyWorkItems_KindEmployeeProfileCorrectionRequest");
            }

            return Server.HtmlEncode(businessKind ?? string.Empty);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取全局资源文本；键缺失时回退为键名，保证界面仍可渲染且便于发现缺口。</zh-CN>
        ///   <en>Reads global resource text; a missing key falls back to the key name so the UI still renders and the gap is easy to spot.</en>
        /// </lang>
        /// </summary>
        /// <param name="key"><l><zh-CN>全局资源键。</zh-CN><en>The global resource key.</en></l></param>
        /// <returns><l><zh-CN>资源文本，或键名本身。</zh-CN><en>The resource text, or the key name itself.</en></l></returns>
        private string GetResource(string key)
        {
            object value = GetGlobalResourceObject("lang", key);
            return value == null ? key : value.ToString();
        }

        /// <summary><lang><zh-CN>把当前登录名解析为门户用户标识；无法解析时返回零。</zh-CN><en>Resolves the current sign-in name to a portal user identifier; returns zero when it cannot be resolved.</en></lang></summary>
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

        /// <summary><lang><zh-CN>读取当前登录名；未认证时返回空串。</zh-CN><en>Reads the current sign-in name; returns an empty string when unauthenticated.</en></lang></summary>
        private string GetCurrentUserName()
        {
            return IsCurrentUserAuthenticated() ? Context.User.Identity.Name : string.Empty;
        }

        /// <summary><lang><zh-CN>判断当前请求是否处于已认证状态。</zh-CN><en>Determines whether the current request is authenticated.</en></lang></summary>
        private bool IsCurrentUserAuthenticated()
        {
            return Context != null && Context.User != null && Context.User.Identity != null && Context.User.Identity.IsAuthenticated;
        }
    }
}
