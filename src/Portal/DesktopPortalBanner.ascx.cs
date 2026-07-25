using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
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
                    LogoffLink = "<a href=\"" + logoffUrl + "\" class=\"SiteLink portal-toplink portal-logoff\">Logoff</a>";
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
                //   <zh-CN>单独构建授权后集合，既保留旧 DataList 绑定方式，也让现代主题布局可以直接复用同一组导航项。</zh-CN>
                //   <en>Build a separate authorized collection so the legacy DataList binding remains intact while modern themed layouts can reuse the same navigation set.</en>
                // </lang>
                var authorizedTabs = new List<ITabItem>();
                for (int i = 0; i < portalSettings.DesktopTabs.Count; i++)
                {
                    ITabItem tab = portalSettings.DesktopTabs[i];

                    // <lang>
                    //   <zh-CN>这里沿用旧门户分号角色串判断；后续细粒度权限扩展不能绕过此处的基本 Tab 可见性边界。</zh-CN>
                    //   <en>This keeps the legacy semicolon-delimited role check; later fine-grained permissions must not bypass the basic Tab visibility boundary here.</en>
                    // </lang>
                    if (PortalSecurity.IsInRoles(tab.AccessRoles))
                    {
                        authorizedTabs.Add(tab);

                        // <lang>
                        //   <zh-CN>选中索引必须基于授权后的导航集合，否则隐藏 Tab 会导致高亮错位。</zh-CN>
                        //   <en>Selected index must be based on the authorized navigation set; hidden Tabs would otherwise shift the highlight.</en>
                        // </lang>
                        if (tab.TabId == portalSettings.ActiveTab.TabId)
                        {
                            Tabs.SelectedIndex = authorizedTabs.Count - 1;
                        }
                    }
                }

                // <lang>
                //   <zh-CN>最终只绑定授权后的 Tab；主题 CSS 可以改变排列方式，但不应改变这里的权限过滤结果。</zh-CN>
                //   <en>Bind only authorized Tabs at the end; theme CSS may change layout but must not alter the authorization-filtered result.</en>
                // </lang>
                Tabs.DataSource = authorizedTabs;
                Tabs.DataBind();
            }
        }
    }
}
