using System;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class SecurityRoles : PortalPage<SecurityRoles>
    {
        private int roleId = -1;
        private string roleName = "";
        private int tabId;
        private int tabIndex;

        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        [Dependency]
        public IRolesDb RolesDB { private get; set; }


        //*******************************************************
        //
        // The Page_Load server event handler on this page is used
        // to populate the role information for the page
        //
        //*******************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            // Verify that the current user has access to access this page
            if (PortalSecurity.IsInRoles("Admins") == false)
            {
                Response.Redirect("~/Admin/EditAccessDenied.aspx");
            }

            // Calculate security roleId
            if (Request.Params["roleid"] != null)
            {
                roleId = Int32.Parse(Request.Params["roleid"]);
            }
            if (Request.Params["rolename"] != null)
            {
                roleName = Request.Params["rolename"];
            }
            if (Request.Params["tabid"] != null)
            {
                tabId = Int32.Parse(Request.Params["tabid"]);
            }
            if (Request.Params["tabindex"] != null)
            {
                tabIndex = Int32.Parse(Request.Params["tabindex"]);
            }


            // If this is the first visit to the page, bind the role data to the datalist
            if (Page.IsPostBack == false)
            {
                BindData();
            }
        }

        //*******************************************************
        //
        // The Save_Click server event handler on this page is used
        // to save the current security settings to the configuration system
        //
        //*******************************************************

        protected void Save_Click(Object Sender, EventArgs e)
        {
            // Obtain PortalSettings from Current Context
            var portalSettings = (PortalSettings) Context.Items["PortalSettings"];

            // Navigate back to admin page
            Response.Redirect("~/DesktopDefault.aspx?tabindex=" + tabIndex + "&tabid=" + tabId);
        }

        //*******************************************************
        //
        // The AddUser_Click server event handler is used to add
        // a new user to this security role 
        //
        //*******************************************************

        protected void AddUser_Click(Object sender, EventArgs e)
        {
            int userId;

            if (((LinkButton) sender).ID == "addNew")
            {
                // add new user to users table
                if ((userId = UsersDB.AddUser(windowsUserName.Text, windowsUserName.Text, "acme")) == -1)
                {
                    Message.Text = "Add New Failed!  There is already an entry for <" + "u" + ">" + windowsUserName.Text +
                                   "<" + "/u" + "> in the Users database." + "<" + "br" + ">" +
                                   "Please use Add Existing for this user.";
                }
            }
            else
            {
                //get user id from dropdownlist of existing users
                userId = Int32.Parse(allUsers.SelectedItem.Value);
            }

            if (userId != -1)
            {
                // Add a new userRole to the database
                RolesDB.AddUserRole(roleId, userId);
            }

            // Rebind list
            BindData();
        }

        //*******************************************************
        //
        // The usersInRole_ItemCommand server event handler on this page 
        // is used to handle the user editing and deleting roles
        // from the usersInRole asp:datalist control
        //
        //*******************************************************

        private void usersInRole_ItemCommand(object sender, DataListCommandEventArgs e)
        {
            var userId = (int) usersInRole.DataKeys[e.Item.ItemIndex];

            if (e.CommandName == "delete")
            {
                // update database
                RolesDB.DeleteUserRole(roleId, userId);

                // Ensure that item is not editable
                usersInRole.EditItemIndex = -1;

                // Repopulate list
                BindData();
            }
        }

        //*******************************************************
        //
        // The BindData helper method is used to bind the list of 
        // security roles for this portal to an asp:datalist server control
        //
        //*******************************************************

        private void BindData()
        {
            // unhide the Windows Authentication UI, if application
            if (User.Identity.AuthenticationType != "Forms")
            {
                windowsUserName.Visible = true;
                addNew.Visible = true;
            }

            // add the role name to the title
            if (roleName != "")
            {
                title.InnerText = "Role Membership: " + roleName;
            }

            // Get the portal's roles from the database
            // bind users in role to DataList
            usersInRole.DataSource = RolesDB.GetRoleMembers(roleId);
            usersInRole.DataBind();

            // bind all portal users to dropdownlist
            allUsers.DataSource = RolesDB.GetUsers();
            allUsers.DataBind();
        }
    }
}