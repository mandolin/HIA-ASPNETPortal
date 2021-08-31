using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class Tabs : PortalModuleControl<Tabs>
    {
        protected readonly List<TabSettings> PortalTabs = new List<TabSettings>();
        private int _tabId;
        private int _tabIndex;

        [Dependency]
        public IModuleDefsDb ModuleDefConfig { private get; set; }

        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        [Dependency]
        public ITabsDb TabsConfig { private get; set; }

        [Dependency]
        public IGlobalsDb PortalConfig { private get; set; }


        //*******************************************************
        //
        // The Page_Load server event handler on this user control is used
        // to populate the current tab settings from the database
        //
        //*******************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            // Verify that the current user has access to access this page
            if (PortalSecurity.IsInRoles("Admins") == false)
            {
                Response.Redirect("~/Admin/EditAccessDenied.aspx");
            }

            if (Request.Params["tabid"] != null)
            {
                _tabId = Int32.Parse(Request.Params["tabid"]);
            }
            if (Request.Params["tabindex"] != null)
            {
                _tabIndex = Int32.Parse(Request.Params["tabindex"]);
            }

            // Obtain PortalSettings from Current Context
            var portalSettings = (PortalSettings) Context.Items["PortalSettings"];

            foreach (ITabItem tab in portalSettings.DesktopTabs)
            {
                PortalTabs.Add(new TabSettings(tab));
            }

            // Give the admin tab a big sort order number, to ensure it's
            // always at the end
            TabSettings adminTab = PortalTabs[PortalTabs.Count - 1];
            adminTab.TabOrder = 99999;

            // If this is the first visit to the page, bind the tab data to the page listbox
            if (Page.IsPostBack == false)
            {
                tabList.DataBind();
            }
        }

        //*******************************************************
        //
        // The UpDown_Click server event handler on this page is
        // used to move a portal module up or down on a tab's layout pane
        //
        //*******************************************************

        protected void UpDown_Click(Object sender, ImageClickEventArgs e)
        {
            string cmd = ((ImageButton) sender).CommandName;

            if (tabList.SelectedIndex != -1)
            {
                int delta;

                // Determine the delta to apply in the order number for the module
                // within the list.  +3 moves down one item; -3 moves up one item

                if (cmd == "down")
                {
                    delta = 3;
                }
                else
                {
                    delta = -3;
                }

                TabSettings t = PortalTabs[tabList.SelectedIndex];
                t.TabOrder += delta;

                // Reset the order numbers for the tabs within the portal  
                OrderTabs();

                // Redirect to this site to refresh
                Response.Redirect("~/DesktopDefault.aspx?tabindex=" + (PortalTabs.Count - 1) + "&tabid=" + _tabId);
            }
        }


        //*******************************************************
        //
        // The DeleteBtn_Click server event handler is used to delete
        // the selected tab from the portal
        //
        //*******************************************************

        protected void DeleteBtn_Click(Object sender, ImageClickEventArgs e)
        {
            if (tabList.SelectedIndex != -1)
            {
                // must delete from database too
                TabSettings t = PortalTabs[tabList.SelectedIndex];
                TabsConfig.DeleteTab(t.TabId);

                // remove item from list
                PortalTabs.RemoveAt(tabList.SelectedIndex);

                // reorder list
                OrderTabs();

                // Redirect to this site to refresh
                Response.Redirect("~/DesktopDefault.aspx?tabindex=" + _tabIndex + "&tabid=" + _tabId);
            }
        }


        //*******************************************************
        //
        // The AddTab_Click server event handler is used to add
        // a new security tab for this portal
        //
        //*******************************************************

        protected void AddTab_Click(Object sender, EventArgs e)
        {
            // Obtain PortalSettings from Current Context
            var portalSettings = (PortalSettings) Context.Items["PortalSettings"];

            // write tab to database
            int tabId = TabsConfig.AddTab(portalSettings.PortalId, "New Tab", 999);

            // New tabs go to the end of the list   
            ITabItem tab = TabsConfig.GetSingleTab(tabId);
            PortalTabs.Add(new TabSettings(tab));

            // reload the _portalSettings from the database
            HttpContext.Current.Items["PortalSettings"] = new PortalSettings(portalSettings.PortalId, tabId,
                                                                             PortalConfig, TabsConfig, ModulesConfig,
                                                                             ModuleDefConfig);

            // Reset the order numbers for the tabs within the list  
            OrderTabs();

            // Redirect to edit page
            Response.Redirect("~/Admin/TabLayout.aspx?tabid=" + tabId);
        }

        //*******************************************************
        //
        // The EditBtn_Click server event handler is used to edit
        // the selected tab within the portal
        //
        //*******************************************************

        protected void EditBtn_Click(Object sender, ImageClickEventArgs e)
        {
            // Redirect to edit page of currently selected tab
            if (tabList.SelectedIndex != -1)
            {
                // Redirect to module settings page
                TabSettings t = PortalTabs[tabList.SelectedIndex];

                Response.Redirect("~/Admin/TabLayout.aspx?tabid=" + t.TabId);
            }
        }

        //*******************************************************
        //
        // The OrderTabs helper method is used to reset the display
        // order for tabs within the portal
        //
        //*******************************************************

        private void OrderTabs()
        {
            int i = 1;

            // sort the arraylist
            PortalTabs.Sort();

            // renumber the order and save to database
            foreach (TabSettings t in PortalTabs)
            {
                // number the items 1, 3, 5, etc. to provide an empty order
                // number when moving items up and down in the list.
                t.TabOrder = i;
                i += 2;

                // rewrite tab to database
                TabsConfig.UpdateTabOrder(t.TabId, t.TabOrder);
            }
        }
    }
}