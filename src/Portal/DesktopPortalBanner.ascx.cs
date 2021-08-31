using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;

namespace ASPNET.StarterKit.Portal
{
    public partial class DesktopPortalBanner : UserControl
    {
        protected string LogoffLink = "";
        public bool ShowTabs = true;
        public int tabIndex;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Obtain PortalSettings from Current Context
            var portalSettings = (PortalSettings) HttpContext.Current.Items["PortalSettings"];

            // Dynamically Populate the Portal Site Name
            siteName.Text = portalSettings.PortalName;

            // If user logged in, customize welcome message
            if (Request.IsAuthenticated)
            {
                WelcomeMessage.Text = "Welcome " + Context.User.Identity.Name + "! <" + "span class=Accent" + ">|<" +
                                      "/span" + ">";

                // if authentication mode is Cookie, provide a logoff link
                if (Context.User.Identity.AuthenticationType == "Forms")
                {
                    LogoffLink = "<" + "span class=\"Accent\">|</span>\n" + "<" + "a href=" +
                                 Global.GetApplicationPath(Request) + "/Admin/Logoff.aspx class=SiteLink> Logoff" + "<" +
                                 "/a>";
                }
            }

            // Dynamically render portal tab strip
            if (ShowTabs)
            {
                tabIndex = portalSettings.ActiveTab.TabIndex;

                // Build list of tabs to be shown to user                                   
                var authorizedTabs = new List<ITabItem>();
                int addedTabs = 0;

                for (int i = 0; i < portalSettings.DesktopTabs.Count; i++)
                {
                    ITabItem tab = portalSettings.DesktopTabs[i];

                    if (PortalSecurity.IsInRoles(tab.AccessRoles))
                    {
                        authorizedTabs.Add(tab);
                    }

                    if (addedTabs == tabIndex)
                    {
                        tabs.SelectedIndex = addedTabs;
                    }

                    addedTabs++;
                }

                // Populate Tab List at Top of the Page with authorized tabs
                tabs.DataSource = authorizedTabs;
                tabs.DataBind();
            }
        }
    }
}