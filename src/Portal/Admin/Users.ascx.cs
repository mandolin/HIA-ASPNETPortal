using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Resources;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>旧后台用户列表和显式创建入口。</zh-CN>
    ///   <en>Legacy administration user list and explicit user-creation entry point.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>本控件要求 <c>Admins</c> 角色。新增用户仍使用既有的占位资料再进入编辑页， 但写入只会发生在管理员点击后的 Web Forms POST，不再由访问编辑地址的 GET 触发。</zh-CN>
    ///   <en>This control requires the <c>Admins</c> role. New users still begin with a legacy placeholder profile before entering the edit page, but the write occurs only from an administrator-initiated Web Forms POST and no longer from a GET to the edit URL.</en>
    /// </lang>
    /// </remarks>
    public partial class Users : PortalModuleControl<Users>
    {
        private const int PlaceholderCreationAttempts = 5;
        private int tabId;
        private int tabIndex;

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
        ///   <zh-CN>角色和可选用户查询依赖。</zh-CN>
        ///   <en>Role and selectable-user query dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IRolesDb RolesDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>执行管理员授权、读取可选导航参数并在首次请求绑定用户列表。</zh-CN>
        ///   <en>Performs administrator authorization, reads optional navigation parameters, and binds the user list on the initial request.</en>
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
        /// <lang>
        ///   <zh-CN>删除当前选择的用户并记录不含资料内容的运营审计。</zh-CN>
        ///   <en>Deletes the selected user and records an operations audit without profile content.</en>
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
        /// <lang>
        ///   <zh-CN>按当前选择的规范用户标识进入资料编辑页。</zh-CN>
        ///   <en>Opens the profile-editing page using the canonical identifier of the currently selected user.</en>
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
        /// <lang>
        ///   <zh-CN>在管理员显式 POST 中创建临时用户，并转入现有资料编辑流程。</zh-CN>
        ///   <en>Creates a placeholder user during an explicit administrator POST, then enters the existing profile-editing flow.</en>
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
