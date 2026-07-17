using System;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：旧后台用户资料、角色和注册审核管理页面。
    ///
    /// English: Legacy administration page for user profiles, roles, and registration review.
    /// </summary>
    /// <remarks>
    /// 中文：页面要求 <c>Admins</c> 角色，并只编辑能由数值 userId 规范解析到的用户。用户创建由
    /// <c>Users.ascx</c> 的显式管理员 POST 完成；本页不会因访问地址缺少用户名而写入数据库。
    /// 角色调整和密码重置会递增目标用户安全版本，使旧身份票据和角色 Cookie 在后续请求中失效。
    ///
    /// English: The page requires the <c>Admins</c> role and edits only a user canonically resolved by numeric userId.
    /// User creation occurs through the explicit administrator POST in <c>Users.ascx</c>; this page never writes to
    /// the database merely because an address lacks a user name. Role changes and password resets increment the target
    /// user's security version so older authentication and role cookies are invalidated on later requests.
    /// </remarks>
    public partial class ManageUsers : PortalPage<ManageUsers>
    {
        private int tabId;
        private int tabIndex;
        private int userId;
        private IUserItem currentUser;

        /// <summary>
        /// 中文：用户和注册审核数据访问依赖。
        ///
        /// English: User and registration-review data-access dependency.
        /// </summary>
        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        /// <summary>
        /// 中文：角色和成员关系数据访问依赖。
        ///
        /// English: Role and membership data-access dependency.
        /// </summary>
        [Dependency]
        public IRolesDb RolesDB { private get; set; }

        /// <summary>
        /// 中文：授权并解析规范用户目标；首次请求绑定用户、角色和审核信息。
        ///
        /// English: Authorizes and resolves the canonical user target, then binds user, role, and review information
        /// on the initial request.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TryInitializeRequest() ||
                !PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminRolesEdit))
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                BindData();
            }
        }

        /// <summary>
        /// 中文：处理仅返回门户页的保存按钮事件。
        ///
        /// English: Handles the save-button event that only returns to the Portal page.
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
        /// 中文：将当前用户加入所选的当前门户角色，并记录不含角色名称的运营审计。
        ///
        /// English: Adds the current user to a selected role of the current Portal and records an operations audit
        /// without the role name.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void AddRole_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest())
            {
                return;
            }

            IRoleItem role;
            if (!TryGetSelectedPortalRole(out role))
            {
                return;
            }

            try
            {
                RolesDB.AddUserRole(role.RoleId, userId);
                PortalOperationAudit.Record(
                    "UserAdministration",
                    "AddRole",
                    "User",
                    userId.ToString(),
                    "Added role id " + role.RoleId + " to user.",
                    Context);
                ShowRegistrationMessage("角色已加入当前用户。", false);
                BindData();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.ManageUsers.AddRole",
                    "Adding a role to user failed. UserId=" + userId + "; RoleId=" + role.RoleId,
                    exception,
                    Context);
                ShowRegistrationMessage("加入角色失败，系统已记录本次错误。事件编号：" + eventId, true);
            }
        }

        /// <summary>
        /// 中文：更新当前用户的邮箱并重置强哈希凭据，随后记录不含密码或邮箱原文的审计。
        ///
        /// English: Updates the current user's email and resets the strong-hash credential, then records audit data
        /// without the password or raw email address.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void UpdateUser_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest() ||
                !PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersEdit) ||
                !PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersResetPassword) ||
                !Page.IsValid)
            {
                return;
            }

            string email;
            if (!PortalAdministrationPolicy.TryNormalizeRequiredSingleLineText(Email.Text, 100, out email))
            {
                ShowRegistrationMessage("邮箱格式无效，未保存本次修改。", true);
                return;
            }

            string passwordPolicyMessage;
            if (!PortalPasswordPolicy.TryValidate(Password.Text, out passwordPolicyMessage))
            {
                ShowRegistrationMessage(passwordPolicyMessage, true);
                return;
            }

            try
            {
                UsersDB.UpdateUser(userId, email, Password.Text);
                PortalOperationAudit.Record(
                    "UserAdministration",
                    "UpdateProfile",
                    "User",
                    userId.ToString(),
                    "Updated user profile.",
                    Context);
                PortalOperationAudit.Record(
                    "SecurityCredentials",
                    "Reset",
                    "User",
                    userId.ToString(),
                    "User credential reset by administrator.",
                    Context);
                RedirectToCurrentUser();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.ManageUsers.UpdateUser",
                    "Updating user profile failed. UserId=" + userId,
                    exception,
                    Context);
                ShowRegistrationMessage("资料更新失败，系统已记录本次错误。事件编号：" + eventId, true);
            }
        }

        /// <summary>
        /// 中文：批准当前用户的注册申请，并记录注册审核操作。
        ///
        /// English: Approves the current user's registration and records the registration-review operation.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void ApproveRegistration_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest() ||
                !PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersEdit))
            {
                return;
            }

            try
            {
                UsersDB.ApproveUser(userId, GetCurrentActor());
                PortalOperationAudit.Record(
                    "Registration",
                    "Approve",
                    "User",
                    userId.ToString(),
                    "Registration approved.",
                    Context);
                ShowRegistrationMessage("Registration approved.", false);
                BindData();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.ManageUsers.ApproveRegistration",
                    "Approving user registration failed. UserId=" + userId,
                    exception,
                    Context);
                ShowRegistrationMessage("审核失败，系统已记录本次错误。事件编号：" + eventId, true);
            }
        }

        /// <summary>
        /// 中文：拒绝当前待审核用户的注册申请，并记录注册审核操作。
        ///
        /// English: Rejects the current pending registration and records the registration-review operation.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void RejectRegistration_Click(object sender, EventArgs e)
        {
            if (!TryInitializeRequest() ||
                !PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersEdit))
            {
                return;
            }

            try
            {
                UsersDB.RejectUser(userId, GetCurrentActor());
                PortalOperationAudit.Record(
                    "Registration",
                    "Reject",
                    "User",
                    userId.ToString(),
                    "Registration rejected.",
                    Context);
                ShowRegistrationMessage("Registration rejected.", false);
                BindData();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.ManageUsers.RejectRegistration",
                    "Rejecting user registration failed. UserId=" + userId,
                    exception,
                    Context);
                ShowRegistrationMessage("拒绝审核失败，系统已记录本次错误。事件编号：" + eventId, true);
            }
        }

        /// <summary>
        /// 中文：移除当前用户的一个当前门户角色，并记录角色成员关系审计。
        ///
        /// English: Removes one current-Portal role from the current user and records a role-membership audit.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：包含命令和 DataList 项索引的事件数据。English: Event data containing the command and DataList item index.</param>
        protected void UserRoles_ItemCommand(object sender, DataListCommandEventArgs e)
        {
            if (!TryInitializeRequest() ||
                !PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminRolesEdit) ||
                !string.Equals(e.CommandName, "delete", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            int roleId;
            if (e.Item == null || e.Item.ItemIndex < 0 || e.Item.ItemIndex >= userRoles.DataKeys.Count ||
                !int.TryParse(userRoles.DataKeys[e.Item.ItemIndex].ToString(), out roleId) || roleId < 0)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            IRoleItem role = FindCurrentPortalRole(roleId);
            if (role == null)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return;
            }

            RolesDB.DeleteUserRole(role.RoleId, userId);
            PortalOperationAudit.Record(
                "UserAdministration",
                "RemoveRole",
                "User",
                userId.ToString(),
                "Removed role id " + role.RoleId + " from user.",
                Context);
            userRoles.EditItemIndex = -1;
            BindData();
        }

        private bool TryInitializeRequest()
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersView))
            {
                return false;
            }

            if (!PortalNavigationPolicy.TryReadPositiveInt32(Request.Params["userid"], out userId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            if (!TryReadOptionalPositiveParameter("tabid", out tabId) ||
                !TryReadOptionalNonNegativeParameter("tabindex", out tabIndex))
            {
                return false;
            }

            currentUser = UsersDB.FindUserById(userId);
            if (currentUser == null)
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            string suppliedUserName = Request.Params["username"];
            if (!string.IsNullOrWhiteSpace(suppliedUserName) &&
                !string.Equals(suppliedUserName, currentUser.Name, StringComparison.Ordinal))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            return true;
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

        private bool TryGetSelectedPortalRole(out IRoleItem role)
        {
            role = null;
            if (allRoles.SelectedItem == null)
            {
                ShowRegistrationMessage("请选择一个有效角色。", true);
                return false;
            }

            int roleId;
            if (!PortalNavigationPolicy.TryReadNonNegativeInt32(allRoles.SelectedItem.Value, out roleId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            role = FindCurrentPortalRole(roleId);
            if (role != null)
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private IRoleItem FindCurrentPortalRole(int roleId)
        {
            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            return RolesDB.GetPortalRoles(portalSettings.PortalId)
                .FirstOrDefault(item => item.RoleId == roleId);
        }

        private void BindData()
        {
            if (currentUser == null)
            {
                return;
            }

            Email.Text = currentUser.Email;
            UserNameText.Text = EncodeDisplay(currentUser.Name);
            BindRegistrationInfo(currentUser.UserId);
            TitleText.Text = Server.HtmlEncode("Manage User: " + (currentUser.Name ?? string.Empty));

            userRoles.DataSource = UsersDB.GetRolesByUser(currentUser.Name);
            userRoles.DataBind();

            PortalSettings portalSettings = PortalContext.GetPortalSettings();
            allRoles.DataSource = RolesDB.GetPortalRoles(portalSettings.PortalId);
            allRoles.DataBind();
        }

        private void BindRegistrationInfo(int currentUserId)
        {
            IUserRegistrationInfo registration = UsersDB.GetRegistrationInfo(currentUserId);
            RegistrationStatus.Text = EncodeDisplay(registration.Status);
            RegistrationSource.Text = EncodeDisplay(registration.Source);
            EmployeeCodeText.Text = EncodeDisplay(EmptyToNone(registration.EmployeeCode));
            InviteCodeText.Text = EncodeDisplay(EmptyToNone(registration.InviteCode));
            RegisteredUtcText.Text = EncodeDisplay(FormatUtc(registration.RegisteredUtc));
            ApprovedUtcText.Text = EncodeDisplay(FormatUtc(registration.ApprovedUtc));
            ApproveRegistrationBtn.Visible =
                string.Equals(registration.Status, PortalUserRegistrationStatuses.PendingApproval, StringComparison.Ordinal) ||
                string.Equals(registration.Status, PortalUserRegistrationStatuses.Rejected, StringComparison.Ordinal);
            RejectRegistrationBtn.Visible =
                string.Equals(registration.Status, PortalUserRegistrationStatuses.PendingApproval, StringComparison.Ordinal);
        }

        private void RedirectToCurrentUser()
        {
            string url = ResolveUrl(
                "~/Admin/ManageUsers.aspx?userId=" + userId +
                "&username=" + Uri.EscapeDataString(currentUser.Name ?? string.Empty) +
                "&tabindex=" + tabIndex +
                "&tabid=" + tabId);
            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, url);
        }

        private string BuildPortalReturnUrl()
        {
            if (tabId <= 0 || tabIndex <= 0)
            {
                return ResolveUrl("~/DesktopDefault.aspx");
            }

            return ResolveUrl("~/DesktopDefault.aspx?tabindex=" + tabIndex + "&tabid=" + tabId);
        }

        private string GetCurrentActor()
        {
            return Context.User == null || Context.User.Identity == null ||
                   string.IsNullOrWhiteSpace(Context.User.Identity.Name)
                ? "admin"
                : Context.User.Identity.Name;
        }

        private void ShowRegistrationMessage(string message, bool isError)
        {
            RegistrationMessage.CssClass = isError ? "NormalRed" : "Normal";
            RegistrationMessage.Text = Server.HtmlEncode(message ?? string.Empty);
        }

        private string EncodeDisplay(string value)
        {
            return Server.HtmlEncode(value ?? string.Empty);
        }

        private static string EmptyToNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }

        private static string FormatUtc(DateTime value)
        {
            return value == DateTime.MinValue ? "(legacy)" : value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
        }

        private static string FormatUtc(DateTime? value)
        {
            return value.HasValue ? FormatUtc(value.Value) : "(none)";
        }
    }
}
