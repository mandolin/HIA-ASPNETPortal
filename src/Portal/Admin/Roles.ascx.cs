using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>旧门户角色定义管理控件。</zh-CN>
    ///   <en>Legacy Portal role-definition administration control.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>角色名称仍写入旧的分号授权字符串；改名会同步更新当前门户的 Tab 访问角色和模块编辑角色。 删除存在成员或授权引用的角色会被拒绝，不执行隐式清理。</zh-CN>
    ///   <en>Role names remain stored in legacy semicolon authorization strings; a rename synchronizes current Portal Tab-access roles and module-edit roles. Deleting a role that has members or authorization references is rejected without implicit cleanup.</en>
    /// </lang>
    /// </remarks>
    public partial class Roles : PortalModuleControl<Roles>
    {
        private int tabId;
        private int tabIndex;

        /// <summary>
        /// <lang>
        ///   <zh-CN>角色数据访问依赖。</zh-CN>
        ///   <en>Role data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IRolesDb RolesDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>Tab 数据访问依赖。</zh-CN>
        ///   <en>Tab data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public ITabsDb TabsConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>模块实例数据访问依赖。</zh-CN>
        ///   <en>Module-instance data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IModulesDb ModulesConfig { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>授权、读取可选导航参数并在首次请求绑定当前门户角色。</zh-CN>
        ///   <en>Authorizes, reads optional navigation parameters, and binds current-Portal roles on the initial request.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
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
        /// <lang>
        ///   <zh-CN>创建一个当前门户内唯一的默认角色，并进入编辑状态。</zh-CN>
        ///   <en>Creates a unique default role in the current Portal and enters edit mode.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>事件数据。</zh-CN>
        ///   <en>Event data.</en>
        /// </l>
        /// </param>
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
        /// <lang>
        ///   <zh-CN>处理角色编辑、改名、删除和成员管理命令。</zh-CN>
        ///   <en>Handles role edit, rename, delete, and membership-management commands.</en>
        /// </lang>
        /// </summary>
        /// <param name="sender">
        /// <l>
        ///   <zh-CN>事件源。</zh-CN>
        ///   <en>Event source.</en>
        /// </l>
        /// </param>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>包含命令和 DataList 项索引的事件数据。</zh-CN>
        ///   <en>Event data containing the command and DataList item index.</en>
        /// </l>
        /// </param>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取角色后台在模块编辑框架中需要保留的 Tab 导航参数。</zh-CN>
        ///   <en>Reads Tab navigation parameters that the role administration control must preserve inside the module-editing frame.</en>
        /// </lang>
        /// </summary>
        /// <returns>
        /// <l>
        ///   <zh-CN>参数缺失或合法时返回 <c>true</c>；非法时已重定向并返回 <c>false</c>。</zh-CN>
        ///   <en><c>true</c> when parameters are absent or valid; <c>false</c> after redirecting for invalid input.</en>
        /// </l>
        /// </returns>
        private bool TryReadNavigationParameters()
        {
            return TryReadOptionalPositiveParameter("tabid", out tabId) &&
                   TryReadOptionalPositiveParameter("tabindex", out tabIndex);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>读取一个可选的正整数导航参数。</zh-CN>
        ///   <en>Reads one optional positive-integer navigation parameter.</en>
        /// </lang>
        /// </summary>
        /// <param name="parameterName">
        /// <l>
        ///   <zh-CN>请求参数名称。</zh-CN>
        ///   <en>Request parameter name.</en>
        /// </l>
        /// </param>
        /// <param name="value">
        /// <l>
        ///   <zh-CN>解析后的数值；参数缺失时为 0。</zh-CN>
        ///   <en>Parsed value, or 0 when the parameter is absent.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>参数缺失或合法时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the parameter is absent or valid.</en>
        /// </l>
        /// </returns>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>非法参数不回显原始值，直接进入统一的编辑拒绝页，避免把后台导航参数变成探测入口。</zh-CN>
        ///   <en>Invalid values are not echoed; the request moves to the shared edit-denied page so administration navigation parameters do not become a probing surface.</en>
        /// </lang>
        /// </remarks>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>从 DataList 命令上下文中解析并验证当前门户角色。</zh-CN>
        ///   <en>Resolves and validates the current-Portal role from the DataList command context.</en>
        /// </lang>
        /// </summary>
        /// <param name="e">
        /// <l>
        ///   <zh-CN>包含 DataList 项和 DataKeys 的命令事件数据。</zh-CN>
        ///   <en>Command event data containing the DataList item and DataKeys.</en>
        /// </l>
        /// </param>
        /// <param name="role">
        /// <l>
        ///   <zh-CN>解析出的当前门户角色；失败时为 <c>null</c>。</zh-CN>
        ///   <en>Resolved current-Portal role, or <c>null</c> on failure.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>角色存在且属于当前门户时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the role exists and belongs to the current Portal.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>在当前门户角色集合中查找指定角色。</zh-CN>
        ///   <en>Finds the specified role within the current Portal's role collection.</en>
        /// </lang>
        /// </summary>
        /// <param name="roleId">
        /// <l>
        ///   <zh-CN>角色标识。</zh-CN>
        ///   <en>Role identifier.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>匹配角色；不属于当前门户或不存在时为 <c>null</c>。</zh-CN>
        ///   <en>Matching role, or <c>null</c> when absent or outside the current Portal.</en>
        /// </l>
        /// </returns>
        private IRoleItem FindCurrentPortalRole(int roleId)
        {
            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            return RolesDB.GetPortalRoles(portalSettings.PortalId)
                .FirstOrDefault(item => item.RoleId == roleId);
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>校验并保存角色改名，同时同步旧门户授权字符串中的精确角色引用。</zh-CN>
        ///   <en>Validates and saves a role rename while synchronizing exact role references in legacy Portal authorization strings.</en>
        /// </lang>
        /// </summary>
        /// <param name="role">
        /// <l>
        ///   <zh-CN>待改名角色。</zh-CN>
        ///   <en>Role to rename.</en>
        /// </l>
        /// </param>
        /// <param name="requestedName">
        /// <l>
        ///   <zh-CN>管理员提交的角色名称。</zh-CN>
        ///   <en>Role name submitted by the administrator.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>改名成功或名称未变化时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the rename succeeds or the name is unchanged.</en>
        /// </l>
        /// </returns>
        private bool TryRenameRole(IRoleItem role, string requestedName)
        {
            string roleName;
            if (!PortalAdministrationPolicy.TryNormalizeRoleName(requestedName, out roleName))
            {
                ShowMessage("角色名称无效，未保存本次修改。");
                return false;
            }

            // <lang>
            //   <zh-CN>同一门户内角色名称必须保持唯一；跨门户同名不在本控件处理范围内。</zh-CN>
            //   <en>Role names must be unique within the same Portal; same names across Portals are outside this control's scope.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>核心管理员角色是旧门户安全兜底角色，当前阶段禁止改名。</zh-CN>
            //   <en>The core administrator role is the legacy Portal safety backstop and cannot be renamed in the current stage.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>同步 Tab 访问角色和模块编辑角色中的精确角色名称引用。</zh-CN>
        ///   <en>Synchronizes exact role-name references in Tab-access roles and module-edit roles.</en>
        /// </lang>
        /// </summary>
        /// <param name="portalSettings">
        /// <l>
        ///   <zh-CN>当前门户设置快照。</zh-CN>
        ///   <en>Current Portal settings snapshot.</en>
        /// </l>
        /// </param>
        /// <param name="oldRoleName">
        /// <l>
        ///   <zh-CN>旧角色名称。</zh-CN>
        ///   <en>Old role name.</en>
        /// </l>
        /// </param>
        /// <param name="newRoleName">
        /// <l>
        ///   <zh-CN>新角色名称。</zh-CN>
        ///   <en>New role name.</en>
        /// </l>
        /// </param>
        /// <remarks>
        /// <lang>
        ///   <zh-CN>此处只替换分号角色列表中的完整角色项，避免把角色名当普通子串误改。</zh-CN>
        ///   <en>This replaces only complete role entries in semicolon role lists, avoiding accidental substring rewrites.</en>
        /// </lang>
        /// </remarks>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>在确认角色无成员、无授权引用后删除角色定义。</zh-CN>
        ///   <en>Deletes a role definition after confirming it has no members and no authorization references.</en>
        /// </lang>
        /// </summary>
        /// <param name="role">
        /// <l>
        ///   <zh-CN>待删除角色。</zh-CN>
        ///   <en>Role to delete.</en>
        /// </l>
        /// </param>
        private void DeleteRole(IRoleItem role)
        {
            // <lang>
            //   <zh-CN>删除路径先做业务防护，再进入数据库删除，避免留下无法从 UI 恢复的权限空洞。</zh-CN>
            //   <en>The delete path performs business guards before database deletion to avoid authorization gaps that the UI cannot recover.</en>
            // </lang>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>检查当前门户的 Tab 和模块授权字符串是否仍引用指定角色。</zh-CN>
        ///   <en>Checks whether current-Portal Tab and module authorization strings still reference the specified role.</en>
        /// </lang>
        /// </summary>
        /// <param name="portalSettings">
        /// <l>
        ///   <zh-CN>当前门户设置快照。</zh-CN>
        ///   <en>Current Portal settings snapshot.</en>
        /// </l>
        /// </param>
        /// <param name="roleName">
        /// <l>
        ///   <zh-CN>待检查角色名称。</zh-CN>
        ///   <en>Role name to check.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>仍存在引用时返回 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when a reference still exists.</en>
        /// </l>
        /// </returns>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>替换分号角色字符串中的完整角色项。</zh-CN>
        ///   <en>Replaces a complete role entry in a semicolon role string.</en>
        /// </lang>
        /// </summary>
        /// <param name="roles">
        /// <l>
        ///   <zh-CN>旧门户分号角色字符串。</zh-CN>
        ///   <en>Legacy Portal semicolon role string.</en>
        /// </l>
        /// </param>
        /// <param name="oldRoleName">
        /// <l>
        ///   <zh-CN>旧角色名称。</zh-CN>
        ///   <en>Old role name.</en>
        /// </l>
        /// </param>
        /// <param name="newRoleName">
        /// <l>
        ///   <zh-CN>新角色名称。</zh-CN>
        ///   <en>New role name.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>替换后的规范化角色字符串。</zh-CN>
        ///   <en>Normalized role string after replacement.</en>
        /// </l>
        /// </returns>
        private static string ReplaceRoleReference(string roles, string oldRoleName, string newRoleName)
        {
            return PortalRoleParser.Join(
                PortalRoleParser.Parse(roles)
                    .Select(role => string.Equals(role, oldRoleName, StringComparison.OrdinalIgnoreCase)
                        ? newRoleName
                        : role));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>为新增角色生成当前门户内唯一的默认名称。</zh-CN>
        ///   <en>Generates a default role name that is unique within the current Portal.</en>
        /// </lang>
        /// </summary>
        /// <param name="roles">
        /// <l>
        ///   <zh-CN>当前门户已有角色集合。</zh-CN>
        ///   <en>Existing role collection for the current Portal.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>可直接插入的默认角色名称。</zh-CN>
        ///   <en>Default role name ready to insert.</en>
        /// </l>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <l>
        ///   <zh-CN>无法在预设范围内生成唯一名称时抛出。</zh-CN>
        ///   <en>Thrown when a unique name cannot be generated within the predefined range.</en>
        /// </l>
        /// </exception>
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

        /// <summary>
        /// <lang>
        ///   <zh-CN>重新绑定当前门户的角色列表。</zh-CN>
        ///   <en>Rebinds the current Portal's role list.</en>
        /// </lang>
        /// </summary>
        private void BindData()
        {
            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            rolesList.DataSource = RolesDB.GetPortalRoles(portalSettings.PortalId);
            rolesList.DataBind();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>向后台页面输出经过 HTML 编码的安全提示。</zh-CN>
        ///   <en>Outputs an HTML-encoded safe message to the administration page.</en>
        /// </lang>
        /// </summary>
        /// <param name="message">
        /// <l>
        ///   <zh-CN>需要展示的提示文本。</zh-CN>
        ///   <en>Message text to display.</en>
        /// </l>
        /// </param>
        private void ShowMessage(string message)
        {
            Message.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
