using System;
using System.Data;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// 中文：旧后台用户资料、角色和注册审核管理页面的 Web Forms code-behind。
    ///
    /// English: Web Forms code-behind for legacy administration of user profiles, roles, and registration review.
    /// </summary>
    /// <remarks>
    /// 中文：页面要求 <c>Admins</c> 角色。缺少用户名的首次访问会创建一个临时命名用户后重定向到编辑地址，
    /// 这是既有流程的写入副作用，本批只记录不改变。角色调整不会立即撤销目标用户现有的角色 Cookie。
    ///
    /// English: The page requires the <c>Admins</c> role. A first request without a user name creates a temporarily
    /// named user and redirects to its edit URL; this is a legacy write side effect recorded but not changed in this batch.
    /// Role changes do not immediately revoke the target user's existing role cookie.
    /// </remarks>
    public partial class ManageUsers : PortalPage<ManageUsers>
    {
        private int tabId;
        private int tabIndex;
        private int userId = -1;
        private string userName = "";

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
        /// 中文：页面加载时执行后台授权、读取目标用户和导航参数，并绑定用户、角色与审核信息。
        ///
        /// English: Performs administration authorization, reads target-user and navigation parameters, and binds user, role, and review information on page load.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // 中文：所有页面入口先执行旧 Admin 角色检查。
            // English: Apply the legacy Admin-role check before every page entry flow.
            PortalAuthorization.RequireAdmin();

            // 中文：当前沿用既有查询参数格式；参数解析强化另行治理，不能在注释批次改变路由行为。
            // English: Retain the existing query-parameter format; stronger parameter parsing belongs to separate work and must not change routing in a documentation batch.
            if (Request.Params["userid"] != null)
            {
                userId = Int32.Parse(Request.Params["userid"]);
            }
            if (Request.Params["username"] != null)
            {
                userName = Request.Params["username"];
            }
            if (Request.Params["tabid"] != null)
            {
                tabId = Int32.Parse(Request.Params["tabid"]);
            }
            if (Request.Params["tabindex"] != null)
            {
                tabIndex = Int32.Parse(Request.Params["tabindex"]);
            }


            // 中文：首次请求加载用户、角色与审核信息。
            // English: Load user, role, and review information on the initial request.
            if (!Page.IsPostBack)
            {
                // 中文：保留旧流程的“无用户名即创建占位用户”副作用，后续产品改造应单独处理。
                // English: Preserve the legacy side effect that creates a placeholder user when no user name is supplied; later product work should address it separately.
                if (String.IsNullOrEmpty(userName))
                {
                    // 中文：生成唯一占位登录名称，直到旧数据层返回成功标识。
                    // English: Generate a unique placeholder sign-in name until the legacy data layer returns a successful identifier.
                    int uid = -1;
                    int i = 0;

                    while (uid == -1)
                    {
                        string timestamp = DateTime.Now.ToString("yyMMddHHmmss");
                        string guidPart = Guid.NewGuid().ToString().Substring(0, 8);
                        userName = $"NewUser_{timestamp}_{guidPart}";
                        
                        var emailStr = userName;
                        uid = UsersDB.AddUser(userName, emailStr, "");
                        
                        i++;
                    }

                    // 中文：创建成功后转入该用户的编辑地址，保持既有后台导航格式。
                    // English: Redirect to the created user's edit URL and retain the legacy administration navigation format.
                    Response.Redirect("~/Admin/ManageUsers.aspx?userId=" + uid + "&username=" + userName + "&tabindex=" +
                                      tabIndex + "&tabid=" + tabId);
                }

                BindData();
            }
        }

        /// <summary>
        /// 中文：处理返回门户页面的保存按钮事件。
        ///
        /// English: Handles the save-button event that returns to the Portal page.
        /// </summary>
        /// <remarks>
        /// 中文：当前方法不写入用户或安全设置，只保留旧页面的返回导航行为。
        ///
        /// English: This method currently writes no user or security settings; it retains only the legacy return-navigation behavior.
        /// </remarks>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void Save_Click(Object sender, EventArgs e)
        {
            // 中文：PortalSettings 由 BeginRequest 装配；此处保留旧读取节奏而不新增状态写入。
            // English: PortalSettings is assembled by BeginRequest; retain the legacy read cadence without adding state writes.
            var portalSettings = PortalContext.GetPortalSettings();

            Response.Redirect("~/DesktopDefault.aspx?tabindex=" + tabIndex + "&tabid=" + tabId);
        }

        /// <summary>
        /// 中文：将当前用户加入所选角色，并记录不含敏感值的运营审计。
        ///
        /// English: Adds the current user to the selected role and records an operation audit without sensitive values.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void AddRole_Click(Object sender, EventArgs e)
        {
            int roleId;

            // 中文：角色成员关系写入后，目标用户已有角色 Cookie 仍可能持续至票据刷新。
            // English: After membership is written, the target user's existing role cookie may remain until ticket refresh.
            roleId = Int32.Parse(allRoles.SelectedValue);

            RolesDB.AddUserRole(roleId, userId);
            PortalOperationAudit.Record(
                "UserAdministration",
                "AddRole",
                "User",
                userId.ToString(),
                "Added role '" + (allRoles.SelectedItem == null ? roleId.ToString() : allRoles.SelectedItem.Text) + "' to user.",
                Context);

            BindData();
        }

        /// <summary>
        /// 中文：更新当前用户的邮箱和密码摘要，并记录不含密码或邮箱原文的审计。
        ///
        /// English: Updates the current user's email and password digest, then records audit data without the password or raw email.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void UpdateUser_Click(Object sender, EventArgs e)
        {
            // 中文：提交历史兼容摘要；密码明文和摘要都不得写入审计内容。
            // English: Submit the legacy-compatible digest; neither password plain text nor digest may enter audit content.
            UsersDB.UpdateUser(userId, Email.Text, PortalSecurity.Encrypt(Password.Text));
            // 中文：审计只保留资料更新事实，不记录密码材料或邮箱原文。
            // English: Audit retains only the profile-update fact, never password material or the raw email address.
            PortalOperationAudit.Record(
                "UserAdministration",
                "UpdateProfile",
                "User",
                userId.ToString(),
                "Updated user profile.",
                Context);

            // 中文：更新后回到同一用户页面，用户名作为 URL 参数进行编码。
            // English: Return to the same user page after update, encoding the user name for the URL parameter.
            Response.Redirect("~/Admin/ManageUsers.aspx?userId=" + userId + "&username=" + Uri.EscapeDataString(userName) + "&tabindex=" +
                              tabIndex + "&tabid=" + tabId);
        }

        /// <summary>
        /// 中文：批准当前用户的注册申请，并记录注册审核操作。
        ///
        /// English: Approves the current user's registration and records the registration-review operation.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void ApproveRegistration_Click(Object sender, EventArgs e)
        {
            PortalAuthorization.RequireAdmin();

            try
            {
                // 中文：审计操作人取当前身份；异常消息只展示诊断事件编号。
                // English: Use the current identity as audit actor; exception messages expose only the diagnostic event identifier.
                string approvedBy = Context.User == null || Context.User.Identity == null
                    ? "admin"
                    : Context.User.Identity.Name;
                UsersDB.ApproveUser(userId, approvedBy);
                PortalOperationAudit.Record(
                    "Registration",
                    "Approve",
                    "User",
                    userId.ToString(),
                    "Registration approved.",
                    Context);
                RegistrationMessage.CssClass = "Normal";
                RegistrationMessage.Text = "Registration approved.";
                BindData();
            }
            catch (Exception ex)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.ManageUsers.ApproveRegistration",
                    "Approving user registration failed. UserId=" + userId,
                    ex,
                    Context);
                RegistrationMessage.CssClass = "NormalRed";
                RegistrationMessage.Text = "审核失败，系统已记录本次错误。事件编号：" + eventId;
            }
        }

        /// <summary>
        /// 中文：拒绝当前待审核用户的注册申请；后续批准可恢复其登录资格。
        ///
        /// English: Rejects the current pending registration; a later approval may restore its sign-in eligibility.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：事件数据。English: Event data.</param>
        protected void RejectRegistration_Click(object sender, EventArgs e)
        {
            PortalAuthorization.RequireAdmin();

            try
            {
                string rejectedBy = Context.User == null || Context.User.Identity == null
                    ? "admin"
                    : Context.User.Identity.Name;
                UsersDB.RejectUser(userId, rejectedBy);
                PortalOperationAudit.Record(
                    "Registration",
                    "Reject",
                    "User",
                    userId.ToString(),
                    "Registration rejected.",
                    Context);
                RegistrationMessage.CssClass = "Normal";
                RegistrationMessage.Text = "Registration rejected.";
                BindData();
            }
            catch (Exception ex)
            {
                string eventId = PortalDiagnostics.Error(
                    "Admin.ManageUsers.RejectRegistration",
                    "Rejecting user registration failed. UserId=" + userId,
                    ex,
                    Context);
                RegistrationMessage.CssClass = "NormalRed";
                RegistrationMessage.Text = "拒绝审核失败，系统已记录本次错误。事件编号：" + eventId;
            }
        }

        /// <summary>
        /// 中文：处理用户角色删除命令，并记录角色成员关系移除审计。
        ///
        /// English: Handles a user-role deletion command and records role-membership removal audit data.
        /// </summary>
        /// <param name="sender">中文：事件源。English: Event source.</param>
        /// <param name="e">中文：包含命令名和 DataList 项索引的事件数据。English: Event data containing the command name and DataList item index.</param>
        /// <param name="sender">发送者对象。</param>
        /// <param name="e">事件参数。</param>
        protected void UserRoles_ItemCommand(object sender, DataListCommandEventArgs e)
        {
            var roleId = (int)userRoles.DataKeys[e.Item.ItemIndex];

            if (!string.Equals(e.CommandName, "delete", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // 中文：移除成员关系不会立即撤销目标用户已签发的角色 Cookie。
            // English: Removing membership does not immediately revoke the target user's issued role cookie.
            RolesDB.DeleteUserRole(roleId, userId);
            PortalOperationAudit.Record(
                "UserAdministration",
                "RemoveRole",
                "User",
                userId.ToString(),
                "Removed role id " + roleId + " from user.",
                Context);

            userRoles.EditItemIndex = -1;
            BindData();
        }

        /// <summary>
        /// 中文：绑定当前用户资料、注册审核状态、已分配角色和可选角色。
        ///
        /// English: Binds current user profile, registration-review status, assigned roles, and available roles.
        /// </summary>
        private void BindData()
        {
            // 中文：密码摘要不读取到页面；用户资料绑定仅包含可显示字段。
            // English: Do not read the password digest into the page; profile binding includes only displayable fields.
            var user = UsersDB.GetSingleUser(userName);

            Email.Text = user.Email;
            BindRegistrationInfo(user.UserId);

            if (!String.IsNullOrEmpty(userName))
            {
                // 中文：标题使用 Label，避免旧 Web Forms 代码块与 InnerText 修改冲突。
                // English: Use Label for the title to avoid legacy Web Forms code-block conflicts with InnerText changes.
                TitleText.Text = "Manage User: " + userName;
            }

            userRoles.DataSource = UsersDB.GetRolesByUser(userName);
            userRoles.DataBind();

            var portalSettings = PortalContext.GetPortalSettings();

            // 中文：只绑定当前门户已定义的角色，角色名称仍由旧数据层维护。
            // English: Bind only roles defined for the current Portal; legacy data access still maintains role names.
            allRoles.DataSource = RolesDB.GetPortalRoles(portalSettings.PortalId);
            allRoles.DataBind();
        }

        private void BindRegistrationInfo(int currentUserId)
        {
            // 中文：旧用户或旧库返回兼容审核视图，不把缺失元数据误显示为待审核。
            // English: Legacy users or databases return a compatible review view and do not misdisplay missing metadata as pending approval.
            IUserRegistrationInfo registration = UsersDB.GetRegistrationInfo(currentUserId);

            RegistrationStatus.Text = registration.Status;
            RegistrationSource.Text = registration.Source;
            EmployeeCodeText.Text = EmptyToNone(registration.EmployeeCode);
            InviteCodeText.Text = EmptyToNone(registration.InviteCode);
            RegisteredUtcText.Text = FormatUtc(registration.RegisteredUtc);
            ApprovedUtcText.Text = FormatUtc(registration.ApprovedUtc);
            // 中文：待审核可批准；已拒绝记录也可重新批准，保持当前最小审核流程可恢复。
            // English: Pending registrations can be approved and rejected records may be approved again, keeping the current minimal review flow recoverable.
            ApproveRegistrationBtn.Visible =
                string.Equals(registration.Status, PortalUserRegistrationStatuses.PendingApproval, StringComparison.Ordinal) ||
                string.Equals(registration.Status, PortalUserRegistrationStatuses.Rejected, StringComparison.Ordinal);
            RejectRegistrationBtn.Visible =
                string.Equals(registration.Status, PortalUserRegistrationStatuses.PendingApproval, StringComparison.Ordinal);
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
