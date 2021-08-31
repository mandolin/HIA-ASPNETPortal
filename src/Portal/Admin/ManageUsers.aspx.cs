using System;
using System.Data;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class ManageUsers : PortalPage<ManageUsers>
    {
        private int tabId;
        private int tabIndex;
        private int userId = -1;
        private string userName = "";

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

            // Calculate userid
            if (Request.Params["userid"] != null)
            {
                userId = Int32.Parse(Request.Params["userid"]);
            }
            if (Request.Params["username"] != null)
            {
                userName = Request.Params["username"];
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
                // new user?
                if (userName == "")
                {
                    // make a unique new user record
                    int uid = -1;
                    int i = 0;

                    while (uid == -1)
                    {
                        string friendlyName = "New User created " + DateTime.Now;
                        userName = "New User" + i;
                        uid = UsersDB.AddUser(friendlyName, userName, "");
                        i++;
                    }

                    // redirect to this page with the corrected querystring args
                    Response.Redirect("~/Admin/ManageUsers.aspx?userId=" + uid + "&username=" + userName + "&tabindex=" +
                                      tabIndex + "&tabid=" + tabId);
                }

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
        // The AddRole_Click server event handler is used to add
        // the user to this security role
        //
        //*******************************************************

        protected void AddRole_Click(Object sender, EventArgs e)
        {
            int roleId;

            //get user id from dropdownlist of existing users
            roleId = Int32.Parse(allRoles.SelectedItem.Value);

            // Add a new userRole to the database
            RolesDB.AddUserRole(roleId, userId);

            // Rebind list
            BindData();
        }

        //*******************************************************
        //
        // The UpdateUser_Click server event handler is used to add
        // the update the user settings
        //
        //*******************************************************

        protected void UpdateUser_Click(Object sender, EventArgs e)
        {
            // update the user record in the database
            UsersDB.UpdateUser(userId, Email.Text, PortalSecurity.Encrypt(Password.Text));

            // redirect to this page with the corrected querystring args
            Response.Redirect("~/Admin/ManageUsers.aspx?userId=" + userId + "&username=" + Email.Text + "&tabindex=" +
                              tabIndex + "&tabid=" + tabId);
        }

        //*******************************************************
        //
        // The UserRoles_ItemCommand server event handler on this page
        // is used to handle deleting the user from roles
        // from the userRoles asp:datalist control
        //
        //*******************************************************

        private void UserRoles_ItemCommand(object sender, DataListCommandEventArgs e)
        {
            var roleId = (int) userRoles.DataKeys[e.Item.ItemIndex];

            // update database
            RolesDB.DeleteUserRole(roleId, userId);

            // Ensure that item is not editable
            userRoles.EditItemIndex = -1;

            // Repopulate list
            BindData();
        }

        //*******************************************************
        //
        // The BindData helper method is used to bind the list of
        // security roles for this portal to an asp:datalist server control
        //
        //*******************************************************

        private void BindData()
        {
            // Bind the Email and Password
            IUserItem item = UsersDB.GetSingleUser(userName);

            Email.Text =item.Email;

            // add the user name to the title
            if (userName != "")
            {
                title.InnerText = "Manage User: " + userName;
            }

            // bind users in role to DataList
            userRoles.DataSource = UsersDB.GetRolesByUser(userName);
            userRoles.DataBind();

            // Obtain PortalSettings from Current Context
            var portalSettings = (PortalSettings) Context.Items["PortalSettings"];

            // Get the portal's roles from the database
            // bind all portal roles to dropdownlist
            allRoles.DataSource = RolesDB.GetPortalRoles(portalSettings.PortalId);
            allRoles.DataBind();
        }
    }
}