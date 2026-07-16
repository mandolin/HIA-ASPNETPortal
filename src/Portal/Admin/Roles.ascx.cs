using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：旧门户角色定义管理控件。
    ///
    /// English: Legacy Portal role-definition administration control.
    /// </summary>
    /// <remarks>
    /// 中文：角色名称仍写入旧的分号授权字符串；改名会同步更新当前门户的 Tab 访问角色和模块编辑角色。
    /// 删除存在成员或授权引用的角色会被拒绝，不执行隐式清理。
    ///
    /// English: Role names remain stored in legacy semicolon authorization strings; a rename synchronizes current
    /// Portal Tab-access roles and module-edit roles. Deleting a role that has members or authorization references is
    /// rejected without implicit cleanup.
    /// </remarks>
    public partial class Roles : PortalModuleControl<Roles>
    {
        private int tabId;
        private int tabIndex;

        /// <summary>中文：角色数据访问依赖。English: Role data-access dependency.</summary>
        [Dependency]
        public IRolesDb RolesDB { private get; set; }

        /// <summary>中文：Tab 数据访问依赖。English: Tab data-access dependency.</summary>
        [Dependency]
        public ITabsDb TabsConfig { private get; set; }

        /// <summary>中文：模块实例数据访问依赖。English: Module-instance data-access dependency.</summary>
        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        /// <summary>
        /// 中文：授权、读取可选导航参数并在首次请求绑定当前门户角色。
        ///
        /// English: Authorizes, reads optional navigation parameters, and binds current-Portal roles on the initial request.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminRolesEdit) || !TryReadNavigationParameters())
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                BindData();
            }
        }

        /// <summary>
        /// 中文：创建一个当前门户内唯一的默认角色，并进入编辑状态。
        ///
        /// English: Creates a unique default role in the current Portal and enters edit mode.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void AddRole_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminRolesEdit) || !TryReadNavigationParameters())
            {
                return;
            }

            try
            {
                PortalSettings portalSettings = PortalContext.GetPortalSettings();
                string roleName = CreateUniqueDefaultRoleName(RolesDB.GetPortalRoles(portalSettings.PortalId));
                int roleId = RolesDB.AddRole(portalSettings.PortalId, roleName);
                PortalOperationAudit.Record(
                    "RoleAdministration",
                    "Create",
                    "Role",
                    roleId.ToString(),
                    "Created role.",
                    Context);
                rolesList.EditItemIndex = rolesList.Items.Count;
                BindData();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.Roles.Add",
                    "Adding a role failed.",
                    exception,
                    Context);
                ShowMessage("角色创建失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        /// <summary>
        /// 中文：处理角色编辑、改名、删除和成员管理命令。
        ///
        /// English: Handles role edit, rename, delete, and membership-management commands.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：包含命令和 DataList 项索引的事件数据。English: Event data containing the command and DataList item index.</param>
        protected void RolesList_ItemCommand(object sender, DataListCommandEventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminRolesEdit) || !TryReadNavigationParameters())
            {
                return;
            }

            IRoleItem role;
            if (!TryGetRoleFromDataList(e, out role))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            if (string.Equals(e.CommandName, "edit", StringComparison.OrdinalIgnoreCase))
            {
                rolesList.EditItemIndex = e.Item.ItemIndex;
                BindData();
                return;
            }

            if (string.Equals(e.CommandName, "apply", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.CommandName, "members", StringComparison.OrdinalIgnoreCase))
            {
                TextBox roleNameTextBox = e.Item.FindControl("roleName") as TextBox;
                if (roleNameTextBox == null || !TryRenameRole(role, roleNameTextBox.Text))
                {
                    return;
                }

                if (string.Equals(e.CommandName, "members", StringComparison.OrdinalIgnoreCase))
                {
                    string url = ResolveUrl(
                        "~/Admin/SecurityRoles.aspx?roleId=" + role.RoleId +
                        "&tabindex=" + tabIndex +
                        "&tabid=" + tabId);
                    PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, url);
                    return;
                }

                rolesList.EditItemIndex = -1;
                BindData();
                return;
            }

            if (string.Equals(e.CommandName, "delete", StringComparison.OrdinalIgnoreCase))
            {
                DeleteRole(role);
            }
        }

        private bool TryReadNavigationParameters()
        {
            return TryReadOptionalPositiveParameter("tabid", out tabId) &&
                   TryReadOptionalPositiveParameter("tabindex", out tabIndex);
        }

        private bool TryReadOptionalPositiveParameter(string parameterName, out int value)
        {
            value = 0;
            string rawValue = Request.Params[parameterName];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadPositiveInt32(rawValue, out value))
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private bool TryGetRoleFromDataList(DataListCommandEventArgs e, out IRoleItem role)
        {
            role = null;
            int roleId;
            if (e.Item == null || e.Item.ItemIndex < 0 || e.Item.ItemIndex >= rolesList.DataKeys.Count ||
                !PortalNavigationPolicy.TryReadNonNegativeInt32(rolesList.DataKeys[e.Item.ItemIndex].ToString(), out roleId))
            {
                return false;
            }

            role = FindCurrentPortalRole(roleId);
            return role != null;
        }

        private IRoleItem FindCurrentPortalRole(int roleId)
        {
            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            return RolesDB.GetPortalRoles(portalSettings.PortalId)
                .FirstOrDefault(item => item.RoleId == roleId);
        }

        private bool TryRenameRole(IRoleItem role, string requestedName)
        {
            string roleName;
            if (!PortalAdministrationPolicy.TryNormalizeRoleName(requestedName, out roleName))
            {
                ShowMessage("角色名称无效，未保存本次修改。");
                return false;
            }

            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            bool duplicate = RolesDB.GetPortalRoles(portalSettings.PortalId).Any(item =>
                item.RoleId != role.RoleId &&
                string.Equals(item.RoleName, roleName, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
            {
                ShowMessage("当前门户已存在同名角色，未保存本次修改。");
                return false;
            }

            if (string.Equals(role.RoleName, roleName, StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(role.RoleName, PortalRoleNames.Administrators, StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage("核心管理员角色不能改名。");
                return false;
            }

            try
            {
                string previousRoleName = role.RoleName;
                RolesDB.UpdateRole(role.RoleId, roleName);
                UpdateRoleReferences(portalSettings, previousRoleName, roleName);
                PortalOperationAudit.Record(
                    "RoleAdministration",
                    "Rename",
                    "Role",
                    role.RoleId.ToString(),
                    "Renamed role and synchronized exact authorization references.",
                    Context);
                return true;
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.Roles.Rename",
                    "Renaming a role failed. RoleId=" + role.RoleId,
                    exception,
                    Context);
                ShowMessage("角色改名失败，系统已记录本次错误。事件编号：" + eventId);
                return false;
            }
        }

        private void UpdateRoleReferences(PortalSettings portalSettings, string oldRoleName, string newRoleName)
        {
            foreach (ITabItem tab in portalSettings.DesktopTabs)
            {
                string updatedTabRoles = ReplaceRoleReference(tab.AccessRoles, oldRoleName, newRoleName);
                if (!string.Equals(tab.AccessRoles ?? string.Empty, updatedTabRoles, StringComparison.Ordinal))
                {
                    TabsConfig.UpdateTab(
                        portalSettings.PortalId,
                        tab.TabId,
                        tab.TabName,
                        tab.TabOrder ?? 0,
                        updatedTabRoles,
                        tab.MobileTabName,
                        tab.ShowMobile ?? false);
                }

                foreach (IModuleItem module in ModulesConfig.GetModulesByTab(tab.TabId))
                {
                    string updatedModuleRoles = ReplaceRoleReference(module.EditRoles, oldRoleName, newRoleName);
                    if (!string.Equals(module.EditRoles ?? string.Empty, updatedModuleRoles, StringComparison.Ordinal))
                    {
                        ModulesConfig.UpdateModule(
                            module.ModuleId,
                            module.ModuleOrder ?? 0,
                            module.PaneName,
                            module.ModuleTitle,
                            module.CacheTimeout ?? 0,
                            updatedModuleRoles,
                            module.ShowMobile ?? false);
                    }
                }
            }
        }

        private void DeleteRole(IRoleItem role)
        {
            if (string.Equals(role.RoleName, PortalRoleNames.Administrators, StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage("核心管理员角色不能删除。");
                return;
            }

            if (RolesDB.GetRoleMembers(role.RoleId).Any())
            {
                ShowMessage("角色仍包含成员，不能删除。");
                return;
            }

            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            if (HasRoleReferences(portalSettings, role.RoleName))
            {
                ShowMessage("角色仍被 Tab 或模块引用，不能删除。");
                return;
            }

            try
            {
                RolesDB.DeleteRole(role.RoleId);
                PortalOperationAudit.Record(
                    "RoleAdministration",
                    "Delete",
                    "Role",
                    role.RoleId.ToString(),
                    "Deleted an unreferenced role without members.",
                    Context);
                rolesList.EditItemIndex = -1;
                BindData();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.Roles.Delete",
                    "Deleting a role failed. RoleId=" + role.RoleId,
                    exception,
                    Context);
                ShowMessage("角色删除失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        private bool HasRoleReferences(PortalSettings portalSettings, string roleName)
        {
            foreach (ITabItem tab in portalSettings.DesktopTabs)
            {
                if (PortalRoleParser.Contains(tab.AccessRoles, roleName))
                {
                    return true;
                }

                if (ModulesConfig.GetModulesByTab(tab.TabId)
                    .Any(module => PortalRoleParser.Contains(module.EditRoles, roleName)))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ReplaceRoleReference(string roles, string oldRoleName, string newRoleName)
        {
            return PortalRoleParser.Join(
                PortalRoleParser.Parse(roles)
                    .Select(role => string.Equals(role, oldRoleName, StringComparison.OrdinalIgnoreCase)
                        ? newRoleName
                        : role));
        }

        private static string CreateUniqueDefaultRoleName(IEnumerable<IRoleItem> roles)
        {
            var existingNames = new HashSet<string>(
                roles.Select(item => item.RoleName ?? string.Empty),
                StringComparer.OrdinalIgnoreCase);
            if (!existingNames.Contains("New Role"))
            {
                return "New Role";
            }

            for (int suffix = 2; suffix < 1000; suffix++)
            {
                string candidate = "New Role " + suffix;
                if (!existingNames.Contains(candidate))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("A unique default role name could not be generated.");
        }

        private void BindData()
        {
            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            rolesList.DataSource = RolesDB.GetPortalRoles(portalSettings.PortalId);
            rolesList.DataBind();
        }

        private void ShowMessage(string message)
        {
            Message.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
