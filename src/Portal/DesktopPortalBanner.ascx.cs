using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Resources;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>桌面门户顶部品牌区、用户区和 Tab 导航控件。</zh-CN>
    ///   <en>Desktop portal header control for the brand area, user area, and Tab navigation.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>P7.4 起标记结构从旧 table 横幅切换为现代 div 壳层，但仍使用服务器端 DataList 绑定已授权 Tab，以保持旧 WebForms 生命周期、权限判断和 URL 规则不变。</zh-CN>
    ///   <en>Starting with P7.4, the markup moves from the legacy table banner to a modern div shell, while still binding authorized Tabs with the server-side DataList so the legacy WebForms lifecycle, permission checks, and URL rules remain intact.</en>
    /// </lang>
    /// </remarks>
    public partial class DesktopPortalBanner : UserControl
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前请求需要输出的注销链接 HTML；未登录或非 Forms 身份时为空。</zh-CN>
        ///   <en>Logoff-link HTML for the current request; empty for anonymous users or non-Forms identities.</en>
        /// </lang>
        /// </summary>
        protected string LogoffLink = "";

        /// <summary>
        /// <lang>
        ///   <zh-CN>是否显示门户 Tab 导航。</zh-CN>
        ///   <en>Indicates whether portal Tab navigation should be rendered.</en>
        /// </lang>
        /// </summary>
        public bool ShowTabs = true;

        /// <summary>
        /// <lang>
        ///   <zh-CN>当前活动 Tab 的历史索引值，保留给旧页面/控件兼容。</zh-CN>
        ///   <en>Legacy index value for the active Tab, retained for compatibility with older pages and controls.</en>
        /// </lang>
        /// </summary>
        public int TabIndex;

        /// <summary>
        /// <lang>
        ///   <zh-CN>本次绑定中"已登记但被入口 gate 阻断"的 Tab 与其原因码，按 TabId 索引；仅在门控启用且当前用户为管理员时填充。</zh-CN>
        ///   <en>Tabs that are registered but blocked by the entry gate, keyed by TabId together with their reason code; populated only when the gate is enabled and the current user is an administrator.</en>
        /// </lang>
        /// </summary>
        private readonly Dictionary<int, string> tabBlockedReasons = new Dictionary<int, string>();

        /// <summary>
        /// <lang>
        ///   <zh-CN>加载站点名称、欢迎消息和当前用户可访问的 Tab 导航。</zh-CN>
        ///   <en>Loads the site name, welcome message, and Tab navigation available to the current user.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>顶栏依赖已在页面生命周期早期写入上下文的 PortalSettings；这里不重新构造配置，避免导航和模块区使用不同快照。</zh-CN>
            //   <en>The header depends on PortalSettings already written into context earlier in the page lifecycle; it does not rebuild settings here, avoiding divergent snapshots between navigation and module content.</en>
            // </lang>
            var portalSettings = PortalContext.GetPortalSettings();

            // <lang>
            //   <zh-CN>站点名称来自当前门户配置，主题层只负责外观，不覆盖这里的业务文本。</zh-CN>
            //   <en>The site name comes from the current Portal configuration; themes handle presentation only and do not override this business text.</en>
            // </lang>
            SiteName.Text = portalSettings.PortalName;

            // <lang>
            //   <zh-CN>登录区域兼容旧 Forms Authentication；匿名请求保持空白，避免主题布局展示伪登录状态。</zh-CN>
            //   <en>The user area remains compatible with legacy Forms Authentication; anonymous requests stay blank so themed layouts do not show a false sign-in state.</en>
            // </lang>
            if (Request.IsAuthenticated)
            {
                WelcomeMessage.Text = string.Format(DesktopBanner.WelcomeMessage, Context.User.Identity.Name);

                // <lang>
                //   <zh-CN>只有 Forms 身份才输出注销链接，Windows/外部身份模式由上游认证机制处理退出。</zh-CN>
                //   <en>Only Forms identities render the logoff link; Windows or external identity modes leave sign-out to the upstream authentication mechanism.</en>
                // </lang>
                if (Context.User.Identity.AuthenticationType == "Forms")
                {
                    string logoffUrl = HttpUtility.HtmlAttributeEncode(
                        Global.GetApplicationPath(Request) + "/Admin/Logoff.aspx");

                    // <lang>
                    //   <zh-CN>注销链接文案改为取自本地资源 DesktopBanner.resx，随界面语言切换；此前为硬编码英文字面量，中文界面下仍显示英文。文本按 HTML 编码后拼接，避免资源值污染标记。</zh-CN>
                    //   <en>The logoff link text now comes from the local resource DesktopBanner.resx and follows the UI language; it was previously a hardcoded English literal that stayed English on Chinese pages. The value is HTML-encoded before concatenation so resource text cannot corrupt the markup.</en>
                    // </lang>
                    LogoffLink = "<a href=\"" + logoffUrl + "\" class=\"SiteLink portal-toplink portal-logoff\">" +
                        HttpUtility.HtmlEncode(DesktopBanner.Logoff) + "</a>";
                }
            }

            // <lang>
            //   <zh-CN>Tab 导航按当前用户可访问角色过滤后再绑定，隐藏 Tab 不应参与显示索引计算。</zh-CN>
            //   <en>Tab navigation is filtered by roles before binding; hidden Tabs must not participate in display-index calculation.</en>
            // </lang>
            if (ShowTabs)
            {
                TabIndex = portalSettings.ActiveTab.TabIndex;

                // <lang>
                //   <zh-CN>第一阶段只做既有角色过滤：保持旧门户分号角色串判断的边界，后续细粒度权限扩展或 gate 都不能削弱它。</zh-CN>
                //   <en>Phase one performs the existing role filtering only: it preserves the legacy semicolon-delimited role boundary, which neither finer-grained permissions nor the gate may weaken.</en>
                // </lang>
                var roleFilteredTabs = new List<ITabItem>();
                for (int i = 0; i < portalSettings.DesktopTabs.Count; i++)
                {
                    ITabItem tab = portalSettings.DesktopTabs[i];

                    if (PortalSecurity.IsInRoles(tab.AccessRoles))
                    {
                        roleFilteredTabs.Add(tab);
                    }
                }

                // <lang>
                //   <zh-CN>ADR 0026 决策二：系统设置驱动的**展示层**过滤（显式键 / 前缀）在角色过滤之后、既有门控之前执行。
                //   它只决定"是否出现在导航"，不改变 AccessRoles 授权结论——直接访问被隐藏页签仍由页面授权判定。
                //   读取失败时读取器已退化为空集合，即"不隐藏任何页签"，因此这里不会把导航打空。</zh-CN>
                //   <en>ADR 0026 decision 2: system-setting-driven **presentation-layer** filtering (explicit keys / prefixes)
                //   runs after role filtering and before the existing gate. It decides only whether a tab appears in the
                //   navigation and never changes AccessRoles outcomes — a directly requested hidden tab is still authorized by
                //   its page. On read failure the reader already degrades to empty sets, meaning "hide nothing", so this step
                //   can never empty the navigation.</en>
                // </lang>
                ApplyNavigationSettingsFilter(roleFilteredTabs);

                // <lang>
                //   <zh-CN>门控开关默认关闭（配置缺失即关闭）；关闭时行为与改造前完全一致，这是出问题时的回滚点。</zh-CN>
                //   <en>The gate switch defaults to off (a missing configuration means off); while off the behaviour is identical to before the change, which is the rollback point if anything goes wrong.</en>
                // </lang>
                bool gateEnabled = PortalNavigationVisibilityPolicy.IsTabGateEnabled(
                    ConfigurationManager.AppSettings["Portal.Navigation.TabGateEnabled"]);

                // <lang>
                //   <zh-CN>授权后集合是最终绑定来源：要么是角色过滤结果本身（门控关闭），要么是叠加 gate 判定后的子集。</zh-CN>
                //   <en>The authorized collection is the final binding source: either the role-filtered result itself (gate off) or the subset remaining after the gate decision.</en>
                // </lang>
                var authorizedTabs = new List<ITabItem>();

                if (gateEnabled && roleFilteredTabs.Count > 0)
                {
                    // <lang>
                    //   <zh-CN>只有"已登记且键可构造"的 Tab 才交给 registry 判定；未登记 Tab 不参与门控，继续沿用角色过滤结果。</zh-CN>
                    //   <en>Only tabs that are registered and yield a buildable key go to the registry; unregistered tabs stay outside the gate and keep the role-filtered outcome.</en>
                    // </lang>
                    var registeredEntries = new Dictionary<int, PortalNavigationEntry>();
                    foreach (ITabItem tab in roleFilteredTabs)
                    {
                        PortalNavigationEntry registeredEntry = PortalNavigationRegistry.FindByKey(
                            PortalNavigationVisibilityPolicy.BuildTabEntryKey(tab.TabName, tab.NavigationKey));
                        if (registeredEntry != null)
                        {
                            registeredEntries[tab.TabId] = registeredEntry;
                        }
                    }

                    // <lang>
                    //   <zh-CN>上下文只组装一次，并与 Admin 动作区共用同一工厂，避免每个 Tab 重复探测权限与 Profile。</zh-CN>
                    //   <en>The context is assembled once through the same factory used by the Admin action area, avoiding repeated permission and Profile probing per tab.</en>
                    // </lang>
                    PortalNavigationVisibilityContext tabContext =
                        PortalNavigationVisibilityContextFactory.Create(registeredEntries.Values);

                    foreach (ITabItem tab in roleFilteredTabs)
                    {
                        PortalNavigationEntry registeredEntry;
                        if (registeredEntries.TryGetValue(tab.TabId, out registeredEntry))
                        {
                            string blockedReason = PortalNavigationVisibilityPolicy.GetBlockedReason(registeredEntry, tabContext);
                            if (!string.IsNullOrEmpty(blockedReason))
                            {
                                // <lang>
                                //   <zh-CN>普通用户看不到被阻断项（不渲染、不留占位）；管理员保留位置并看到禁用态与原因，与动作区既有口径一致。</zh-CN>
                                //   <en>Regular users never see a blocked tab (it is neither rendered nor left as a placeholder); administrators keep the position and see the disabled state with its reason, matching the existing action-area convention.</en>
                                // </lang>
                                if (!tabContext.IsAdministrator)
                                {
                                    continue;
                                }

                                tabBlockedReasons[tab.TabId] = blockedReason;
                            }
                        }

                        authorizedTabs.Add(tab);
                    }
                }
                else
                {
                    authorizedTabs.AddRange(roleFilteredTabs);
                }

                // <lang>
                //   <zh-CN>选中索引必须基于最终集合计算，否则被移除的 Tab 会导致高亮错位。</zh-CN>
                //   <en>The selected index must be computed from the final collection, otherwise a removed tab would shift the highlight.</en>
                // </lang>
                for (int i = 0; i < authorizedTabs.Count; i++)
                {
                    if (authorizedTabs[i].TabId == portalSettings.ActiveTab.TabId)
                    {
                        Tabs.SelectedIndex = i;
                    }
                }

                // <lang>
                //   <zh-CN>最终只绑定授权后的 Tab；主题 CSS 可以改变排列方式，但不应改变这里的角色过滤与 gate 判定结果。</zh-CN>
                //   <en>Bind only authorized tabs at the end; theme CSS may change layout but must not alter the role-filtered or gated result.</en>
                // </lang>
                Tabs.DataSource = authorizedTabs;
                Tabs.DataBind();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>页签名解析的降级原因；仅 JSON 列损坏时非空，供诊断记录使用，不参与渲染。</zh-CN>
        ///   <en>Fallback reason of the tab-name resolution; non-blank only when the JSON column is damaged, used for diagnostics and never for rendering.</en>
        /// </lang>
        /// </summary>
        private string tabNameDiagnostic;

        /// <summary>
        /// <lang>
        ///   <zh-CN>构造 Tab 导航地址，供标记层数据绑定使用。</zh-CN>
        ///   <en>Builds a tab navigation URL for markup data binding.</en>
        /// </lang>
        /// </summary>
        /// <param name="itemIndex">
        /// <l>
        ///   <zh-CN>最终集合中的显示下标。</zh-CN>
        ///   <en>Display index within the final collection.</en>
        /// </l>
        /// </param>
        /// <param name="tab">
        /// <l>
        ///   <zh-CN>当前 Tab；为 null 时返回空字符串。</zh-CN>
        ///   <en>Current tab; returns an empty string when null.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>应用相对的可导航地址；数值使用固定区域性格式化，避免本地化分隔符进入 URL。</zh-CN>
        ///   <en>Application-relative navigable URL; numbers use invariant formatting so localized separators never enter the URL.</en>
        /// </l>
        /// </returns>
        protected string BuildTabUrl(int itemIndex, ITabItem tab)
        {
            // <lang>
            //   <zh-CN>Tab 缺失时不生成地址，由调用方保持空白链接而不是猜测目标。</zh-CN>
            //   <en>When the tab is missing no URL is produced: the caller keeps a blank link instead of guessing a target.</en>
            // </lang>
            if (tab == null)
            {
                return string.Empty;
            }

            // <lang>
            //   <zh-CN>地址形状改由共享构造点提供（P77.3 收敛）：此前页签导航与待办深链各自拼接同一形状，存在随时间漂移的风险。</zh-CN>
            //   <en>The URL shape now comes from the shared construction point (converged in P77.3): tab navigation and work-item deep links had each assembled the same shape, which risked drifting apart over time.</en>
            // </lang>
            return PortalDesktopTabUrl.Build(Global.GetApplicationPath(Request), itemIndex, tab.TabId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按绑定结果切换 Tab 项形态：被 gate 阻断的项由管理员看到禁用态与原因，其余情况保持普通链接。</zh-CN>
        ///   <en>Switches a tab item between its forms according to the bound result: a gate-blocked item shows a disabled state with its reason to administrators while every other case stays a normal link.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据；非数据项行（页眉、页脚）直接忽略。</zh-CN>
        ///   <en>Event data; non-item rows (header, footer) are ignored.</en>
        /// </l>
        /// </param>
        protected void Tabs_ItemDataBound(object sender, DataListItemEventArgs e)
        {
            // <lang>
            //   <zh-CN>只有普通与交替数据项承载 Tab；其它行没有可切换的禁用态，直接返回。</zh-CN>
            //   <en>Only regular and alternating item rows carry a tab; every other row has no disabled state to switch, so return immediately.</en>
            // </lang>
            if (e == null || e.Item == null ||
                (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem))
            {
                return;
            }

            // <lang>
            //   <zh-CN>取出本行绑定的 Tab；类型不符时不做任何 DOM 改动。</zh-CN>
            //   <en>Take the tab bound to this row; when the type does not match, no DOM change is made.</en>
            // </lang>
            ITabItem boundTab = e.Item.DataItem as ITabItem;
            string blockedReason;

            // <lang>
            //   <zh-CN>未登记阻断原因的项保持模板默认形态（普通链接 + 隐藏禁用元素），因此门控关闭时不会出现任何多余输出。</zh-CN>
            //   <en>A tab without a recorded blocked reason keeps the template default (normal link plus hidden disabled element), so nothing extra is emitted while the gate is off.</en>
            // </lang>
            if (boundTab == null || !tabBlockedReasons.TryGetValue(boundTab.TabId, out blockedReason))
            {
                return;
            }

            // <lang>
            //   <zh-CN>链接与禁用元素都在模板内，切换二者可见性即可保持原有排列位置与主题类语义。</zh-CN>
            //   <en>Both the link and the disabled element live inside the template, so switching their visibility keeps the original position and the theme class semantics.</en>
            // </lang>
            HyperLink tabLink = e.Item.FindControl("TabLink") as HyperLink;
            Label disabledTab = e.Item.FindControl("TabDisabled") as Label;

            if (tabLink != null)
            {
                tabLink.Visible = false;
            }

            if (disabledTab != null)
            {
                disabledTab.Visible = true;
                disabledTab.Text = BuildTabDisplayName(boundTab);

                // <lang>
                //   <zh-CN>原因码只进入工具提示，不成为可见文案，避免内部诊断信息变成普通界面文本。
                //   提示文本同样用本地化名，使管理员看到的名称与界面一致。</zh-CN>
                //   <en>The reason code only goes into the tooltip and never becomes visible text, keeping internal diagnostics out of the ordinary interface copy.
                //   The tooltip uses the localized name as well so administrators see the same label the interface shows.</en>
                // </lang>
                disabledTab.ToolTip = BuildTabDisplayName(boundTab) + " (" + blockedReason + ")";
                disabledTab.Attributes["aria-disabled"] = "true";
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按系统设置移除导航页签：先判显式隐藏键，再判前缀排除。</zh-CN>
        ///   <en>Removes navigation tabs per system settings: explicit hidden keys first, then excluded prefixes.</en>
        /// </lang>
        /// </summary>
        /// <param name="tabs">
        /// <l>
        ///   <zh-CN>已通过角色过滤的可变页签集合；方法就地移除命中项。</zh-CN>
        ///   <en>Mutable role-filtered tab collection; matching items are removed in place.</en>
        /// </l>
        /// </param>
        private void ApplyNavigationSettingsFilter(List<ITabItem> tabs)
        {
            // <lang>
            //   <zh-CN>三个配置项都来自已登记的系统设置定义（DB 覆盖 → appSettings → 默认）；读取失败只会得到空集合或默认值。</zh-CN>
            //   <en>All three come from registered system-setting definitions (database override → appSettings → default); a read failure yields only empty sets or defaults.</en>
            // </lang>
            IList<string> hiddenKeys = PortalNavigationSettings.HiddenNavigationKeys(Context);
            IList<string> excludedPrefixes = PortalNavigationSettings.ExcludedNavigationKeyPrefixes(Context);
            bool logHidden = PortalNavigationSettings.LogHiddenTabs(Context);

            // <lang>
            //   <zh-CN>ADR 0027：业务功能入口**只对匿名隐藏**——登录用户看得见、匿名看不见。
            //   若对所有人隐藏，登录用户就找不到功能；若对所有人放开，匿名会点进"模块因未认证而自隐"的空白页。
            //   因此这里先判定当前请求是否已认证，再决定这份清单是否生效。</zh-CN>
            //   <en>ADR 0027: business-capability entries are hidden **from anonymous visitors only** — visible to signed-in users,
            //   invisible to anonymous ones. Hiding them from everyone leaves signed-in users unable to find the capability, while
            //   showing them to everyone lets anonymous visitors land on a page whose module self-hides because it is unauthenticated.
            //   This therefore decides whether the list applies based on whether the current request is authenticated.</en>
            // </lang>
            bool isAuthenticated = Context.User != null
                && Context.User.Identity != null
                && Context.User.Identity.IsAuthenticated;

            IList<string> anonymousHiddenKeys = new string[0];
            if (!isAuthenticated)
            {
                anonymousHiddenKeys = PortalNavigationSettings.AnonymousHiddenNavigationKeys(Context);
            }

            // <lang>
            //   <zh-CN>没有任何配置时立即返回：门控关闭场景下的导航输出与改造前逐字节一致，这是回滚点。</zh-CN>
            //   <en>Return immediately when nothing is configured: with the gate off the navigation output stays byte-for-byte identical to before the change, which is the rollback point.</en>
            // </lang>
            if (hiddenKeys.Count == 0 && excludedPrefixes.Count == 0 && anonymousHiddenKeys.Count == 0)
            {
                return;
            }

            // <lang>
            //   <zh-CN>命中项先收集到独立集合，遍历结束后再统一移除，避免在枚举过程中修改原集合。</zh-CN>
            //   <en>Collect matches in a separate collection and remove them after enumeration, avoiding modification of the source collection while iterating.</en>
            // </lang>
            List<ITabItem> toRemove = new List<ITabItem>();

            foreach (ITabItem tab in tabs)
            {
                // <lang>
                //   <zh-CN>稳定键优先取 NavigationKey；为空时前缀判定回退 TabName、显式键判定回退 TabId，与 ADR 契约一致。</zh-CN>
                //   <en>The stable key prefers NavigationKey; when blank, prefix matching falls back to TabName and explicit-key matching falls back to TabId, per the ADR contract.</en>
                // </lang>
                string navigationKey = (tab.NavigationKey ?? string.Empty).Trim();
                string tabName = (tab.TabName ?? string.Empty).Trim();
                string tabIdText = tab.TabId.ToString(CultureInfo.InvariantCulture);
                string hiddenReason = string.Empty;

                // <lang>
                //   <zh-CN>显式隐藏优先于前缀排除：显式配置是"点名"，语义应压过"按类隐藏"。</zh-CN>
                //   <en>Explicit hiding outranks prefix exclusion: an explicit entry names a tab and is more specific than class-based hiding.</en>
                // </lang>
                for (int keyIndex = 0; keyIndex < hiddenKeys.Count; keyIndex++)
                {
                    // <lang>
                    //   <zh-CN>逐项修剪；空项被视为无效配置直接跳过，避免空串匹配到所有页签。</zh-CN>
                    //   <en>Trim each entry; blank items are treated as invalid and skipped so an empty string cannot match every tab.</en>
                    // </lang>
                    string candidate = (hiddenKeys[keyIndex] ?? string.Empty).Trim();
                    if (candidate.Length == 0)
                    {
                        continue;
                    }

                    // <lang>
                    //   <zh-CN>NavigationKey 存在时只按稳定键匹配；为空时才回退 TabId，避免双重匹配语义含混。</zh-CN>
                    //   <en>When NavigationKey exists, match the stable key only; fall back to TabId only when it is blank, avoiding ambiguous dual matching.</en>
                    // </lang>
                    bool matched = navigationKey.Length > 0
                        ? string.Equals(candidate, navigationKey, StringComparison.OrdinalIgnoreCase)
                        : string.Equals(candidate, tabIdText, StringComparison.OrdinalIgnoreCase);

                    if (matched)
                    {
                        hiddenReason = "Navigation.HiddenNavigationKeys";
                        break;
                    }
                }

                // <lang>
                //   <zh-CN>前缀排除仅在前一步未命中时判定；前缀为空同样跳过。</zh-CN>
                //   <en>Prefix exclusion is evaluated only when the previous step did not match; blank prefixes are likewise skipped.</en>
                // </lang>
                if (hiddenReason.Length == 0)
                {
                    string prefixTarget = navigationKey.Length > 0 ? navigationKey : tabName;
                    if (prefixTarget.Length > 0)
                    {
                        for (int prefixIndex = 0; prefixIndex < excludedPrefixes.Count; prefixIndex++)
                        {
                            // <lang>
                            //   <zh-CN>逐项修剪并按不区分大小写匹配前缀；命中即记录原因并停止。</zh-CN>
                            //   <en>Trim each item and match the prefix case-insensitively; on a hit, record the reason and stop.</en>
                            // </lang>
                            string prefix = (excludedPrefixes[prefixIndex] ?? string.Empty).Trim();
                            if (prefix.Length == 0)
                            {
                                continue;
                            }

                            if (prefixTarget.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            {
                                hiddenReason = "Navigation.ExcludedNavigationKeyPrefixes";
                                break;
                            }
                        }
                    }
                }

                // <lang>
                //   <zh-CN>仅对匿名隐藏的清单：登录请求下 `anonymousHiddenKeys` 恒为空集合，故本分支不会作用于已登录用户。</zh-CN>
                //   <en>The anonymous-only list: for an authenticated request `anonymousHiddenKeys` is always empty, so this branch
                //   never applies to signed-in users.</en>
                // </lang>
                if (hiddenReason.Length == 0 && anonymousHiddenKeys.Count > 0)
                {
                    string keyTarget = navigationKey.Length > 0 ? navigationKey : tabIdText;
                    for (int anonymousIndex = 0; anonymousIndex < anonymousHiddenKeys.Count; anonymousIndex++)
                    {
                        string candidate = (anonymousHiddenKeys[anonymousIndex] ?? string.Empty).Trim();
                        if (candidate.Length == 0)
                        {
                            continue;
                        }

                        if (string.Equals(candidate, keyTarget, StringComparison.OrdinalIgnoreCase))
                        {
                            hiddenReason = "Navigation.AnonymousHiddenNavigationKeys";
                            break;
                        }
                    }
                }

                if (hiddenReason.Length > 0)
                {
                    toRemove.Add(tab);

                    // <lang>
                    //   <zh-CN>诊断只包含页签 ID、稳定键（或回退名）与原因码，不含用户身份、请求参数或配置原文。</zh-CN>
                    //   <en>Diagnostics contain only the tab id, stable key (or fallback name), and reason code — no user identity, request parameters, or raw configuration.</en>
                    // </lang>
                    if (logHidden)
                    {
                        PortalDiagnostics.Warn(
                            "Navigation.TabHidden",
                            "Tab " + tabIdText + " (" + (navigationKey.Length > 0 ? navigationKey : tabName) + ") hidden by " + hiddenReason + ".");
                    }
                }
            }

            // <lang>
            //   <zh-CN>统一移除命中项；未命中的页签保持原有相对顺序，避免因过滤产生位置漂移。</zh-CN>
            //   <en>Remove matched items in one pass; unmatched tabs keep their relative order so filtering never causes position drift.</en>
            // </lang>
            for (int removeIndex = 0; removeIndex < toRemove.Count; removeIndex++)
            {
                tabs.Remove(toRemove[removeIndex]);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按当前界面文化解析页签显示名（中文列 / 英文列 / 其它语言 JSON 列）。</zh-CN>
        ///   <en>Resolves the tab display name by the current UI culture (Chinese column / English column / other-languages JSON column).</en>
        /// </lang>
        /// </summary>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>页签名本地化前，这一行就是 `boundTab.TabName`；本地化后必须经解析器，否则英文界面会显示中文页签名。
        ///   解析器内部降级到中文列，因此这里不再判空 —— 但**门控键不走本方法**，键仍由 `NavigationKey` 派生，
        ///   否则"同一个页签在不同语言下键不同"会让 registry 失配。</zh-CN>
        ///   <en>Before tab names were localized this line was simply `boundTab.TabName`; afterwards it must go through the resolver, or an English UI would show Chinese tab names. The resolver degrades to the Chinese column internally, so no blank check is needed here — but the **gate key never goes through this method**: the key still derives from `NavigationKey`, otherwise "the same tab having different keys per language" would break registry matching.</en>
        /// </lang>
        /// </remarks>
        /// <param name="tab">
        /// <l>
        ///   <zh-CN>当前绑定的页签。</zh-CN>
        ///   <en>Currently bound tab.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>该界面文化下的页签显示名。</zh-CN>
        ///   <en>Tab display name for the current UI culture.</en>
        /// </l>
        /// </returns>
        protected string BuildTabDisplayName(ITabItem tab)
        {
            if (tab == null)
            {
                return string.Empty;
            }

            // <lang>
            //   <zh-CN>JSON 列损坏时记 Warn：页签名仍能显示，但脏数据要被发现。缺翻译不记录，避免噪声。</zh-CN>
            //   <en>Record a Warn when the JSON column is damaged: the tab name still renders, but dirty data must be surfaced. A missing translation is not recorded, to avoid noise.</en>
            // </lang>
            string displayName = PortalDisplayNameResolver.Resolve(
                tab.TabName,
                tab.TabNameEn,
                tab.TabNameLocalizedJson,
                CultureInfo.CurrentUICulture,
                out tabNameDiagnostic);

            if (!string.IsNullOrEmpty(tabNameDiagnostic))
            {
                PortalDiagnostics.Warn("TabName", tabNameDiagnostic);
            }

            return displayName;
        }
    }
}
