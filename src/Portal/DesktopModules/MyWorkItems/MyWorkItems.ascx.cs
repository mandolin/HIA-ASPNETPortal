using System;
using System.Collections.Generic;
using System.Globalization;
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
            if (!PortalAuthorization.HasAnyPermission(PortalPermissionKeys.BusinessWorkItemsView))
            {
                ContentPanel.Visible = false;
                FailurePanel.Visible = false;
                PortalOperationAudit.Record(
                    PortalOperationAuditEvents.BusinessModuleCategory,
                    "WorkItemsViewed",
                    "WorkItem",
                    GetCurrentUserId().ToString(CultureInfo.InvariantCulture),
                    "My work items view denied. MissingPermission=" + PortalPermissionKeys.BusinessWorkItemsView,
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
                    "WorkItemsViewed",
                    "WorkItem",
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
            WorkItemRepeater.DataBind();
            EmptyPanel.Visible = items.Count == 0;
            EmptyLabel.Text = ShowOverdueOnly ? GetResource("MyWorkItems_EmptyNoMatch") : GetResource("MyWorkItems_EmptyNone");

            // <lang>
            //   <zh-CN>查看与筛选只记录条数与筛选态，不写标题或业务正文，避免审计承载业务数据。</zh-CN>
            //   <en>Viewing and filtering record only the row count and filter state; titles and domain content are never written so the audit does not carry business data.</en>
            // </lang>
            PortalOperationAudit.Record(
                PortalOperationAuditEvents.BusinessModuleCategory,
                "WorkItemsViewed",
                "WorkItem",
                userId.ToString(CultureInfo.InvariantCulture),
                "My work items viewed. Count=" + items.Count.ToString(CultureInfo.InvariantCulture) +
                "; OverdueOnly=" + ShowOverdueOnly.ToString(CultureInfo.InvariantCulture),
                Context);
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
                    : "—";
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
            //   <zh-CN>前台可达目标尚未确证（T3），因此当前一律不渲染跳转链接；确证映射后再启用链接与"不可达"提示。</zh-CN>
            //   <en>Front-end reachable targets are not yet confirmed (T3), so no navigation link is rendered for now; links and the unreachable hint are enabled after the mapping is confirmed.</en>
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

            if (unavailableLabel != null)
            {
                unavailableLabel.Visible = false;
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析待办对应业务对象的前台可达地址；当前返回空串，待 T3 确证映射后填充。</zh-CN>
        ///   <en>Resolves the front-end reachable URL of the business object behind a work item; it currently returns an empty string pending the T3 mapping confirmation.</en>
        /// </lang>
        /// </summary>
        /// <param name="item"><l><zh-CN>待办投影。</zh-CN><en>The work-item projection.</en></l></param>
        /// <returns><l><zh-CN>可达的相对地址；未确证或不可达时为空串。</zh-CN><en>A reachable relative URL, or an empty string when unconfirmed or unreachable.</en></l></returns>
        private static string ResolveItemUrl(PortalWorkItemInfo item)
        {
            return string.Empty;
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
