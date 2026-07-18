using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using Resources;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：桌面门户顶部品牌区、用户区和 Tab 导航控件。
    ///
    /// English: Desktop portal header control for the brand area, user area, and Tab navigation.
    /// </summary>
    /// <remarks>
    /// 中文：P7.4 起标记结构从旧 table 横幅切换为现代 div 壳层，但仍使用服务器端 DataList 绑定已授权 Tab，
    /// 以保持旧 WebForms 生命周期、权限判断和 URL 规则不变。
    ///
    /// English: Starting with P7.4, the markup moves from the legacy table banner to a modern div shell, while still
    /// binding authorized Tabs with the server-side DataList so the legacy WebForms lifecycle, permission checks, and URL
    /// rules remain intact.
    /// </remarks>
    public partial class DesktopPortalBanner : UserControl
    {
        /// <summary>
        /// 中文：当前请求需要输出的注销链接 HTML；未登录或非 Forms 身份时为空。
        ///
        /// English: Logoff-link HTML for the current request; empty for anonymous users or non-Forms identities.
        /// </summary>
        protected string LogoffLink = "";

        /// <summary>
        /// 中文：是否显示门户 Tab 导航。
        ///
        /// English: Indicates whether portal Tab navigation should be rendered.
        /// </summary>
        public bool ShowTabs = true;

        /// <summary>
        /// 中文：当前活动 Tab 的历史索引值，保留给旧页面/控件兼容。
        ///
        /// English: Legacy index value for the active Tab, retained for compatibility with older pages and controls.
        /// </summary>
        public int TabIndex;

        /// <summary>
        /// 中文：加载站点名称、欢迎消息和当前用户可访问的 Tab 导航。
        ///
        /// English: Loads the site name, welcome message, and Tab navigation available to the current user.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // 从当前上下文中获取 PortalSettings
            var portalSettings = PortalContext.GetPortalSettings();

            // 动态填充门户站点名称
            SiteName.Text = portalSettings.PortalName;

            // 如果用户已登录，定制欢迎消息
            if (Request.IsAuthenticated)
            {
                //lang.
                
                WelcomeMessage.Text = string.Format(DesktopBanner.WelcomeMessage, Context.User.Identity.Name);

                // 如果身份验证模式是 Cookie，提供注销链接
                if (Context.User.Identity.AuthenticationType == "Forms")
                {
                    string logoffUrl = HttpUtility.HtmlAttributeEncode(
                        Global.GetApplicationPath(Request) + "/Admin/Logoff.aspx");
                    LogoffLink = "<a href=\"" + logoffUrl + "\" class=\"SiteLink portal-toplink portal-logoff\">Logoff</a>";
                }
            }

            // 动态渲染门户选项卡条
            if (ShowTabs)
            {
                TabIndex = portalSettings.ActiveTab.TabIndex;

                // 构建要显示给用户的选项卡列表
                var authorizedTabs = new List<ITabItem>();
                for (int i = 0; i < portalSettings.DesktopTabs.Count; i++)
                {
                    ITabItem tab = portalSettings.DesktopTabs[i];

                    // 检查用户是否在允许的角色中
                    if (PortalSecurity.IsInRoles(tab.AccessRoles))
                    {
                        authorizedTabs.Add(tab);

                        // 选中索引必须基于授权后的导航集合，否则隐藏 Tab 会导致高亮错位。
                        // Selected index must be based on the authorized navigation set; hidden Tabs would otherwise shift the highlight.
                        if (tab.TabId == portalSettings.ActiveTab.TabId)
                        {
                            Tabs.SelectedIndex = authorizedTabs.Count - 1;
                        }
                    }
                }

                // 在页面顶部填充具有授权的选项卡列表
                Tabs.DataSource = authorizedTabs;
                Tabs.DataBind();
            }
        }
    }
}
