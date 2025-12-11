using System;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

// 定义命名空间
namespace ASPNET.StarterKit.Portal
{
    // 定义一个继承自PortalPage<SecurityRoles>的部分类SecurityRoles
    public partial class SecurityRoles : PortalPage<SecurityRoles>
    {
        // 私有变量，用于保存角色ID
        private int roleId = -1;
        // 私有变量，用于保存角色名称
        private string roleName = "";
        // 私有变量，用于保存页ID
        private int tabId;
        // 私有变量，用于保存页索引
        private int tabIndex;

        // 使用依赖注入标记属性定义IUsersDb接口的属性
        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        // 使用依赖注入标记属性定义IRolesDb接口的属性
        [Dependency]
        public IRolesDb RolesDB { private get; set; }

        //*******************************************************
        //
        // Page_Load服务器事件处理器用于加载页面并填充角色信息
        //
        //*******************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            // 验证当前用户是否有权限访问此页面
            if (PortalSecurity.IsInRoles("Admins") == false)
            {
                // 如果没有权限，则重定向到无访问权限页面
                Response.Redirect("~/Admin/EditAccessDenied.aspx");
            }

            // 计算安全角色ID
            if (Request.Params["roleid"] != null)
            {
                // 从请求参数中获取角色ID
                roleId = Int32.Parse(Request.Params["roleid"]);
            }
            if (Request.Params["rolename"] != null)
            {
                // 从请求参数中获取角色名称
                roleName = Request.Params["rolename"];
            }
            if (Request.Params["tabid"] != null)
            {
                // 从请求参数中获取页ID
                tabId = Int32.Parse(Request.Params["tabid"]);
            }
            if (Request.Params["tabindex"] != null)
            {
                // 从请求参数中获取页索引
                tabIndex = Int32.Parse(Request.Params["tabindex"]);
            }

            // 如果是首次访问页面，则绑定角色数据到DataList控件
            if (Page.IsPostBack == false)
            {
                BindData();
            }
        }

        //*******************************************************
        //
        // Save_Click服务器事件处理器用于保存当前的安全设置到配置系统
        //
        //*******************************************************

        protected void Save_Click(Object Sender, EventArgs e)
        {
            // 从当前上下文中获取PortalSettings
            var portalSettings = (PortalSettings)Context.Items["PortalSettings"];

            // 导航回管理员页面
            Response.Redirect("~/DesktopDefault.aspx?tabindex=" + tabIndex + "&tabid=" + tabId);
        }

        //*******************************************************
        //
        // AddUser_Click服务器事件处理器用于向此安全角色添加新用户
        //
        //*******************************************************

        protected void AddUser_Click(Object sender, EventArgs e)
        {
            // 初始化用户ID为-1
            int userId = -1;

            if (((LinkButton)sender).ID == "addNew")
            {
                /* note：暂不支持Windows用户机制
                // 将新用户添加到用户表中
                if ((userId = UsersDB.AddUser(windowsUserName.Text, windowsUserName.Text, "acme")) == -1)
                {
                    // 如果添加失败，显示错误消息
                    Message.Text = "Add New Failed! There is already an entry for <u>" + windowsUserName.Text + "</u> in the Users database.<br>Please use Add Existing for this user.";
                }
                */
            }
            else
            {
                // 从现有用户的下拉列表中获取用户ID
                userId = Int32.Parse(allUsers.SelectedItem.Value);
            }

            if (userId != -1)
            {
                // 向数据库中添加新的用户角色
                RolesDB.AddUserRole(roleId, userId);
            }

            // 重新绑定列表
            BindData();
        }

        //*******************************************************
        //
        // usersInRole_ItemCommand服务器事件处理器用于处理从usersInRole DataList控件中删除用户的角色
        //
        //*******************************************************

        private void usersInRole_ItemCommand(object sender, DataListCommandEventArgs e)
        {
            // 获取用户的ID
            var userId = (int)usersInRole.DataKeys[e.Item.ItemIndex];

            if (e.CommandName == "delete")
            {
                // 更新数据库
                RolesDB.DeleteUserRole(roleId, userId);

                // 确保项目不可编辑
                usersInRole.EditItemIndex = -1;

                // 重新填充列表
                BindData();
            }
        }

        //*******************************************************
        //
        // BindData辅助方法用于将此门户的安全角色列表绑定到DataList服务器控件
        //
        //*******************************************************

        private void BindData()
        {
            // 如果应用程序使用的是Windows身份验证而不是表单身份验证，则显示Windows身份验证UI
            if (User.Identity.AuthenticationType != "Forms")
            {
                /* note：暂不支持Windows用户机制
                // 显示Windows用户界面组件
                windowsUserName.Visible = true;
                addNew.Visible = true;
                */
            }

            // 在标题中添加角色名称
            if (roleName != "")
            {
                title.InnerText = "Role Membership: " + roleName;
            }

            // 从数据库获取门户的角色
            // 将角色中的用户绑定到DataList
            usersInRole.DataSource = RolesDB.GetRoleMembers(roleId);
            usersInRole.DataBind();

            // 将所有门户用户绑定到下拉列表
            allUsers.DataSource = RolesDB.GetUsers();
            allUsers.DataBind();
        }
    }
}