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
                            PortalNavigationVisibilityPolicy.BuildTabEntryKey(tab.TabName));
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

            return Global.GetApplicationPath(Request) + "/DesktopDefault.aspx?tabindex=" +
                itemIndex.ToString(CultureInfo.InvariantCulture) + "&tabid=" +
                tab.TabId.ToString(CultureInfo.InvariantCulture);
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
                disabledTab.Text = boundTab.TabName;

                // <lang>
                //   <zh-CN>原因码只进入工具提示，不成为可见文案，避免内部诊断信息变成普通界面文本。</zh-CN>
                //   <en>The reason code only goes into the tooltip and never becomes visible text, keeping internal diagnostics out of the ordinary interface copy.</en>
                // </lang>
                disabledTab.ToolTip = boundTab.TabName + " (" + blockedReason + ")";
                disabledTab.Attributes["aria-disabled"] = "true";
            }
        }
    }
}
