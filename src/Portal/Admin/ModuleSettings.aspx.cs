using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class ModuleSettingsPage : PortalPage<ModuleSettingsPage>
    {
        private int moduleId;
        private int tabId;

        [Dependency]
        public IRolesDb RolesDb { private get; set; }

        [Dependency]
        public IModulesDb ModulesDb { private get; set; }

        /// <summary>
        /// 页面加载时初始化模块设置。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // 验证当前用户是否有权访问此页面
            PortalAuthorization.RequireAdmin();

            // 获取请求中的模块ID和标签ID
            moduleId = Request.Params["mid"] != null ? int.Parse(Request.Params["mid"]) : 0;
            tabId = Request.Params["tabid"] != null ? int.Parse(Request.Params["tabid"]) : 0;

            // 如果不是回发请求，则加载数据
            if (!IsPostBack)
            {
                BindData();
            }
        }

        /// <summary>
        /// 用户点击“应用模块更改”按钮时保存模块设置。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">事件参数。</param>
        protected void ApplyChanges_Click(Object Sender, EventArgs e)
        {
            // 获取PortalSettings
            var portalSettings = GetPortalSettings();

            // 获取模块设置对象
            var module = GetModule();

            if (module != null)
            {
                // 构建授权编辑角色字符串
                string editRoles = ConstructAuthorizedRoles(authEditRoles.Items);

                // 更新模块设置
                ModulesDb.UpdateModule(moduleId, module.ModuleOrder, module.PaneName, moduleTitle.Text,
                                       int.Parse(cacheTime.Text), editRoles, showMobile.Checked);

                // 重新绑定数据
                BindData();
            }

            // 重新导航到管理页面
            Response.Redirect($"TabLayout.aspx?tabid={tabId}");
        }

        /// <summary>
        /// 绑定数据到页面控件。
        /// </summary>
        private void BindData()
        {
            // 获取模块设置对象
            var module = GetModule();

            if (module != null)
            {
                // 更新文本框设置
                moduleTitle.Text = module.ModuleTitle;
                cacheTime.Text = module.CacheTime.ToString();
                showMobile.Checked = module.ShowMobile;

                // 填充角色列表
                PopulateRoleList(PortalRoleParser.Parse(module.AuthorizedEditRoles), RolesDb.GetPortalRoles(GetPortalSettings().PortalId));
            }
        }

        /// <summary>
        /// 构建授权编辑角色字符串。
        /// </summary>
        /// <param name="items">包含角色的项集合。</param>
        /// <returns>授权编辑角色字符串。</returns>
        private string ConstructAuthorizedRoles(ListItemCollection items)
        {
            // 初始化编辑角色字符串
            var editRoles = "";

            // 遍历所有项，如果被选中则添加到编辑角色字符串中
            foreach (ListItem item in items)
            {
                if (item.Selected)
                {
                    editRoles += item.Text + ";";
                }
            }

            // 移除最后一个分号
            if (!string.IsNullOrEmpty(editRoles))
            {
                editRoles = editRoles.TrimEnd(';');
            }

            return editRoles;
        }

        /// <summary>
        /// 填充角色列表。
        /// </summary>
        /// <param name="authorizedRoles">已授权的角色数组。</param>
        /// <param name="roles">所有角色集合。</param>
        private void PopulateRoleList(string[] authorizedRoles, IEnumerable<IRoleItem> roles)
        {
            // 清除现有复选框列表项
            authEditRoles.Items.Clear();

            // 添加"All Users"项
            var allItem = new ListItem(PortalRoleNames.AllUsers, PortalRoleNames.AllUsers);
            allItem.Selected = authorizedRoles.Any(role => string.Equals(role, PortalRoleNames.AllUsers, StringComparison.OrdinalIgnoreCase));
            authEditRoles.Items.Add(allItem);

            // 添加其他角色项
            foreach (var role in roles)
            {
                var item = new ListItem(role.RoleName, role.RoleId.ToString());
                item.Selected = authorizedRoles.Any(authorizedRole => string.Equals(authorizedRole, role.RoleName, StringComparison.OrdinalIgnoreCase));
                authEditRoles.Items.Add(item);
            }
        }

        /// <summary>
        /// 获取指定模块的设置。
        /// </summary>
        /// <returns>模块设置对象。</returns>
        private ModuleSettings GetModule()
        {
            var portalSettings = GetPortalSettings();

            return portalSettings?.ActiveTab?.Modules?.FirstOrDefault(m => m.ModuleId == moduleId);
        }

        /// <summary>
        /// 获取当前上下文的PortalSettings。
        /// </summary>
        /// <returns>PortalSettings对象。</returns>
        private PortalSettings GetPortalSettings()
        {
            return PortalContext.GetPortalSettings();
        }
    }
}
