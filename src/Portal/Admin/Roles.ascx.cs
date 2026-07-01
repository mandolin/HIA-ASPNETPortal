using System;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

// 定义命名空间
namespace ASPNET.StarterKit.Portal
{
    // 定义一个继承自PortalModuleControl<Roles>的部分类Roles
    public partial class Roles : PortalModuleControl<Roles>
    {
        // 私有变量，用于保存页ID
        private int tabId;
        // 私有变量，用于保存页索引
        private int tabIndex;

        // 使用依赖注入标记属性定义IRolesDb接口的属性
        [Dependency]
        public IRolesDb RolesDB { private get; set; }

        //*******************************************************
        //
        // Page_Load服务器事件处理器用于加载页面并填充当前的角色设置
        //
        //*******************************************************

        protected void Page_Load(object sender, EventArgs e)
        {
            // 验证当前用户是否有权限访问此页面
            PortalAuthorization.RequireAdmin();

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
        // AddRole_Click服务器事件处理器用于为门户添加一个新的安全角色
        //
        //*******************************************************

        protected void AddRole_Click(Object Sender, EventArgs e)
        {
            // 从当前上下文中获取PortalSettings
            var portalSettings = PortalContext.GetPortalSettings();

            // 向数据库中添加一个新角色
            RolesDB.AddRole(portalSettings.PortalId, "New Role");

            // 设置编辑项目的索引为最后一个项目
            rolesList.EditItemIndex = rolesList.Items.Count;

            // 重新绑定列表
            BindData();
        }

        //*******************************************************
        //
        // RolesList_ItemCommand服务器事件处理器用于处理从RolesList DataList控件中编辑和删除角色
        //
        //*******************************************************

        protected void RolesList_ItemCommand(object sender, DataListCommandEventArgs e)
        {
            // 获取角色ID
            var roleId = (int)rolesList.DataKeys[e.Item.ItemIndex];

            if (e.CommandName == "edit")
            {
                // 如果点击了“编辑”按钮旁边的项目，则设置可编辑项目索引
                rolesList.EditItemIndex = e.Item.ItemIndex;

                // 重新填充DataList控件
                BindData();
            }
            else if (e.CommandName == "apply")
            {
                // 应用更改
                string _roleName = ((TextBox)e.Item.FindControl("roleName")).Text;

                // 更新数据库
                RolesDB.UpdateRole(roleId, _roleName);

                // 禁用可编辑项目访问
                rolesList.EditItemIndex = -1;

                // 重新填充DataList控件
                BindData();
            }
            else if (e.CommandName == "delete")
            {
                // 更新数据库
                RolesDB.DeleteRole(roleId);

                // 确保项目不可编辑
                rolesList.EditItemIndex = -1;

                // 重新填充列表
                BindData();
            }
            else if (e.CommandName == "members")
            {
                // 首先保存角色名称更改
                string _roleName = ((TextBox)e.Item.FindControl("roleName")).Text;
                RolesDB.UpdateRole(roleId, _roleName);

                // 重定向到编辑页面
                Response.Redirect("~/Admin/SecurityRoles.aspx?roleId=" + roleId + "&rolename=" + _roleName +
                                  "&tabindex=" + tabIndex + "&tabid=" + tabId);
            }
        }

        //*******************************************************
        //
        // BindData辅助方法用于将此门户的安全角色列表绑定到DataList服务器控件
        //
        //*******************************************************

        private void BindData()
        {
            // 从当前上下文中获取PortalSettings
            var portalSettings = PortalContext.GetPortalSettings();

            // 从数据库获取门户的角色
            rolesList.DataSource = RolesDB.GetPortalRoles(portalSettings.PortalId);
            rolesList.DataBind();
        }
    }
}
