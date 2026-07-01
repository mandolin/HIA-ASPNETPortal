using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using Resources;

namespace ASPNET.StarterKit.Portal
{
    public partial class DesktopPortalBanner : UserControl
    {
        // Logoff 链接
        protected string LogoffLink = "";

        // 是否显示选项卡
        public bool ShowTabs = true;

        // 当前选项卡索引
        public int TabIndex;

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
                    LogoffLink = "<span class=\"Accent\">|</span>\n" +
                                 "<a href=" + Global.GetApplicationPath(Request) + "/Admin/Logoff.aspx class=SiteLink> Logoff" +
                                 "</a>";
                }
            }

            // 动态渲染门户选项卡条
            if (ShowTabs)
            {
                TabIndex = portalSettings.ActiveTab.TabIndex;

                // 构建要显示给用户的选项卡列表
                var authorizedTabs = new List<ITabItem>();
                int addedTabs = 0;

                for (int i = 0; i < portalSettings.DesktopTabs.Count; i++)
                {
                    ITabItem tab = portalSettings.DesktopTabs[i];

                    // 检查用户是否在允许的角色中
                    if (PortalSecurity.IsInRoles(tab.AccessRoles))
                    {
                        authorizedTabs.Add(tab);
                    }

                    // 如果当前选项卡是活动选项卡，设置选中索引
                    if (addedTabs == TabIndex)
                    {
                        Tabs.SelectedIndex = addedTabs;
                    }

                    addedTabs++;
                }

                // 在页面顶部填充具有授权的选项卡列表
                Tabs.DataSource = authorizedTabs;
                Tabs.DataBind();
            }
        }
    }
}
