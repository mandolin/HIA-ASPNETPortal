using System;
using System.Web;
using Microsoft.Practices.Unity;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>公开自主注册页面的 Web Forms code-behind。</zh-CN>
    ///   <en>Web Forms code-behind for the public self-registration page.</en>
    /// </lang>
    /// </summary>
    /// <remarks>
    /// <lang>
    ///   <zh-CN>只有 <see cref="PortalRegistrationOptions.AllowSelfRegistration"/> 为 <c>true</c> 时才可访问。 当前空邀请码允许非邀请注册；带邀请码时校验启用状态、UTC 到期时间与使用次数。是否要求邀请码是后续独立设置， 不能在此页面隐式改变。需要审核的注册不会自动登录。</zh-CN>
    ///   <en>Access is allowed only when <see cref="PortalRegistrationOptions.AllowSelfRegistration"/> is <c>true</c>. Empty invite codes currently allow non-invite registration; supplied invite codes validate enabled state, UTC expiration, and usage count. Requiring invite codes is a later independent setting and must not be changed implicitly here. Registrations requiring approval do not sign in automatically.</en>
    /// </lang>
    /// </remarks>
    public partial class Register : PortalPage<Register>
    {
        /// <summary>
        /// <lang>
        ///   <zh-CN>用户与注册审核数据访问依赖。</zh-CN>
        ///   <en>User and registration-review data-access dependency.</en>
        /// </lang>
        /// </summary>
        [Dependency]
        public IUsersDb UsersDB { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>页面加载时验证自主注册开关并配置邀请码相关的员工号校验。</zh-CN>
        ///   <en>Validates the self-registration switch on page load and configures employee-code validation for invite registration.</en>
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
            if (!PortalRegistrationOptions.AllowSelfRegistration)
            {
                Response.Redirect("~/Admin/AccessDenied.aspx");
            }

            ConfigureRegistrationForm();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>处理自主注册提交，创建用户、记录注册审计，并按审核开关决定是否直接登录。</zh-CN>
        ///   <en>Handles self-registration submission, creates the user, records registration audit data, and decides immediate sign-in from the approval switch.</en>
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
        protected void RegisterBtn_Click(object sender, EventArgs e)
        {
            // <lang>
            //   <zh-CN>每次提交重查开关，避免页面初次加载后部署设置发生变化时绕过限制。</zh-CN>
            //   <en>Recheck the switch on every submit so deployment-setting changes after initial page load cannot bypass the restriction.</en>
            // </lang>
            if (!PortalRegistrationOptions.AllowSelfRegistration)
            {
                Response.Redirect("~/Admin/AccessDenied.aspx");
            }

            // <lang>
            //   <zh-CN>只在 Web Forms 基础验证通过后继续创建用户。</zh-CN>
            //   <en>Continue user creation only after base Web Forms validation succeeds.</en>
            // </lang>
            if (Page.IsValid)
            {
                var userName = Name.Text.Trim();
                var email = Email.Text.Trim();
                string employeeCode = EmployeeCode.Text.Trim();
                string inviteCode = CurrentInviteCode;
                string submittedPassword;
                string submittedConfirmPassword;
                string inviteMessage;
                string passwordPolicyMessage;

                if (!TryResolveRegistrationPasswords(out submittedPassword, out submittedConfirmPassword))
                {
                    ClearSubmittedPasswordFields();
                    return;
                }

                ClearSubmittedPasswordFields();

                if (string.IsNullOrEmpty(submittedPassword) || string.IsNullOrEmpty(submittedConfirmPassword))
                {
                    Message.Text = "'Password' and 'Confirm Password' must not be left blank.";
                    return;
                }

                if (!string.Equals(submittedPassword, submittedConfirmPassword, StringComparison.Ordinal))
                {
                    Message.Text = "Password fields do not match.";
                    return;
                }

                if (!UsersDB.ValidateRegistrationInvite(inviteCode, out inviteMessage))
                {
                    Message.Text = inviteMessage;
                    return;
                }

                if (!PortalPasswordPolicy.TryValidate(
                    submittedPassword,
                    BuildPasswordPolicyContextTerms(userName, email, employeeCode),
                    out passwordPolicyMessage))
                {
                    Message.Text = passwordPolicyMessage;
                    return;
                }

                if (PortalRegistrationOptions.IsEmployeeCodeRequired(inviteCode) &&
                    string.IsNullOrWhiteSpace(employeeCode) &&
                    !PortalRegistrationOptions.AllowPendingEmployeeBinding)
                {
                    Message.Text = "Employee Code is required for invitation registration.";
                    return;
                }

                int userId;
                try
                {
                    // <lang>
                    //   <zh-CN>只传递一次性密码输入；数据层负责强哈希写入，异常与审计信息不得包含密码或邀请码原文。</zh-CN>
                    //   <en>Pass only one-time password input; the data layer owns strong-hash writes, and exceptions/audits must not contain passwords or raw invite codes.</en>
                    // </lang>
                    userId = UsersDB.AddSelfRegisteredUser(
                        userName,
                        email,
                        submittedPassword,
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
                    // <lang>
                    //   <zh-CN>记录注册状态变化；审计不可用不阻断已成功的注册事务。</zh-CN>
                    //   <en>Record the registration state change; unavailable auditing does not block a successful registration transaction.</en>
                    // </lang>
                    PortalOperationAudit.Record(
                        PortalOperationAuditEvents.UserLifecycleCategory,
                        PortalOperationAuditEvents.RegistrationSubmitted,
                        PortalOperationAuditEvents.UserTargetType,
                        userId.ToString(),
                        "Self-registration submitted.",
                        Context);

                    if (PortalRegistrationOptions.RequireRegistrationApproval)
                    {
                        // <lang>
                        //   <zh-CN>待审核用户不签发认证票据，管理员批准后才满足登录条件。</zh-CN>
                        //   <en>Pending users receive no authentication ticket and meet sign-in conditions only after administrator approval.</en>
                        // </lang>
                        RegisterBtn.Visible = false;
                        Message.CssClass = "Normal";
                        Message.Text = "Registration submitted. Please wait for administrator approval.";
                        return;
                    }

                    // <lang>
                    //   <zh-CN>关闭审核时保持既有直接登录行为，但身份票据需带当前安全版本。</zh-CN>
                    //   <en>Preserve legacy immediate sign-in behavior when approval is disabled, while carrying the current security version in the identity ticket.</en>
                    // </lang>
                    PortalAuthenticationService.SignIn(
                        Response,
                        Request,
                        userName,
                        UsersDB.GetSecurityVersionByUserName(userName),
                        false);
                    Response.Redirect("~/DesktopDefault.aspx");
                }
                else
                {
                    // <lang>
                    //   <zh-CN>保持对外提示泛化，避免暴露数据库或邀请码校验细节。</zh-CN>
                    //   <en>Keep the user-facing message generic and avoid exposing database or invite-validation details.</en>
                    // </lang>
                    Message.Text = "Registration failed. The user name or email may already exist, or registration metadata is not available.";
                }
            }
        }

        private string CurrentInviteCode
        {
            get
            {
                // <lang>
                //   <zh-CN>空值代表当前允许的非邀请注册，不在这里强制改写为拒绝。</zh-CN>
                //   <en>An empty value represents currently allowed non-invite registration and is not forced to rejection here.</en>
                // </lang>
                string inviteCode = Request.QueryString["invite"];
                return string.IsNullOrWhiteSpace(inviteCode) ? string.Empty : inviteCode.Trim();
            }
        }

        private void ConfigureRegistrationForm()
        {
            // <lang>
            //   <zh-CN>只有邀请注册且未允许待绑定员工号时，员工号成为本页必填项。</zh-CN>
            //   <en>Employee code becomes required only for invite registration when pending employee binding is not allowed.</en>
            // </lang>
            bool employeeCodeRequired = PortalRegistrationOptions.IsEmployeeCodeRequired(CurrentInviteCode) &&
                                        !PortalRegistrationOptions.AllowPendingEmployeeBinding;
            EmployeeCodeRequiredValidator.Enabled = employeeCodeRequired;
            EmployeeCodeRequiredHint.Visible = employeeCodeRequired;
            ConfigurePasswordSubmission();
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>按加密开关配置注册页密码提交脚本和旧验证器。</zh-CN>
        ///   <en>Configures registration password-submission scripts and legacy validators from the encryption switch.</en>
        /// </lang>
        /// </summary>
        private void ConfigurePasswordSubmission()
        {
            bool encryptedSubmissionRequired = PortalPasswordSubmissionCrypto.IsEncryptedSubmissionRequired();
            RequiredFieldValidator3.Enabled = !encryptedSubmissionRequired;
            RequiredFieldValidator4.Enabled = !encryptedSubmissionRequired;
            CompareValidator1.Enabled = !encryptedSubmissionRequired;

            if (!encryptedSubmissionRequired)
            {
                RegisterBtn.OnClientClick = string.Empty;
                return;
            }

            Page.ClientScript.RegisterClientScriptInclude(
                typeof(Register),
                "JSEncryptIE6",
                ResolveUrl("~/Scripts/Security/jsencrypt-ie6.min.js"));

            Page.ClientScript.RegisterClientScriptInclude(
                typeof(Register),
                "PortalLoginPasswordEncryption",
                ResolveUrl("~/Scripts/Security/PortalLoginPasswordEncryption.js"));

            RegisterBtn.OnClientClick = string.Format(
                "return PortalLoginPasswordEncryption.encryptPasswordFields([{0},{1}],'{2}','{3}');",
                BuildPasswordFieldScriptObject(Password.ClientID, EncryptedPassword.ClientID),
                BuildPasswordFieldScriptObject(ConfirmPassword.ClientID, EncryptedConfirmPassword.ClientID),
                HttpUtility.JavaScriptStringEncode(ResolveUrl("~/Security/LoginPasswordKey.ashx")),
                HttpUtility.JavaScriptStringEncode(Message.ClientID));
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>解析注册页提交的密码和确认密码，优先使用加密隐藏字段。</zh-CN>
        ///   <en>Resolves registration password and confirmation values, preferring encrypted hidden fields.</en>
        /// </lang>
        /// </summary>
        /// <param name="submittedPassword">
        /// <l>
        ///   <zh-CN>当前请求内使用的明文密码。</zh-CN>
        ///   <en>Plain password for this request.</en>
        /// </l>
        /// </param>
        /// <param name="submittedConfirmPassword">
        /// <l>
        ///   <zh-CN>当前请求内使用的确认密码。</zh-CN>
        ///   <en>Plain confirmation password for this request.</en>
        /// </l>
        /// </param>
        /// <returns>
        /// <l>
        ///   <zh-CN>提交满足当前加密策略时为 <c>true</c>。</zh-CN>
        ///   <en><c>true</c> when the submission satisfies the current encryption policy.</en>
        /// </l>
        /// </returns>
        private bool TryResolveRegistrationPasswords(
            out string submittedPassword,
            out string submittedConfirmPassword)
        {
            submittedPassword = string.Empty;
            submittedConfirmPassword = string.Empty;

            bool hasEncryptedPassword = !string.IsNullOrWhiteSpace(EncryptedPassword.Value);
            bool hasEncryptedConfirmPassword = !string.IsNullOrWhiteSpace(EncryptedConfirmPassword.Value);

            if (hasEncryptedPassword || hasEncryptedConfirmPassword)
            {
                if (!hasEncryptedPassword || !hasEncryptedConfirmPassword)
                {
                    PortalDiagnostics.Warn(
                        "PasswordSubmissionEncryption",
                        "Registration password submission was incomplete: one encrypted password field was missing.",
                        Context);
                    Message.Text = "密码提交不完整，请刷新页面后重试。";
                    return false;
                }

                string[] submittedPasswords;
                string failureCode;
                string eventId;
                if (PortalPasswordSubmissionCrypto.TryDecryptSubmittedPasswords(
                    Context,
                    new[] { EncryptedPassword.Value, EncryptedConfirmPassword.Value },
                    out submittedPasswords,
                    out failureCode,
                    out eventId))
                {
                    submittedPassword = submittedPasswords.Length > 0 ? submittedPasswords[0] : string.Empty;
                    submittedConfirmPassword = submittedPasswords.Length > 1 ? submittedPasswords[1] : string.Empty;
                    return true;
                }

                Message.Text = "密码提交验证失败，请刷新页面后重试。";
                return false;
            }

            if (PortalPasswordSubmissionCrypto.IsEncryptedSubmissionRequired())
            {
                PortalDiagnostics.Warn(
                    "PasswordSubmissionEncryption",
                    "Registration password was submitted without the required encrypted fields.",
                    Context);
                Message.Text = "密码提交验证失败，请刷新页面后重试。";
                return false;
            }

            submittedPassword = Password.Text ?? string.Empty;
            submittedConfirmPassword = ConfirmPassword.Text ?? string.Empty;
            return true;
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>清空注册页密码字段，避免异常路径和回显残留。</zh-CN>
        ///   <en>Clears registration password fields to avoid exception-path and echo residue.</en>
        /// </lang>
        /// </summary>
        private void ClearSubmittedPasswordFields()
        {
            Password.Text = string.Empty;
            ConfirmPassword.Text = string.Empty;
            EncryptedPassword.Value = string.Empty;
            EncryptedConfirmPassword.Value = string.Empty;
        }

        private static string BuildPasswordFieldScriptObject(string passwordElementId, string encryptedElementId)
        {
            return string.Format(
                "{{passwordElementId:'{0}',encryptedElementId:'{1}'}}",
                HttpUtility.JavaScriptStringEncode(passwordElementId),
                HttpUtility.JavaScriptStringEncode(encryptedElementId));
        }

        private static string[] BuildPasswordPolicyContextTerms(string userName, string email, string employeeCode)
        {
            return new[]
            {
                userName ?? string.Empty,
                email ?? string.Empty,
                employeeCode ?? string.Empty
            };
        }
    }
}
