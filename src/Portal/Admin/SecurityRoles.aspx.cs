using System;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>旧门户角色成员关系管理页面。</zh-CN>
    ///   <en>Legacy Portal role-membership administration page.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>角色名称始终从当前门户的 roleId 读取，不信任 URL 中的显示名称。角色成员增删不会立即撤销 目标用户的既有角色 Cookie。</zh-CN>
    ///   <en>The role name is always read from the current Portal roleId and never trusted from a URL display value. Adding or removing membership does not immediately revoke the target user's existing role cookie.</en>
    /// </lang>
    /// </remarks>
    public partial class SecurityRoles : PortalPage<SecurityRoles>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>当前通过权限和门户角色集合校验的角色标识。</zh-CN>
        ///   <en>The role identifier verified against permissions and the current Portal role set.</en>
        /// </lang>
        /// </summary>
        private int roleId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选的安全回跳 Tab 标识。</zh-CN>
        ///   <en>The optional Tab identifier used for safe return navigation.</en>
        /// </lang>
        /// </summary>
        private int tabId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选的安全回跳 Tab 索引。</zh-CN>
        ///   <en>The optional Tab index used for safe return navigation.</en>
        /// </lang>
        /// </summary>
        private int tabIndex;

        /// <summary>
        /// <lang>
        ///   <zh-CN>已通过权限和角色集合校验的当前门户角色快照。</zh-CN>
        ///   <en>The current Portal-role snapshot after permission and role-set validation.</en>
        /// </lang>
        /// </summary>
        private IRoleItem currentRole;

        /// <summary>
        /// <lang>
        ///   <zh-CN>用户数据访问依赖。</zh-CN>
        ///   <en>User data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>角色和成员关系数据访问依赖。</zh-CN>
        ///   <en>Role and membership data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IRolesDb RolesDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>授权、验证当前门户角色并在首次请求绑定成员列表。</zh-CN>
        ///   <en>Authorizes, validates the current-Portal role, and binds membership lists on the initial request.</en>
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
            //   <zh-CN>先重做权限、角色和导航参数门禁，避免未验证角色进入绑定或成员事件。</zh-CN>
            //   <en>Reapply permission, role, and navigation gates before binding or membership events can use an unverified role.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>仅首次请求加载成员和可选用户，保留 Web Forms 回发字段。</zh-CN>
            //   <en>Load members and selectable users only on the initial request, preserving Web Forms postback fields.</en>
            // </lang>
            if (!Page.IsPostBack)
            {
                BindData();
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>返回门户后台主页，不额外写入角色关系。</zh-CN>
        ///   <en>Returns to the Portal administration home without writing additional role relationships.</en>
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
        protected void Save_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>保存按钮不直接写角色关系，只在重新验证后执行受控回跳。</zh-CN>
            //   <en>The save button does not write membership directly; it performs controlled return navigation only after revalidation.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, BuildPortalReturnUrl());
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>将选择的既有用户加入当前角色。</zh-CN>
        ///   <en>Adds the selected existing user to the current role.</en>
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
        protected void AddUser_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>成员新增事件重新解析角色和请求上下文，防止陈旧页面状态绕过门禁。</zh-CN>
            //   <en>Re-resolve the role and request context for member addition so stale page state cannot bypass the gate.</en>
            // </lang>
            if (!TryInitializeRequest())
            {
                return;
            }

            // <lang>
            //   <zh-CN>没有选择项时不访问用户服务，也不改变角色关系。</zh-CN>
            //   <en>Do not call the user service or change membership when no option is selected.</en>
            // </lang>
            if (allUsers.SelectedItem == null)
            {
                ShowMessage("请选择一个有效用户。");
                return;
            }

            // <lang>
            //   <zh-CN>选中值必须是正整数且对应当前可读取用户，避免直接信任下拉项值。</zh-CN>
            //   <en>Require a positive id that resolves to a readable user instead of trusting the dropdown value directly.</en>
            // </lang>
            int userId;
            if (!PortalNavigationPolicy.TryReadPositiveInt32(allUsers.SelectedItem.Value, out userId) ||
                UsersDB.FindUserById(userId) == null)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            try
            {
                // <lang>
                //   <zh-CN>只向已验证角色加入已验证用户，并在成功后记录审计、刷新读模型。</zh-CN>
                //   <en>Add only the verified user to the verified role, then audit success and refresh the read model.</en>
                // </lang>
                RolesDB.AddUserRole(roleId, userId);
                PortalOperationAudit.Record(
                    "RoleAdministration",
                    "AddMember",
                    "Role",
                    roleId.ToString(),
                    "Added user id " + userId + " to role.",
                    Context);
                BindData();
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>成员新增异常写入统一诊断并返回事件编号，不暴露数据层细节。</zh-CN>
                //   <en>Record member-add failures through shared diagnostics and return an event id without exposing data-layer details.</en>
                // </lang>
                string eventId = PortalDiagnostics.Error(
                    "Admin.SecurityRoles.AddUser",
                    "Adding a role member failed. RoleId=" + roleId + "; UserId=" + userId,
                    exception,
                    Context);
                ShowMessage("角色成员添加失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>从当前角色移除选择的成员。</zh-CN>
        ///   <en>Removes the selected member from the current role.</en>
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
        protected void usersInRole_ItemCommand(object sender, DataListCommandEventArgs e)
        {
            // <lang>
            //   <zh-CN>仅处理不区分大小写的 delete 命令，其他命令保持无副作用。</zh-CN>
            //   <en>Handle only the case-insensitive delete command; other commands remain side-effect free.</en>
            // </lang>
            if (!TryInitializeRequest() || !string.Equals(e.CommandName, "delete", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // <lang>
            //   <zh-CN>校验行对象、索引、DataKeys 和用户存在性，防止篡改行索引删除其他用户。</zh-CN>
            //   <en>Validate the row, index, DataKeys entry, and user existence so a tampered row index cannot delete another user.</en>
            // </lang>
            int userId;
            if (e.Item == null || e.Item.ItemIndex < 0 || e.Item.ItemIndex >= usersInRole.DataKeys.Count ||
                !PortalNavigationPolicy.TryReadPositiveInt32(usersInRole.DataKeys[e.Item.ItemIndex].ToString(), out userId) ||
                UsersDB.FindUserById(userId) == null)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            try
            {
                // <lang>
                //   <zh-CN>删除成功后清除编辑索引、写入审计并刷新成员读模型。</zh-CN>
                //   <en>After successful deletion, clear the edit index, write the audit, and refresh the membership read model.</en>
                // </lang>
                RolesDB.DeleteUserRole(roleId, userId);
                PortalOperationAudit.Record(
                    "RoleAdministration",
                    "RemoveMember",
                    "Role",
                    roleId.ToString(),
                    "Removed user id " + userId + " from role.",
                    Context);
                usersInRole.EditItemIndex = -1;
                BindData();
            }
            catch (Exception exception)
            {
                // <lang>
                //   <zh-CN>成员移除异常沿用统一诊断和低敏页面反馈。</zh-CN>
                //   <en>Member-removal failures use shared diagnostics and low-sensitivity page feedback.</en>
                // </lang>
                string eventId = PortalDiagnostics.Error(
                    "Admin.SecurityRoles.RemoveUser",
                    "Removing a role member failed. RoleId=" + roleId + "; UserId=" + userId,
                    exception,
                    Context);
                ShowMessage("角色成员移除失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        private bool TryInitializeRequest()
        {
            // <lang>
            //   <zh-CN>角色编辑权限、角色标识和可选回跳参数共同构成入口门禁。</zh-CN>
            //   <en>The role-edit permission, role id, and optional return parameters form the entry gate.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminRolesEdit) ||
                !PortalNavigationPolicy.TryReadNonNegativeInt32(Request.Params["roleid"], out roleId) ||
                !TryReadOptionalPositiveParameter("tabid", out tabId) ||
                !TryReadOptionalNonNegativeParameter("tabindex", out tabIndex))
            {
                // <lang>
                //   <zh-CN>只有已具备角色编辑权限的请求才重定向到编辑拒绝页，其他请求沿用授权组件处理。</zh-CN>
                //   <en>Redirect to edit-denied only when role-edit permission is known; other requests retain the authorization component's handling.</en>
                // </lang>
                if (PortalAuthorization.HasPermission(PortalPermissionKeys.AdminRolesEdit))
                {
                    PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                }

                return false;
            }

            // <lang>
            //   <zh-CN>读取当前门户角色快照，角色名称和成员范围均来自此数据源而非 URL 显示值。</zh-CN>
            //   <en>Read the current Portal role snapshot so role names and membership scope come from data, not URL display values.</en>
            // </lang>
            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            // <lang>
            //   <zh-CN>只在当前门户角色集合中匹配 roleId，阻断跨门户或不存在角色的操作。</zh-CN>
            //   <en>Match roleId only within the current Portal role set, blocking cross-Portal or missing-role operations.</zh-CN>
            // </lang>
            currentRole = RolesDB.GetPortalRoles(portalSettings.PortalId)
                .FirstOrDefault(role => role.RoleId == roleId);
            if (currentRole != null)
            {
                return true;
            }

            // <lang>
            //   <zh-CN>已解析但不存在的角色不允许继续绑定、添加或删除成员。</zh-CN>
            //   <en>A parsed but missing role cannot continue to bind, add, or remove members.</en>
            // </lang>
            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

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
            //   <zh-CN>非法回跳参数统一拒绝，不使用未经验证的上下文构造 URL。</zh-CN>
            //   <en>Reject invalid return parameters consistently instead of constructing a URL from unverified context.</en>
            // </lang>
            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private bool TryReadOptionalNonNegativeParameter(string parameterName, out int value)
        {
            value = 0;
            // <lang>
            //   <zh-CN>索引允许零，但仍需通过非负整数策略后才能参与回跳。</zh-CN>
            //   <en>The index permits zero but must pass non-negative validation before it participates in navigation.</en>
            // </lang>
            string rawValue = Request.Params[parameterName];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadNonNegativeInt32(rawValue, out value))
            {
                return true;
            }

            // <lang>
            //   <zh-CN>非法索引不降级为默认值，直接进入编辑拒绝路径。</zh-CN>
            //   <en>Do not downgrade an invalid index to a default; route it directly to edit-denied.</en>
            // </lang>
            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private void BindData()
        {
            // <lang>
            //   <zh-CN>标题使用已验证角色快照，成员和用户列表从当前门户数据源绑定。</zh-CN>
            //   <en>Use the verified role snapshot for the title and bind member/user lists from current Portal data sources.</en>
            // </lang>
            title.InnerText = "Role Membership: " + (currentRole.RoleName ?? string.Empty);
            usersInRole.DataSource = RolesDB.GetRoleMembers(roleId);
            usersInRole.DataBind();
            allUsers.DataSource = RolesDB.GetUsers();
            allUsers.DataBind();
        }

        private string BuildPortalReturnUrl()
        {
            // <lang>
            //   <zh-CN>仅当 Tab 标识和索引均为正数时构造带上下文回跳，否则回到固定桌面首页。</zh-CN>
            //   <en>Build a contextual return URL only when both Tab id and index are positive; otherwise use the fixed desktop home.</en>
            // </lang>
            if (tabId <= 0 || tabIndex <= 0)
            {
                return ResolveUrl("~/DesktopDefault.aspx");
            }

            return ResolveUrl("~/DesktopDefault.aspx?tabindex=" + tabIndex + "&tabid=" + tabId);
        }

        private void ShowMessage(string message)
        {
            // <lang>
            //   <zh-CN>所有提示先 HTML 编码并将空值归一化，避免异常文本进入标记输出。</zh-CN>
            //   <en>HTML-encode every message and normalize null before it reaches markup output.</en>
            // </lang>
            Message.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
