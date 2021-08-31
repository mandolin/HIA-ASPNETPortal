using System;
using System.Collections.Generic;
using System.Data;
using System.Web;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class ModuleSettingsPage : PortalPage<ModuleSettingsPage>
    {
        private int moduleId;
        private int tabId;

        [Dependency]
        public IRolesDb RolesDB { private get; set; }

        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        //*******************************************************
        //
        // The Page_Load server event handler on this page is used
        // to populate the module settings on the page
        //
        //*******************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            // Verify that the current user has access to access this page
            if (PortalSecurity.IsInRoles("Admins") == false)
            {
                Response.Redirect("~/Admin/EditAccessDenied.aspx");
            }

            // Determine Module to Edit
            if (Request.Params["mid"] != null)
            {
                moduleId = Int32.Parse(Request.Params["mid"]);
            }
            // Determine Tab to Edit
            if (Request.Params["tabid"] != null)
            {
                tabId = Int32.Parse(Request.Params["tabid"]);
            }

            if (Page.IsPostBack == false)
            {
                BindData();
            }
        }

        //*******************************************************
        //
        // The ApplyChanges_Click server event handler on this page is used
        // to save the module settings into the portal configuration system
        //
        //*******************************************************

        protected void ApplyChanges_Click(Object Sender, EventArgs e)
        {
            // Obtain PortalSettings from Current Context
            var portalSettings = (PortalSettings) HttpContext.Current.Items["PortalSettings"];

            object value = GetModule();
            if (value != null)
            {
                var m = (ModuleSettings) value;

                // Construct Authorized User Roles String
                string editRoles = "";

                foreach (ListItem item in authEditRoles.Items)
                {
                    if (item.Selected)
                    {
                        editRoles = editRoles + item.Text + ";";
                    }
                }

                // update module
                ModulesConfig.UpdateModule(moduleId, m.ModuleOrder, m.PaneName, moduleTitle.Text,
                                           Int32.Parse(cacheTime.Text), editRoles, showMobile.Checked);

                // Update Textbox Settings
                moduleTitle.Text = m.ModuleTitle;
                cacheTime.Text = m.CacheTime.ToString();

                // Populate checkbox list with all security roles for this portal
                // and "check" the ones already configured for this module
                IEnumerable<IRoleItem> roles = RolesDB.GetPortalRoles(portalSettings.PortalId);

                // Clear existing items in checkboxlist
                authEditRoles.Items.Clear();

                var allItem = new ListItem();
                allItem.Text = "All Users";

                if (m.AuthorizedEditRoles.LastIndexOf("All Users") > -1)
                {
                    allItem.Selected = true;
                }

                authEditRoles.Items.Add(allItem);

                foreach (IRoleItem role in roles)
                {
                    var item = new ListItem();
                    item.Text = role.RoleName;
                    item.Value = role.RoleId.ToString();

                    if ((m.AuthorizedEditRoles.LastIndexOf(item.Text)) > -1)
                    {
                        item.Selected = true;
                    }

                    authEditRoles.Items.Add(item);
                }
            }

            // Navigate back to admin page
            Response.Redirect("TabLayout.aspx?tabid=" + tabId);
        }

        //*******************************************************
        //
        // The BindData helper method is used to populate a asp:datalist
        // server control with the current "edit access" permissions
        // set within the portal configuration system
        //
        //*******************************************************

        private void BindData()
        {
            // Obtain PortalSettings from Current Context
            var portalSettings = (PortalSettings) HttpContext.Current.Items["PortalSettings"];

            object value = GetModule();
            if (value != null)
            {
                var m = (ModuleSettings) value;

                // Update Textbox Settings
                moduleTitle.Text = m.ModuleTitle;
                cacheTime.Text = m.CacheTime.ToString();
                showMobile.Checked = m.ShowMobile;

                // Populate checkbox list with all security roles for this portal
                // and "check" the ones already configured for this module
                IEnumerable<IRoleItem> roles = RolesDB.GetPortalRoles(portalSettings.PortalId);

                // Clear existing items in checkboxlist
                authEditRoles.Items.Clear();

                var allItem = new ListItem();
                allItem.Text = "All Users";

                if (m.AuthorizedEditRoles.LastIndexOf("All Users") > -1)
                {
                    allItem.Selected = true;
                }

                authEditRoles.Items.Add(allItem);

                foreach (IRoleItem role in roles)
                {
                    var item = new ListItem();
                    item.Text = role.RoleName;
                    item.Value = role.RoleId.ToString();

                    if ((m.AuthorizedEditRoles.LastIndexOf(item.Text)) > -1)
                    {
                        item.Selected = true;
                    }

                    authEditRoles.Items.Add(item);
                }
            }
        }

        private ModuleSettings GetModule()
        {
            // Obtain PortalSettings for this tab
            var portalSettings = (PortalSettings) HttpContext.Current.Items["PortalSettings"];

            // Obtain selected module data
            foreach (ModuleSettings _module in portalSettings.ActiveTab.Modules)
            {
                if (_module.ModuleId == moduleId)
                {
                    return _module;
                }
            }
            return null;
        }
    }
}