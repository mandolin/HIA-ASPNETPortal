using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Resources;

namespace ASPNET.StarterKit.Portal
{
    public partial class Users : PortalModuleControl<Users>
    {
        private int tabId;
        private int tabIndex;

        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        [Dependency]
        public IRolesDb RolesDB { private get; set; }


        //*******************************************************
        //
        // The Page_Load server event handler on this user control is used
        // to populate the current roles settings from the configuration system
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
        // The DeleteUser_Click server event handler is used to add
        // a new security role for this portal
        //
        //*******************************************************

        protected void DeleteUser_Click(Object Sender, ImageClickEventArgs e)
        {
            // get user id from dropdownlist of users
            UsersDB.DeleteUser(Int32.Parse(allUsers.SelectedItem.Value));

            // Rebind list
            BindData();
        }

        //*******************************************************
        //
        // The EditUser_Click server event handler is used to add
        // a new security role for this portal
        //
        //*******************************************************

        private void EditUser_Click(Object Sender, CommandEventArgs e)
        {
            // get user id from dropdownlist of users
            int userId = -1;
            string _userName = "";

            if (e.CommandName == "edit")
            {
                userId = Int32.Parse(allUsers.SelectedItem.Value);
                _userName = allUsers.SelectedItem.Text;
            }

            // redirect to edit page
            Response.Redirect("~/Admin/ManageUsers.aspx?userId=" + userId + "&username=" + _userName + "&tabindex=" +
                              tabIndex + "&tabid=" + tabId);
        }

        //*******************************************************
        //
        // The BindData helper method is used to bind the list of 
        // users for this portal to an asp:DropDownList server control
        //
        //*******************************************************

        private void BindData()
        {
            // change the message between Windows and Forms authentication
            if (Context.User.Identity.AuthenticationType != "Forms")
            {
                Message.Text = lang.Admin_Users_FormMsg;
            }
            else
            {
                Message.Text = lang.Admin_Users_OtherMsg;
            }

            // Get the list of registered users from the database
            // bind all portal users to dropdownlist
            allUsers.DataSource = RolesDB.GetUsers();
            allUsers.DataBind();
        }
    }
}