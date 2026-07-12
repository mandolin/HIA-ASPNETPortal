using System;
using System.Web.Security;
using Microsoft.Practices.Unity;
using Unity;

// 定义命名空间
namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    ///   Summary description for Register.
    ///   注册类的简要描述。
    /// </summary>
    public partial class Register : PortalPage<Register>
    {
        // 使用依赖注入标记属性定义IUsersDb接口的属性
        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        /// <summary>
        /// 自主注册关闭时拒绝访问公开注册页。
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!PortalRegistrationOptions.AllowSelfRegistration)
            {
                Response.Redirect("~/Admin/AccessDenied.aspx");
            }

            ConfigureRegistrationForm();
        }

        // 注册按钮点击事件处理程序
        protected void RegisterBtn_Click(object sender, EventArgs e)
        {
            if (!PortalRegistrationOptions.AllowSelfRegistration)
            {
                Response.Redirect("~/Admin/AccessDenied.aspx");
            }

            // 只有在页面上的所有表单字段都有效的情况下才尝试登录
            if (Page.IsValid)
            {
                // 从表单中获取用户输入的用户名，并去除前后空格
                var userName = Name.Text.Trim();
                // 从表单中获取用户输入的电子邮件地址，并去除前后空格
                var email = Email.Text.Trim();
                string employeeCode = EmployeeCode.Text.Trim();
                string inviteCode = CurrentInviteCode;
                string inviteMessage;

                if (!UsersDB.ValidateRegistrationInvite(inviteCode, out inviteMessage))
                {
                    Message.Text = inviteMessage;
                    return;
                }

                if (PortalRegistrationOptions.IsEmployeeCodeRequired(inviteCode) &&
                    string.IsNullOrWhiteSpace(employeeCode) &&
                    !PortalRegistrationOptions.AllowPendingEmployeeBinding)
                {
                    Message.Text = "Employee Code is required for invitation registration.";
                    return;
                }

                // 尝试将新用户添加到门户用户数据库中
                // 如果返回值大于-1，则表示用户成功添加到数据库
                int userId;
                try
                {
                    userId = UsersDB.AddSelfRegisteredUser(
                        userName,
                        email,
                        PortalSecurity.Encrypt(Password.Text),
                        employeeCode,
                        inviteCode,
                        PortalRegistrationOptions.RequireRegistrationApproval);
                }
                catch (Exception ex)
                {
                    string eventId = PortalDiagnostics.Error(
                        "Admin.Register.SelfRegistration",
                        "Self-registration failed for userName=" + userName + "; email=" + email,
                        ex,
                        Context);
                    Message.Text = "Registration failed. The system recorded this error. Event ID: " + eventId;
                    return;
                }

                if (userId > -1)
                {
                    // 注册成功后记录状态变更审计；审计缺表或写入失败不会阻断注册结果。
                    // Audit the successful state change; missing or failed auditing never blocks registration.
                    PortalOperationAudit.Record(
                        "Registration",
                        "Submit",
                        "User",
                        userId.ToString(),
                        "Self-registration submitted.",
                        Context);

                    if (PortalRegistrationOptions.RequireRegistrationApproval)
                    {
                        // 默认注册后进入待审核，不自动登录；管理员批准后用户才能登录。
                        // By default a new registration is pending approval and cannot sign in until approved.
                        RegisterBtn.Visible = false;
                        Message.CssClass = "Normal";
                        Message.Text = "Registration submitted. Please wait for administrator approval.";
                        return;
                    }

                    // 不需要审核时沿用旧行为：注册成功后直接登录。
                    // When approval is disabled, keep the legacy behavior and sign in immediately.
                    FormsAuthentication.SetAuthCookie(userName, false);
                    Response.Redirect("~/DesktopDefault.aspx");
                }
                else
                {
                    // 如果注册失败，则更新Message标签内容
                    Message.Text = "Registration failed. The user name or email may already exist, or registration metadata is not available.";
                }
            }
        }

        private string CurrentInviteCode
        {
            get
            {
                string inviteCode = Request.QueryString["invite"];
                return string.IsNullOrWhiteSpace(inviteCode) ? string.Empty : inviteCode.Trim();
            }
        }

        private void ConfigureRegistrationForm()
        {
            bool employeeCodeRequired = PortalRegistrationOptions.IsEmployeeCodeRequired(CurrentInviteCode) &&
                                        !PortalRegistrationOptions.AllowPendingEmployeeBinding;
            EmployeeCodeRequiredValidator.Enabled = employeeCodeRequired;
            EmployeeCodeRequiredHint.Visible = employeeCodeRequired;
        }
    }
}
