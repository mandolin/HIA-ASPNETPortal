using System;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：旧门户角色成员关系管理页面。
    ///
    /// English: Legacy Portal role-membership administration page.
    /// </summary>
    /// <remarks>
    /// 中文：角色名称始终从当前门户的 roleId 读取，不信任 URL 中的显示名称。角色成员增删不会立即撤销
    /// 目标用户的既有角色 Cookie。
    ///
    /// English: The role name is always read from the current Portal roleId and never trusted from a URL display
    /// value. Adding or removing membership does not immediately revoke the target user's existing role cookie.
    /// </remarks>
    public partial class SecurityRoles : PortalPage<SecurityRoles>
    {
        private int roleId;
        private int tabId;
        private int tabIndex;
        private IRoleItem currentRole;

        /// <summary>中文：用户数据访问依赖。English: User data-access dependency.</summary>
        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        /// <summary>中文：角色和成员关系数据访问依赖。English: Role and membership data-access dependency.</summary>
        [Dependency]
        public IRolesDb RolesDB { private get; set; }

        /// <summary>
        /// 中文：授权、验证当前门户角色并在首次请求绑定成员列表。
        ///
        /// English: Authorizes, validates the current-Portal role, and binds membership lists on the initial request.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                BindData();
            }
        }

        /// <summary>
        /// 中文：返回门户后台主页，不额外写入角色关系。
        ///
        /// English: Returns to the Portal administration home without writing additional role relationships.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Save_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, BuildPortalReturnUrl());
        }

        /// <summary>
        /// 中文：将选择的既有用户加入当前角色。
        ///
        /// English: Adds the selected existing user to the current role.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void AddUser_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            if (allUsers.SelectedItem == null)
            {
                ShowMessage("请选择一个有效用户。");
                return;
            }

            int userId;
            if (!PortalNavigationPolicy.TryReadPositiveInt32(allUsers.SelectedItem.Value, out userId) ||
                UsersDB.FindUserById(userId) == null)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            try
            {
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
                string eventId = PortalDiagnostics.Error(
                    "Admin.SecurityRoles.AddUser",
                    "Adding a role member failed. RoleId=" + roleId + "; UserId=" + userId,
                    exception,
                    Context);
                ShowMessage("角色成员添加失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        /// <summary>
        /// 中文：从当前角色移除选择的成员。
        ///
        /// English: Removes the selected member from the current role.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：包含命令和 DataList 项索引的事件数据。English: Event data containing the command and DataList item index.</param>
        protected void usersInRole_ItemCommand(object sender, DataListCommandEventArgs e)
        {
            if (!TryInitializeRequest() || !string.Equals(e.CommandName, "delete", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

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
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminRolesEdit) ||
                !PortalNavigationPolicy.TryReadNonNegativeInt32(Request.Params["roleid"], out roleId) ||
                !TryReadOptionalPositiveParameter("tabid", out tabId) ||
                !TryReadOptionalNonNegativeParameter("tabindex", out tabIndex))
            {
                if (PortalAuthorization.HasPermission(PortalPermissionKeys.AdminRolesEdit))
                {
                    PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                }

                return false;
            }

            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            currentRole = RolesDB.GetPortalRoles(portalSettings.PortalId)
                .FirstOrDefault(role => role.RoleId == roleId);
            if (currentRole != null)
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
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

        private bool TryReadOptionalNonNegativeParameter(string parameterName, out int value)
        {
            value = 0;
            string rawValue = Request.Params[parameterName];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (PortalNavigationPolicy.TryReadNonNegativeInt32(rawValue, out value))
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private void BindData()
        {
            title.InnerText = "Role Membership: " + (currentRole.RoleName ?? string.Empty);
            usersInRole.DataSource = RolesDB.GetRoleMembers(roleId);
            usersInRole.DataBind();
            allUsers.DataSource = RolesDB.GetUsers();
            allUsers.DataBind();
        }

        private string BuildPortalReturnUrl()
        {
            if (tabId <= 0 || tabIndex <= 0)
            {
                return ResolveUrl("~/DesktopDefault.aspx");
            }

            return ResolveUrl("~/DesktopDefault.aspx?tabindex=" + tabIndex + "&tabid=" + tabId);
        }

        private void ShowMessage(string message)
        {
            Message.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
