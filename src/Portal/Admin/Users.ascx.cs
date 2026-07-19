using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Resources;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：旧后台用户列表和显式创建入口。
    ///
    /// English: Legacy administration user list and explicit user-creation entry point.
    /// </summary>
    /// <remarks>
    /// 中文：本控件要求 <c>Admins</c> 角色。新增用户仍使用既有的占位资料再进入编辑页，
    /// 但写入只会发生在管理员点击后的 Web Forms POST，不再由访问编辑地址的 GET 触发。
    ///
    /// English: This control requires the <c>Admins</c> role. New users still begin with a legacy placeholder
    /// profile before entering the edit page, but the write occurs only from an administrator-initiated Web Forms
    /// POST and no longer from a GET to the edit URL.
    /// </remarks>
    public partial class Users : PortalModuleControl<Users>
    {
        private const int PlaceholderCreationAttempts = 5;
        private int tabId;
        private int tabIndex;

        /// <summary>
        /// 中文：用户数据访问依赖。
        ///
        /// English: User data-access dependency.
        /// </summary>
        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        /// <summary>
        /// 中文：角色和可选用户查询依赖。
        ///
        /// English: Role and selectable-user query dependency.
        /// </summary>
        [Dependency]
        public IRolesDb RolesDB { private get; set; }

        /// <summary>
        /// 中文：执行管理员授权、读取可选导航参数并在首次请求绑定用户列表。
        ///
        /// English: Performs administrator authorization, reads optional navigation parameters, and binds the user
        /// list on the initial request.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersView) || !TryReadNavigationParameters())
            {
                return;
            }

            if (!Page.IsPostBack)
            {
                BindData();
            }
        }

        /// <summary>
        /// 中文：删除当前选择的用户并记录不含资料内容的运营审计。
        ///
        /// English: Deletes the selected user and records an operations audit without profile content.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void btn_DeleteUser_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersEdit) || !TryReadNavigationParameters())
            {
                return;
            }

            IUserItem user;
            if (!TryGetSelectedUser(out user))
            {
                return;
            }

            try
            {
                UsersDB.DeleteUser(user.UserId);
                PortalOperationAudit.Record(
                    "UserAdministration",
                    "Delete",
                    "User",
                    user.UserId.ToString(),
                    "Deleted user from the legacy administration list.",
                    Context);
                BindData();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.Users.DeleteUser",
                    "Deleting a user from the admin Users module failed. UserId=" + user.UserId,
                    exception,
                    Context);
                ShowMessage("删除失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        /// <summary>
        /// 中文：按当前选择的规范用户标识进入资料编辑页。
        ///
        /// English: Opens the profile-editing page using the canonical identifier of the currently selected user.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void EditUser_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersView) || !TryReadNavigationParameters())
            {
                return;
            }

            IUserItem user;
            if (!TryGetSelectedUser(out user))
            {
                return;
            }

            RedirectToManageUser(user);
        }

        /// <summary>
        /// 中文：在管理员显式 POST 中创建临时用户，并转入现有资料编辑流程。
        ///
        /// English: Creates a placeholder user during an explicit administrator POST, then enters the existing
        /// profile-editing flow.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void AddUser_Click(object sender, EventArgs e)
        {
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersEdit) || !TryReadNavigationParameters())
            {
                return;
            }

            for (int attempt = 0; attempt < PlaceholderCreationAttempts; attempt++)
            {
                string placeholderName = CreatePlaceholderUserName();
                int userId = UsersDB.AddUser(placeholderName, placeholderName, string.Empty);
                if (userId <= 0)
                {
                    continue;
                }

                IUserItem user = UsersDB.FindUserById(userId);
                if (user == null)
                {
                    break;
                }

                PortalOperationAudit.Record(
                    "UserAdministration",
                    "CreatePlaceholder",
                    "User",
                    user.UserId.ToString(),
                    "Created an administrator placeholder user.",
                    Context);
                RedirectToManageUser(user);
                return;
            }

            ShowMessage("无法创建新用户，系统未完成本次写入。");
        }

        private bool TryReadNavigationParameters()
        {
            return TryReadOptionalPositiveParameter("tabid", out tabId) &&
                   TryReadOptionalNonNegativeParameter("tabindex", out tabIndex);
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

        private bool TryGetSelectedUser(out IUserItem user)
        {
            user = null;
            if (ddl_AllUsers.SelectedItem == null)
            {
                ShowMessage("请选择一个有效用户。");
                return false;
            }

            int userId;
            if (!PortalNavigationPolicy.TryReadPositiveInt32(ddl_AllUsers.SelectedItem.Value, out userId))
            {
                PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
                return false;
            }

            user = UsersDB.FindUserById(userId);
            if (user != null)
            {
                return true;
            }

            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private static string CreatePlaceholderUserName()
        {
            return "NewUser_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "_" +
                   Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private void RedirectToManageUser(IUserItem user)
        {
            string url = ResolveUrl(
                "~/Admin/ManageUsers.aspx?userId=" + user.UserId +
                "&username=" + Uri.EscapeDataString(user.Name ?? string.Empty) +
                "&tabindex=" + tabIndex +
                "&tabid=" + tabId);
            PortalNavigationPolicy.RedirectToSafeReturnUrl(Context, url);
        }

        private void BindData()
        {
            try
            {
                if (Context.User.Identity.AuthenticationType != "Forms")
                {
                    Message.Text = lang.Admin_Users_FormMsg;
                }
                else
                {
                    Message.Text = lang.Admin_Users_OtherMsg;
                }

                ddl_AllUsers.DataSource = RolesDB.GetUsers();
                ddl_AllUsers.DataBind();
            }
            catch (Exception exception)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.Users.BindData",
                    "Binding users in the admin Users module failed.",
                    exception,
                    Context);
                ShowMessage("数据绑定失败，系统已记录本次错误。事件编号：" + eventId);
            }
        }

        private void ShowMessage(string message)
        {
            Message.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
