using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Resources;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 用户管理控件，用于展示和管理门户站点的用户信息。
    /// </summary>
    public partial class Users : PortalModuleControl<Users>
    {
        private int tabId;
        private int tabIndex;

        /// <summary>
        /// 用户数据库接口实例。
        /// </summary>
        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        /// <summary>
        /// 角色数据库接口实例。
        /// </summary>
        [Dependency]
        public IRolesDb RolesDB { private get; set; }


        /* 此用户控件上的页面加载服务器事件处理程序，用于从配置系统中填充当前角色设置
        
         The Page_Load server event handler on this user control is used
         to populate the current roles settings from the configuration system
        
        */

        /// <summary>
        /// 页面加载事件处理程序。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">事件参数。</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // 验证当前用户是否有访问权限
            // Verify that the current user has access to access this page
            if (!PortalSecurity.IsInRoles("Admins"))
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

            // 如果不是回发请求，则绑定数据
            // If this is the first visit to the page, bind the role data to the datalist
            if (Page.IsPostBack == false)
            {
                BindData();
            }
        }
        
        /// <summary>
        /// 删除用户的按钮点击事件处理程序。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">事件参数。</param>
        protected void btn_DeleteUser_Click(Object sender, ImageClickEventArgs e)
        {
            // 从下拉列表获取用户ID
            // get user id from dropdownlist of users
            var userId = int.Parse(ddl_AllUsers.SelectedItem.Value);

            try
            {
                UsersDB.DeleteUser(userId);

                // 重新绑定数据
                BindData();
            }
            catch (Exception ex)
            {
                // 处理异常
                Message.Text = $"删除失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 编辑用户的按钮点击事件处理程序。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">事件参数。</param>
        protected void EditUser_Click(Object sender, ImageClickEventArgs imageClickEventArgs)
        {
            int userId = -1;
            string userName = "";

            // 获取用户ID和用户名
            userId = Int32.Parse(ddl_AllUsers.SelectedItem.Value);
            userName = ddl_AllUsers.SelectedItem.Text;
        
            // 重定向到用户管理页面
            Response.Redirect($"~/Admin/ManageUsers.aspx?userId={userId}&username={Uri.EscapeDataString(userName)}&tabindex={tabIndex}&tabid={tabId}");
        }

        /// <summary>
        /// 添加用户的按钮点击事件处理程序。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">事件参数。</param>
        protected void AddUser_Click(Object sender, EventArgs e)
        {
            int userId = -1;
            string userName = "";

            // 重定向到用户管理页面
            Response.Redirect($"~/Admin/ManageUsers.aspx?userId={userId}&username={Uri.EscapeDataString(userName)}&tabindex={tabIndex}&tabid={tabId}");
        }

        /// <summary>
        /// 绑定用户数据到下拉列表。
        /// </summary>
        private void BindData()
        {
            try
            {
                // 设置不同的消息文本，取决于身份验证类型
                if (Context.User.Identity.AuthenticationType != "Forms")
                {
                    Message.Text = lang.Admin_Users_FormMsg;
                }
                else
                {
                    Message.Text = lang.Admin_Users_OtherMsg;
                }

                // 从数据库获取注册用户列表，并绑定到下拉列表
                ddl_AllUsers.DataSource = RolesDB.GetUsers();
                ddl_AllUsers.DataBind();
            }
            catch (Exception ex)
            {
                // 处理异常
                Message.Text = $"数据绑定失败: {ex.Message}";
            }
        }


    }
}