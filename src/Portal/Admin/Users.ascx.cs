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
        /// <summary>
        /// <lang>
        ///   <zh-CN>占位用户创建的固定重试上限。</zh-CN>
        ///   <en>Fixed retry limit for placeholder-user creation.</en>
        /// </lang>
        /// </summary>
        private const int PlaceholderCreationAttempts = 5;

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选的用户编辑回跳 Tab 标识。</zh-CN>
        ///   <en>Optional Tab identifier preserved for user-edit return navigation.</en>
        /// </lang>
        /// </summary>
        private int tabId;

        /// <summary>
        /// <lang>
        ///   <zh-CN>可选的用户编辑回跳 Tab 索引。</zh-CN>
        ///   <en>Optional Tab index preserved for user-edit return navigation.</en>
        /// </lang>
        /// </summary>
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
            // <lang>
            //   <zh-CN>查看权限和导航参数是列表及后续编辑/删除入口的共同门禁。</zh-CN>
            //   <en>View permission and navigation parameters gate the list and the later edit/delete entry points.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersView) || !TryReadNavigationParameters())
            {
                return;
            }

            // <lang>
            //   <zh-CN>只在首次请求绑定可选用户列表，保留回发时的选择状态。</zh-CN>
            //   <en>Bind selectable users only on the initial request, preserving the postback selection state.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>删除需要独立编辑权限，并复用选中用户的正数标识和数据存在性校验。</zh-CN>
            //   <en>Deletion requires separate edit permission and reuses positive-id and data-existence checks for the selected user.</en>
            // </lang>
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
                // <lang>
                //   <zh-CN>删除成功后记录不含资料内容的低敏审计并刷新列表。</zh-CN>
                //   <en>After deletion, record a low-sensitivity audit without profile content and refresh the list.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>删除失败只向页面返回事件编号，不暴露底层异常。</zh-CN>
                //   <en>On deletion failure, return only the event identifier to the page without exposing the underlying exception.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>编辑入口只接受经过数据层复核的当前选中用户，并保留安全回跳参数。</zh-CN>
            //   <en>The edit entry accepts only the selected user revalidated by the data layer and preserves safe return parameters.</en>
            // </lang>
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
            // <lang>
            //   <zh-CN>显式 POST 才能进入占位用户创建流程，避免通过 GET 访问编辑地址触发写入。</zh-CN>
            //   <en>Only an explicit POST enters placeholder creation, preventing a GET to the edit URL from triggering a write.</en>
            // </lang>
            if (!PortalAuthorization.EnsurePermission(Context, PortalPermissionKeys.AdminUsersEdit) || !TryReadNavigationParameters())
            {
                return;
            }

            for (int attempt = 0; attempt < PlaceholderCreationAttempts; attempt++)
            {
                // <lang>
                //   <zh-CN>每次尝试使用不可预测的占位名；成功后立即重新读取用户并转入既有编辑流程。</zh-CN>
                //   <en>Each attempt uses an unpredictable placeholder name; after success, reload the user and enter the existing edit flow.</en>
                // </lang>
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

            // <lang>
            //   <zh-CN>达到固定尝试上限仍失败时不再继续写入，仅展示受控提示。</zh-CN>
            //   <en>When the fixed attempt limit is exhausted, stop writing and show only a controlled message.</en>
            // </lang>
            ShowMessage("无法创建新用户，系统未完成本次写入。");
        }

        private bool TryReadNavigationParameters()
        {
            // <lang>
            //   <zh-CN>用户列表与编辑回跳共同读取 Tab 标识和索引，任一非法值都阻断后续操作。</zh-CN>
            //   <en>The list and edit return path read the Tab id and index together; any invalid value blocks further processing.</en>
            // </lang>
            return TryReadOptionalPositiveParameter("tabid", out tabId) &&
                   TryReadOptionalNonNegativeParameter("tabindex", out tabIndex);
        }

        private bool TryReadOptionalPositiveParameter(string parameterName, out int value)
        {
            value = 0;
            // <lang>
            //   <zh-CN>缺失参数使用兼容默认值 0；提供的值必须是正整数。</zh-CN>
            //   <en>Use the compatibility default of 0 when absent; a supplied value must be a positive integer.</en>
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
            //   <zh-CN>非法用户回跳参数直接进入拒绝页，不把原始输入传入管理页。</zh-CN>
            //   <en>Route invalid user return parameters to the denied page without forwarding raw input to the management page.</en>
            // </lang>
            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private bool TryReadOptionalNonNegativeParameter(string parameterName, out int value)
        {
            value = 0;
            // <lang>
            //   <zh-CN>回跳索引允许零，但必须通过非负整数校验。</zh-CN>
            //   <en>The return index permits zero but must pass non-negative-integer validation.</en>
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
            //   <zh-CN>非法索引不降级为默认值，直接拒绝继续导航。</zh-CN>
            //   <en>Do not downgrade an invalid index to a default; reject further navigation directly.</en>
            // </lang>
            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private bool TryGetSelectedUser(out IUserItem user)
        {
            user = null;
            // <lang>
            //   <zh-CN>必须存在当前下拉选项；空选择只显示受控提示，不调用数据层。</zh-CN>
            //   <en>A current dropdown item is required; an empty selection shows a controlled message without calling the data layer.</en>
            // </lang>
            if (ddl_AllUsers.SelectedItem == null)
            {
                ShowMessage("请选择一个有效用户。");
                return false;
            }

            // <lang>
            //   <zh-CN>选项值先解析为正数用户标识，再重新读取用户，避免信任控件回发值。</zh-CN>
            //   <en>Parse the option as a positive user id and reload the user, rather than trusting the posted control value.</en>
            // </lang>
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

            // <lang>
            //   <zh-CN>用户不存在时统一拒绝继续处理，避免编辑或删除悬空标识。</zh-CN>
            //   <en>Reject further processing when the user no longer exists, avoiding edits or deletes against a dangling identifier.</en>
            // </lang>
            PortalNavigationPolicy.RedirectToEditAccessDenied(Context);
            return false;
        }

        private static string CreatePlaceholderUserName()
        {
            // <lang>
            //   <zh-CN>占位名仅用于短生命周期的管理员编辑入口，不承担登录凭据或业务显示语义。</zh-CN>
            //   <en>The placeholder name is only for the short-lived administrator edit entry and is not a login credential or business display value.</en>
            // </lang>
            return "NewUser_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "_" +
                   Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private void RedirectToManageUser(IUserItem user)
        {
            // <lang>
            //   <zh-CN>编辑地址只携带已验证用户标识、编码后的名称和已校验导航参数，并交给安全回跳策略。</zh-CN>
            //   <en>Carry only the verified user id, encoded name, and validated navigation parameters through the safe-return policy.</en>
            // </lang>
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
                // <lang>
                //   <zh-CN>提示文案按现有认证类型兼容分支选择，用户选项来自角色服务只读查询。</zh-CN>
                //   <en>Choose the message through the existing authentication-type compatibility branch and load options from the role service's read-only query.</en>
                // </lang>
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
                // <lang>
                //   <zh-CN>绑定失败不暴露服务细节，只记录诊断事件并显示事件编号。</zh-CN>
                //   <en>On binding failure, record a diagnostic event and show only its identifier without exposing service details.</en>
                // </lang>
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
            // <lang>
            //   <zh-CN>所有用户管理提示统一 HTML 编码，避免旧控件把输入或诊断文本当作标记。</zh-CN>
            //   <en>HTML-encode every user-administration message so input or diagnostics cannot be emitted as markup.</en>
            // </lang>
            Message.Text = Server.HtmlEncode(message ?? string.Empty);
        }
    }
}
