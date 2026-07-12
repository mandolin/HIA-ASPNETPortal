using System;
using System.Data;
using System.Web.UI.WebControls;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    public partial class ManageUsers : PortalPage<ManageUsers>
    {
        private int tabId;
        private int tabIndex;
        private int userId = -1;
        private string userName = "";

        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        [Dependency]
        public IRolesDb RolesDB { private get; set; }


        /// <summary>
        /// 页面加载事件处理程序用于加载页面数据。
        /// </summary>
        /// <param name="sender">发送者对象。</param>
        /// <param name="e">事件参数。</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // 验证当前用户是否有访问此页面的权限
            // Verify that the current user has access to access this page
            PortalAuthorization.RequireAdmin();

            // Calculate userid
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


            // 如果是首次访问页面，则绑定角色数据到 DataList 控件
            if (!Page.IsPostBack)
            {
                // 如果是新用户
                if (String.IsNullOrEmpty(userName))
                {
                    // 创建一个唯一的用户记录
                    int uid = -1;
                    int i = 0;

                    while (uid == -1)
                    {
                        // 获取当前时间并格式化为较短的字符串
                        string timestamp = DateTime.Now.ToString("yyMMddHHmmss");

                        // 生成一个新的GUID，并将其转换为字符串形式后截取前8位
                        string guidPart = Guid.NewGuid().ToString().Substring(0, 8);

                        // 构建用户名
                        userName = $"NewUser_{timestamp}_{guidPart}";
                        
                        var emailStr = userName;
                        uid = UsersDB.AddUser(userName, emailStr, "");
                        
                        i++;
                    }

                    // 重定向到此页面并附带修正后的查询字符串参数
                    Response.Redirect("~/Admin/ManageUsers.aspx?userId=" + uid + "&username=" + userName + "&tabindex=" +
                                      tabIndex + "&tabid=" + tabId);
                }

                BindData();
            }
        }

        /// <summary>
        /// 保存点击事件处理程序用于保存当前的安全设置到配置系统。
        /// </summary>
        /// <param name="sender">发送者对象。</param>
        /// <param name="e">事件参数。</param>
        protected void Save_Click(Object sender, EventArgs e)
        {
            // 获取当前上下文中的 PortalSettings
            var portalSettings = PortalContext.GetPortalSettings();

            // 导航回管理页面
            Response.Redirect("~/DesktopDefault.aspx?tabindex=" + tabIndex + "&tabid=" + tabId);
        }

        //*******************************************************
        //
        // The AddRole_Click server event handler is used to add
        // the user to this security role
        //
        //*******************************************************

        /// <summary>
        /// 添加角色点击事件处理程序用于将用户添加到安全角色中。
        /// </summary>
        /// <param name="sender">发送者对象。</param>
        /// <param name="e">事件参数。</param>
        protected void AddRole_Click(Object sender, EventArgs e)
        {
            int roleId;

            // 从下拉列表获取角色ID
            roleId = Int32.Parse(allRoles.SelectedValue);

            // 将新用户角色添加到数据库
            RolesDB.AddUserRole(roleId, userId);
            PortalOperationAudit.Record(
                "UserAdministration",
                "AddRole",
                "User",
                userId.ToString(),
                "Added role '" + (allRoles.SelectedItem == null ? roleId.ToString() : allRoles.SelectedItem.Text) + "' to user.",
                Context);

            // 重新绑定列表
            BindData();
        }

        /// <summary>
        /// 更新用户点击事件处理程序用于更新用户的设置。
        /// </summary>
        /// <param name="sender">发送者对象。</param>
        /// <param name="e">事件参数。</param>
        protected void UpdateUser_Click(Object sender, EventArgs e)
        {
            // 在数据库中更新用户记录
            UsersDB.UpdateUser(userId, Email.Text, PortalSecurity.Encrypt(Password.Text));
            // 审计仅记录资料已更新，不记录密码、密码摘要或邮箱原文。
            // The audit records profile update only, never password material or the raw email address.
            PortalOperationAudit.Record(
                "UserAdministration",
                "UpdateProfile",
                "User",
                userId.ToString(),
                "Updated user profile.",
                Context);

            // 重定向到此页面并附带修正后的查询字符串参数
            Response.Redirect("~/Admin/ManageUsers.aspx?userId=" + userId + "&username=" + Uri.EscapeDataString(userName) + "&tabindex=" +
                              tabIndex + "&tabid=" + tabId);
        }

        /// <summary>
        /// 批准当前用户的注册申请。
        /// </summary>
        protected void ApproveRegistration_Click(Object sender, EventArgs e)
        {
            PortalAuthorization.RequireAdmin();

            try
            {
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
        /// 拒绝当前待审核用户的注册申请；后续仍可通过批准动作恢复为可登录状态。
        /// Rejects the current pending registration; a later approval may restore sign-in eligibility.
        /// </summary>
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
        /// 处理用户角色删除命令。
        /// </summary>
        /// <param name="sender">发送者对象。</param>
        /// <param name="e">事件参数。</param>
        protected void UserRoles_ItemCommand(object sender, DataListCommandEventArgs e)
        {
            var roleId = (int)userRoles.DataKeys[e.Item.ItemIndex];

            if (!string.Equals(e.CommandName, "delete", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // 更新数据库
            RolesDB.DeleteUserRole(roleId, userId);
            PortalOperationAudit.Record(
                "UserAdministration",
                "RemoveRole",
                "User",
                userId.ToString(),
                "Removed role id " + roleId + " from user.",
                Context);

            // 确保项不可编辑
            userRoles.EditItemIndex = -1;

            // 重新填充列表
            BindData();
        }

        /// <summary>
        /// 绑定数据方法用于绑定门户的安全角色列表到 DataList 控件。
        /// </summary>
        private void BindData()
        {
            // 绑定 Email 和 Password
            var user = UsersDB.GetSingleUser(userName);

            Email.Text = user.Email;
            BindRegistrationInfo(user.UserId);

            // 添加用户名到标题
            if (!String.IsNullOrEmpty(userName))
            {
                // 标题控件使用 Label，避免旧 WebForms 代码块与 InnerText 修改冲突。
                TitleText.Text = "Manage User: " + userName;
            }

            // 绑定用户所属角色到 DataList
            userRoles.DataSource = UsersDB.GetRolesByUser(userName);
            userRoles.DataBind();

            // 获取当前上下文中的 PortalSettings
            var portalSettings = PortalContext.GetPortalSettings();

            // 从数据库获取门户的角色
            // 绑定所有门户角色到下拉列表
            allRoles.DataSource = RolesDB.GetPortalRoles(portalSettings.PortalId);
            allRoles.DataBind();
        }

        private void BindRegistrationInfo(int currentUserId)
        {
            IUserRegistrationInfo registration = UsersDB.GetRegistrationInfo(currentUserId);

            RegistrationStatus.Text = registration.Status;
            RegistrationSource.Text = registration.Source;
            EmployeeCodeText.Text = EmptyToNone(registration.EmployeeCode);
            InviteCodeText.Text = EmptyToNone(registration.InviteCode);
            RegisteredUtcText.Text = FormatUtc(registration.RegisteredUtc);
            ApprovedUtcText.Text = FormatUtc(registration.ApprovedUtc);
            // 待审核可批准；已拒绝记录也允许管理员重新批准，避免把最小审核动作做成不可逆死路。
            // Pending registrations can be approved, and rejected records may be approved again by an administrator.
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
