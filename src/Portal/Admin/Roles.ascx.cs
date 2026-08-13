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
        /// <summary>
        /// <lang>
        ///   <zh-CN>可选的当前门户 Tab 标识，用于角色管理回跳上下文。</zh-CN>
        ///   <en>The optional current-Portal Tab id used to preserve role-management return context.</en>
        /// </lang>
        /// </summary>
        private int tabId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选的当前门户 Tab 索引，用于角色成员页安全回跳。</zh-CN>
        ///   <en>The optional current-Portal Tab index used for safe return navigation to membership management.</en>
        /// </lang>
        /// </summary>
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
            // <lang>
            //   <zh-CN>权限和导航参数是角色列表、创建、编辑、删除及成员入口的共同门禁。</zh-CN>
            //   <en>Permission and navigation parameters gate role listing, creation, editing, deletion, and membership entry points.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminRolesEdit) || !TryReadNavigationParameters())
            {
                return;
            }

            // <lang>
            //   <zh-CN>只在首次请求绑定角色列表，避免回发覆盖正在编辑的控件状态。</zh-CN>
            //   <en>Bind the role list only on the initial request so postback editing state is not overwritten.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>创建事件再次验证权限和导航上下文，防止陈旧模块状态触发新增。</zh-CN>
            //   <en>Revalidate permission and navigation context before creation so stale module state cannot trigger an add.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminRolesEdit) || !TryReadNavigationParameters())
            {
                return;
            }

            try
            {
                // <lang>
                //   <zh-CN>从当前门户读取角色集合，生成唯一默认名称后写入角色定义。</zh-CN>
                //   <en>Read current-Portal roles, generate a unique default name, and persist the new role definition.</en>
                // </lang>
                PortalSettings portalSettings = PortalContext.GetPortalSettings();
                // <lang>
                //   <zh-CN>默认名称生成器只在当前门户集合内保证唯一。</zh-CN>
                //   <en>The default-name generator guarantees uniqueness only within the current Portal collection.</en>
                // </lang>
                string roleName = CreateUniqueDefaultRoleName(RolesDB.GetPortalRoles(portalSettings.PortalId));
                // <lang>
                //   <zh-CN>保存成功后记录角色创建审计并刷新列表进入编辑状态。</zh-CN>
                //   <en>After persistence succeeds, audit role creation and refresh the list in edit state.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>创建异常写入诊断并只显示事件编号。</zh-CN>
                //   <en>Record creation failures through diagnostics and display only the event id.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>每个命令先验证页面权限、导航参数和 DataList 角色归属。</zh-CN>
            //   <en>Validate page permission, navigation parameters, and DataList role ownership before every command.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminRolesEdit) || !TryReadNavigationParameters())
            {
                return;
            }

            // <lang>
            //   <zh-CN>DataList 键必须解析为当前门户角色，避免篡改索引操作其他角色。</zh-CN>
            //   <en>The DataList key must resolve to a current-Portal role so a tampered index cannot target another role.</en>
            // </lang>
            IRoleItem role;
            if (!TryGetRoleFromDataList(e, out role))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            // <lang>
            //   <zh-CN>edit 命令只切换当前行编辑状态并刷新展示，不写入角色。</zh-CN>
            //   <en>The edit command only switches the current row to edit mode and refreshes display without persisting a role.</en>
            // </lang>
            if (string.Equals(e.CommandName, "edit", StringComparison.OrdinalIgnoreCase))
            {
                rolesList.EditItemIndex = e.Item.ItemIndex;
                BindData();
                return;
            }

            if (string.Equals(e.CommandName, "apply", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.CommandName, "members", StringComparison.OrdinalIgnoreCase))
            {
                // <lang>
                //   <zh-CN>apply/members 共用名称规范化和改名流程，失败时不继续导航或刷新。</zh-CN>
                //   <en>apply and members share the rename normalization flow; failure stops navigation and refresh.</en>
                // </lang>
                TextBox roleNameTextBox = e.Item.FindControl("roleName") as TextBox;
                if (roleNameTextBox == null || !TryRenameRole(role, roleNameTextBox.Text))
                {
                    return;
                }

                if (string.Equals(e.CommandName, "members", StringComparison.OrdinalIgnoreCase))
                {
                    // <lang>
                    //   <zh-CN>成员入口只携带已验证角色和兼容导航参数，并交给安全导航策略。</zh-CN>
                    //   <en>The membership entry carries only the verified role and compatibility navigation parameters through safe navigation.</en>
                    // </lang>
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
                // <lang>
                //   <zh-CN>删除命令交由 DeleteRole 统一执行管理员保护、成员和引用检查。</zh-CN>
                //   <en>Delegate delete to DeleteRole so administrator, member, and reference guards stay centralized.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>两个可选参数分别保留 Tab 标识和索引；任一非法值都会阻断后续命令。</zh-CN>
            //   <en>Read the optional Tab id and index together; any invalid value blocks subsequent commands.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>缺失参数保留兼容默认值 0；存在参数必须通过正整数策略。</zh-CN>
            //   <en>Keep the compatibility default of 0 when absent; a supplied value must pass positive-integer validation.</en>
            // </lang>
            string rawValue = Request.Params[parameterName];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadPositiveInt32(rawValue, out value))
            {
                return true;
            }

            // <lang>
            //   <zh-CN>非法导航参数统一拒绝，不将原始值回显到后台页面。</zh-CN>
            //   <en>Reject invalid navigation input consistently without echoing the raw value into the administration page.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>先验证行对象和 DataKeys 索引，再解析非负角色标识。</zh-CN>
            //   <en>Validate the row and DataKeys index before parsing the non-negative role identifier.</en>
            // </lang>
            role = null;
            // <lang>
            //   <zh-CN>角色标识来自服务器 DataKeys，不直接信任客户端行文本。</zh-CN>
            //   <en>The role id comes from server DataKeys rather than trusting client row text.</en>
            // </lang>
            int roleId;
            if (e.Item == null || e.Item.ItemIndex < 0 || e.Item.ItemIndex >= rolesList.DataKeys.Count ||
                !PortalNavigationPolicy.TryReadNonNegativeInt32(rolesList.DataKeys[e.Item.ItemIndex].ToString(), out roleId))
            {
                return false;
            }

            // <lang>
            //   <zh-CN>只接受当前门户集合中的角色，阻断跨门户或陈旧角色操作。</zh-CN>
            //   <en>Accept only a role in the current Portal collection, blocking cross-Portal or stale operations.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>从当前门户设置读取角色集合，角色名称和成员范围不来自请求参数。</zh-CN>
            //   <en>Read the role collection from current Portal settings so names and membership scope do not come from request parameters.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>名称先经过角色名策略归一化；无效输入不访问角色或授权数据。</zh-CN>
            //   <en>Normalize the name through the role-name policy first; invalid input performs no role or authorization data access.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>读取当前门户快照并在同一门户内排除当前角色后检查重名。</zh-CN>
            //   <en>Read the current Portal snapshot and check duplicates within that Portal while excluding the current role.</en>
            // </lang>
            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            // <lang>
            //   <zh-CN>重名比较不区分大小写，避免授权字符串出现看似不同但实际冲突的角色。</zh-CN>
            //   <en>Compare names case-insensitively to prevent authorization conflicts that differ only by case.</en>
            // </lang>
            bool duplicate = RolesDB.GetPortalRoles(portalSettings.PortalId).Any(item =>
                item.RoleId != role.RoleId &&
                string.Equals(item.RoleName, roleName, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
            {
                ShowMessage("当前门户已存在同名角色，未保存本次修改。");
                return false;
            }

            // <lang>
            //   <zh-CN>名称未变化时直接视为成功，不触碰数据库或授权引用。</zh-CN>
            //   <en>Treat an unchanged name as success without touching persistence or authorization references.</en>
            // </lang>
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
                // <lang>
                //   <zh-CN>保存旧名称用于精确同步；角色更新成功后再更新 Tab/模块授权引用并写审计。</zh-CN>
                //   <en>Keep the old name for exact synchronization; update the role first, then references and audit.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>改名或引用同步失败写入统一诊断并返回事件编号。</zh-CN>
                //   <en>Record rename or reference-synchronization failures through shared diagnostics and return an event id.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>遍历当前门户 Tab 及其模块，只更新完整角色项发生变化的记录。</zh-CN>
            //   <en>Walk current-Portal Tabs and modules, updating only records whose complete role entries changed.</en>
            // </lang>
            foreach (ITabItem tab in portalSettings.DesktopTabs)
            {
                // <lang>
                //   <zh-CN>先计算 Tab 访问角色的新串；无变化时不产生写入。</zh-CN>
                //   <en>Compute the new Tab access-role string first; unchanged values produce no write.</en>
                // </lang>
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
                    // <lang>
                    //   <zh-CN>模块编辑角色同样按完整项替换，保留原有布局和模块字段。</zh-CN>
                    //   <en>Replace complete module edit-role entries while preserving existing layout and module fields.</en>
                    // </lang>
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

            // <lang>
            //   <zh-CN>引用检查覆盖当前门户 Tab 和模块编辑角色，删除前不隐式清理授权。</zh-CN>
            //   <en>Reference checks cover current-Portal Tab and module edit roles; deletion never performs implicit authorization cleanup.</en>
            // </lang>
            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            if (HasRoleReferences(portalSettings, role.RoleName))
            {
                ShowMessage("角色仍被 Tab 或模块引用，不能删除。");
                return;
            }

            try
            {
                // <lang>
                //   <zh-CN>仅在管理员、成员和授权引用检查均通过后删除，并记录成功审计。</zh-CN>
                //   <en>Delete only after administrator, member, and authorization-reference guards pass, then record success.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>删除异常写入诊断并保持页面低敏反馈。</zh-CN>
                //   <en>Record deletion failures through diagnostics and keep page feedback low sensitivity.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>对每个 Tab 先检查访问角色，再检查其模块编辑角色；发现一处即阻断删除。</zh-CN>
            //   <en>Check each Tab's access roles and then its module edit roles; any match blocks deletion immediately.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>先按旧分号协议解析完整角色项，再仅替换大小写不敏感匹配项并重新拼接。</zh-CN>
            //   <en>Parse complete entries under the legacy semicolon contract, replace only case-insensitive matches, and join them again.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>把现有角色名放入不区分大小写集合，控制默认名称生成的唯一性范围。</zh-CN>
            //   <en>Place existing names in a case-insensitive set to constrain default-name uniqueness.</en>
            // </lang>
            var existingNames = new HashSet<string>(
                roles.Select(item => item.RoleName ?? string.Empty),
                StringComparer.OrdinalIgnoreCase);
            if (!existingNames.Contains("New Role"))
            {
                return "New Role";
            }

            // <lang>
            //   <zh-CN>在有限后缀范围内寻找第一个未占用名称，避免无限循环。</zh-CN>
            //   <en>Search for the first unused suffix within a bounded range to avoid an unbounded loop.</en>
            // </lang>
            for (int suffix = 2; suffix < 1000; suffix++)
            {
                // <lang>
                //   <zh-CN>候选名称由固定前缀和当前后缀组成，并再次通过集合检查。</zh-CN>
                //   <en>Build each candidate from the fixed prefix and current suffix, then check it against the set.</en>
                // </lang>
                string candidate = "New Role " + suffix;
                if (!existingNames.Contains(candidate))
                {
                    return candidate;
                }
            }

            // <lang>
            //   <zh-CN>所有预设名称均被占用时显式失败，不回退到重复名称。</zh-CN>
            //   <en>Fail explicitly when all preset names are occupied instead of falling back to a duplicate.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>从当前门户读取角色集合并绑定列表；绑定阶段不执行写入。</zh-CN>
            //   <en>Read the current-Portal role collection and bind the list without performing writes.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>提示统一 HTML 编码并将空值归一化，避免数据或异常文本进入标记输出。</zh-CN>
            //   <en>HTML-encode messages and normalize null so data or exception text cannot enter markup output.</en>
            // </lang>
            Message.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
